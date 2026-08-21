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
            return entry == null ? new ResolvedAction { Slot = slot } : TalentResolver.Resolve(entry);
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

            entry.ActionId = actionId;
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

        void NotifyChanged()
        {
            Changed?.Invoke();
        }
    }
}
