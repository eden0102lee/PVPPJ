using System.Collections.Generic;
using System.Linq;
using P09.Modular.Humanoid.Data;
using GamePJ.Combat;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GamePJ.Player
{
    /// <summary>
    /// Test-arena weapon switcher only. Armor and other appearance slots use P09 Demo.unity.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class P09EquipmentController : MonoBehaviour
    {
        const int MaleSexId = 1;

        [SerializeField] Transform modelRoot;
        [SerializeField] EditPartDataContainer weaponContainer;
        [SerializeField] EditPartDataContainer shieldContainer;
        [SerializeField] List<WeaponGroupData> weaponGroups = new();

        readonly AvatarEditData editData = new();
        List<WeaponEditPartData> weaponCatalog = new();

        void Awake()
        {
            if (modelRoot == null)
            {
                modelRoot = transform.Find("P09_Visual");
            }

            EnsureCatalogLoaded();
            editData.SexId = MaleSexId;
            editData.SetId(EditPartType.Weapon, 1);
            RefreshWeaponVisual();
        }

        public int WeaponId => editData.WeaponId;
        public string WeaponLabel => FindWeapon(WeaponId)?.DisplayName ?? "無武器";

        public float WeaponMaxRange =>
            (FindWeapon(WeaponId)?.WeaponGroupId ?? 1) == 1
                ? CombatRangeRules.MeleeWeaponMaxRange
                : CombatRangeRules.RangedWeaponMaxRange;

        public bool IsMeleeWeapon => (FindWeapon(WeaponId)?.WeaponGroupId ?? 1) == 1;

        public void CycleWeapon(int delta)
        {
            if (weaponCatalog.Count == 0)
            {
                return;
            }

            var index = weaponCatalog.FindIndex(w => w.ContentId == WeaponId);
            if (index < 0)
            {
                index = 0;
            }

            index = (index + delta + weaponCatalog.Count) % weaponCatalog.Count;
            EquipWeapon(weaponCatalog[index].ContentId);
        }

        public void EquipWeapon(int contentId)
        {
            editData.SetId(EditPartType.Weapon, contentId);
            var weapon = FindWeapon(contentId);
            var group = weaponGroups.FirstOrDefault(g => g.WeaponGroupId == (weapon?.WeaponGroupId ?? 0));
            if (group != null && group.IsUnEquippedShield)
            {
                editData.SetId(EditPartType.Shield, 0);
                RefreshSlot(EditPartType.Shield);
            }

            RefreshSlot(EditPartType.Weapon);
        }

        void RefreshWeaponVisual()
        {
            RefreshSlot(EditPartType.Weapon);
        }

        void RefreshSlot(EditPartType slot)
        {
            if (modelRoot == null)
            {
                return;
            }

            var (currentId, dataList) = GetPartData(slot);
            foreach (Transform child in modelRoot.GetComponentsInChildren<Transform>(true))
            {
                UpdateRenderer(child, currentId, dataList);
            }
        }

        void UpdateRenderer(Transform child, int currentId, List<IEditPartData> dataList)
        {
            foreach (var data in dataList)
            {
                if (child.name == data.MeshName)
                {
                    child.gameObject.SetActive(data.ContentId == currentId);
                }
                else if (child.name == string.Format(data.MeshName, "Male"))
                {
                    child.gameObject.SetActive(editData.SexId == MaleSexId && data.ContentId == currentId);
                }
                else if (child.name == string.Format(data.MeshName, "Female") ||
                         child.name == string.Format(data.MeshName, "Fem"))
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        (int currentId, List<IEditPartData> dataList) GetPartData(EditPartType type)
        {
            return (editData.GetCurrentId(type), GetPartList(type));
        }

        List<IEditPartData> GetPartList(EditPartType type)
        {
            var container = type == EditPartType.Shield ? shieldContainer : weaponContainer;
            return container?.PartDataList ?? new List<IEditPartData>();
        }

        WeaponEditPartData FindWeapon(int contentId)
        {
            return weaponCatalog.FirstOrDefault(w => w.ContentId == contentId);
        }

        void EnsureCatalogLoaded()
        {
            if (weaponContainer != null)
            {
                CacheWeaponCatalog();
                return;
            }

#if UNITY_EDITOR
            const string root = "Assets/P09_Modular_Humanoid/Scenes/DemoScene_Data/ScriptableObject/";
            weaponContainer = LoadContainer(root + "EditPartDataContainer_Weapon.asset");
            shieldContainer = LoadContainer(root + "EditPartDataContainer_Shield.asset");
            weaponGroups = new List<WeaponGroupData>
            {
                LoadWeaponGroup(root + "WeaponGroup/WeaponGroupData_1.asset"),
                LoadWeaponGroup(root + "WeaponGroup/WeaponGroupData_2.asset"),
                LoadWeaponGroup(root + "WeaponGroup/WeaponGroupData_3.asset")
            };
            weaponGroups.RemoveAll(w => w == null);
#endif
            CacheWeaponCatalog();
        }

        void CacheWeaponCatalog()
        {
            weaponCatalog = GetPartList(EditPartType.Weapon).Cast<WeaponEditPartData>().OrderBy(w => w.ContentId).ToList();
        }

#if UNITY_EDITOR
        static EditPartDataContainer LoadContainer(string path)
        {
            return AssetDatabase.LoadAssetAtPath<EditPartDataContainer>(path);
        }

        static WeaponGroupData LoadWeaponGroup(string path)
        {
            return AssetDatabase.LoadAssetAtPath<WeaponGroupData>(path);
        }
#endif
    }
}
