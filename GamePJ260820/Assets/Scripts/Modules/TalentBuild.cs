using System;
using UnityEngine;

namespace GamePJ.Modules
{
    [Serializable]
    public sealed class SlotBuild
    {
        public ActionSlotId Slot;
        public string ActionId = string.Empty;
        public string[] ModifierIds = new string[TalentTypes.ModifierSlotCount];

        public bool HasAction => !string.IsNullOrEmpty(ActionId);

        public SlotBuild Clone()
        {
            var copy = new SlotBuild
            {
                Slot = Slot,
                ActionId = ActionId,
                ModifierIds = new string[TalentTypes.ModifierSlotCount]
            };

            for (var i = 0; i < TalentTypes.ModifierSlotCount; i++)
            {
                copy.ModifierIds[i] = i < ModifierIds.Length ? ModifierIds[i] : string.Empty;
            }

            return copy;
        }
    }

    [Serializable]
    public sealed class TalentBuild
    {
        public SlotBuild[] Slots = CreateDefaultSlots();

        public SlotBuild GetSlot(ActionSlotId slot)
        {
            foreach (var entry in Slots)
            {
                if (entry.Slot == slot)
                {
                    return entry;
                }
            }

            return null;
        }

        public int CountChoiceSlots()
        {
            var count = 0;
            foreach (var slot in new[] { ActionSlotId.Attack5, ActionSlotId.Dodge, ActionSlotId.Parry })
            {
                var entry = GetSlot(slot);
                if (entry != null && entry.HasAction)
                {
                    count++;
                }
            }

            return count;
        }

        public bool CanEnableChoiceSlot(ActionSlotId slot)
        {
            if (!slot.IsChoiceSlot())
            {
                return true;
            }

            var entry = GetSlot(slot);
            if (entry != null && entry.HasAction)
            {
                return true;
            }

            return CountChoiceSlots() < 2;
        }

        public static TalentBuild CreateDefault()
        {
            var build = new TalentBuild { Slots = CreateDefaultSlots() };
            build.GetSlot(ActionSlotId.Attack1).ActionId = "act_slash_str";
            build.GetSlot(ActionSlotId.Attack1).ModifierIds[0] = "mod_stun_str";
            build.GetSlot(ActionSlotId.Attack2).ActionId = "act_thrust_agi";
            build.GetSlot(ActionSlotId.Dodge).ActionId = "act_roll_dodge_agi";
            return build;
        }

        static SlotBuild[] CreateDefaultSlots()
        {
            var values = (ActionSlotId[])Enum.GetValues(typeof(ActionSlotId));
            var slots = new SlotBuild[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                slots[i] = new SlotBuild
                {
                    Slot = values[i],
                    ActionId = string.Empty,
                    ModifierIds = new string[TalentTypes.ModifierSlotCount]
                };
            }

            return slots;
        }
    }

    public sealed class ResolvedAction
    {
        public ActionSlotId Slot;
        public ActionDefinition Action;
        public readonly System.Collections.Generic.List<ModifierDefinition> Modifiers = new();

        public float DamageCoeff;
        public float Startup;
        public float Range;
        public float Radius;
        public float Cooldown;
        public float ChargeDuration;
        public float HasteWhileCharge;
        public float SlowWhileCharge;
        public bool RootSelfWhileCharge;
        public float ChargeRangeBonus;
        public float PerfectStun;
        public float PerfectArmorBreak;
        public float PerfectSlowPct;
        public float PerfectSlowDuration;
        public float OnHitStun;
        public float OnHitSlowPct;
        public float OnHitSlowDuration;
        public float BurnDps;
        public float BurnDuration;
        public float PoisonDps;
        public float PoisonDuration;
        public float ArmorBreakPct;
        public float DamagePct;
        public float LifestealPct;
        public float DodgeDistance;
        public float IFrameDuration;
        public float ParryWindow;
        public float CooldownRefund;
        public float ChargeGainPct;
        public float PursuitCoeff;
        public float BuffAttackSpeedPct;
        public float BuffDuration;
        public float SuperArmorDuration;
        public float RiposteStartupReduction;
        public float EscapeDistance;
        public float KnockbackRadius;
        public float KnockbackDistance;

        public bool CanCharge => Action != null && Action.Kind is ActionKind.MeleeArc or ActionKind.MeleeThrust or ActionKind.Projectile or ActionKind.Aoe;

        public string Summary()
        {
            if (Action == null)
            {
                return "未配置動作";
            }

            return $"{Action.DisplayName} ({Action.Attribute.ShortName()})  傷害×{DamageCoeff:0.00}  射程 {Range:0.0}m  破防+{ArmorBreakPct * 100f:0}%  傷害+{DamagePct * 100f:0}%";
        }
    }

