#if UNITY_EDITOR
using System;
using GamePJ.Arena;
using GamePJ.CameraSystems;
using GamePJ.Player;
using GamePJ.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GamePJ.EditorTools
{
    public static class TalentTestArenaBuilder
    {
        const string ScenePath = "Assets/Scenes/TalentTestArena.unity";
        const string HeroPrefabPath = "Assets/Prefabs/Hero_P09.prefab";
        const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";

        [MenuItem("GamePJ/Setup Talent Test Arena")]
        public static void SetupFromMenu()
        {
            SetupInternal(showDialog: true);
        }

        public static void RunFromCommandLine()
        {
            SetupInternal(showDialog: false);
        }

        static void SetupInternal(bool showDialog)
        {
            try
            {
                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayProgressBar("GamePJ", "Creating Talent Test Arena...", 0.2f);
                }

                EnsureGroundLayer();
                CreateScene();
                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayProgressBar("GamePJ", "Saving scene...", 0.85f);
                }

                UpdateBuildSettings();
                AssetDatabase.SaveAssets();
                Debug.Log("[GamePJ] Talent Test Arena is ready: " + ScenePath);
            }
            catch (Exception ex)
            {
                Debug.LogError("[GamePJ] Talent Test Arena build failed: " + ex);
                if (showDialog && !Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog("GamePJ", "Talent Test Arena 建立失敗：\n" + ex.Message, "OK");
                }
                else
                {
                    throw;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        static void CreateScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var defaultCamera = GameObject.Find("Main Camera");
            if (defaultCamera != null)
            {
                UnityEngine.Object.DestroyImmediate(defaultCamera);
            }

            var root = new GameObject("TalentTestArena");
            var bootstrap = root.AddComponent<TalentTestArenaBootstrap>();
            CircularArenaFactory.Build(root.transform);

            var heroPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HeroPrefabPath);
            GameObject hero;
            if (heroPrefab != null)
            {
                hero = PrefabUtility.InstantiatePrefab(heroPrefab) as GameObject;
            }
            else
            {
                hero = CreateFallbackHero();
            }

            hero.name = heroPrefab != null ? "Hero_P09" : "Hero_Fallback";
            hero.transform.position = new Vector3(0f, 0f, -4f);
            TalentTestArenaBootstrap.ConfigureHero(hero);

            var dummy = DummyBrain.Create(new Vector3(0f, 0f, 4.5f));
            dummy.BindHero(hero.transform);

            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            cameraGo.AddComponent<AudioListener>();
            AddUrpCameraData(cameraGo);
            var follow = cameraGo.AddComponent<TiltedFollowCamera>();
            follow.Configure(hero.transform, new Vector3(0f, 7f, -6f));
            cameraGo.transform.position = hero.transform.position + new Vector3(0f, 7f, -6f);
            cameraGo.transform.rotation = Quaternion.Euler(52f, 0f, 0f);

            if (hero.TryGetComponent<HeroMotor>(out var motor))
            {
                motor.SetPlayerCamera(camera);
                var groundLayer = LayerMask.NameToLayer("Ground");
                if (groundLayer >= 0)
                {
                    motor.SetGroundMask(1 << groundLayer);
                }
            }

            if (hero.TryGetComponent<HeroInputController>(out var input))
            {
                var inputAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(InputActionsPath);
                var serialized = new SerializedObject(input);
                var property = serialized.FindProperty("inputActions");
                if (property != null)
                {
                    property.objectReferenceValue = inputAsset;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            var hud = new GameObject("TalentTestHud").AddComponent<TalentTestHud>();
            hud.Bind(
                hero.GetComponent<GamePJ.Modules.TalentLoadout>(),
                hero.GetComponent<GamePJ.Combat.CombatCaster>(),
                dummy,
                hero.GetComponent<GamePJ.Combat.CombatActor>());

            var serializedBootstrap = new SerializedObject(bootstrap);
            serializedBootstrap.FindProperty("heroPrefab").objectReferenceValue = heroPrefab;
            serializedBootstrap.FindProperty("hero").objectReferenceValue = hero.transform;
            serializedBootstrap.FindProperty("dummy").objectReferenceValue = dummy;
            serializedBootstrap.FindProperty("gameplayCamera").objectReferenceValue = camera;
            serializedBootstrap.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
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
            UnityEngine.Object.DestroyImmediate(body.GetComponent<Collider>());
            return root;
        }

        static void AddUrpCameraData(GameObject cameraGo)
        {
            var urpCameraType = Type.GetType(
                "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
            if (urpCameraType != null && cameraGo.GetComponent(urpCameraType) == null)
            {
                cameraGo.AddComponent(urpCameraType);
            }
        }

        static void EnsureGroundLayer()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("ProjectSettings/TagManager.asset"));
            var layers = tagManager.FindProperty("layers");
            if (layers == null)
            {
                return;
            }

            const int groundIndex = 6;
            var layer = layers.GetArrayElementAtIndex(groundIndex);
            if (layer != null && string.IsNullOrEmpty(layer.stringValue))
            {
                layer.stringValue = "Ground";
                tagManager.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static void UpdateBuildSettings()
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>
            {
                new(ScenePath, true),
                new("Assets/Scenes/TestArena.unity", true)
            };

            foreach (var existing in EditorBuildSettings.scenes)
            {
                if (existing.path == ScenePath || existing.path == "Assets/Scenes/TestArena.unity")
                {
                    continue;
                }

                scenes.Add(existing);
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
