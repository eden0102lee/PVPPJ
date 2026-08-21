using UnityEngine;

namespace GamePJ.Modules
{
    [DisallowMultipleComponent]
    public sealed class TalentLoadout : MonoBehaviour
    {
        public TalentBuild Build = TalentBuild.CreateDefault();

        public event System.Action Changed;

        public ResolvedAction Resolve(ActionSlotId slot)
        {
            var entry = Build.GetSlot(slot);
            if (entry == null)
            {
                return new ResolvedAction { Slot = slot };
            }

            var weapon = Build.GetBoundWeapon(entry);
            return TalentResolver.Resolve(entry, weapon);
        }

        public bool SetEquippedWeapon(int weaponSlot, WeaponArchetype archetype)
        {
            if (weaponSlot == (int)WeaponLoadoutSlot.Secondary)
            {
                Build.SecondaryWeapon = archetype;
            }
            else
            {
                Build.PrimaryWeapon = archetype;
            }

            SanitizeInvalidActions();
            NotifyChanged();
            return true;
        }

        public bool CycleEquippedWeapon(int weaponSlot, int delta)
        {
            var options = new System.Collections.Generic.List<WeaponArchetype>();
            foreach (var archetype in WeaponProfileCatalog.Archetypes())
            {
                options.Add(archetype);
            }

            var current = weaponSlot == (int)WeaponLoadoutSlot.Secondary
                ? Build.SecondaryWeapon
                : Build.PrimaryWeapon;
            var index = Mathf.Max(0, options.IndexOf(current));
            index = (index + delta + options.Count) % options.Count;
            return SetEquippedWeapon(weaponSlot, options[index]);
        }

        public bool SetBoundWeapon(ActionSlotId slot, int weaponSlot)
        {
            var entry = Build.GetSlot(slot);
            if (entry == null)
            {
                return false;
            }

            entry.BoundWeaponSlot = Mathf.Clamp(weaponSlot, 0, 1);
            SanitizeInvalidActions();
            NotifyChanged();
            return true;
        }

        public bool CycleBoundWeapon(ActionSlotId slot, int delta)
        {
            var entry = Build.GetSlot(slot);
            if (entry == null)
            {
                return false;
            }

            var next = (entry.BoundWeaponSlot + delta + 2) % 2;
            return SetBoundWeapon(slot, next);
        }

        public bool SetAction(ActionSlotId slot, string actionId)
        {
            var entry = Build.GetSlot(slot);
            if (entry == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(actionId))
            {
                entry.ActionId = string.Empty;
                NotifyChanged();
                return true;
            }

            if (!Build.CanEnableChoiceSlot(slot) && !entry.HasAction)
            {
                return false;
            }

            var definition = ActionCatalog.Get(actionId);
            if (definition == null || (definition.Slots & slot.ToMask()) == 0)
            {
                return false;
            }

            var weapon = Build.GetBoundWeapon(entry);
            if (!weapon.AllowsAction(definition))
            {
                return false;
            }

            entry.ActionId = actionId;
            NotifyChanged();
            return true;
        }

        public bool SetAnimation(ActionSlotId slot, string animationId)
        {
            var entry = Build.GetSlot(slot);
            if (entry == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(animationId))
            {
                entry.AnimationId = string.Empty;
                NotifyChanged();
                return true;
            }

            if (ActionAnimationCatalog.Get(animationId) == null)
            {
                return false;
            }

            entry.AnimationId = animationId;
            NotifyChanged();
            return true;
        }

        public bool SetModifier(ActionSlotId slot, int index, string modifierId)
        {
            var entry = Build.GetSlot(slot);
            if (entry == null || index < 0 || index >= TalentTypes.ModifierSlotCount)
            {
                return false;
            }

            if (string.IsNullOrEmpty(modifierId))
            {
                entry.ModifierIds[index] = string.Empty;
                NotifyChanged();
                return true;
            }

            var definition = ModifierCatalog.Get(modifierId);
            if (definition == null || (definition.Slots & slot.ToMask()) == 0)
            {
                return false;
            }

            if (definition.Unique)
            {
                for (var i = 0; i < entry.ModifierIds.Length; i++)
                {
                    if (i != index && entry.ModifierIds[i] == modifierId)
                    {
                        return false;
                    }
                }
            }

            entry.ModifierIds[index] = modifierId;
            NotifyChanged();
            return true;
        }

        void SanitizeInvalidActions()
        {
            foreach (var entry in Build.Slots)
            {
                if (entry == null || !entry.HasAction)
                {
                    continue;
                }

                var weapon = Build.GetBoundWeapon(entry);
                var action = ActionCatalog.Get(entry.ActionId);
                if (action == null || !weapon.AllowsAction(action))
                {
                    entry.ActionId = string.Empty;
                }
            }
        }

        void NotifyChanged()
        {
            Changed?.Invoke();
        }
    }
}
