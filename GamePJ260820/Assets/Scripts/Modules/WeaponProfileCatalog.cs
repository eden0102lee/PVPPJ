using System;
using System.Collections.Generic;

namespace GamePJ.Modules
{
    public enum WeaponArchetype
    {
        None,
        Longsword,
        Greatsword,
        Rapier,
        Shield,
        Bow,
        Staff
    }

    public enum WeaponLoadoutSlot
    {
        Primary = 0,
        Secondary = 1
    }

    [Flags]
    public enum ActionCategory
    {
        None = 0,
        MeleeAll = 1 << 0,
        PhysicalStrike = 1 << 1,
        Parry = 1 << 2,
        MagicShot = 1 << 3,
        PhysicalShot = 1 << 4,
        MagicAoe = 1 << 5,
        Dodge = 1 << 6
    }

    public sealed class WeaponProfile
    {
        public WeaponArchetype Archetype;
        public string DisplayName;
        public float Range;
        public ActionCategory AllowedCategories;
        public float DamagePctBonus;
        public float CooldownMultiplier = 1f;
        public float ParryWindowBonus;
        public float MoveSpeedMultiplier = 1f;
        public float ArmorBreakBonus;
        public float AilmentEffectMultiplier = 1f;
        public bool AilmentTargetsMagic;
        public float KnockbackMultiplier = 1f;
        public int P09WeaponContentId;
        public int P09ShieldContentId;

        public bool Allows(ActionCategory actionCategories)
        {
            if (actionCategories == ActionCategory.None)
            {
                return false;
            }

            if ((actionCategories & ActionCategory.Dodge) != 0)
            {
                return true;
            }

            return (AllowedCategories & actionCategories) != 0;
        }

        public bool AllowsAction(ActionDefinition action)
        {
            return action != null && Allows(action.Categories);
        }

        public string BonusSummary()
        {
            var parts = new System.Collections.Generic.List<string>();
            if (DamagePctBonus > 0f)
            {
                parts.Add($"傷害+{DamagePctBonus * 100f:0}%");
            }

            if (CooldownMultiplier < 0.999f)
            {
                parts.Add($"CD×{CooldownMultiplier:0.00}");
            }
            else if (CooldownMultiplier > 1.001f)
            {
                parts.Add($"CD×{CooldownMultiplier:0.00}");
            }

            if (ParryWindowBonus > 0f)
            {
                parts.Add($"格擋窗+{ParryWindowBonus:0.00}s");
            }

            if (MoveSpeedMultiplier < 0.999f)
            {
                parts.Add($"移速×{MoveSpeedMultiplier:0.00}");
            }

            if (ArmorBreakBonus > 0f)
            {
                parts.Add($"破防+{ArmorBreakBonus * 100f:0}%");
            }

            if (AilmentEffectMultiplier > 1.001f)
            {
                parts.Add(AilmentTargetsMagic ? "法術異常↑" : "物理異常↑");
            }

            if (KnockbackMultiplier > 1.001f)
            {
                parts.Add($"擊退×{KnockbackMultiplier:0.0}");
            }

            return parts.Count == 0 ? "無加成" : string.Join("　", parts);
        }

        public string AllowedActionsSummary()
        {
            var parts = new List<string>();
            if ((AllowedCategories & ActionCategory.MeleeAll) != 0)
            {
                parts.Add("近戰");
            }

            if ((AllowedCategories & ActionCategory.PhysicalStrike) != 0)
            {
                parts.Add("物理打擊");
            }

            if ((AllowedCategories & ActionCategory.Parry) != 0)
            {
                parts.Add("格擋");
            }

            if ((AllowedCategories & ActionCategory.PhysicalShot) != 0)
            {
                parts.Add("物理射擊");
            }

            if ((AllowedCategories & ActionCategory.MagicShot) != 0)
            {
                parts.Add("魔法射擊");
            }

            if ((AllowedCategories & ActionCategory.MagicAoe) != 0)
            {
                parts.Add("魔法範圍");
            }

            return string.Join("、", parts);
        }
    }

    public static class WeaponProfileCatalog
    {
        static readonly WeaponProfile[] Profiles =
        {
            new()
            {
                Archetype = WeaponArchetype.None,
                DisplayName = "無",
                Range = 0.5f,
                AllowedCategories = ActionCategory.Parry | ActionCategory.PhysicalStrike,
                P09WeaponContentId = 0
            },
            new()
            {
                Archetype = WeaponArchetype.Longsword,
                DisplayName = "長劍",
                Range = 1.5f,
                AllowedCategories = ActionCategory.MeleeAll | ActionCategory.Parry,
                CooldownMultiplier = 0.85f,
                P09WeaponContentId = 1
            },
            new()
            {
                Archetype = WeaponArchetype.Greatsword,
                DisplayName = "巨劍",
                Range = 2.5f,
                AllowedCategories = ActionCategory.MeleeAll | ActionCategory.Parry,
                DamagePctBonus = 0.15f,
                ParryWindowBonus = 0.05f,
                MoveSpeedMultiplier = 0.85f,
                P09WeaponContentId = 2
            },
            new()
            {
                Archetype = WeaponArchetype.Rapier,
                DisplayName = "細劍",
                Range = 2f,
                AllowedCategories = ActionCategory.MeleeAll | ActionCategory.MagicShot,
                ArmorBreakBonus = 0.2f,
                P09WeaponContentId = 3
            },
            new()
            {
                Archetype = WeaponArchetype.Shield,
                DisplayName = "盾牌",
                Range = 0.5f,
                AllowedCategories = ActionCategory.Parry,
                ParryWindowBonus = 0.08f,
                P09ShieldContentId = 1
            },
            new()
            {
                Archetype = WeaponArchetype.Bow,
                DisplayName = "弓",
                Range = 10f,
                AllowedCategories = ActionCategory.PhysicalShot,
                CooldownMultiplier = 1.15f,
                AilmentEffectMultiplier = 1.25f,
                P09WeaponContentId = 10
            },
            new()
            {
                Archetype = WeaponArchetype.Staff,
                DisplayName = "長杖",
                Range = 10f,
                AllowedCategories = ActionCategory.PhysicalStrike | ActionCategory.MagicShot |
                                    ActionCategory.MagicAoe | ActionCategory.Parry,
                CooldownMultiplier = 1.15f,
                AilmentEffectMultiplier = 1.25f,
                AilmentTargetsMagic = true,
                KnockbackMultiplier = 1.3f,
                P09WeaponContentId = 6
            }
        };

        static readonly Dictionary<WeaponArchetype, WeaponProfile> ByArchetype = new();

        static WeaponProfileCatalog()
        {
            foreach (var profile in Profiles)
            {
                ByArchetype[profile.Archetype] = profile;
            }
        }

        public static IReadOnlyList<WeaponProfile> All => Profiles;

        public static WeaponProfile Get(WeaponArchetype archetype)
        {
            return ByArchetype.TryGetValue(archetype, out var profile) ? profile : ByArchetype[WeaponArchetype.None];
        }

        public static IEnumerable<WeaponArchetype> Archetypes()
        {
            foreach (var profile in Profiles)
            {
                yield return profile.Archetype;
            }
        }
    }
}
