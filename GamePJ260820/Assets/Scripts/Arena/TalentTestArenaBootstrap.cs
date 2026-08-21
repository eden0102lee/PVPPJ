using GamePJ.CameraSystems;
using GamePJ.Combat;
using GamePJ.Modules;
using GamePJ.Player;
using GamePJ.UI;
using UnityEngine;

namespace GamePJ.Arena
{
    [DisallowMultipleComponent]
    public sealed class TalentTestArenaBootstrap : MonoBehaviour
    {
        [SerializeField] GameObject heroPrefab;
        [SerializeField] Transform hero;
        [SerializeField] DummyBrain dummy;
        [SerializeField] Camera gameplayCamera;

        public Transform Hero => hero;
        public DummyBrain Dummy => dummy;

        void Awake()
        {
            EnsureArena();
            EnsureHero();
            EnsureDummy();
            EnsureCamera();
            EnsureHud();
            CombatLog.Clear();
            CombatLog.Push("天賦測試場就緒：改詞條後立刻攻擊即可驗證");
            CombatLog.Push("換武器：- =　攻擊範圍：V　外觀裝備請至 Assets/P09_Modular_Humanoid/Scenes/Demo.unity");
        }

        public void BindExisting(Transform boundHero, DummyBrain boundDummy, Camera boundCamera)
        {
            hero = boundHero;
            dummy = boundDummy;
            gameplayCamera = boundCamera;
        }

        void EnsureArena()
        {
            if (transform.Find("CircularArena") == null)
            {
                CircularArenaFactory.Build(transform);
            }
        }

        void EnsureHero()
        {
            if (hero == null)
            {
                var existing = GameObject.Find("Hero_P09");
                hero = existing != null ? existing.transform : null;
            }

            if (hero == null && heroPrefab == null)
            {
#if UNITY_EDITOR
                heroPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Hero_P09.prefab");
#endif
            }

            if (hero == null && heroPrefab != null)
            {
                var instance = Instantiate(heroPrefab);
                instance.name = "Hero_P09";
                hero = instance.transform;
            }

            if (hero == null)
            {
                hero = CreateFallbackHero().transform;
            }

            hero.position = new Vector3(0f, 0f, -4f);
            hero.rotation = Quaternion.identity;
            ConfigureHero(hero.gameObject);
        }

        void EnsureDummy()
        {
            if (dummy == null)
            {
                dummy = FindFirstObjectByType<DummyBrain>();
            }

            if (dummy == null)
            {
                dummy = DummyBrain.Create(new Vector3(0f, 0f, 4.5f));
            }

            dummy.BindHero(hero);
        }

        void EnsureCamera()
        {
            if (gameplayCamera == null)
            {
                gameplayCamera = Camera.main;
            }

            if (gameplayCamera == null)
            {
                var cameraGo = new GameObject("Main Camera");
                cameraGo.tag = "MainCamera";
                gameplayCamera = cameraGo.AddComponent<Camera>();
                cameraGo.AddComponent<AudioListener>();
            }

            if (gameplayCamera.GetComponent<TiltedFollowCamera>() == null)
            {
                gameplayCamera.gameObject.AddComponent<TiltedFollowCamera>();
            }

            gameplayCamera.GetComponent<TiltedFollowCamera>().Configure(hero, new Vector3(0f, 7f, -6f));
            gameplayCamera.transform.position = hero.position + new Vector3(0f, 7f, -6f);
            gameplayCamera.transform.rotation = Quaternion.Euler(52f, 0f, 0f);

            if (hero.TryGetComponent<HeroMotor>(out var motor))
            {
                motor.SetPlayerCamera(gameplayCamera);
                var groundLayer = LayerMask.NameToLayer("Ground");
                if (groundLayer >= 0)
                {
                    motor.SetGroundMask(1 << groundLayer);
                }
            }
        }

        void EnsureHud()
        {
            var hud = FindFirstObjectByType<TalentTestHud>();
            if (hud == null)
            {
                hud = new GameObject("TalentTestHud").AddComponent<TalentTestHud>();
            }

            hud.Bind(
                hero.GetComponent<TalentLoadout>(),
                hero.GetComponent<CombatCaster>(),
                dummy,
                hero.GetComponent<CombatActor>(),
                hero.GetComponent<P09EquipmentController>(),
                hero.GetComponent<AttackRangeVisualizer>());
        }

        public static void ConfigureHero(GameObject heroGo)
        {
            if (heroGo.GetComponent<CombatActor>() == null)
            {
                heroGo.AddComponent<CombatActor>().Configure(TeamId.Player, 200f);
            }

            if (heroGo.GetComponent<StatusController>() == null)
            {
                heroGo.AddComponent<StatusController>();
            }

            if (heroGo.GetComponent<TalentLoadout>() == null)
            {
                heroGo.AddComponent<TalentLoadout>();
            }

            if (heroGo.GetComponent<CombatCaster>() == null)
            {
                heroGo.AddComponent<CombatCaster>();
            }

            if (heroGo.transform.Find("P09_Visual") != null && heroGo.GetComponent<P09EquipmentController>() == null)
            {
                heroGo.AddComponent<P09EquipmentController>();
            }

            if (heroGo.GetComponent<AttackRangeVisualizer>() == null)
            {
                heroGo.AddComponent<AttackRangeVisualizer>();
            }
        }

        static GameObject CreateFallbackHero()
        {
            var root = new GameObject("Hero_Fallback");
            var controller = root.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            root.AddComponent<HeroInputController>();
            root.AddComponent<HeroMotor>();
            root.AddComponent<HeroAnimatorBridge>();

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            Destroy(body.GetComponent<Collider>());
            return root;
        }
    }
}
