#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using GamePJ.CameraSystems;
using GamePJ.Player;
using GamePJ.UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GamePJ.EditorTools
{
    [InitializeOnLoad]
    public static class HeroPrototypeAssetBuilder
    {
        const string HeroPrefabPath = "Assets/Prefabs/Hero_P09.prefab";
        const string ControllerPath = "Assets/Art/Animations/Hero_P09.controller";
        const string ScenePath = "Assets/Scenes/TestArena.unity";
        const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";
        const string P09VisualPath = "Assets/P09_Modular_Humanoid/Model_DATA/Prefab/P09_Human_Variant_Male.prefab";
        const string ToonScapesScenePath = "Assets/ToonScapes/Spring Isles/Demo/Demo_Scene_Day.unity";
        const string P09IdleClipPath = "Assets/P09_Modular_Humanoid/Scenes/DemoScene_Data/Animation/Demo_Pose/P09_Male_idle.anim";
        const string P09WalkClipPath = "Assets/P09_Modular_Humanoid/Scenes/DemoScene_Data/Animation/Other/Run_A_v01.anim";
        const string P09AttackClipPath = "Assets/P09_Modular_Humanoid/Scenes/DemoScene_Data/Animation/Demo_Pose/P09_Atack.anim";

        static readonly Vector3 DefaultSpawnXZ = new(370f, 0f, 520f);

        static bool buildAttemptedThisSession;

        static HeroPrototypeAssetBuilder()
        {
            EditorApplication.delayCall += TryBuildIfMissing;
        }

        [MenuItem("GamePJ/Build Prototype Assets")]
        public static void BuildFromMenu()
        {
            BuildAll(force: true);
        }

        [MenuItem("GamePJ/Repair TestArena Scene")]
        public static void RepairSceneFromMenu()
        {
            BuildAll(force: true);
        }

        [MenuItem("GamePJ/Setup P09 + ToonScapes Test Arena")]
        public static void SetupP09ToonScapesFromMenu()
        {
            BuildAll(force: true);
        }

        public static void RunFromCommandLine()
        {
            buildAttemptedThisSession = false;
            BuildAll(force: true);
        }

        static void TryBuildIfMissing()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || buildAttemptedThisSession)
            {
                return;
            }

            if (!SceneIsValid() || !File.Exists(GetProjectPath(HeroPrefabPath)))
            {
                BuildAll(force: true);
            }
        }

        static bool SceneIsValid()
        {
            var scenePath = GetProjectPath(ScenePath);
            if (!File.Exists(scenePath))
            {
                return false;
            }

            var sceneText = File.ReadAllText(scenePath);
            if (!sceneText.Contains("Hero_P09") || !sceneText.Contains("TestArenaHints"))
            {
                return false;
            }

            if (!sceneText.Contains("WindManager") && !sceneText.Contains("Terrain_"))
            {
                return false;
            }

            var ids = new HashSet<string>();
            foreach (Match match in Regex.Matches(sceneText, @"--- !u!\d+ &(\d+)"))
            {
                if (!ids.Add(match.Groups[1].Value))
                {
                    Debug.LogWarning("[GamePJ] TestArena scene has duplicate local IDs. Rebuilding scene.");
                    return false;
                }
            }

            return true;
        }

        static void BuildAll(bool force)
        {
            if (buildAttemptedThisSession && !force)
            {
                return;
            }

            buildAttemptedThisSession = true;

            try
            {
                EditorUtility.DisplayProgressBar("GamePJ", "Building P09 hero and TestArena...", 0.1f);

                EnsureFolder("Assets/Prefabs");
                EnsureFolder("Assets/Art/Animations");
                EnsureFolder("Assets/Scenes");

                ValidateRequiredAssets();

                EditorUtility.DisplayProgressBar("GamePJ", "Creating animator controller...", 0.25f);
                var controller = GetOrCreateController(force);

                EditorUtility.DisplayProgressBar("GamePJ", "Creating Hero_P09 prefab...", 0.45f);
                var heroPrefab = GetOrCreateHeroPrefab(controller, force);

                EditorUtility.DisplayProgressBar("GamePJ", "Building TestArena scene (this may take a few minutes)...", 0.65f);
                CreateOrUpdateScene(heroPrefab, force);

                EditorUtility.DisplayProgressBar("GamePJ", "Updating build settings...", 0.9f);
                UpdateBuildSettings();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[GamePJ] P09 hero + ToonScapes TestArena are ready.");
            }
            catch (Exception ex)
            {
                Debug.LogError("[GamePJ] Prototype build failed: " + ex);
                EditorUtility.DisplayDialog(
                    "GamePJ Build Failed",
                    "Prototype build failed.\n\n" + ex.Message + "\n\nSee Console for details.",
                    "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        static void ValidateRequiredAssets()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(P09VisualPath) == null)
            {
                throw new FileNotFoundException($"Missing P09 visual prefab: {P09VisualPath}");
            }

            if (!File.Exists(GetProjectPath(ToonScapesScenePath)))
            {
                throw new FileNotFoundException($"Missing ToonScapes scene: {ToonScapesScenePath}");
            }
        }

        static string GetProjectPath(string assetPath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parts = path.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        static AnimatorController GetOrCreateController(bool force)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller != null && !force)
            {
                return controller;
            }

            var idle = LoadRequiredClip(P09IdleClipPath);
            var walk = LoadRequiredClip(P09WalkClipPath);
            var attack = LoadRequiredClip(P09AttackClipPath);

            if (controller != null && force)
            {
                AssetDatabase.DeleteAsset(ControllerPath);
            }

            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.parameters = new[]
            {
                new AnimatorControllerParameter { name = "Speed", type = AnimatorControllerParameterType.Float },
                new AnimatorControllerParameter { name = "Attack", type = AnimatorControllerParameterType.Trigger }
            };

            var rootStateMachine = controller.layers[0].stateMachine;
            var idleState = rootStateMachine.AddState("Idle", new Vector3(250f, 0f, 0f));
            idleState.motion = idle;

            var walkState = rootStateMachine.AddState("Walk", new Vector3(250f, 100f, 0f));
            walkState.motion = walk;

            var attackState = rootStateMachine.AddState("Attack", new Vector3(500f, 50f, 0f));
            attackState.motion = attack;

            rootStateMachine.defaultState = idleState;

            var idleToWalk = idleState.AddTransition(walkState);
            idleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            idleToWalk.hasExitTime = false;
            idleToWalk.duration = 0.15f;

            var walkToIdle = walkState.AddTransition(idleState);
            walkToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            walkToIdle.hasExitTime = false;
            walkToIdle.duration = 0.15f;

            var anyToAttack = rootStateMachine.AddAnyStateTransition(attackState);
            anyToAttack.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
            anyToAttack.hasExitTime = false;
            anyToAttack.duration = 0.05f;
            anyToAttack.canTransitionToSelf = false;

            var attackToIdle = attackState.AddTransition(idleState);
            attackToIdle.hasExitTime = true;
            attackToIdle.exitTime = 0.92f;
            attackToIdle.duration = 0.12f;

            EditorUtility.SetDirty(controller);
            return controller;
        }

        static AnimationClip LoadRequiredClip(string path)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                throw new FileNotFoundException($"Missing animation clip: {path}");
            }

            return clip;
        }

        static GameObject GetOrCreateHeroPrefab(RuntimeAnimatorController controller, bool force)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(HeroPrefabPath);
            if (existing != null && !force)
            {
                return existing;
            }

            var root = new GameObject("Hero_P09");
            var characterController = root.AddComponent<CharacterController>();
            characterController.height = 1.8f;
            characterController.radius = 0.35f;
            characterController.center = new Vector3(0f, 0.9f, 0f);
            characterController.stepOffset = 0.35f;

            root.AddComponent<HeroInputController>();
            root.AddComponent<HeroMotor>();
            root.AddComponent<HeroAnimatorBridge>();

            var input = root.GetComponent<HeroInputController>();
            var inputAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(InputActionsPath);
            SetSerializedReference(input, "inputActions", inputAsset);

            var p09Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(P09VisualPath);
            var visual = PrefabUtility.InstantiatePrefab(p09Prefab) as GameObject;
            if (visual == null)
            {
                UnityEngine.Object.DestroyImmediate(root);
                throw new InvalidOperationException("Failed to instantiate P09 visual prefab.");
            }

            visual.name = "P09_Visual";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;

            DisableCollidersRecursive(visual);
            ConfigureP09Animators(visual, controller);

            var bridge = root.GetComponent<HeroAnimatorBridge>();
            SetSerializedReference(bridge, "animator", visual.GetComponentInChildren<Animator>());

            if (existing == null)
            {
                PrefabUtility.SaveAsPrefabAsset(root, HeroPrefabPath);
            }
            else
            {
                PrefabUtility.SaveAsPrefabAssetAndConnect(root, HeroPrefabPath, InteractionMode.AutomatedAction);
            }

            UnityEngine.Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(HeroPrefabPath);
        }

        static void ConfigureP09Animators(GameObject visualRoot, RuntimeAnimatorController controller)
        {
            var animators = visualRoot.GetComponentsInChildren<Animator>(true);
            Animator primaryAnimator = null;

            foreach (var animator in animators)
            {
                if (primaryAnimator == null)
                {
                    primaryAnimator = animator;
                    animator.runtimeAnimatorController = controller;
                    animator.applyRootMotion = false;
                    animator.enabled = true;
                }
                else
                {
                    animator.enabled = false;
                }
            }

            if (primaryAnimator == null)
            {
                primaryAnimator = visualRoot.AddComponent<Animator>();
                primaryAnimator.runtimeAnimatorController = controller;
                primaryAnimator.applyRootMotion = false;
            }
        }

        static void DisableCollidersRecursive(GameObject root)
        {
            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
        }

        static void CreateOrUpdateScene(GameObject heroPrefab, bool force)
        {
            if (SceneIsValid() && !force)
            {
                return;
            }

            EnsureToonScapesSceneCopy(force);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            RemoveGameplayObjects();

            var spawnPosition = FindSpawnPosition(DefaultSpawnXZ);
            var hero = PrefabUtility.InstantiatePrefab(heroPrefab) as GameObject;
            if (hero == null)
            {
                throw new InvalidOperationException("Failed to instantiate Hero_P09 prefab.");
            }

            hero.transform.position = spawnPosition;
            hero.transform.rotation = Quaternion.identity;

            var gameplayCamera = SetupGameplayCamera(hero.transform, spawnPosition);
            SetupHints();

            if (hero.TryGetComponent<HeroMotor>(out var motor) && gameplayCamera != null)
            {
                SetSerializedReference(motor, "playerCamera", gameplayCamera);
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
        }

        static void EnsureToonScapesSceneCopy(bool force)
        {
            var targetPath = GetProjectPath(ScenePath);
            var sourcePath = GetProjectPath(ToonScapesScenePath);

            if (!force && File.Exists(targetPath))
            {
                var existing = File.ReadAllText(targetPath);
                if (existing.Contains("WindManager") || existing.Contains("Terrain_"))
                {
                    return;
                }
            }

            var targetDirectory = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            File.Copy(sourcePath, targetPath, overwrite: true);
            AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceUpdate);
        }

        static void RemoveGameplayObjects()
        {
            DestroyIfExists("Hero_P09");
            DestroyIfExists("Hero_Placeholder");
            DestroyIfExists("TestArenaHints");
            DestroyIfExists("Main Camera");

            var oldCamera = GameObject.Find("Camera");
            if (oldCamera != null)
            {
                UnityEngine.Object.DestroyImmediate(oldCamera);
            }
        }

        static void DestroyIfExists(string objectName)
        {
            var target = GameObject.Find(objectName);
            if (target != null)
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        static Vector3 FindSpawnPosition(Vector3 sampleXZ)
        {
            var best = new Vector3(sampleXZ.x, sampleXZ.y + 120f, sampleXZ.z);
            var terrains = UnityEngine.Object.FindObjectsByType<Terrain>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            foreach (var terrain in terrains)
            {
                var local = sampleXZ - terrain.transform.position;
                if (local.x < 0f || local.z < 0f)
                {
                    continue;
                }

                var size = terrain.terrainData.size;
                if (local.x > size.x || local.z > size.z)
                {
                    continue;
                }

                var height = terrain.SampleHeight(sampleXZ) + terrain.transform.position.y;
                if (height > best.y - 100f)
                {
                    best = new Vector3(sampleXZ.x, height + 0.1f, sampleXZ.z);
                }
            }

            return best;
        }

        static Camera SetupGameplayCamera(Transform heroTarget, Vector3 spawnPosition)
        {
            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";

            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            cameraGo.AddComponent<AudioListener>();
            AddUrpCameraData(cameraGo);

            var followCamera = cameraGo.AddComponent<TiltedFollowCamera>();
            SetSerializedReference(followCamera, "target", heroTarget);

            cameraGo.transform.position = spawnPosition + new Vector3(0f, 12f, -10f);
            cameraGo.transform.rotation = Quaternion.Euler(50f, 0f, 0f);

            return camera;
        }

        static void SetupHints()
        {
            var hintsGo = new GameObject("TestArenaHints");
            hintsGo.AddComponent<TestArenaHints>();
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

        static void SetSerializedReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static void UpdateBuildSettings()
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>
            {
                new(ScenePath, true)
            };

            const string TalentPath = "Assets/Scenes/TalentTestArena.unity";
            if (System.IO.File.Exists(GetProjectPath(TalentPath)))
            {
                scenes.Insert(0, new EditorBuildSettingsScene(TalentPath, true));
            }

            foreach (var existing in EditorBuildSettings.scenes)
            {
                if (existing.path == ScenePath || existing.path == TalentPath)
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
