using System;

namespace GamePJ.Modules
{
    public enum HeroAttribute
    {
        None,
        Strength,
        Agility,
        Intelligence
    }

    public enum ActionSlotId
    {
        Attack1,
        Attack2,
        Attack3,
        Attack4,
        Attack5,
        Dodge,
        Parry
    }

    [Flags]
    public enum ActionSlotMask
    {
        None = 0,
        Attack1 = 1 << 0,
        Attack2 = 1 << 1,
        Attack3 = 1 << 2,
        Attack4 = 1 << 3,
        Attack5 = 1 << 4,
        Dodge = 1 << 5,
        Parry = 1 << 6,
        Attacks = Attack1 | Attack2 | Attack3 | Attack4 | Attack5,
        All = Attacks | Dodge | Parry
    }

    public enum ActionKind
    {
        MeleeArc,
        MeleeThrust,
        Projectile,
        Aoe,
        Dodge,
        Parry
    }

    public enum ModifierCategory
    {
        Charge,
        Ailment,
        PerfectDodge,
        PerfectParry,
        Damage,
        Passive
    }

    public static class TalentTypes
    {
        public const int ModifierSlotCount = 4;

        public static ActionSlotMask ToMask(this ActionSlotId slot)
        {
            return slot switch
            {
                ActionSlotId.Attack1 => ActionSlotMask.Attack1,
                ActionSlotId.Attack2 => ActionSlotMask.Attack2,
                ActionSlotId.Attack3 => ActionSlotMask.Attack3,
                ActionSlotId.Attack4 => ActionSlotMask.Attack4,
                ActionSlotId.Attack5 => ActionSlotMask.Attack5,
                ActionSlotId.Dodge => ActionSlotMask.Dodge,
                ActionSlotId.Parry => ActionSlotMask.Parry,
                _ => ActionSlotMask.None
            };
        }

        public static bool IsAttack(this ActionSlotId slot)
        {
            return slot <= ActionSlotId.Attack5;
        }

        public static bool IsChoiceSlot(this ActionSlotId slot)
        {
            return slot is ActionSlotId.Attack5 or ActionSlotId.Dodge or ActionSlotId.Parry;
        }

        public static string DisplayName(this ActionSlotId slot)
        {
            return slot switch
            {
                ActionSlotId.Attack1 => "攻擊 1",
                ActionSlotId.Attack2 => "攻擊 2",
                ActionSlotId.Attack3 => "攻擊 3",
                ActionSlotId.Attack4 => "攻擊 4",
                ActionSlotId.Attack5 => "攻擊 5",
                ActionSlotId.Dodge => "閃避",
                ActionSlotId.Parry => "格擋",
                _ => slot.ToString()
            };
        }

        public static string DefaultKey(this ActionSlotId slot)
        {
            return slot switch
            {
                ActionSlotId.Attack1 => "左鍵",
                ActionSlotId.Attack2 => "右鍵",
                ActionSlotId.Attack3 => "Q",
                ActionSlotId.Attack4 => "E",
                ActionSlotId.Attack5 => "R",
                ActionSlotId.Dodge => "Shift",
                ActionSlotId.Parry => "Space",
                _ => string.Empty
            };
        }

        public static string ShortName(this HeroAttribute attribute)
        {
            return attribute switch
            {
                HeroAttribute.Strength => "STR",
                HeroAttribute.Agility => "AGI",
                HeroAttribute.Intelligence => "INT",
                _ => "—"
            };
        }

        public static string DisplayName(this HeroAttribute attribute)
        {
            return attribute switch
            {
                HeroAttribute.Strength => "力量",
                HeroAttribute.Agility => "敏捷",
                HeroAttribute.Intelligence => "智力",
                _ => "無"
            };
        }
    }
}
