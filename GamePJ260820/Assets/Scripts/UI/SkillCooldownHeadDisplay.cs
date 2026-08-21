using GamePJ.Combat;
using GamePJ.Modules;
using UnityEngine;

namespace GamePJ.UI
{
    [DisallowMultipleComponent]
    public sealed class SkillCooldownHeadDisplay : MonoBehaviour
    {
        const float HeadOffsetY = 2.35f;
        const float SlotSize = 34f;
        const float SlotGap = 4f;

        [SerializeField] Transform anchor;
        [SerializeField] Vector2 screenOffset = new(0f, 8f);

        TalentLoadout loadout;
        CombatCaster caster;
        Camera worldCamera;
        static Texture2D whitePixel;

        public void Bind(TalentLoadout talent, CombatCaster combat, Camera camera = null)
        {
            loadout = talent;
            caster = combat;
            worldCamera = camera;
            anchor ??= talent != null ? talent.transform : transform;
        }

        void LateUpdate()
        {
            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }
        }

        void OnGUI()
        {
            if (loadout == null || caster == null || anchor == null || worldCamera == null)
            {
                return;
            }

            EnsureGuiResources();
            var worldPos = anchor.position + Vector3.up * HeadOffsetY;
            var screen = worldCamera.WorldToScreenPoint(worldPos);
            if (screen.z <= 0f)
            {
                return;
            }

            var activeSlots = CollectActiveSlots();
            if (activeSlots.Count == 0)
            {
                return;
            }

            var totalWidth = activeSlots.Count * SlotSize + (activeSlots.Count - 1) * SlotGap;
            var startX = screen.x - totalWidth * 0.5f + screenOffset.x;
            var startY = Screen.height - screen.y - SlotSize * 0.5f + screenOffset.y;

            for (var i = 0; i < activeSlots.Count; i++)
            {
                var slot = activeSlots[i];
                var rect = new Rect(startX + i * (SlotSize + SlotGap), startY, SlotSize, SlotSize);
                DrawSlotIcon(rect, slot);
            }
        }

        System.Collections.Generic.List<ActionSlotId> CollectActiveSlots()
        {
            var list = new System.Collections.Generic.List<ActionSlotId>(7);
            foreach (var slot in new[]
                     {
                         ActionSlotId.Attack1, ActionSlotId.Attack2, ActionSlotId.Attack3,
                         ActionSlotId.Attack4, ActionSlotId.Attack5, ActionSlotId.Dodge, ActionSlotId.Parry
                     })
            {
                var entry = loadout.Build.GetSlot(slot);
                if (entry != null && entry.HasAction)
                {
                    list.Add(slot);
                }
            }

            return list;
        }

        void DrawSlotIcon(Rect rect, ActionSlotId slot)
        {
            var resolved = loadout.Resolve(slot);
            var baseColor = SlotColor(resolved?.Action?.Attribute ?? HeroAttribute.None);
            GUI.color = baseColor;
            GUI.DrawTexture(rect, whitePixel);

            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, rect.height - 2f), whitePixel);

            var label = SlotShortLabel(slot);
            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 11,
                normal = { textColor = Color.white }
            };
            GUI.color = Color.white;
            GUI.Label(rect, label, style);

            var remain01 = caster.GetCooldownRemaining01(slot);
            if (remain01 > 0.001f)
            {
                RadialGuiDrawer.DrawFilledRadial360(rect, remain01, new Color(0.2f, 0.2f, 0.2f, 0.72f));
            }

            GUI.color = Color.white;
        }

        static Color SlotColor(HeroAttribute attribute)
        {
            return attribute switch
            {
                HeroAttribute.Strength => new Color(0.82f, 0.35f, 0.28f, 0.95f),
                HeroAttribute.Agility => new Color(0.28f, 0.72f, 0.42f, 0.95f),
                HeroAttribute.Intelligence => new Color(0.35f, 0.55f, 0.92f, 0.95f),
                _ => new Color(0.45f, 0.45f, 0.45f, 0.95f)
            };
        }

        static string SlotShortLabel(ActionSlotId slot)
        {
            return slot switch
            {
                ActionSlotId.Attack1 => "1",
                ActionSlotId.Attack2 => "2",
                ActionSlotId.Attack3 => "Q",
                ActionSlotId.Attack4 => "E",
                ActionSlotId.Attack5 => "R",
                ActionSlotId.Dodge => "D",
                ActionSlotId.Parry => "P",
                _ => "?"
            };
        }

        static void EnsureGuiResources()
        {
            if (whitePixel != null)
            {
                return;
            }

            whitePixel = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            whitePixel.SetPixel(0, 0, Color.white);
            whitePixel.Apply();
        }
    }

    static class RadialGuiDrawer
    {
        static Material radialMaterial;

        public static void DrawFilledRadial360(Rect rect, float remaining01, Color color)
        {
            remaining01 = Mathf.Clamp01(remaining01);
            if (remaining01 <= 0.001f || Event.current.type != EventType.Repaint)
            {
                return;
            }

            radialMaterial ??= new Material(Shader.Find("Hidden/Internal-Colored"))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            radialMaterial.SetPass(0);

            var center = new Vector3(rect.x + rect.width * 0.5f, rect.y + rect.height * 0.5f, 0f);
            var radius = rect.width * 0.5f;
            const int segments = 36;
            var sweep = 360f * remaining01;
            var startAngle = -90f;

            GL.PushMatrix();
            GL.LoadPixelMatrix(0, Screen.width, Screen.height, 0);
            GL.Begin(GL.TRIANGLES);
            GL.Color(color);
            for (var i = 0; i < segments; i++)
            {
                var t0 = startAngle + sweep * (i / (float)segments);
                var t1 = startAngle + sweep * ((i + 1) / (float)segments);
                var p0 = center + AngleToVector(t0) * radius;
                var p1 = center + AngleToVector(t1) * radius;
                GL.Vertex(center);
                GL.Vertex(p0);
                GL.Vertex(p1);
            }

            GL.End();
            GL.PopMatrix();
        }

        static Vector3 AngleToVector(float degrees)
        {
            var rad = degrees * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
        }
    }
}
