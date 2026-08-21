using GamePJ.Modules;
using GamePJ.Player;
using System.Collections.Generic;
using UnityEngine;

namespace GamePJ.Combat
{
    [DisallowMultipleComponent]
    public sealed class CombatProjectile : MonoBehaviour
    {
        CombatActor source;
        ResolvedAction action;
        Vector3 direction;
        float speed;
        float life;
        float radius;
        bool spent;

        public void Launch(CombatActor owner, ResolvedAction resolved, Vector3 dir, float projectileSpeed, float range, float hitRadius)
        {
            source = owner;
            action = resolved;
            direction = dir.normalized;
            speed = projectileSpeed;
            radius = hitRadius;
            life = range / Mathf.Max(1f, projectileSpeed);
        }

        void Update()
        {
            if (spent)
            {
                return;
            }

            var step = speed * Time.deltaTime;
            transform.position += direction * step;
            life -= Time.deltaTime;

            var hits = Physics.OverlapSphere(transform.position, radius);
            foreach (var hit in hits)
            {
                var target = hit.GetComponentInParent<CombatActor>();
                if (target == null || target == source || target.Team == source.Team)
                {
                    continue;
                }

                CombatCaster.ApplyResolvedHit(source, target, action, 1f, "投射物");
                spent = true;
                Destroy(gameObject);
                return;
            }

            if (life <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }

    [DisallowMultipleComponent]
    public sealed class CombatCaster : MonoBehaviour
    {
        const float TapChargeThreshold = 0.12f;
        const float PerfectChargeWindow = 0.08f;
        const float BaseAttackDamage = 100f;

        [SerializeField] Transform firePoint;
        [SerializeField] LayerMask hitMask = ~0;

        HeroInputController input;
        HeroMotor motor;
        HeroAnimatorBridge animatorBridge;
        TalentLoadout loadout;
        CombatActor actor;
        StatusController status;
        AttackRangeVisualizer rangeVisualizer;
        P09EquipmentController equipment;

        ActionSlotId chargingSlot;
        bool isCharging;
        float chargeTime;
        float nextStartupSkip;
        float stanceArmorBreak;
        float stanceDamageBonus;
        float riposteUntil;
        readonly float[] cooldownUntil = new float[7];

        public float Charge01 { get; private set; }
        public bool IsCharging => isCharging;
        public ActionSlotId ChargingSlot => chargingSlot;
        public bool InIFrames { get; private set; }
        public bool InParryWindow { get; private set; }
        public float ChargeBank { get; private set; }

        public static CombatCaster PlayerCaster { get; private set; }

        void Awake()
        {
            input = GetComponent<HeroInputController>();
            motor = GetComponent<HeroMotor>();
            animatorBridge = GetComponent<HeroAnimatorBridge>();
            loadout = GetComponent<TalentLoadout>();
            actor = GetComponent<CombatActor>();
            status = GetComponent<StatusController>();
            rangeVisualizer = GetComponent<AttackRangeVisualizer>();
            equipment = GetComponent<P09EquipmentController>();
            animatorBridge?.SetCombatDriven(true);
            if (actor != null)
            {
                actor.Died += OnActorDied;
                if (actor.Team == TeamId.Player)
                {
                    PlayerCaster = this;
                }
            }
        }

        void OnDestroy()
        {
            if (actor != null)
            {
                actor.Died -= OnActorDied;
            }

            if (PlayerCaster == this)
            {
                PlayerCaster = null;
            }
        }

        void OnActorDied()
        {
            if (actor != null && actor.Team == TeamId.Player)
            {
                CombatLog.Push("英雄倒下，1 秒後重置");
                Invoke(nameof(ResetHero), 1f);
            }
        }

        void ResetHero()
        {
            actor?.ResetState();
            status?.Clear();
            isCharging = false;
            Charge01 = 0f;
        }

        void Update()
        {
            if (input == null || loadout == null || actor == null || actor.IsDead)
            {
                return;
            }

            motor?.SetMoveSpeedMultiplier(status != null ? status.MoveSpeedMultiplier : 1f);
            motor?.SetMovementLocked(status != null && status.MovementLocked);

            InIFrames = motor != null && motor.IsDashing;
            TickCharge();
            TryStartActions();
        }

        void TickCharge()
        {
            if (!isCharging)
            {
                Charge01 = 0f;
                motor?.SetChargeHaste(0f);
                motor?.SetMovementLocked(status != null && status.MovementLocked);
                return;
            }

            var resolved = loadout.Resolve(chargingSlot);
            var duration = Mathf.Max(0.35f, resolved.ChargeDuration);
            chargeTime += Time.deltaTime;
            Charge01 = Mathf.Clamp01(chargeTime / duration);
            if (ChargeBank > 0f)
            {
                Charge01 = Mathf.Clamp01(Charge01 + ChargeBank);
            }

            if (resolved.RootSelfWhileCharge)
            {
                motor?.SetMovementLocked(true);
            }

            motor?.SetChargeHaste(resolved.HasteWhileCharge);

            if (resolved.SlowWhileCharge > 0f)
            {
                ApplyChargeSlow(resolved);
            }

            var held = IsSlotHeld(chargingSlot);
            if (!held || Charge01 >= 1f && chargeTime >= duration + 0.05f)
            {
                ReleaseCharge(resolved);
            }
        }

        void TryStartActions()
        {
            if (input.UiBlocksGameplay)
            {
                return;
            }

            if (status != null && status.Has(StatusType.Stun))
            {
                return;
            }

            if (IsPressed(ActionSlotId.Dodge))
            {
                TryDodge();
                return;
            }

            if (IsPressed(ActionSlotId.Parry))
            {
                TryParry();
                return;
            }

            if (isCharging)
            {
                return;
            }

            foreach (var slot in new[]
                     {
                         ActionSlotId.Attack1, ActionSlotId.Attack2, ActionSlotId.Attack3,
                         ActionSlotId.Attack4, ActionSlotId.Attack5
                     })
            {
                if (!IsPressed(slot))
                {
                    continue;
                }

                var resolved = loadout.Resolve(slot);
                if (resolved.Action == null)
                {
                    CombatLog.Push($"{slot.DisplayName()} 尚未配置動作");
                    continue;
                }

                if (Time.time < cooldownUntil[(int)slot])
                {
                    continue;
                }

                isCharging = true;
                chargingSlot = slot;
                chargeTime = ChargeBank;
                ChargeBank = 0f;
                CombatLog.Push($"開始續力 {slot.DisplayName()}「{resolved.Action.DisplayName}」");
                return;
            }
        }

        void ReleaseCharge(ResolvedAction resolved)
        {
            isCharging = false;
            motor?.SetChargeHaste(0f);
            var tap = chargeTime < TapChargeThreshold && Charge01 < 0.15f;
            var charge = tap ? 0f : Charge01;
            var perfect = !tap && charge >= 1f - PerfectChargeWindow / Mathf.Max(0.35f, resolved.ChargeDuration);
            FireAttack(resolved, charge, perfect);
            Charge01 = 0f;
        }

        void FireAttack(ResolvedAction resolved, float charge, bool perfect)
        {
            var startup = resolved.Startup;
            if (Time.time < riposteUntil)
            {
                startup *= 1f - Mathf.Clamp01(resolved.RiposteStartupReduction);
                CombatLog.Push("快速反擊：前搖縮短");
            }

            if (nextStartupSkip > 0f)
            {
                startup = 0f;
                nextStartupSkip = 0f;
                CombatLog.Push("完美閃避：取消本次前搖");
            }

            if (status != null)
            {
                startup /= 1f + status.AttackSpeedBonus;
            }

            cooldownUntil[(int)resolved.Slot] = Time.time + resolved.Cooldown;
            animatorBridge?.PlayAttack();
            StartCoroutine(ExecuteAttack(resolved, charge, perfect, startup));
        }

        System.Collections.IEnumerator ExecuteAttack(ResolvedAction resolved, float charge, bool perfect, float startup)
        {
            if (startup > 0f)
            {
                yield return new WaitForSeconds(startup);
            }

            if (actor == null || actor.IsDead)
            {
                yield break;
            }

            var origin = firePoint != null ? firePoint.position : actor.AimPosition;
            var mousePoint = GetMouseGroundPoint();
            var weaponRange = equipment != null
                ? equipment.WeaponMaxRange
                : CombatRangeRules.MeleeWeaponMaxRange;
            var shape = AttackHitShape.Compute(resolved, transform, mousePoint, weaponRange, charge);
            var label = resolved.Action.DisplayName;
            ResolveShapeHits(shape, resolved, charge, perfect, label);

            if (shape.RegionCount > 0 && shape.GetRegion(0).Type == HitRegionType.AimCircle)
            {
                var aim = shape.GetRegion(0);
                DrawPulse(aim.Center, aim.Radius, new Color(0.4f, 0.7f, 1f, 0.35f));
            }

            rangeVisualizer?.Flash(shape);
        }

        Vector3 GetMouseGroundPoint()
        {
            if (motor != null && motor.TryGetMouseGroundPoint(out var point))
            {
                return point;
            }

            return transform.position + transform.forward * 2f;
        }

        void ResolveShapeHits(AttackHitShape shape, ResolvedAction resolved, float charge, bool perfect, string label)
        {
            if (!shape.IsValid)
            {
                return;
            }

            var seen = new HashSet<CombatActor>();
            var hitAny = false;
            for (var i = 0; i < shape.RegionCount; i++)
            {
                var region = shape.GetRegion(i);
                var queryRadius = region.QueryRadius + 0.5f;
                var hits = Physics.OverlapSphere(
                    region.QueryCenter,
                    queryRadius,
                    hitMask,
                    QueryTriggerInteraction.Ignore);
                foreach (var hit in hits)
                {
                    var target = hit.GetComponentInParent<CombatActor>();
                    if (target == null || target == actor || target.Team == actor.Team || seen.Contains(target))
                    {
                        continue;
                    }

                    if (!shape.Contains(target.transform.position))
                    {
                        continue;
                    }

                    seen.Add(target);
                    hitAny = true;
                    ApplyResolvedHit(actor, target, resolved, charge, label, perfect, this);
                }
            }

            if (!hitAny)
            {
                CombatLog.Push($"{resolved.Action.DisplayName} 未命中（武器射程 {shape.WeaponMaxRange:0.0}m）");
            }
        }

        public static void ApplyResolvedHit(
            CombatActor source,
            CombatActor target,
            ResolvedAction resolved,
            float charge,
            string label,
            bool perfect = false,
            CombatCaster caster = null)
        {
            if (source == null || target == null || resolved?.Action == null)
            {
                return;
            }

            var sourceStatus = source.GetComponent<StatusController>();
            var targetStatus = target.GetComponent<StatusController>();

            var damageBonus = resolved.DamagePct;
            var armorBreak = resolved.ArmorBreakPct + resolved.PerfectArmorBreak * (perfect ? 1f : 0f);
            if (sourceStatus != null)
            {
                damageBonus += sourceStatus.OutgoingDamageBonus;
                armorBreak += sourceStatus.ArmorBreakBonus;
            }

            if (caster != null)
            {
                damageBonus += caster.stanceDamageBonus;
                armorBreak += caster.stanceArmorBreak;
            }

            var againstGuard = target.IsGuarding ? 1f + armorBreak : 1f;
            var raw = BaseAttackDamage * resolved.DamageCoeff * (1f + damageBonus) * againstGuard;
            var dealt = target.ApplyDamage(raw, source);

            if (resolved.LifestealPct > 0f)
            {
                source.Heal(dealt * resolved.LifestealPct);
            }

            if (targetStatus != null)
            {
                if (resolved.OnHitStun > 0f)
                {
                    targetStatus.Add(StatusType.Stun, resolved.OnHitStun, 1f, false);
                }

                if (perfect && resolved.PerfectStun > 0f)
                {
                    targetStatus.Add(StatusType.Stun, resolved.PerfectStun, 1f, false);
                }

                if (resolved.OnHitSlowPct > 0f)
                {
                    targetStatus.Add(StatusType.Slow, resolved.OnHitSlowDuration, resolved.OnHitSlowPct);
                }

                if (perfect && resolved.PerfectSlowPct > 0f)
                {
                    targetStatus.Add(StatusType.Slow, resolved.PerfectSlowDuration, resolved.PerfectSlowPct);
                }

                if (resolved.BurnDps > 0f)
                {
                    targetStatus.Add(StatusType.Burn, resolved.BurnDuration, resolved.BurnDps);
                }

                if (resolved.PoisonDps > 0f)
                {
                    targetStatus.Add(StatusType.Poison, resolved.PoisonDuration, resolved.PoisonDps);
                }
            }

            var guardText = target.IsGuarding ? $" 對格擋×{againstGuard:0.00}(破防+{armorBreak * 100f:0}%)" : string.Empty;
            CombatLog.Push(
                $"{label}「{resolved.Action.DisplayName}」命中 {target.name}  {dealt:0} 傷害{guardText}" +
                $"{(perfect ? " 滿蓄" : string.Empty)}" +
                $"{(resolved.OnHitStun > 0f ? $" 擊暈{resolved.OnHitStun:0.0}s" : string.Empty)}");

            if (caster != null)
            {
                caster.ConsumeStance();
            }
        }

        void ConsumeStance()
        {
            stanceDamageBonus = 0f;
            stanceArmorBreak = 0f;
        }

        void TryDodge()
        {
            var resolved = loadout.Resolve(ActionSlotId.Dodge);
            if (resolved.Action == null)
            {
                CombatLog.Push("閃避槽未配置");
                return;
            }

            if (Time.time < cooldownUntil[(int)ActionSlotId.Dodge])
            {
                return;
            }

            isCharging = false;
            cooldownUntil[(int)ActionSlotId.Dodge] = Time.time + resolved.Cooldown;
            var direction = motor != null && motor.PlanarVelocity.sqrMagnitude > 0.01f
                ? motor.PlanarVelocity.normalized
                : transform.forward;
            motor?.Dash(direction, resolved.DodgeDistance, 0.22f);
            InIFrames = true;
            nextStartupSkip = 1f;
            CombatLog.Push($"翻滾閃避 {resolved.DodgeDistance:0.0}m（無敵 {resolved.IFrameDuration:0.00}s）");
            StartCoroutine(EndIFrames(resolved.IFrameDuration));
        }

        System.Collections.IEnumerator EndIFrames(float duration)
        {
            yield return new WaitForSeconds(duration);
            InIFrames = false;
        }

        void TryParry()
        {
            var resolved = loadout.Resolve(ActionSlotId.Parry);
            if (resolved.Action == null)
            {
                CombatLog.Push("格擋槽未配置");
                return;
            }

            if (Time.time < cooldownUntil[(int)ActionSlotId.Parry])
            {
                return;
            }

            cooldownUntil[(int)ActionSlotId.Parry] = Time.time + resolved.Cooldown;
            InParryWindow = true;
            CombatLog.Push($"進入格擋窗口 {resolved.ParryWindow:0.00}s");
            StartCoroutine(EndParry(resolved.ParryWindow));
        }

        System.Collections.IEnumerator EndParry(float duration)
        {
            yield return new WaitForSeconds(duration);
            InParryWindow = false;
        }

        public void NotifyIncomingHit(CombatActor attacker, float damage)
        {
            if (InIFrames)
            {
                CombatLog.Push("完美閃避成功");
                ApplyPerfectDodge(attacker);
                return;
            }

            if (InParryWindow)
            {
                CombatLog.Push("完美格擋成功");
                ApplyPerfectParry(attacker);
                return;
            }

            actor.ApplyDamage(damage, attacker);
            CombatLog.Push($"英雄受到 {damage:0} 傷害");
        }

        void ApplyPerfectDodge(CombatActor attacker)
        {
            var resolved = loadout.Resolve(ActionSlotId.Dodge);
            nextStartupSkip = 1f;
            if (resolved.CooldownRefund > 0f)
            {
                RefundRandomCooldown(resolved.CooldownRefund);
            }

            ChargeBank = Mathf.Clamp01(ChargeBank + resolved.ChargeGainPct);
            if (resolved.BuffDuration > 0f && resolved.BuffAttackSpeedPct > 0f)
            {
                status?.Add(StatusType.AttackSpeedBuff, resolved.BuffDuration, resolved.BuffAttackSpeedPct);
            }

            if (resolved.PursuitCoeff > 0f && attacker != null)
            {
                var pursuit = loadout.Resolve(ActionSlotId.Attack1);
                if (pursuit.Action != null)
                {
                    var cloneCoeff = pursuit.DamageCoeff * resolved.PursuitCoeff;
                    pursuit.DamageCoeff = cloneCoeff;
                    ApplyResolvedHit(actor, attacker, pursuit, 0f, "追擊");
                }
            }
        }

        void ApplyPerfectParry(CombatActor attacker)
        {
            var resolved = loadout.Resolve(ActionSlotId.Parry);
            status?.Add(StatusType.Stance, 3f, 1f, false);
            if (resolved.SuperArmorDuration > 0f)
            {
                status?.Add(StatusType.SuperArmor, resolved.SuperArmorDuration, 1f, false);
            }

            if (resolved.DamagePct > 0f)
            {
                stanceDamageBonus += resolved.DamagePct;
                status?.Add(StatusType.DamageBuff, Mathf.Max(2f, resolved.BuffDuration), resolved.DamagePct);
            }

            if (resolved.ArmorBreakPct > 0f)
            {
                stanceArmorBreak += resolved.ArmorBreakPct;
                status?.Add(StatusType.ArmorBreakBuff, 3f, resolved.ArmorBreakPct);
            }

            if (resolved.RiposteStartupReduction > 0f)
            {
                riposteUntil = Time.time + 3f;
            }

            if (resolved.EscapeDistance > 0f)
            {
                motor?.Dash(-transform.forward, resolved.EscapeDistance, 0.18f);
                CombatLog.Push($"脫離戰場 後撤 {resolved.EscapeDistance:0.0}m");
            }

            if (resolved.KnockbackRadius > 0f && attacker != null)
            {
                var colliders = Physics.OverlapSphere(transform.position, resolved.KnockbackRadius);
                foreach (var hit in colliders)
                {
                    var target = hit.GetComponentInParent<CombatActor>();
                    if (target == null || target == actor)
                    {
                        continue;
                    }

                    target.Knockback(transform.position, resolved.KnockbackDistance);
                }

                CombatLog.Push($"範圍擊退 半徑 {resolved.KnockbackRadius:0.0}m");
            }
        }

        void RefundRandomCooldown(float seconds)
        {
            var best = -1;
            var bestRemain = 0f;
            for (var i = 0; i < cooldownUntil.Length; i++)
            {
                var remain = cooldownUntil[i] - Time.time;
                if (remain > bestRemain)
                {
                    bestRemain = remain;
                    best = i;
                }
            }

            if (best >= 0)
            {
                cooldownUntil[best] = Mathf.Max(Time.time, cooldownUntil[best] - seconds);
                CombatLog.Push($"恢復冷卻 {(ActionSlotId)best} -{seconds:0.0}s");
            }
        }

        void ApplyChargeSlow(ResolvedAction resolved)
        {
            var mousePoint = GetMouseGroundPoint();
            var weaponRange = equipment != null
                ? equipment.WeaponMaxRange
                : CombatRangeRules.MeleeWeaponMaxRange;
            var shape = AttackHitShape.Compute(resolved, transform, mousePoint, weaponRange, Charge01);
            if (!shape.IsValid)
            {
                return;
            }

            var queryRadius = 0f;
            for (var i = 0; i < shape.RegionCount; i++)
            {
                queryRadius = Mathf.Max(queryRadius, shape.GetRegion(i).QueryRadius);
            }

            var hits = Physics.OverlapSphere(transform.position, Mathf.Max(1.5f, queryRadius + shape.WeaponMaxRange));
            foreach (var hit in hits)
            {
                var target = hit.GetComponentInParent<CombatActor>();
                if (target == null || target == actor || target.Team == actor.Team)
                {
                    continue;
                }

                var targetStatus = target.GetComponent<StatusController>();
                targetStatus?.Add(StatusType.Slow, 0.15f, resolved.SlowWhileCharge, false);
            }
        }

        bool IsPressed(ActionSlotId slot)
        {
            return slot switch
            {
                ActionSlotId.Attack1 => input.Attack1PressedThisFrame,
                ActionSlotId.Attack2 => input.Attack2PressedThisFrame,
                ActionSlotId.Attack3 => input.Attack3PressedThisFrame,
                ActionSlotId.Attack4 => input.Attack4PressedThisFrame,
                ActionSlotId.Attack5 => input.Attack5PressedThisFrame,
                ActionSlotId.Dodge => input.DodgePressedThisFrame,
                ActionSlotId.Parry => input.ParryPressedThisFrame,
                _ => false
            };
        }

        bool IsSlotHeld(ActionSlotId slot)
        {
            return slot switch
            {
                ActionSlotId.Attack1 => input.Attack1Held,
                ActionSlotId.Attack2 => input.Attack2Held,
                ActionSlotId.Attack3 => input.Attack3Held,
                ActionSlotId.Attack4 => input.Attack4Held,
                ActionSlotId.Attack5 => input.Attack5Held,
                _ => false
            };
        }

        static void DrawPulse(Vector3 center, float radius, Color color)
        {
            var pulse = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pulse.name = "AoePulse";
            pulse.transform.position = center + Vector3.up * 0.05f;
            pulse.transform.localScale = new Vector3(radius * 2f, 0.02f, radius * 2f);
            Destroy(pulse.GetComponent<Collider>());
            var renderer = pulse.GetComponent<Renderer>();
            renderer.sharedMaterial = new Material(renderer.sharedMaterial) { color = color };
            Object.Destroy(pulse, 0.25f);
        }

        public float CooldownRemaining(ActionSlotId slot)
        {
            return Mathf.Max(0f, cooldownUntil[(int)slot] - Time.time);
        }
    }
}
