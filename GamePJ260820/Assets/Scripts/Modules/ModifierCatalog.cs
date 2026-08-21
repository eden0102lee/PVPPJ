using System;
using System.Collections.Generic;

namespace GamePJ.Modules
{
    public sealed class ModifierDefinition
    {
        public string Id;
        public string DisplayName;
        public ModifierCategory Category;
        public HeroAttribute Attribute;
        public ActionSlotMask Slots = ActionSlotMask.Attacks;
        public bool Stackable = true;
        public float HastePct;
        public float SlowPct;
        public float SlowDuration;
        public bool RootSelf;
        public float ChargeRangePct = 0.15f;
        public float StunDuration;
        public float ArmorBreakPct;
        public float DamagePct;
        public float BurnDps;
        public float BurnDuration;
        public float PoisonDps;
        public float PoisonDuration;
        public float LifestealPct;
        public float CooldownRefund;
        public float ChargeGainPct;
        public float PursuitCoeff;
        public float BuffAttackSpeedPct;
        public float BuffDuration;
        public float SuperArmorDuration;
        public float RiposteStartupReduction = 0.8f;
        public float EscapeDistance;
        public float KnockbackRadius;
        public float KnockbackDistance;
        public bool Unique;
        public string Notes;
    }

    public static class ModifierCatalog
    {
        static readonly ModifierDefinition[] Definitions =
        {
            new()
            {
                Id = "mod_charge_haste_agi",
                DisplayName = "加速續力",
                Category = ModifierCategory.Charge,
                Attribute = HeroAttribute.Agility,
                HastePct = 0.2f,
                Notes = "續力期間自身加速"
            },
            new()
            {
                Id = "mod_charge_slow_str",
                DisplayName = "緩速續力",
                Category = ModifierCategory.Charge,
                Attribute = HeroAttribute.Strength,
                SlowPct = 0.25f,
                Notes = "續力期間敵人緩速"
            },
            new()
            {
                Id = "mod_channel_root_int",
                DisplayName = "定點詠唱",
                Category = ModifierCategory.Charge,
                Attribute = HeroAttribute.Intelligence,
                Stackable = false,
                Unique = true,
                RootSelf = true,
                ChargeRangePct = 0.2f,
                Notes = "續力期間自身完全停止"
            },
            new()
            {
                Id = "mod_charge_range_up",
                DisplayName = "續力增程",
                Category = ModifierCategory.Charge,
                Attribute = HeroAttribute.None,
                ChargeRangePct = 0.15f,
                Notes = "續力期間攻擊範圍 +15%/格"
            },
            new()
            {
                Id = "mod_perfect_charge_stun_str",
                DisplayName = "滿續力擊暈",
                Category = ModifierCategory.Charge,
                Attribute = HeroAttribute.Strength,
                StunDuration = 0.5f,
                Notes = "Perfect Charge 附加暈眩"
            },
            new()
            {
                Id = "mod_perfect_charge_break_agi",
                DisplayName = "滿續力破防",
                Category = ModifierCategory.Charge,
                Attribute = HeroAttribute.Agility,
                ArmorBreakPct = 0.3f,
                Notes = "Perfect Charge 破防傷害"
            },
            new()
            {
                Id = "mod_perfect_charge_slow_int",
                DisplayName = "滿續力緩速",
                Category = ModifierCategory.Charge,
                Attribute = HeroAttribute.Intelligence,
                SlowPct = 0.3f,
                SlowDuration = 2f,
                Notes = "Perfect Charge 緩速"
            },
            new()
            {
                Id = "mod_stun_str",
                DisplayName = "擊暈",
                Category = ModifierCategory.Ailment,
                Attribute = HeroAttribute.Strength,
                StunDuration = 0.4f,
                Notes = "命中附加暈眩"
            },
            new()
            {
                Id = "mod_slow_debuff",
                DisplayName = "緩速",
                Category = ModifierCategory.Ailment,
                Attribute = HeroAttribute.Intelligence,
                SlowPct = 0.25f,
                SlowDuration = 2f,
                Notes = "命中附加緩速"
            },
            new()
            {
                Id = "mod_burn_dot",
                DisplayName = "燃燒",
                Category = ModifierCategory.Ailment,
                Attribute = HeroAttribute.Intelligence,
                BurnDps = 8f,
                BurnDuration = 3f,
                Notes = "命中附加燃燒 DoT"
            },
            new()
            {
                Id = "mod_poison_dot",
                DisplayName = "中毒",
                Category = ModifierCategory.Ailment,
                Attribute = HeroAttribute.Intelligence,
                PoisonDps = 5f,
                PoisonDuration = 5f,
                Notes = "命中附加中毒"
            },
            new()
            {
                Id = "mod_dodge_cd_refund",
                DisplayName = "恢復冷卻",
                Category = ModifierCategory.PerfectDodge,
                Attribute = HeroAttribute.Strength,
                Slots = ActionSlotMask.Dodge,
                CooldownRefund = 1f,
                Notes = "完美閃避：隨機技能 CD -1s"
            },
            new()
            {
                Id = "mod_dodge_charge_pct",
                DisplayName = "獲得續力%",
                Category = ModifierCategory.PerfectDodge,
                Attribute = HeroAttribute.Strength,
                Slots = ActionSlotMask.Dodge,
                ChargeGainPct = 0.25f,
                Notes = "完美閃避回饋續力進度"
            },
            new()
            {
                Id = "mod_dodge_pursuit",
                DisplayName = "追擊",
                Category = ModifierCategory.PerfectDodge,
                Attribute = HeroAttribute.Agility,
                Slots = ActionSlotMask.Dodge,
                PursuitCoeff = 0.5f,
                Notes = "完美閃避後追加短距攻擊"
            },
            new()
            {
                Id = "mod_dodge_buff",
                DisplayName = "獲得增益",
                Category = ModifierCategory.PerfectDodge,
                Attribute = HeroAttribute.None,
                Slots = ActionSlotMask.Dodge,
                BuffAttackSpeedPct = 0.15f,
                BuffDuration = 2f,
                Notes = "完美閃避攻速增益"
            },
            new()
            {
                Id = "mod_dodge_pursuit_int",
                DisplayName = "智力追擊",
                Category = ModifierCategory.PerfectDodge,
                Attribute = HeroAttribute.Intelligence,
                Slots = ActionSlotMask.Dodge,
                PursuitCoeff = 0.4f,
                Notes = "完美閃避後魔法追擊"
            },
            new()
            {
                Id = "mod_parry_superarmor",
                DisplayName = "霸體",
                Category = ModifierCategory.PerfectParry,
                Attribute = HeroAttribute.Strength,
                Slots = ActionSlotMask.Parry,
                Stackable = false,
                Unique = true,
                SuperArmorDuration = 1.5f,
                Notes = "完美格擋獲得霸體"
            },
            new()
            {
                Id = "mod_parry_dmg_up",
                DisplayName = "傷害增加",
                Category = ModifierCategory.PerfectParry,
                Attribute = HeroAttribute.Strength,
                Slots = ActionSlotMask.Parry,
                DamagePct = 0.25f,
                BuffDuration = 3f,
                Notes = "架式期間下次攻擊傷害增加"
            },
            new()
            {
                Id = "mod_parry_riposte",
                DisplayName = "快速反擊",
                Category = ModifierCategory.PerfectParry,
                Attribute = HeroAttribute.Agility,
                Slots = ActionSlotMask.Parry,
                RiposteStartupReduction = 0.8f,
                Notes = "格擋後前搖大幅縮短"
            },
            new()
            {
                Id = "mod_parry_escape",
                DisplayName = "脫離戰場",
                Category = ModifierCategory.PerfectParry,
                Attribute = HeroAttribute.Agility,
                Slots = ActionSlotMask.Parry,
                Stackable = false,
                Unique = true,
                EscapeDistance = 5f,
                Notes = "格擋後短距後撤"
            },
            new()
            {
                Id = "mod_parry_guard_break",
                DisplayName = "破防傷害",
                Category = ModifierCategory.PerfectParry,
                Attribute = HeroAttribute.Agility,
                Slots = ActionSlotMask.Parry,
                ArmorBreakPct = 0.35f,
                Notes = "架式期間破防傷害"
            },
            new()
            {
                Id = "mod_parry_knockback_aoe",
                DisplayName = "範圍擊退",
                Category = ModifierCategory.PerfectParry,
                Attribute = HeroAttribute.Intelligence,
                Slots = ActionSlotMask.Parry,
                KnockbackRadius = 3f,
                KnockbackDistance = 2f,
                Notes = "完美格擋擊退周圍"
            },
            new()
            {
                Id = "mod_armor_break_pct",
                DisplayName = "破防 [20%]",
                Category = ModifierCategory.Damage,
                Attribute = HeroAttribute.Agility,
                ArmorBreakPct = 0.2f,
                Notes = "命中破防傷害"
            },
            new()
            {
                Id = "mod_dmg_up_pct",
                DisplayName = "傷害增加 [15%]",
                Category = ModifierCategory.Damage,
                Attribute = HeroAttribute.Strength,
                DamagePct = 0.15f,
                Notes = "傷害加成"
            },
            new()
            {
                Id = "mod_charge_haste_break_30",
                DisplayName = "加速續力-破防 [30%]",
                Category = ModifierCategory.Charge,
                Attribute = HeroAttribute.Agility,
                HastePct = 0.2f,
                ArmorBreakPct = 0.3f,
                Notes = "續力加速 + 破防 30%"
            },
            new()
            {
                Id = "mod_charge_haste_break_20",
                DisplayName = "加速續力-破防 [20%]",
                Category = ModifierCategory.Charge,
                Attribute = HeroAttribute.Agility,
                HastePct = 0.2f,
                ArmorBreakPct = 0.2f,
                Notes = "續力加速 + 破防 20%"
            },
            new()
            {
                Id = "mod_charge_haste_break_40",
                DisplayName = "加速續力-破防 [40%]",
                Category = ModifierCategory.Charge,
                Attribute = HeroAttribute.Agility,
                HastePct = 0.2f,
                ArmorBreakPct = 0.4f,
                Notes = "續力加速 + 破防 40%"
            },
            new()
            {
                Id = "mod_lifesteal",
                DisplayName = "吸血",
                Category = ModifierCategory.Passive,
                Attribute = HeroAttribute.None,
                LifestealPct = 0.08f,
                Notes = "傷害 8% 轉生命"
            },
            new()
            {
                Id = "mod_vision_up",
                DisplayName = "視野",
                Category = ModifierCategory.Passive,
                Attribute = HeroAttribute.None,
                Stackable = false,
                Unique = true,
                Notes = "測試場無視野系統，僅作配置預覽"
            }
        };

        static readonly Dictionary<string, ModifierDefinition> ById = new(StringComparer.Ordinal);

        static ModifierCatalog()
        {
            foreach (var definition in Definitions)
            {
                ById[definition.Id] = definition;
            }
        }

        public static IReadOnlyList<ModifierDefinition> All => Definitions;

        public static ModifierDefinition Get(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            return ById.TryGetValue(id, out var definition) ? definition : null;
        }

        public static List<ModifierDefinition> ForSlot(ActionSlotId slot)
        {
            var mask = slot.ToMask();
            var list = new List<ModifierDefinition>();
            foreach (var definition in Definitions)
            {
                if ((definition.Slots & mask) != 0)
                {
                    list.Add(definition);
                }
            }

            return list;
        }
    }
}
