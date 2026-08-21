#if UNITY_EDITOR
using UnityEditor;

namespace GamePJ.EditorTools
{
    public static class HeroPrototypeBatchRunner
    {
        public static void RunFromCommandLine()
        {
            AssetDatabase.Refresh();
            HeroPrototypeAssetBuilder.RunFromCommandLine();
            EditorApplication.Exit(0);
        }
    }
}
#endif