    public static class TalentResolver
    {
        public const float BaseChargeRangeBonus = 0.4f;

        public static ResolvedAction Resolve(SlotBuild slot)
        {
            if (slot == null)
            {
                return new ResolvedAction();
            }

            var resolved = new ResolvedAction { Slot = slot.Slot };
            if (!slot.HasAction)
            {
                return resolved;
            }

            var action = ActionCatalog.Get(slot.ActionId);
            if (action == null)
            {
                return resolved;
            }

            resolved.Action = action;
            resolved.DamageCoeff = action.DamageCoeff;
            resolved.Startup = action.Startup;
            resolved.Range = action.Range;
            resolved.Radius = action.Radius;
            resolved.Cooldown = action.Cooldown;
            resolved.ChargeDuration = action.ChargeDuration;
            resolved.DodgeDistance = action.DodgeDistance;
            resolved.IFrameDuration = action.IFrameDuration;
            resolved.ParryWindow = action.ParryWindow;
            resolved.ChargeRangeBonus = resolved.CanCharge ? BaseChargeRangeBonus : 0f;

            if (slot.ModifierIds == null)
            {
                return resolved;
            }

            foreach (var modifierId in slot.ModifierIds)
            {
                var modifier = ModifierCatalog.Get(modifierId);
                if (modifier == null)
                {
                    continue;
                }

                resolved.Modifiers.Add(modifier);

                switch (modifier.Id)
                {
                    case "mod_charge_haste_agi":
                    case "mod_charge_haste_break_20":
                    case "mod_charge_haste_break_30":
                    case "mod_charge_haste_break_40":
                        resolved.HasteWhileCharge += modifier.HastePct;
                        resolved.ArmorBreakPct += modifier.ArmorBreakPct;
                        break;
                    case "mod_charge_slow_str":
                        resolved.SlowWhileCharge += modifier.SlowPct;
                        break;
                    case "mod_channel_root_int":
                        resolved.RootSelfWhileCharge = true;
                        resolved.ChargeRangeBonus += modifier.ChargeRangePct;
                        break;
                    case "mod_charge_range_up":
                        resolved.ChargeRangeBonus += modifier.ChargeRangePct;
                        break;
                    case "mod_perfect_charge_stun_str":
                        resolved.PerfectStun += modifier.StunDuration;
                        break;
                    case "mod_perfect_charge_break_agi":
                        resolved.PerfectArmorBreak += modifier.ArmorBreakPct;
                        break;
                    case "mod_perfect_charge_slow_int":
                        resolved.PerfectSlowPct += modifier.SlowPct;
                        resolved.PerfectSlowDuration = Mathf.Max(resolved.PerfectSlowDuration, modifier.SlowDuration);
                        break;
                    default:
                        resolved.OnHitStun += modifier.StunDuration;
                        resolved.OnHitSlowPct += modifier.SlowDuration > 0f ? modifier.SlowPct : 0f;
                        if (modifier.SlowDuration > 0f)
                        {
                            resolved.OnHitSlowDuration = Mathf.Max(resolved.OnHitSlowDuration, modifier.SlowDuration);
                        }

                        resolved.ArmorBreakPct += modifier.ArmorBreakPct;
                        resolved.DamagePct += modifier.DamagePct;
                        resolved.BurnDps += modifier.BurnDps;
                        resolved.BurnDuration = Mathf.Max(resolved.BurnDuration, modifier.BurnDuration);
                        resolved.PoisonDps += modifier.PoisonDps;
                        resolved.PoisonDuration = Mathf.Max(resolved.PoisonDuration, modifier.PoisonDuration);
                        resolved.LifestealPct += modifier.LifestealPct;
                        resolved.CooldownRefund += modifier.CooldownRefund;
                        resolved.ChargeGainPct += modifier.ChargeGainPct;
                        resolved.PursuitCoeff += modifier.PursuitCoeff;
                        resolved.BuffAttackSpeedPct += modifier.BuffAttackSpeedPct;
                        resolved.BuffDuration = Mathf.Max(resolved.BuffDuration, modifier.BuffDuration);
                        resolved.SuperArmorDuration = Mathf.Max(resolved.SuperArmorDuration, modifier.SuperArmorDuration);
                        if (modifier.RiposteStartupReduction > 0f)
                        {
                            resolved.RiposteStartupReduction = Mathf.Max(resolved.RiposteStartupReduction, modifier.RiposteStartupReduction);
                        }

                        resolved.EscapeDistance = Mathf.Max(resolved.EscapeDistance, modifier.EscapeDistance);
                        resolved.KnockbackRadius = Mathf.Max(resolved.KnockbackRadius, modifier.KnockbackRadius);
                        resolved.KnockbackDistance += modifier.KnockbackDistance;
                        break;
                }
            }

            return resolved;
        }
    }
}
