using System;
using System.Collections.Generic;

namespace GamePJ.Modules
{
    public sealed class ActionAnimationDefinition
    {
        public string Id;
        public string DisplayName;
        public string StateName = "Attack";
        public string ProtofactorClipName;
        public string SourceControllerPath;
        public string Notes;

        public bool HasProtofactorClip =>
            !string.IsNullOrEmpty(ProtofactorClipName) &&
            !string.IsNullOrEmpty(SourceControllerPath);
    }

    public static class ActionAnimationCatalog
    {
        const string PfRoot = "Assets/PROTOFACTOR/UltimateAnimationCollection/Animations/";
        const string OneHanded = PfRoot + "1Handed Melee Weapon Animset/1HandedMeleeWeapon_Controller.controller";
        const string SwordShield = PfRoot + "Sword&Shield Animset/Sword&Shield_Controller.controller";
        const string Fencing = PfRoot + "Fencing Animset/Fencing_Controller.controller";
        const string Bow = PfRoot + "Bow & Arrow Animset/Bow&Arrow_Controller.controller";
        const string Wizard = PfRoot + "Wizard Animset/Wizard_Controller.controller";
        const string BareFists = PfRoot + "Combat Bare Fists Animset/CombatBareFists_Controller.controller";

        static readonly ActionAnimationDefinition[] Definitions =
        {
            new()
            {
                Id = "anim_attack_default",
                DisplayName = "單手普攻 A",
                StateName = "Attack",
                ProtofactorClipName = "AttackA1hMelee",
                SourceControllerPath = OneHanded
            },
            new()
            {
                Id = "anim_attack_heavy",
                DisplayName = "單手重擊 C",
                StateName = "Attack",
                ProtofactorClipName = "AttackCForward1hMelee",
                SourceControllerPath = OneHanded
            },
            new()
            {
                Id = "anim_attack_slash",
                DisplayName = "單手三連斬",
                StateName = "Attack",
                ProtofactorClipName = "3HitComboAForward1hMelee",
                SourceControllerPath = OneHanded
            },
            new()
            {
                Id = "anim_attack_thrust",
                DisplayName = "劍術刺擊",
                StateName = "Attack",
                ProtofactorClipName = "Attack1AForwardFencing",
                SourceControllerPath = Fencing
            },
            new()
            {
                Id = "anim_attack_shoot",
                DisplayName = "弓箭射擊",
                StateName = "Attack",
                ProtofactorClipName = "ShootArrowAllInOne",
                SourceControllerPath = Bow
            },
            new()
            {
                Id = "anim_attack_cast",
                DisplayName = "法師施法",
                StateName = "Attack",
                ProtofactorClipName = "CastSpell1Wizard",
                SourceControllerPath = Wizard
            },
            new()
            {
                Id = "anim_dodge_roll",
                DisplayName = "前翻閃避",
                StateName = "Attack",
                ProtofactorClipName = "DodgeForwardCombat",
                SourceControllerPath = BareFists
            },
            new()
            {
                Id = "anim_parry_shield",
                DisplayName = "單手格擋",
                StateName = "Attack",
                ProtofactorClipName = "IdleBlock1hMelee",
                SourceControllerPath = OneHanded
            },
            new()
            {
                Id = "anim_pf_slash_combo",
                DisplayName = "單手三連斬 B",
                StateName = "Attack",
                ProtofactorClipName = "3HitComboBForward1hMelee",
                SourceControllerPath = OneHanded
            },
            new()
            {
                Id = "anim_pf_melee_b",
                DisplayName = "單手普攻 B",
                StateName = "Attack",
                ProtofactorClipName = "AttackB1hMelee",
                SourceControllerPath = OneHanded
            },
            new()
            {
                Id = "anim_pf_fencing_combo",
                DisplayName = "劍術連擊",
                StateName = "Attack",
                ProtofactorClipName = "Attack3HitComboFencing",
                SourceControllerPath = Fencing
            },
            new()
            {
                Id = "anim_pf_shield_block",
                DisplayName = "盾輕格擋",
                StateName = "Attack",
                ProtofactorClipName = "BlockLightS&S",
                SourceControllerPath = SwordShield
            },
            new()
            {
                Id = "anim_pf_cast_aoe",
                DisplayName = "法師範圍術",
                StateName = "Attack",
                ProtofactorClipName = "CastSpell5Wizard",
                SourceControllerPath = Wizard
            }
        };

        static readonly Dictionary<string, ActionAnimationDefinition> ById = new(StringComparer.Ordinal);

        static ActionAnimationCatalog()
        {
            foreach (var definition in Definitions)
            {
                ById[definition.Id] = definition;
            }
        }

        public static IReadOnlyList<ActionAnimationDefinition> All => Definitions;

        public static ActionAnimationDefinition Get(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            return ById.TryGetValue(id, out var definition) ? definition : null;
        }

        public static string ResolveStateName(string animationId, string fallback = "Attack")
        {
            return Get(animationId)?.StateName ?? fallback;
        }
    }
}
