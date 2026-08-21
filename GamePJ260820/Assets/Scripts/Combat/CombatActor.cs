using System.Collections.Generic;
using UnityEngine;

namespace GamePJ.Combat
{
    public enum TeamId
    {
        Player,
        Dummy
    }

    public enum StatusType
    {
        Slow,
        Haste,
        Root,
        Stun,
        Burn,
        Poison,
        SuperArmor,
        Stance,
        DamageBuff,
        AttackSpeedBuff,
        ArmorBreakBuff
    }

    public sealed class StatusInstance
    {
        public StatusType Type;
        public float Remaining;
        public float Magnitude;
        public int Stacks = 1;
    }

    [DisallowMultipleComponent]
    public sealed class CombatActor : MonoBehaviour
    {
        [SerializeField] TeamId team;
        [SerializeField] float maxHealth = 200f;
        [SerializeField] Transform aimPoint;

        public TeamId Team => team;
        public float MaxHealth => maxHealth;
        public float Health { get; private set; }
        public bool IsDead => Health <= 0f;
        public bool IsGuarding { get; set; }
        public Vector3 AimPosition => aimPoint != null ? aimPoint.position : transform.position + Vector3.up * 1.1f;

        public event System.Action<float, CombatActor> Damaged;
        public event System.Action Died;
        public event System.Action ResetCompleted;

        void Awake()
        {
            Health = maxHealth;
        }

        public void Configure(TeamId newTeam, float newMaxHealth)
        {
            team = newTeam;
            maxHealth = newMaxHealth;
            Health = newMaxHealth;
        }

        public void ResetState()
        {
            Health = maxHealth;
            IsGuarding = false;
            ResetCompleted?.Invoke();
        }

        public void Heal(float amount)
        {
            if (amount <= 0f || IsDead)
            {
                return;
            }

            Health = Mathf.Min(maxHealth, Health + amount);
        }

        public float ApplyDamage(float amount, CombatActor source)
        {
            if (amount <= 0f || IsDead)
            {
                return 0f;
            }

            Health = Mathf.Max(0f, Health - amount);
            Damaged?.Invoke(amount, source);
            if (IsDead)
            {
                Died?.Invoke();
            }

            return amount;
        }

        public void Knockback(Vector3 origin, float distance)
        {
            var direction = transform.position - origin;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
            {
                direction = -transform.forward;
            }

            transform.position += direction.normalized * distance;
        }
    }

    [DisallowMultipleComponent]
    public sealed class StatusController : MonoBehaviour
    {
        readonly List<StatusInstance> statuses = new();
        CombatActor actor;

        public float MoveSpeedMultiplier { get; private set; } = 1f;
        public bool MovementLocked { get; private set; }
        public bool HasSuperArmor { get; private set; }
        public bool HasStance { get; private set; }
        public float DamageTakenMultiplier { get; private set; } = 1f;
        public float OutgoingDamageBonus { get; private set; }
        public float AttackSpeedBonus { get; private set; }
        public float ArmorBreakBonus { get; private set; }

        public IReadOnlyList<StatusInstance> Statuses => statuses;

        void Awake()
        {
            actor = GetComponent<CombatActor>();
        }

        void Update()
        {
            var dt = Time.deltaTime;
            for (var i = statuses.Count - 1; i >= 0; i--)
            {
                var status = statuses[i];
                if (status.Type is StatusType.Burn or StatusType.Poison && actor != null)
                {
                    actor.ApplyDamage(status.Magnitude * dt, null);
                }

                status.Remaining -= dt;
                if (status.Remaining <= 0f)
                {
                    statuses.RemoveAt(i);
                }
            }

            Recalculate();
        }

        public void Clear()
        {
            statuses.Clear();
            Recalculate();
        }

        public void Add(StatusType type, float duration, float magnitude, bool stackable = true)
        {
            if (type is StatusType.Stun or StatusType.Root && HasSuperArmor)
            {
                CombatLog.Push("霸體免疫控制");
                return;
            }

            if (!stackable)
            {
                foreach (var existing in statuses)
                {
                    if (existing.Type == type)
                    {
                        existing.Remaining = Mathf.Max(existing.Remaining, duration);
                        existing.Magnitude = Mathf.Max(existing.Magnitude, magnitude);
                        Recalculate();
                        return;
                    }
                }
            }

            statuses.Add(new StatusInstance
            {
                Type = type,
                Remaining = duration,
                Magnitude = magnitude
            });
            Recalculate();
        }

        public bool Has(StatusType type)
        {
            foreach (var status in statuses)
            {
                if (status.Type == type)
                {
                    return true;
                }
            }

            return false;
        }

        public string Describe()
        {
            if (statuses.Count == 0)
            {
                return "無";
            }

            var parts = new List<string>(statuses.Count);
            foreach (var status in statuses)
            {
                parts.Add($"{ToChinese(status.Type)} {status.Remaining:0.0}s");
            }

            return string.Join("、", parts);
        }

        void Recalculate()
        {
            var speed = 1f;
            var locked = false;
            var superArmor = false;
            var stance = false;
            var outgoing = 0f;
            var attackSpeed = 0f;
            var armorBreak = 0f;

            foreach (var status in statuses)
            {
                switch (status.Type)
                {
                    case StatusType.Slow:
                        speed -= status.Magnitude;
                        break;
                    case StatusType.Haste:
                        speed += status.Magnitude;
                        break;
                    case StatusType.Root:
                    case StatusType.Stun:
                        locked = true;
                        break;
                    case StatusType.SuperArmor:
                        superArmor = true;
                        break;
                    case StatusType.Stance:
                        stance = true;
                        break;
                    case StatusType.DamageBuff:
                        outgoing += status.Magnitude;
                        break;
                    case StatusType.AttackSpeedBuff:
                        attackSpeed += status.Magnitude;
                        break;
                    case StatusType.ArmorBreakBuff:
                        armorBreak += status.Magnitude;
                        break;
                }
            }

            MoveSpeedMultiplier = Mathf.Clamp(speed, 0.15f, 2.5f);
            MovementLocked = locked;
            HasSuperArmor = superArmor;
            HasStance = stance;
            OutgoingDamageBonus = outgoing;
            AttackSpeedBonus = attackSpeed;
            ArmorBreakBonus = armorBreak;
        }

        static string ToChinese(StatusType type)
        {
            return type switch
            {
                StatusType.Slow => "緩速",
                StatusType.Haste => "加速",
                StatusType.Root => "定身",
                StatusType.Stun => "暈眩",
                StatusType.Burn => "燃燒",
                StatusType.Poison => "中毒",
                StatusType.SuperArmor => "霸體",
                StatusType.Stance => "架式",
                StatusType.DamageBuff => "傷害↑",
                StatusType.AttackSpeedBuff => "攻速↑",
                StatusType.ArmorBreakBuff => "破防↑",
                _ => type.ToString()
            };
        }
    }

    public static class CombatLog
    {
        public const int Capacity = 16;
        static readonly Queue<string> Lines = new();

        public static event System.Action<string> MessageAdded;

        public static IReadOnlyCollection<string> Snapshot => Lines.ToArray();

        public static void Push(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            var line = $"[{Time.time:0.00}] {message}";
            Lines.Enqueue(line);
            while (Lines.Count > Capacity)
            {
                Lines.Dequeue();
            }

            MessageAdded?.Invoke(line);
            Debug.Log("[TalentTest] " + message);
        }

        public static void Clear()
        {
            Lines.Clear();
        }
    }
}
