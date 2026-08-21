using GamePJ.Modules;
using UnityEngine;

namespace GamePJ.Combat
{
    public static class CombatRangeRules
    {
        public const float MeleeWeaponMaxRange = 2f;
        public const float RangedWeaponMaxRange = 10f;
        public const float BodyHitRadius = 0.3f;
        public const float MinSlashArcAngle = 30f;
        public const float MaxSlashArcAngle = 360f;
        public const float MinAimCircleRadius = 1f;
        public const float MaxAimCircleRadius = 3f;
    }

    public enum HitRegionType
    {
        BodyCircle,
        SelfArc,
        ForwardRect,
        AimCircle
    }

    public readonly struct HitRegion
    {
        public readonly HitRegionType Type;
        public readonly Vector3 Origin;
        public readonly Vector3 Center;
        public readonly Vector3 Forward;
        public readonly float Radius;
        public readonly float ArcAngle;
        public readonly float Length;
        public readonly float HalfWidth;

        public HitRegion(
            HitRegionType type,
            Vector3 origin,
            Vector3 center,
            Vector3 forward,
            float radius,
            float arcAngle,
            float length,
            float halfWidth)
        {
            Type = type;
            Origin = origin;
            Center = center;
            Forward = forward;
            Radius = radius;
            ArcAngle = arcAngle;
            Length = length;
            HalfWidth = halfWidth;
        }

        public bool Contains(Vector3 worldPoint)
        {
            switch (Type)
            {
                case HitRegionType.BodyCircle:
                case HitRegionType.AimCircle:
                    return PlanarDistance(Center, worldPoint) <= Radius;
                case HitRegionType.SelfArc:
                {
                    var to = worldPoint - Origin;
                    to.y = 0f;
                    if (to.sqrMagnitude > Radius * Radius)
                    {
                        return false;
                    }

                    if (ArcAngle >= 359f)
                    {
                        return true;
                    }

                    var forward = Forward;
                    forward.y = 0f;
                    return forward.sqrMagnitude < 0.0001f || Vector3.Angle(forward.normalized, to) <= ArcAngle * 0.5f;
                }
                case HitRegionType.ForwardRect:
                {
                    var to = worldPoint - Origin;
                    to.y = 0f;
                    var forward = Forward;
                    forward.y = 0f;
                    if (forward.sqrMagnitude < 0.0001f)
                    {
                        return false;
                    }

                    forward.Normalize();
                    var proj = Vector3.Dot(to, forward);
                    if (proj < 0f || proj > Length)
                    {
                        return false;
                    }

                    var perp = to - forward * proj;
                    return perp.sqrMagnitude <= HalfWidth * HalfWidth;
                }
                default:
                    return false;
            }
        }

        public float QueryRadius
        {
            get
            {
                return Type switch
                {
                    HitRegionType.ForwardRect => Mathf.Sqrt(Length * Length * 0.25f + HalfWidth * HalfWidth),
                    _ => Radius
                };
            }
        }

        public Vector3 QueryCenter
        {
            get
            {
                return Type switch
                {
                    HitRegionType.ForwardRect => Origin + Forward.normalized * (Length * 0.5f),
                    HitRegionType.SelfArc => Origin,
                    _ => Center
                };
            }
        }

        static float PlanarDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }

    public readonly struct AttackHitShape
    {
        public const int MaxRegions = 3;

        public readonly bool IsValid;
        public readonly HitRegion Region0;
        public readonly HitRegion Region1;
        public readonly HitRegion Region2;
        public readonly int RegionCount;
        public readonly float WeaponMaxRange;
        public readonly Vector3 AimPoint;

        AttackHitShape(
            int count,
            HitRegion r0,
            HitRegion r1,
            HitRegion r2,
            float weaponMaxRange,
            Vector3 aimPoint)
        {
            IsValid = count > 0;
            RegionCount = count;
            Region0 = r0;
            Region1 = r1;
            Region2 = r2;
            WeaponMaxRange = weaponMaxRange;
            AimPoint = aimPoint;
        }

        public HitRegion GetRegion(int index)
        {
            return index switch
            {
                0 => Region0,
                1 => Region1,
                2 => Region2,
                _ => default
            };
        }

        public bool Contains(Vector3 worldPoint)
        {
            for (var i = 0; i < RegionCount; i++)
            {
                if (GetRegion(i).Contains(worldPoint))
                {
                    return true;
                }
            }

            return false;
        }

        public static AttackHitShape Compute(
            ResolvedAction resolved,
            Transform actor,
            Vector3 mouseGroundPoint,
            float weaponMaxRange,
            float charge)
        {
            if (resolved?.Action == null || actor == null)
            {
                return default;
            }

            var actorPos = actor.position;
            var effectiveRange = weaponMaxRange * (1f + resolved.ChargeRangeBonus * charge);
            var aimPoint = ClampAimPoint(actorPos, mouseGroundPoint, effectiveRange);
            var forward = aimPoint - actorPos;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = actor.forward;
                forward.y = 0f;
            }

            forward.Normalize();
            var rectLength = Mathf.Min(Vector3.Distance(actorPos, aimPoint), effectiveRange);

            return resolved.Action.Id switch
            {
                "act_slash_str" => BuildSlash(actorPos, forward, effectiveRange, resolved, charge),
                "act_strike_str" => BuildRectWithBody(actorPos, forward, rectLength, 0.6f),
                "act_thrust_agi" => BuildRectWithBody(actorPos, forward, rectLength, 0.35f),
                "act_shot_phys_agi" => BuildForwardRect(actorPos, forward, rectLength, 0.25f),
                "act_aoe_int" or "act_shot_magic_int" => BuildAimCircle(aimPoint, charge, resolved.ChargeRangeBonus),
                _ => BuildLegacy(resolved, actor, forward, effectiveRange, charge, actorPos, aimPoint)
            };
        }

