using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GamePJ.Modules
{
    /// <summary>
    /// Lazy-loads PROTOFACTOR animation clips from animator controllers for action playback.
    /// </summary>
    public static class ActionAnimationClipCache
    {
        static readonly Dictionary<string, AnimationClip> Clips = new();
        static readonly Dictionary<string, RuntimeAnimatorController> Controllers = new();

        public static AnimationClip GetClip(ActionAnimationDefinition definition)
        {
            if (definition == null ||
                string.IsNullOrEmpty(definition.ProtofactorClipName) ||
                string.IsNullOrEmpty(definition.SourceControllerPath))
            {
                return null;
            }

            var key = definition.SourceControllerPath + "|" + definition.ProtofactorClipName;
            if (Clips.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var clip = FindClipInController(definition.SourceControllerPath, definition.ProtofactorClipName);
            Clips[key] = clip;
            return clip;
        }

        static AnimationClip FindClipInController(string controllerPath, string clipName)
        {
            var controller = LoadController(controllerPath);
            if (controller == null)
            {
                return null;
            }

            foreach (var clip in controller.animationClips)
            {
                if (clip != null && clip.name == clipName)
                {
                    return clip;
                }
            }

            return null;
        }

        static RuntimeAnimatorController LoadController(string path)
        {
            if (Controllers.TryGetValue(path, out var cached))
            {
                return cached;
            }

            RuntimeAnimatorController controller = null;
#if UNITY_EDITOR
            controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path);
#endif
            Controllers[path] = controller;
            return controller;
        }
    }
}
