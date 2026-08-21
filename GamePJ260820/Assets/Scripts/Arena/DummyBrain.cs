using GamePJ.Combat;
using UnityEngine;

namespace GamePJ.Arena
{
    [DisallowMultipleComponent]
    public sealed class DummyBrain : MonoBehaviour
    {
        [SerializeField] float autoAttackInterval = 2.8f;
        [SerializeField] float telegraph = 0.7f;
        [SerializeField] float dummyDamage = 25f;
        [SerializeField] float attackRange = 2.2f;

        CombatActor actor;
        StatusController status;
        Transform hero;
        float nextAttack = -1f;
        float telegraphUntil;
        bool striking;

        public bool AutoAttack { get; set; }
        public bool GuardMode { get; set; }

        public static DummyBrain Create(Vector3 position)
        {
            var root = new GameObject("TrainingDummy");
            root.transform.position = position;
            root.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            CreatePart(root.transform, PrimitiveType.Cylinder, "Pole", new Vector3(0f, 0.9f, 0f), new Vector3(0.18f, 0.9f, 0.18f), new Color(0.35f, 0.28f, 0.2f));
            CreatePart(root.transform, PrimitiveType.Capsule, "Bag", new Vector3(0f, 1.25f, 0f), new Vector3(1.05f, 1.15f, 1.05f), new Color(0.72f, 0.32f, 0.22f));
            CreatePart(root.transform, PrimitiveType.Cylinder, "Base", new Vector3(0f, 0.08f, 0f), new Vector3(1.4f, 0.08f, 1.4f), new Color(0.22f, 0.22f, 0.22f));

            var collider = root.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 1.1f, 0f);
            collider.radius = 0.55f;
            collider.height = 2.2f;

            var actor = root.AddComponent<CombatActor>();
            actor.Configure(TeamId.Dummy, 2000f);
            root.AddComponent<StatusController>();
            return root.AddComponent<DummyBrain>();
        }

        static void CreatePart(Transform parent, PrimitiveType type, string partName, Vector3 localPosition, Vector3 localScale, Color color)
        {
            var part = GameObject.CreatePrimitive(type);
            part.name = partName;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            var renderer = part.GetComponent<Renderer>();
            if (renderer != null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit")
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

        void Awake()
        {
            actor = GetComponent<CombatActor>();
            status = GetComponent<StatusController>();
            if (actor != null)
            {
                actor.Died += OnDied;
            }
        }

        public void BindHero(Transform target)
        {
            hero = target;
        }

        public void ResetDummy()
        {
            actor?.ResetState();
            status?.Clear();
            striking = false;
            telegraphUntil = 0f;
            nextAttack = AutoAttack ? Time.time + 1.2f : -1f;
            transform.position = new Vector3(0f, 0f, 4.5f);
            transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            CombatLog.Push("沙包已重置");
        }

        void OnDied()
        {
            CombatLog.Push("沙包倒下，1.5 秒後重置");
            Invoke(nameof(ResetDummy), 1.5f);
        }

        void Update()
        {
            if (actor == null || actor.IsDead)
            {
                return;
            }

            actor.IsGuarding = GuardMode;

            if (status != null && status.MovementLocked)
            {
                return;
            }

            if (hero != null)
            {
                var look = hero.position - transform.position;
                look.y = 0f;
                if (look.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        Quaternion.LookRotation(look),
                        8f * Time.deltaTime);
                }
            }

            if (!AutoAttack)
            {
                return;
            }

            if (striking)
            {
                if (Time.time >= telegraphUntil)
                {
                    Strike();
                    striking = false;
                    nextAttack = Time.time + autoAttackInterval;
                }

                return;
            }

            if (nextAttack < 0f)
            {
                nextAttack = Time.time + autoAttackInterval;
            }

            if (Time.time >= nextAttack)
            {
                striking = true;
                telegraphUntil = Time.time + telegraph;
                CombatLog.Push("沙包開始前搖（可用閃避／格擋測試）");
            }
        }

        void Strike()
        {
            if (hero == null)
            {
                return;
            }

            var caster = hero.GetComponent<CombatCaster>();
            var distance = Vector3.Distance(
                new Vector3(transform.position.x, 0f, transform.position.z),
                new Vector3(hero.position.x, 0f, hero.position.z));
            if (distance > attackRange)
            {
                CombatLog.Push("沙包攻擊落空（距離不足）");
                return;
            }

            if (caster != null)
            {
                caster.NotifyIncomingHit(actor, dummyDamage);
            }
        }

        void OnGUI()
        {
            if (actor == null)
            {
                return;
            }

            var camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            var screen = camera.WorldToScreenPoint(actor.AimPosition + Vector3.up * 0.55f);
            if (screen.z < 0f)
            {
                return;
            }

            var width = 90f;
            var x = screen.x - width * 0.5f;
            var y = Screen.height - screen.y - 18f;
            var ratio = actor.MaxHealth <= 0f ? 0f : actor.Health / actor.MaxHealth;
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(new Rect(x, y, width, 10f), Texture2D.whiteTexture);
            GUI.color = Color.Lerp(new Color(0.85f, 0.2f, 0.2f), new Color(0.3f, 0.85f, 0.35f), ratio);
            GUI.DrawTexture(new Rect(x, y, width * ratio, 10f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };
            GUI.Label(new Rect(x, y - 16f, width, 16f), $"{actor.Health:0}/{actor.MaxHealth:0}", style);
            if (GuardMode)
            {
                GUI.Label(new Rect(x, y + 10f, width, 16f), "格擋中", style);
            }
        }

        void OnDrawGizmos()
        {
            if (!striking)
            {
                return;
            }

            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.35f);
            Gizmos.DrawSphere(transform.position + transform.forward * 1.1f, 1.1f);
        }
    }
}
