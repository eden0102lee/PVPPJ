using UnityEngine;

namespace GamePJ.UI
{
    [DisallowMultipleComponent]
    public sealed class TestArenaHints : MonoBehaviour
    {
        [SerializeField] string hintText =
            "WASD：移動\n滑鼠：面向\n左鍵：攻擊\n場景：ToonScapes Spring Isles";

        void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.box)
            {
                fontSize = 16,
                alignment = TextAnchor.UpperLeft,
                padding = new RectOffset(12, 12, 12, 12)
            };

            GUI.Box(new Rect(16f, 16f, 260f, 90f), hintText, style);
        }
    }
}
