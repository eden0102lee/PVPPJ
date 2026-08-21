using System.Collections.Generic;
using GamePJ.Arena;
using GamePJ.Combat;
using GamePJ.Modules;
using GamePJ.Player;
using UnityEngine;

namespace GamePJ.UI
{
    [DisallowMultipleComponent]
    public sealed class TalentTestHud : MonoBehaviour
    {
        TalentLoadout loadout;
        CombatCaster caster;
        DummyBrain dummy;
        CombatActor heroActor;
        HeroInputController input;
        StatusController heroStatus;
        StatusController dummyStatus;
        P09EquipmentController equipment;
        AttackRangeVisualizer rangeVisualizer;

        bool panelOpen = true;
        ActionSlotId selectedSlot = ActionSlotId.Attack1;
        Vector2 logScroll;
        Rect talentRect;
        Rect logRect;

        public void Bind(
            TalentLoadout talent,
            CombatCaster combat,
            DummyBrain dummyBrain,
            CombatActor hero,
            P09EquipmentController outfit = null,
            AttackRangeVisualizer rangeDisplay = null)
        {
            loadout = talent;
            caster = combat;
            dummy = dummyBrain;
            heroActor = hero;
            equipment = outfit;
            rangeVisualizer = rangeDisplay;
            input = talent != null ? talent.GetComponent<HeroInputController>() : null;
            heroStatus = talent != null ? talent.GetComponent<StatusController>() : null;
            dummyStatus = dummy != null ? dummy.GetComponent<StatusController>() : null;
            rangeVisualizer?.Bind(
                combat,
                talent,
                talent != null ? talent.GetComponent<HeroMotor>() : null,
                outfit);
        }

        void Update()
        {
            if (input == null)
            {
                input = FindFirstObjectByType<HeroInputController>();
            }

            if (loadout == null)
            {
                loadout = FindFirstObjectByType<TalentLoadout>();
                caster = FindFirstObjectByType<CombatCaster>();
                dummy = FindFirstObjectByType<DummyBrain>();
                heroActor = loadout != null ? loadout.GetComponent<CombatActor>() : null;
                heroStatus = loadout != null ? loadout.GetComponent<StatusController>() : null;
                dummyStatus = dummy != null ? dummy.GetComponent<StatusController>() : null;
            }

            if (input != null && input.TalentTogglePressedThisFrame)
            {
                panelOpen = !panelOpen;
            }

            if (input != null && input.DummyResetPressedThisFrame)
            {
                dummy?.ResetDummy();
                heroActor?.ResetState();
                heroStatus?.Clear();
            }

            if (input != null && input.DummyAutoTogglePressedThisFrame && dummy != null)
            {
                dummy.AutoAttack = !dummy.AutoAttack;
                CombatLog.Push(dummy.AutoAttack ? "沙包自動攻擊：開" : "沙包自動攻擊：關");
            }

            if (input != null && input.DummyGuardTogglePressedThisFrame && dummy != null)
            {
                dummy.GuardMode = !dummy.GuardMode;
                CombatLog.Push(dummy.GuardMode ? "沙包格擋：開（用來測破防）" : "沙包格擋：關");
            }

            if (input != null && input.RangeTogglePressedThisFrame && rangeVisualizer != null)
            {
                rangeVisualizer.Visible = !rangeVisualizer.Visible;
                CombatLog.Push(rangeVisualizer.Visible ? "攻擊範圍顯示：開" : "攻擊範圍顯示：關");
            }

            if (equipment != null && input != null)
            {
                if (input.WeaponPrevPressedThisFrame)
                {
                    equipment.CycleWeapon(-1);
                    CombatLog.Push($"換武器 → {equipment.WeaponLabel}");
                }

                if (input.WeaponNextPressedThisFrame)
                {
                    equipment.CycleWeapon(1);
                    CombatLog.Push($"換武器 → {equipment.WeaponLabel}");
                }
            }

            if (rangeVisualizer != null)
            {
                rangeVisualizer.PreviewSlot = selectedSlot;
            }
        }

        void OnGUI()
        {
            if (Event.current is { type: EventType.KeyDown, keyCode: KeyCode.Tab })
            {
                Event.current.Use();
            }

            var mouse = Event.current != null ? Event.current.mousePosition : Vector2.zero;
            talentRect = new Rect(12f, 12f, 430f, Mathf.Min(Screen.height - 140f, 620f));
            logRect = new Rect(Screen.width - 392f, 12f, 380f, 340f);
            var overUi = panelOpen && talentRect.Contains(mouse) || logRect.Contains(mouse);
            if (input != null)
            {
                input.UiBlocksGameplay = overUi;
                input.UiBlocksAiming = overUi;
            }

            GUI.color = Color.white;
            DrawHints();
            DrawVerification();
            if (panelOpen)
            {
                DrawTalentPanel();
            }

            DrawChargeBar();
        }

