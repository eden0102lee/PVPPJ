using UnityEngine;

namespace GamePJ.Arena
{
    public static class CircularArenaFactory
    {
        public const float Radius = 12f;
        public const int Segments = 36;
        public const float WallHeight = 1.15f;
        public const float AirWallHeight = 8f;

        public static Transform Build(Transform parent)
        {
            var root = new GameObject("CircularArena").transform;
            root.SetParent(parent, false);

            CreateFloor(root);
            CreateLowWalls(root);
            CreateAirWalls(root);
            CreateCenterMark(root);
            return root;
        }

        static void CreateFloor(Transform parent)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            floor.name = "ArenaFloor";
            floor.transform.SetParent(parent, false);
            floor.transform.localPosition = new Vector3(0f, -0.1f, 0f);
            floor.transform.localScale = new Vector3(Radius * 2f, 0.1f, Radius * 2f);
            floor.layer = LayerMask.NameToLayer("Ground") >= 0 ? LayerMask.NameToLayer("Ground") : 0;
            ApplyColor(floor, new Color(0.28f, 0.3f, 0.27f));
        }

        static void CreateLowWalls(Transform parent)
        {
            var walls = new GameObject("LowWalls").transform;
            walls.SetParent(parent, false);
            var chord = 2f * Mathf.PI * Radius / Segments;
            for (var i = 0; i < Segments; i++)
            {
                var angle = i / (float)Segments * Mathf.PI * 2f;
                var position = new Vector3(Mathf.Cos(angle) * Radius, WallHeight * 0.5f, Mathf.Sin(angle) * Radius);
                var segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
                segment.name = $"LowWall_{i:00}";
                segment.transform.SetParent(walls, false);
                segment.transform.position = position;
                segment.transform.rotation = Quaternion.LookRotation(position);
                segment.transform.localScale = new Vector3(chord + 0.08f, WallHeight, 0.38f);
                ApplyColor(segment, new Color(0.45f, 0.38f, 0.3f));
            }
        }

        static void CreateAirWalls(Transform parent)
        {
            var walls = new GameObject("AirWalls").transform;
            walls.SetParent(parent, false);
            var airRadius = Radius + 0.28f;
            var chord = 2f * Mathf.PI * airRadius / Segments;
            for (var i = 0; i < Segments; i++)
            {
                var angle = i / (float)Segments * Mathf.PI * 2f;
                var position = new Vector3(Mathf.Cos(angle) * airRadius, AirWallHeight * 0.5f, Mathf.Sin(angle) * airRadius);
                var segment = new GameObject($"AirWall_{i:00}");
                segment.transform.SetParent(walls, false);
                segment.transform.position = position;
                segment.transform.rotation = Quaternion.LookRotation(new Vector3(position.x, 0f, position.z));
                var box = segment.AddComponent<BoxCollider>();
                box.size = new Vector3(chord + 0.1f, AirWallHeight, 0.22f);
            }
        }

        static void CreateCenterMark(Transform parent)
        {
            var mark = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mark.name = "CenterMark";
            mark.transform.SetParent(parent, false);
            mark.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            mark.transform.localScale = new Vector3(2.4f, 0.02f, 2.4f);
            var collider = mark.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying)
                {
                    Object.Destroy(collider);
                }
                else
                {
                    Object.DestroyImmediate(collider);
                }
            }
            ApplyColor(mark, new Color(0.72f, 0.55f, 0.22f));
        }

        static void ApplyColor(GameObject go, Color color)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                         ?? Shader.Find("Standard")
                         ?? Shader.Find("Sprites/Default");
            var material = new Material(shader) { color = color };
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            renderer.sharedMaterial = material;
        }
    }
}
