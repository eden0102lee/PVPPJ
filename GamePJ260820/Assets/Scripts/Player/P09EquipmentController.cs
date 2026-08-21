using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GamePJ.Modules;
using P09.Modular.Humanoid.Data;
using GamePJ.Combat;
using UnityEngine;
using UnityEngine.Animations;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GamePJ.Player
{
    /// <summary>
    /// Syncs P09 weapon/shield meshes from dual-weapon loadout on TalentLoadout.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class P09EquipmentController : MonoBehaviour
    {
        const int MaleSexId = 1;

        [SerializeField] Transform modelRoot;
        [SerializeField] EditPartDataContainer weaponContainer;
        [SerializeField] EditPartDataContainer shieldContainer;
        [SerializeField] List<WeaponGroupData> weaponGroups = new();
        [SerializeField] TalentLoadout loadout;

        readonly AvatarEditData editData = new();
        List<WeaponEditPartData> weaponCatalog = new();

        Transform handAttachRight;
        Transform handAttachLeft;
        Transform bowAttachBack;
        Transform staffAttachBack;

        void Awake()
        {
            loadout ??= GetComponent<TalentLoadout>();
            if (modelRoot == null)
            {
                modelRoot = transform.Find("P09_Visual");
            }

            EnsureCatalogLoaded();
            editData.SexId = MaleSexId;
            CacheAttachPoints();
            RefreshFromLoadout();
        }

        void OnEnable()
        {
            if (loadout != null)
            {
                loadout.Changed += RefreshFromLoadout;
            }
        }

        void OnDisable()
        {
            if (loadout != null)
            {
                loadout.Changed -= RefreshFromLoadout;
            }
        }

        void Start()
        {
            StartCoroutine(ApplyHoldPoseNextFrame());
        }

        IEnumerator ApplyHoldPoseNextFrame()
        {
            yield return null;
            ApplyWeaponHoldPose();
        }

        public WeaponArchetype PrimaryArchetype =>
            loadout != null ? loadout.Build.PrimaryWeapon : WeaponArchetype.Longsword;

        public WeaponArchetype SecondaryArchetype =>
            loadout != null ? loadout.Build.SecondaryWeapon : WeaponArchetype.None;

        public string WeaponLabel =>
            $"{WeaponProfileCatalog.Get(PrimaryArchetype).DisplayName} / {WeaponProfileCatalog.Get(SecondaryArchetype).DisplayName}";

        public float WeaponMaxRange => WeaponProfileCatalog.Get(PrimaryArchetype).Range;

        public float GetWeaponRange(int weaponSlot)
        {
            if (loadout == null)
            {
                return CombatRangeRules.MeleeWeaponMaxRange;
            }

            return loadout.Build.GetEquippedWeapon(weaponSlot).Range;
        }

        public bool IsMeleeWeapon =>
            PrimaryArchetype is WeaponArchetype.Longsword or WeaponArchetype.Greatsword or WeaponArchetype.Rapier;

        public void RefreshFromLoadout()
        {
            if (modelRoot == null)
            {
                return;
            }

            HideAllEquipmentMeshes();

            if (loadout == null)
            {
                ShowWeapon(1);
                editData.SetId(EditPartType.Weapon, 1);
                editData.SetId(EditPartType.Shield, 0);
                ApplyWeaponHoldPose();
                return;
            }

            var primary = WeaponProfileCatalog.Get(PrimaryArchetype);
            var secondary = WeaponProfileCatalog.Get(SecondaryArchetype);

            if (PrimaryArchetype == WeaponArchetype.Shield)
            {
                ShowShield(primary.P09ShieldContentId);
            }
            else if (primary.P09WeaponContentId > 0)
            {
                ShowWeapon(primary.P09WeaponContentId);
            }

            if (SecondaryArchetype == WeaponArchetype.Shield)
            {
                ShowShield(secondary.P09ShieldContentId);
            }
            else if (secondary.P09WeaponContentId > 0 &&
                     secondary.P09WeaponContentId != primary.P09WeaponContentId)
            {
                ShowWeapon(secondary.P09WeaponContentId);
            }

            editData.SetId(EditPartType.Weapon, primary.P09WeaponContentId);
            editData.SetId(
                EditPartType.Shield,
                PrimaryArchetype == WeaponArchetype.Shield
                    ? primary.P09ShieldContentId
                    : SecondaryArchetype == WeaponArchetype.Shield
                        ? secondary.P09ShieldContentId
                        : 0);

            ApplyWeaponHoldPose();
        }

        void HideAllEquipmentMeshes()
        {
            if (modelRoot == null)
            {
                return;
            }

            foreach (var data in GetPartList(EditPartType.Weapon))
            {
                SetMeshActive(data.MeshName, false);
            }

            foreach (var data in GetPartList(EditPartType.Shield))
            {
                SetMeshActive(data.MeshName, false);
            }
        }

        void ShowWeapon(int contentId)
        {
            var weapon = weaponCatalog.FirstOrDefault(w => w.ContentId == contentId);
            if (weapon == null)
            {
                return;
            }

            SetMeshActive(weapon.MeshName, true, contentId);
        }

        void ShowShield(int contentId)
        {
            if (contentId <= 0)
            {
                return;
            }

            var shield = GetPartList(EditPartType.Shield).FirstOrDefault(d => d.ContentId == contentId);
            if (shield == null)
            {
                return;
            }

            SetMeshActive(shield.MeshName, true, contentId);
        }

        void SetMeshActive(string meshName, bool active, int contentId = -1)
        {
            foreach (Transform child in modelRoot.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == meshName ||
                    child.name == string.Format(meshName, "Male"))
                {
                    var shouldShow = active && (contentId < 0 || MatchesContentId(child.name, meshName, contentId));
                    child.gameObject.SetActive(shouldShow);
                }
            }
        }

        static bool MatchesContentId(string childName, string meshName, int contentId)
        {
            return childName == meshName || childName == string.Format(meshName, "Male");
        }

        void CacheAttachPoints()
        {
            if (modelRoot == null)
            {
                return;
            }

            foreach (var transform in modelRoot.GetComponentsInChildren<Transform>(true))
            {
                switch (transform.name)
                {
                    case "Weapon_Target_Hand_R":
                        handAttachRight = transform;
                        break;
                    case "Shield_Target_Hand_L":
                        handAttachLeft = transform;
                        break;
                    case "Bow_Target_Back":
                        bowAttachBack = transform;
                        break;
                    case "Staff_Target_Back":
                        staffAttachBack = transform;
                        break;
                }
            }
        }

        void ApplyWeaponHoldPose()
        {
            if (modelRoot == null)
            {
                return;
            }

            if (handAttachRight == null)
            {
                CacheAttachPoints();
            }

            var primary = WeaponProfileCatalog.Get(PrimaryArchetype);
            var secondary = WeaponProfileCatalog.Get(SecondaryArchetype);

            AttachArchetypeMesh(primary, handAttachRight, bowAttachBack, staffAttachBack);
            if (SecondaryArchetype == WeaponArchetype.Shield)
            {
                AttachShieldMeshes(handAttachLeft);
            }

            if (PrimaryArchetype == WeaponArchetype.Shield)
            {
                AttachShieldMeshes(handAttachLeft);
            }
            else if (SecondaryArchetype is WeaponArchetype.Bow or WeaponArchetype.Staff)
            {
                AttachArchetypeMesh(secondary, handAttachRight, bowAttachBack, staffAttachBack);
            }
        }

        void AttachArchetypeMesh(
            WeaponProfile profile,
            Transform handTarget,
            Transform bowBack,
            Transform staffBack)
        {
            if (profile.Archetype == WeaponArchetype.None || profile.Archetype == WeaponArchetype.Shield)
            {
                return;
            }

            var weapon = weaponCatalog.FirstOrDefault(w => w.ContentId == profile.P09WeaponContentId);
            if (weapon == null)
            {
                return;
            }

            var meshName = weapon.MeshName;
            foreach (var transform in modelRoot.GetComponentsInChildren<Transform>(true))
            {
                if (!transform.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (transform.name != meshName &&
                    transform.name != string.Format(meshName, "Male"))
                {
                    continue;
                }

                var attach = profile.Archetype == WeaponArchetype.Bow
                    ? bowBack ?? handTarget
                    : profile.Archetype == WeaponArchetype.Staff
                        ? staffBack ?? handTarget
                        : handTarget;
                ApplyHandPoseToTransform(transform, attach);
            }
        }

        void AttachShieldMeshes(Transform handTarget)
        {
            if (handTarget == null || editData.ShieldId <= 0)
            {
                return;
            }

            var shieldData = GetPartList(EditPartType.Shield)
                .FirstOrDefault(d => d.ContentId == editData.ShieldId);
            if (shieldData == null)
            {
                return;
            }

            foreach (var transform in modelRoot.GetComponentsInChildren<Transform>(true))
            {
                if (!transform.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (transform.name != shieldData.MeshName &&
                    transform.name != string.Format(shieldData.MeshName, "Male"))
                {
                    continue;
                }

                ApplyHandPoseToTransform(transform, handTarget);
            }
        }

        void ApplyHandPoseToTransform(Transform weaponTransform, Transform handTarget)
        {
            if (handTarget == null)
            {
                return;
            }

            var constraint = weaponTransform.GetComponent<ParentConstraint>();
            if (constraint == null)
            {
                constraint = weaponTransform.GetComponentInParent<ParentConstraint>();
            }

            if (constraint != null)
            {
                SetConstraintTarget(constraint, handTarget);
                return;
            }

            if (weaponTransform != handTarget)
            {
                weaponTransform.SetParent(handTarget, false);
            }
        }

        static void SetConstraintTarget(ParentConstraint constraint, Transform handTarget)
        {
            if (constraint == null || handTarget == null)
            {
                return;
            }

            constraint.enabled = true;
            constraint.constraintActive = true;
            constraint.weight = 1f;

            var sources = new List<ConstraintSource>(constraint.sourceCount);
            var handIndex = -1;
            for (var i = 0; i < constraint.sourceCount; i++)
            {
                var source = constraint.GetSource(i);
                if (source.sourceTransform == handTarget)
                {
                    handIndex = i;
                    source.weight = 1f;
                }
                else
                {
                    source.weight = 0f;
                }

                sources.Add(source);
            }

            if (handIndex < 0)
            {
                sources.Add(new ConstraintSource { sourceTransform = handTarget, weight = 1f });
            }

            constraint.SetSources(sources);
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