        void DrawHints()
        {
            var style = BoxStyle(13);
            GUI.Box(
                new Rect(12f, Screen.height - 128f, 760f, 116f),
                "WASD 移動　滑鼠面向　左鍵續力/攻擊1　右鍵攻擊2　Q/E/R 攻擊3-5\n" +
                "Shift 閃避　Space 格擋　Tab 天賦樹　F 重置　G 沙包自動攻擊　H 沙包格擋\n" +
                "- = 換武器　V 攻擊範圍顯示　外觀裝備請至 P09 Demo.unity\n" +
                "改動作／詞條後立即生效，攻擊沙包即可驗證傷害、異常、續力與破防。",
                style);
        }

        void DrawVerification()
        {
            var dummyActor = dummy != null ? dummy.GetComponent<CombatActor>() : null;
            var resolved = loadout != null ? loadout.Resolve(selectedSlot) : null;
            var dummyHp = dummyActor != null ? $"{dummyActor.Health:0}/{dummyActor.MaxHealth:0}" : "—";
            var heroHp = heroActor != null ? $"{heroActor.Health:0}/{heroActor.MaxHealth:0}" : "—";
            var body =
                $"即時驗證\n" +
                $"英雄 HP {heroHp}　狀態 {heroStatus?.Describe() ?? "—"}\n" +
                $"沙包 HP {dummyHp}　狀態 {dummyStatus?.Describe() ?? "—"}\n" +
                $"沙包 {(dummy != null && dummy.AutoAttack ? "自動攻擊中" : "待機")} / {(dummy != null && dummy.GuardMode ? "格擋中" : "未格擋")}\n" +
                $"目前槽 {selectedSlot.DisplayName()}：{(resolved != null ? resolved.Summary() : "—")}";

            GUI.Box(logRect, body, BoxStyle(13));
            GUILayout.BeginArea(new Rect(logRect.x + 8f, logRect.y + 108f, logRect.width - 16f, logRect.height - 116f));
            logScroll = GUILayout.BeginScrollView(logScroll);
            foreach (var line in CombatLog.Snapshot)
            {
                GUILayout.Label(line, LabelStyle(12));
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        void DrawTalentPanel()
        {
            GUI.Box(talentRect, "天賦樹（測試場：全部解鎖，改完即時套用）", BoxStyle(14));
            GUILayout.BeginArea(new Rect(talentRect.x + 10f, talentRect.y + 28f, talentRect.width - 20f, talentRect.height - 36f));

            GUILayout.Label($"3 選 2：攻擊5 / 閃避 / 格擋 目前啟用 {loadout?.Build.CountChoiceSlots() ?? 0}/2", LabelStyle(12));
            GUILayout.BeginHorizontal();
            DrawSlotButton(ActionSlotId.Attack1);
            DrawSlotButton(ActionSlotId.Attack2);
            DrawSlotButton(ActionSlotId.Attack3);
            DrawSlotButton(ActionSlotId.Attack4);
            DrawSlotButton(ActionSlotId.Attack5);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            DrawSlotButton(ActionSlotId.Dodge);
            DrawSlotButton(ActionSlotId.Parry);
            GUILayout.EndHorizontal();
            GUILayout.Space(6f);

            if (loadout == null)
            {
                GUILayout.Label("找不到 TalentLoadout");
                GUILayout.EndArea();
                return;
            }

            var entry = loadout.Build.GetSlot(selectedSlot);
            GUILayout.Label("動作本體", LabelStyle(13));
            DrawCycleRow(
                CurrentActionLabel(entry),
                () => CycleAction(selectedSlot, -1),
                () => CycleAction(selectedSlot, 1),
                () => loadout.SetAction(selectedSlot, string.Empty));

            var resolved = loadout.Resolve(selectedSlot);
            GUILayout.Label(resolved.Summary(), LabelStyle(12));
            GUILayout.Space(4f);
            GUILayout.Label("修飾詞條（最多 4，可重複疊加）", LabelStyle(13));
            for (var i = 0; i < TalentTypes.ModifierSlotCount; i++)
            {
                var index = i;
                GUILayout.BeginHorizontal();
                GUILayout.Label($"槽 {i + 1}", GUILayout.Width(36f));
                DrawCycleRow(
                    CurrentModifierLabel(entry, i),
                    () => CycleModifier(selectedSlot, index, -1),
                    () => CycleModifier(selectedSlot, index, 1),
                    () => loadout.SetModifier(selectedSlot, index, string.Empty));
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(8f);
            DrawResolvedDetails(resolved);
            GUILayout.EndArea();
        }

        void DrawSlotButton(ActionSlotId slot)
        {
            var label = $"{slot.DisplayName()}\n{slot.DefaultKey()}";
            var color = GUI.backgroundColor;
            GUI.backgroundColor = selectedSlot == slot ? new Color(0.85f, 0.7f, 0.3f) : color;
            if (GUILayout.Button(label, GUILayout.Height(42f)))
            {
                selectedSlot = slot;
            }

            GUI.backgroundColor = color;
        }

        void DrawResolvedDetails(ResolvedAction resolved)
        {
            if (resolved?.Action == null)
            {
                GUILayout.Label("此槽未啟用。攻擊 1、2 開局必有；攻擊 5／閃避／格擋最多選兩個。", LabelStyle(12));
                return;
            }

            var weaponRange = equipment != null
                ? equipment.WeaponMaxRange
                : CombatRangeRules.MeleeWeaponMaxRange;
            var lines = new List<string>
            {
                $"前搖 {resolved.Startup:0.00}s　CD {resolved.Cooldown:0.00}s　武器射程 {weaponRange:0.0}m"
            };

            if (heroActor != null)
            {
                var motor = heroActor.GetComponent<HeroMotor>();
                var mousePoint = motor != null && motor.TryGetMouseGroundPoint(out var point)
                    ? point
                    : heroActor.transform.position + heroActor.transform.forward * 2f;
                var previewShape = AttackHitShape.Compute(resolved, heroActor.transform, mousePoint, weaponRange, 0f);
                if (previewShape.IsValid)
                {
                    lines.Add(DescribeShape(resolved.Action.Id, previewShape));
                }
            }

            if (resolved.CanCharge)
            {
                lines.Add($"續力 滿蓄射程+{resolved.ChargeRangeBonus * 100f:0}%　加速 {resolved.HasteWhileCharge * 100f:0}%　敵緩速 {resolved.SlowWhileCharge * 100f:0}%　定點 {(resolved.RootSelfWhileCharge ? "是" : "否")}");
            }

            if (resolved.OnHitStun > 0f)
            {
                lines.Add($"命中擊暈 {resolved.OnHitStun:0.00}s");
            }

            if (resolved.BurnDps > 0f)
            {
                lines.Add($"燃燒 {resolved.BurnDps:0}/s × {resolved.BurnDuration:0.0}s");
            }

            if (resolved.PoisonDps > 0f)
            {
                lines.Add($"中毒 {resolved.PoisonDps:0}/s × {resolved.PoisonDuration:0.0}s");
            }

            if (resolved.LifestealPct > 0f)
            {
                lines.Add($"吸血 {resolved.LifestealPct * 100f:0}%");
            }

            if (resolved.Slot == ActionSlotId.Dodge)
            {
                lines.Add($"位移 {resolved.DodgeDistance:0.0}m　追擊係數 {resolved.PursuitCoeff:0.00}　續力回饋 {resolved.ChargeGainPct * 100f:0}%");
            }

            if (resolved.Slot == ActionSlotId.Parry)
            {
                lines.Add($"格擋窗 {resolved.ParryWindow:0.00}s　霸體 {resolved.SuperArmorDuration:0.0}s　擊退 {resolved.KnockbackDistance:0.0}m");
            }

            foreach (var line in lines)
            {
                GUILayout.Label(line, LabelStyle(12));
            }
        }

        static string DescribeShape(string actionId, AttackHitShape shape)
        {
            return actionId switch
            {
                "act_slash_str" =>
                    $"斬擊弧 {shape.GetRegion(1).ArcAngle:0}° / 半徑 {shape.GetRegion(1).Radius:0.0}m + 身周 {CombatRangeRules.BodyHitRadius:0.1}m",
                "act_strike_str" or "act_thrust_agi" =>
                    $"長方形 {shape.GetRegion(1).Length:0.0}m × {shape.GetRegion(1).HalfWidth * 2f:0.1}m + 身周 {CombatRangeRules.BodyHitRadius:0.1}m",
                "act_shot_phys_agi" =>
                    $"長方形 {shape.GetRegion(0).Length:0.0}m × {shape.GetRegion(0).HalfWidth * 2f:0.1}m",
                "act_aoe_int" or "act_shot_magic_int" =>
                    $"滑鼠圓 {shape.GetRegion(0).Radius:0.0}m（續力 1~3m）",
                _ => $"命中區域 ×{shape.RegionCount}"
            };
        }

        void DrawCycleRow(string label, System.Action prev, System.Action next, System.Action clear)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("<", GUILayout.Width(28f)))
            {
                prev();
            }

            GUILayout.Box(label, GUILayout.Height(24f));
            if (GUILayout.Button(">", GUILayout.Width(28f)))
            {
                next();
            }

            if (GUILayout.Button("清", GUILayout.Width(32f)))
            {
                clear();
            }

            GUILayout.EndHorizontal();
        }

        void CycleAction(ActionSlotId slot, int delta)
        {
            var options = new List<string> { string.Empty };
            foreach (var action in ActionCatalog.ForSlot(slot))
            {
                options.Add(action.Id);
            }

            var current = loadout.Build.GetSlot(slot)?.ActionId ?? string.Empty;
            var index = Mathf.Max(0, options.IndexOf(current));
            for (var step = 0; step < options.Count; step++)
            {
                index = (index + delta + options.Count) % options.Count;
                var id = options[index];
                if (loadout.SetAction(slot, id))
                {
                    var name = string.IsNullOrEmpty(id) ? "（空）" : ActionCatalog.Get(id).DisplayName;
                    CombatLog.Push($"{slot.DisplayName()} 動作 → {name}");
                    return;
                }
            }

            CombatLog.Push("無法啟用：攻擊5／閃避／格擋最多 3 選 2");
        }

        void CycleModifier(ActionSlotId slot, int modifierIndex, int delta)
        {
            var options = new List<string> { string.Empty };
            foreach (var modifier in ModifierCatalog.ForSlot(slot))
            {
                options.Add(modifier.Id);
            }

            var current = loadout.Build.GetSlot(slot)?.ModifierIds[modifierIndex] ?? string.Empty;
            var index = Mathf.Max(0, options.IndexOf(current));
            index = (index + delta + options.Count) % options.Count;
            var id = options[index];
            if (loadout.SetModifier(slot, modifierIndex, id))
            {
                var name = string.IsNullOrEmpty(id) ? "（空）" : ModifierCatalog.Get(id).DisplayName;
                CombatLog.Push($"{slot.DisplayName()} 詞條{modifierIndex + 1} → {name}");
            }
        }

        static string CurrentActionLabel(SlotBuild entry)
        {
            if (entry == null || !entry.HasAction)
            {
                return "（空）";
            }

            var action = ActionCatalog.Get(entry.ActionId);
            return action == null ? entry.ActionId : $"{action.DisplayName} ({action.Attribute.ShortName()})";
        }

        static string CurrentModifierLabel(SlotBuild entry, int index)
        {
            if (entry == null || entry.ModifierIds == null || index >= entry.ModifierIds.Length)
            {
                return "（空）";
            }

            var id = entry.ModifierIds[index];
            var modifier = ModifierCatalog.Get(id);
            return modifier == null ? "（空）" : $"{modifier.DisplayName} ({modifier.Attribute.ShortName()})";
        }

        void DrawChargeBar()
        {
            if (caster == null || !caster.IsCharging)
            {
                return;
            }

            var width = 280f;
            var rect = new Rect((Screen.width - width) * 0.5f, Screen.height - 148f, width, 22f);
            GUI.color = new Color(0f, 0f, 0f, 0.5f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.Lerp(new Color(0.4f, 0.75f, 1f), new Color(1f, 0.85f, 0.25f), caster.Charge01);
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * caster.Charge01, rect.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(rect, $"續力 {caster.ChargingSlot.DisplayName()}  {caster.Charge01 * 100f:0}%", new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            });
        }

        static GUIStyle BoxStyle(int fontSize)
        {
            return new GUIStyle(GUI.skin.box)
            {
                fontSize = fontSize,
                alignment = TextAnchor.UpperLeft,
                padding = new RectOffset(10, 10, 8, 8),
                wordWrap = true
            };
        }

        static GUIStyle LabelStyle(int fontSize)
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                wordWrap = true
            };
        }
    }
}
