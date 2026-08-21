#if UNITY_EDITOR
using System.IO;
using UnityEditor;

namespace GamePJ.EditorTools
{
    public static class PluginConflictBatchRunner
    {
        public static void RunFromCommandLine()
        {
            AssetDatabase.Refresh();
            var report = PluginConflictAuditor.BuildReport();
            var reportPath = Path.Combine(Directory.GetCurrentDirectory(), "Assets/Editor/PluginSetup/last-audit-report.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
            File.WriteAllText(reportPath, report);

            var hasMissingLilToon = report.Contains("MISSING: lilToon shader GUID");
            var hasMissingPackages = report.Contains("MISSING: ");
            if (hasMissingLilToon)
            {
                EditorApplication.Exit(1);
                return;
            }

            if (hasMissingPackages)
            {
                // Missing Asset Store binaries are expected until the user re-downloads them.
                UnityEngine.Debug.LogWarning("Audit completed with missing Asset Store .unitypackage files. See Assets/Docs/PLUGIN_SETUP.md");
            }

            EditorApplication.Exit(0);
        }
    }
}
#endif