        static AttackHitShape BuildSlash(Vector3 actorPos, Vector3 forward, float arcRadius, ResolvedAction resolved, float charge)
        {
            var arcAngle = Mathf.Lerp(
                CombatRangeRules.MinSlashArcAngle,
                Mathf.Clamp(resolved.Action.ArcAngle, CombatRangeRules.MinSlashArcAngle, CombatRangeRules.MaxSlashArcAngle),
                Mathf.Clamp01(charge));

            var body = new HitRegion(
                HitRegionType.BodyCircle,
                actorPos,
                actorPos,
                forward,
                CombatRangeRules.BodyHitRadius,
                360f,
                0f,
                0f);
            var arc = new HitRegion(
                HitRegionType.SelfArc,
                actorPos,
                actorPos,
                forward,
                arcRadius,
                arcAngle,
                0f,
                0f);
            return new AttackHitShape(2, body, arc, default, arcRadius, actorPos);
        }

        static AttackHitShape BuildRectWithBody(Vector3 actorPos, Vector3 forward, float length, float halfWidth)
        {
            var body = new HitRegion(
                HitRegionType.BodyCircle,
                actorPos,
                actorPos,
                forward,
                CombatRangeRules.BodyHitRadius,
                360f,
                0f,
                0f);
            var rect = new HitRegion(
                HitRegionType.ForwardRect,
                actorPos,
                actorPos,
                forward,
                0f,
                0f,
                Mathf.Max(0.05f, length),
                halfWidth);
            return new AttackHitShape(2, body, rect, default, length, actorPos + forward * length);
        }

        static AttackHitShape BuildForwardRect(Vector3 actorPos, Vector3 forward, float length, float halfWidth)
        {
            var rect = new HitRegion(
                HitRegionType.ForwardRect,
                actorPos,
                actorPos,
                forward,
                0f,
                0f,
                Mathf.Max(0.05f, length),
                halfWidth);
            return new AttackHitShape(1, rect, default, default, length, actorPos + forward * length);
        }

        static AttackHitShape BuildAimCircle(Vector3 aimPoint, float charge, float chargeRangeBonus)
        {
            var radius = Mathf.Lerp(
                CombatRangeRules.MinAimCircleRadius,
                CombatRangeRules.MaxAimCircleRadius,
                Mathf.Clamp01(charge));
            radius *= 1f + chargeRangeBonus * charge;
            var circle = new HitRegion(
                HitRegionType.AimCircle,
                aimPoint,
                aimPoint,
                Vector3.forward,
                radius,
                360f,
                0f,
                0f);
            return new AttackHitShape(1, circle, default, default, radius, aimPoint);
        }

        static AttackHitShape BuildLegacy(
            ResolvedAction resolved,
            Transform actor,
            Vector3 forward,
            float effectiveRange,
            float charge,
            Vector3 actorPos,
            Vector3 aimPoint)
        {
            switch (resolved.Action.Kind)
            {
                case ActionKind.MeleeThrust:
                    return BuildRectWithBody(actorPos, forward, effectiveRange * 0.85f, 0.35f);
                case ActionKind.Projectile:
                    return BuildForwardRect(actorPos, forward, effectiveRange, resolved.Action.Radius);
                case ActionKind.Aoe:
                    return BuildAimCircle(aimPoint, charge, resolved.ChargeRangeBonus);
                default:
                    return BuildSlash(actorPos, forward, effectiveRange, resolved, charge);
            }
        }

        static Vector3 ClampAimPoint(Vector3 actorPos, Vector3 mouseGroundPoint, float maxRange)
        {
            var delta = mouseGroundPoint - actorPos;
            delta.y = 0f;
            if (delta.sqrMagnitude <= maxRange * maxRange)
            {
                mouseGroundPoint.y = actorPos.y;
                return mouseGroundPoint;
            }

            return actorPos + delta.normalized * maxRange;
        }
    }
}
