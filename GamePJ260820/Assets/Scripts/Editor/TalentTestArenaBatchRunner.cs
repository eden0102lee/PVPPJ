#if UNITY_EDITOR
using UnityEditor;

namespace GamePJ.EditorTools
{
    public static class TalentTestArenaBatchRunner
    {
        public static void RunFromCommandLine()
        {
            AssetDatabase.Refresh();
            TalentTestArenaBuilder.RunFromCommandLine();
            EditorApplication.Exit(0);
        }
    }
}
#endif
