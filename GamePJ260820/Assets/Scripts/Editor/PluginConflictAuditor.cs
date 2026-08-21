#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace GamePJ.EditorTools
{
    public static class PluginConflictAuditor
    {
        const string LilToonShaderGuid = "efa77a80ca0344749b4f19fdd5891cbe";
        const string ReportPath = "Assets/Editor/PluginSetup/last-audit-report.txt";

        static readonly (string Label, string RelativePath, string Notes)[] ExpectedUnityPackages =
        {
            (
                "P09 lilToon installer (legacy)",
                "Assets/P09_Modular_Humanoid/Shader_installer/jp.lilxyzw.liltoon-1.x.x-installer.unitypackage",
                "Optional: lilToon is installed via Package Manager (jp.lilxyzw.liltoon@2.3.4)."
            ),
            (
                "P09 MagicaCloth2 setup",
                "Assets/P09_Modular_Humanoid/Setup_MagicaCloth2.unitypackage",
                "Re-download from Asset Store and double-click to import MagicaCloth2 collider setup for P09."
            ),
            (
                "CombatGirls UTS SDF (Unity 6 URP)",
                "Assets/CombatGirlsCharacterPack/Unity Chan Toon Shader - SDF - Unity6_URP_SwordShield.unitypackage",
                "Preferred for CombatGirls toon look on Unity 6 URP."
            ),
            (
                "CombatGirls UTS SDF (URP)",
                "Assets/CombatGirlsCharacterPack/Unity Chan Toon Shader - SDF - URP_SwordShield.unitypackage",
                "Fallback if Unity 6 package is unavailable."
            ),
            (
                "Vefects Shader Graph assets",
                "Assets/Vefects/Stylized VFX/Shader Graph/Stylized VFX Shader Graph Assets.unitypackage",
                "Required for URP-compatible Vefects VFX shaders."
            ),
        };

        [MenuItem("GamePJ/Plugin Setup/Run Conflict Audit")]
        public static void RunAuditFromMenu()
        {
            var report = BuildReport();
            WriteReport(report);
            Debug.Log(report);
            EditorUtility.DisplayDialog("Plugin Conflict Audit", "Audit complete. See Console and:\n" + ReportPath, "OK");
        }

        [MenuItem("GamePJ/Plugin Setup/Import Available .unitypackage Files")]
        public static void ImportAvailableUnityPackages()
        {
            var imported = new List<string>();
            foreach (var entry in ExpectedUnityPackages)
            {
                var fullPath = Path.Combine(Application.dataPath, "..", entry.RelativePath).Replace('\\', '/');
                fullPath = Path.GetFullPath(fullPath);
                if (!File.Exists(fullPath))
                {
                    continue;
                }

                AssetDatabase.ImportPackage(entry.RelativePath, interactive: false);
                imported.Add(entry.Label);
            }

            if (imported.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "Import .unitypackage",
                    "No expected .unitypackage files were found on disk.\nRe-download them from the Asset Store into the paths listed in Assets/Docs/PLUGIN_SETUP.md.",
                    "OK");
                return;
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Import .unitypackage", "Imported:\n- " + string.Join("\n- ", imported), "OK");
        }

        [MenuItem("GamePJ/Plugin Setup/Fix URP Depth and Opaque Textures")]
        public static void FixUrpDepthAndOpaqueTextures()
        {
            var updated = 0;
            var guids = AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                if (asset == null)
                {
                    continue;
                }

                var serialized = new SerializedObject(asset);
                var depth = serialized.FindProperty("m_RequireDepthTexture");
                var opaque = serialized.FindProperty("m_RequireOpaqueTexture");
                if (depth == null || opaque == null)
                {
                    continue;
                }

                var changed = false;
                if (!depth.boolValue)
                {
                    depth.boolValue = true;
                    changed = true;
                }

                if (!opaque.boolValue)
                {
                    opaque.boolValue = true;
                    changed = true;
                }

                if (!changed)
                {
                    continue;
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
                updated++;
            }

            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog(
                "URP Settings",
                updated > 0
                    ? $"Enabled Depth Texture and Opaque Texture on {updated} URP asset(s)."
                    : "All URP assets already had Depth Texture and Opaque Texture enabled.",
                "OK");
        }

        public static string BuildReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("GamePJ Plugin Conflict Audit");
            sb.AppendLine("Generated: " + DateTime.Now.ToString("u"));
            sb.AppendLine();

            AppendPackageSection(sb);
            AppendLilToonDuplicateSection(sb);
            AppendMissingUnityPackagesSection(sb);
            AppendShaderSection(sb);
            AppendUrpSection(sb);
            AppendToonStackSection(sb);

            return sb.ToString();
        }

        static void AppendPackageSection(StringBuilder sb)
        {
            sb.AppendLine("== Package Manager ==");
            var manifestPath = Path.Combine(Application.dataPath, "..", "Packages", "manifest.json");
            if (File.Exists(manifestPath))
            {
                var manifest = File.ReadAllText(manifestPath);
                sb.AppendLine(manifest.Contains("jp.lilxyzw.liltoon")
                    ? "OK: jp.lilxyzw.liltoon is listed in manifest.json"
                    : "MISSING: jp.lilxyzw.liltoon not in manifest.json");
                sb.AppendLine(manifest.Contains("com.unity.toonshader")
                    ? "INFO: com.unity.toonshader (UTS3 preview) is installed for CombatGirls fallback"
                    : "INFO: com.unity.toonshader not installed");
            }

            sb.AppendLine();
        }

        static void AppendLilToonDuplicateSection(StringBuilder sb)
        {
            sb.AppendLine("== lilToon duplicate install check ==");
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var embeddedPath = Path.Combine(projectRoot, "Packages", "jp.lilxyzw.liltoon");
            var embeddedExists = Directory.Exists(embeddedPath);

            var packageCacheRoot = Path.Combine(projectRoot, "Library", "PackageCache");
            string[] cachedCopies = Array.Empty<string>();
            if (Directory.Exists(packageCacheRoot))
            {
                cachedCopies = Directory.GetDirectories(packageCacheRoot, "jp.lilxyzw.liltoon@*");
            }

            sb.AppendLine(embeddedExists
                ? "OK: embedded lilToon at Packages/jp.lilxyzw.liltoon"
                : "MISSING: embedded lilToon at Packages/jp.lilxyzw.liltoon");

            if (cachedCopies.Length == 0)
            {
                sb.AppendLine("OK: no jp.lilxyzw.liltoon@* folders in Library/PackageCache");
            }
            else if (embeddedExists)
            {
                sb.AppendLine("DUPLICATE: lilToon exists in both Packages/ and PackageCache:");
                foreach (var copy in cachedCopies)
                {
                    sb.AppendLine("  " + copy);
                }

                sb.AppendLine("  Fix: close Unity, delete Library/PackageCache/jp.lilxyzw.liltoon@*, reopen project.");
            }
            else
            {
                sb.AppendLine("INFO: lilToon only in PackageCache (no embedded copy):");
                foreach (var copy in cachedCopies)
                {
                    sb.AppendLine("  " + copy);
                }
            }

            sb.AppendLine();
        }

        static void AppendMissingUnityPackagesSection(StringBuilder sb)
        {
            sb.AppendLine("== Missing .unitypackage binaries ==");
            foreach (var entry in ExpectedUnityPackages)
            {
                var fullPath = Path.Combine(Application.dataPath, "..", entry.RelativePath);
                var exists = File.Exists(fullPath);
                sb.AppendLine(exists ? "OK: " : "MISSING: ");
                sb.AppendLine("  " + entry.Label);
                sb.AppendLine("  Path: " + entry.RelativePath);
                sb.AppendLine("  Notes: " + entry.Notes);
            }

            sb.AppendLine();
        }

        static void AppendShaderSection(StringBuilder sb)
        {
            sb.AppendLine("== Shader / material checks ==");

            var lilShader = AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(LilToonShaderGuid));
            sb.AppendLine(lilShader != null
                ? "OK: lilToon shader GUID resolves (" + lilShader.name + ")"
                : "MISSING: lilToon shader GUID " + LilToonShaderGuid + " (P09 materials will be pink)");

            var p09Broken = CountBrokenMaterials("Assets/P09_Modular_Humanoid", LilToonShaderGuid);
            sb.AppendLine("P09 materials referencing lilToon GUID: " + p09Broken + " unresolved");

            var vefectsBirpShaders = Directory.GetFiles(
                    Path.Combine(Application.dataPath, "Vefects"),
                    "*BIRP*.shader",
                    SearchOption.AllDirectories)
                .Length;
            sb.AppendLine("Vefects BIRP shader files present: " + vefectsBirpShaders + " (need URP/Shader Graph package for production VFX)");

            var combatGirlsStandard = CountBuiltInStandardMaterials("Assets/CombatGirlsCharacterPack");
            sb.AppendLine("CombatGirls materials on Built-in Standard: " + combatGirlsStandard + " (import UTS SDF Unity6 URP package or convert to UTS3)");

            sb.AppendLine();
        }

        static void AppendUrpSection(StringBuilder sb)
        {
            sb.AppendLine("== URP distortion prerequisites ==");
            var guids = AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                var serialized = new SerializedObject(asset);
                var depth = serialized.FindProperty("m_RequireDepthTexture")?.boolValue ?? false;
                var opaque = serialized.FindProperty("m_RequireOpaqueTexture")?.boolValue ?? false;
                sb.AppendLine(path + ": Depth=" + depth + ", Opaque=" + opaque);
            }

            sb.AppendLine();
        }

        static void AppendToonStackSection(StringBuilder sb)
        {
            sb.AppendLine("== Toon stack mapping ==");
            sb.AppendLine("P09_Modular_Humanoid -> lilToon (jp.lilxyzw.liltoon via UPM)");
            sb.AppendLine("CombatGirlsCharacterPack -> UTS SDF Unity6 URP package OR com.unity.toonshader");
            sb.AppendLine("ToonScapes -> ToonScapes/URP/* custom shaders");
            sb.AppendLine("Project default RP -> URP 17 (PC_RPAsset / Mobile_RPAsset)");
            sb.AppendLine();
        }

        static int CountBrokenMaterials(string root, string shaderGuid)
        {
            var lilShader = AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(shaderGuid));
            var count = 0;
            var guids = AssetDatabase.FindAssets("t:Material", new[] { root });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    count++;
                    continue;
                }

                if (material.shader == null || material.shader.name.Contains("Hidden/InternalErrorShader"))
                {
                    count++;
                    continue;
                }

                if (lilShader == null && path.EndsWith(".mat", StringComparison.OrdinalIgnoreCase))
                {
                    var text = File.ReadAllText(path);
                    if (text.Contains(shaderGuid))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        static int CountBuiltInStandardMaterials(string root)
        {
            return AssetDatabase.FindAssets("t:Material", new[] { root })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<Material>)
                .Count(material => material != null && material.shader != null && material.shader.name == "Standard");
        }

        static void WriteReport(string report)
        {
            var directory = Path.GetDirectoryName(Path.Combine(Application.dataPath, "..", ReportPath));
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(Path.Combine(Application.dataPath, "..", ReportPath), report);
            AssetDatabase.Refresh();
        }
    }
}
#endif
