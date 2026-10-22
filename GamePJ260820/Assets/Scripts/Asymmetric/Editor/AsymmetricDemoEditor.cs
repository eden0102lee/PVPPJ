#if UNITY_EDITOR
using GamePJ.Asymmetric;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GamePJ.EditorTools
{
    public static class AsymmetricDemoEditor
    {
        public const string ScenePath = "Assets/Scenes/AsymmetricDemo.unity";
        [MenuItem("GamePJ/Asymmetric Demo/Open Demo")]
        public static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath);
        }
        [MenuItem("GamePJ/Asymmetric Demo/Build Windows Demo")]
        public static void Build()
        {
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { ScenePath }, locationPathName = "Builds/AsymmetricDemo/AsymmetricDemo.exe",
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new System.Exception("Asymmetric DEMO build failed: " + report.summary.result);
        }
        // Can also be invoked via Unity -batchmode -executeMethod ...Validate.
        public static void Validate()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            bool found = false;
            foreach (var root in scene.GetRootGameObjects()) if (root.GetComponent<AsymmetricDemo>() != null) found = true;
            EditorSceneManager.CloseScene(scene, true);
            if (!found) throw new System.Exception("Demo scene bootstrap is missing.");
            Debug.Log("ASYMMETRIC_DEMO_VALIDATION_PASSED");
        }
    }
}
#endif
