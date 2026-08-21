using System;
using System.Collections.Generic;

namespace GamePJ.Modules
{
    public sealed class ActionDefinition
    {
        public string Id;
        public string DisplayName;
        public HeroAttribute Attribute;
        public ActionSlotMask Slots;
        public ActionKind Kind;
        public float DamageCoeff = 1f;
        public float Startup = 0.25f;
        public float Active = 0.12f;
        public float Recovery = 0.18f;
        public float Range = 2f;
        public float Radius = 1.2f;
        public float ArcAngle = 90f;
        public float Cooldown = 0.45f;
        public float ChargeDuration = 1f;
        public float ProjectileSpeed = 18f;
        public float DodgeDistance = 4f;
        public float IFrameDuration = 0.25f;
        public float ParryWindow = 0.2f;
        public string Notes;
    }

    public static class ActionCatalog
    {
        static readonly ActionDefinition[] Definitions =
        {
            new()
            {
                Id = "act_strike_str",
                DisplayName = "打擊",
                Attribute = HeroAttribute.Strength,
                Slots = ActionSlotMask.Attacks,
                Kind = ActionKind.MeleeArc,
                DamageCoeff = 1f,
                Startup = 0.25f,
                Range = 2f,
                Radius = 0.6f,
                ArcAngle = 70f,
                Cooldown = 0.55f,
                Notes = "短距高爆發，角色至滑鼠長方形"
            },
            new()
            {
                Id = "act_slash_str",
                DisplayName = "斬擊",
                Attribute = HeroAttribute.Strength,
                Slots = ActionSlotMask.Attacks,
                Kind = ActionKind.MeleeArc,
                DamageCoeff = 1.1f,
                Startup = 0.3f,
                Range = 2f,
                Radius = 0.3f,
                ArcAngle = 360f,
                Cooldown = 0.6f,
                Notes = "30~360° 弧斬 + 身周 0.3m"
            },
            new()
            {
                Id = "act_thrust_agi",
                DisplayName = "刺擊",
                Attribute = HeroAttribute.Agility,
                Slots = ActionSlotMask.Attacks,
                Kind = ActionKind.MeleeThrust,
                DamageCoeff = 0.9f,
                Startup = 0.15f,
                Range = 2f,
                Radius = 0.35f,
                ArcAngle = 25f,
                Cooldown = 0.35f,
                Notes = "角色至滑鼠長方形突刺"
            },
            new()
            {
                Id = "act_shot_phys_agi",
                DisplayName = "射擊（物理）",
                Attribute = HeroAttribute.Agility,
                Slots = ActionSlotMask.Attacks,
                Kind = ActionKind.Projectile,
                DamageCoeff = 0.85f,
                Startup = 0.2f,
                Range = 10f,
                Radius = 0.25f,
                ProjectileSpeed = 22f,
                Cooldown = 0.45f,
                Notes = "角色至滑鼠長方形（遠程武器）"
            },
            new()
            {
                Id = "act_aoe_int",
                DisplayName = "範圍（AOE）",
                Attribute = HeroAttribute.Intelligence,
                Slots = ActionSlotMask.Attacks,
                Kind = ActionKind.Aoe,
                DamageCoeff = 0.75f,
                Startup = 0.35f,
                Range = 10f,
                Radius = 3f,
                Cooldown = 0.8f,
                Notes = "滑鼠位置 1~3m 圓形"
            },
            new()
            {
                Id = "act_shot_magic_int",
                DisplayName = "射擊（魔法）",
                Attribute = HeroAttribute.Intelligence,
                Slots = ActionSlotMask.Attacks,
                Kind = ActionKind.Aoe,
                DamageCoeff = 0.8f,
                Startup = 0.25f,
                Range = 10f,
                Radius = 3f,
                ProjectileSpeed = 16f,
                Cooldown = 0.5f,
                Notes = "滑鼠位置 1~3m 圓形"
            },
            new()
            {
                Id = "act_roll_dodge_agi",
                DisplayName = "翻滾閃避",
                Attribute = HeroAttribute.Agility,
                Slots = ActionSlotMask.Dodge,
                Kind = ActionKind.Dodge,
                Startup = 0.02f,
                DodgeDistance = 4f,
                IFrameDuration = 0.25f,
                Cooldown = 6f,
                Notes = "取消下一動作前搖"
            },
            new()
            {
                Id = "act_shield_parry_str",
                DisplayName = "架盾格擋",
                Attribute = HeroAttribute.Strength,
                Slots = ActionSlotMask.Parry,
                Kind = ActionKind.Parry,
                Startup = 0.02f,
                ParryWindow = 0.2f,
                Cooldown = 8f,
                Notes = "免疫該次傷害並獲得架式"
            }
        };

        static readonly Dictionary<string, ActionDefinition> ById = new(StringComparer.Ordinal);

        static ActionCatalog()
        {
            foreach (var definition in Definitions)
            {
                ById[definition.Id] = definition;
            }
        }

        public static IReadOnlyList<ActionDefinition> All => Definitions;

        public static ActionDefinition Get(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            return ById.TryGetValue(id, out var definition) ? definition : null;
        }

        public static List<ActionDefinition> ForSlot(ActionSlotId slot)
        {
            var mask = slot.ToMask();
            var list = new List<ActionDefinition>();
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
