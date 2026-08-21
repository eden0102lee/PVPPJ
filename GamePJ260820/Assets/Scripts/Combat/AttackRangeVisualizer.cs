using System.Collections.Generic;
using GamePJ.Modules;
using GamePJ.Player;
using UnityEngine;

namespace GamePJ.Combat
{
    [DisallowMultipleComponent]
    public sealed class AttackRangeVisualizer : MonoBehaviour
    {
        const int ArcSegments = 36;

        [SerializeField] Transform aimOrigin;
        [SerializeField] Color meleeColor = new(1f, 0.45f, 0.2f, 0.85f);
        [SerializeField] Color bodyColor = new(1f, 0.75f, 0.35f, 0.85f);
        [SerializeField] Color aoeColor = new(0.35f, 0.7f, 1f, 0.85f);
        [SerializeField] float lineWidth = 0.07f;
        [SerializeField] bool showByDefault = true;

        CombatCaster caster;
        TalentLoadout loadout;
        HeroMotor motor;
        P09EquipmentController equipment;
        LineRenderer primaryLine;
        LineRenderer secondaryLine;
        GameObject fillDisc;
        float flashUntil;

        readonly List<Vector3> pointBuffer = new(64);

        public bool Visible { get; set; } = true;
        public ActionSlotId PreviewSlot { get; set; } = ActionSlotId.Attack1;

        public void Bind(
            CombatCaster combatCaster,
            TalentLoadout talentLoadout,
            HeroMotor heroMotor = null,
            P09EquipmentController weaponSwitcher = null,
            Transform firePoint = null)
        {
            caster = combatCaster;
            loadout = talentLoadout;
            motor = heroMotor;
            equipment = weaponSwitcher;
            if (firePoint != null)
            {
                aimOrigin = firePoint;
            }
        }

        void Awake()
        {
            Visible = showByDefault;
            primaryLine = CreateLineRenderer("RangePrimary");
            secondaryLine = CreateLineRenderer("RangeSecondary");
            fillDisc = CreateFillDisc();
        }

        void LateUpdate()
        {
            if (!Visible || loadout == null)
            {
                SetActive(false);
                return;
            }

            var slot = caster != null && caster.IsCharging ? caster.ChargingSlot : PreviewSlot;
            var resolved = loadout.Resolve(slot);
            if (resolved?.Action == null)
            {
                SetActive(false);
                return;
            }

            var charge = caster != null && caster.IsCharging ? caster.Charge01 : 0f;
            var shape = BuildPreviewShape(resolved, charge);
            if (!shape.IsValid)
            {
                SetActive(false);
                return;
            }

            var alpha = Time.time < flashUntil ? 1f : caster != null && caster.IsCharging ? 0.95f : 0.55f;
            DrawShape(shape, alpha);
        }

        public void Flash(AttackHitShape shape)
        {
            flashUntil = Time.time + 0.28f;
            DrawShape(shape, 1f);
        }

        AttackHitShape BuildPreviewShape(ResolvedAction resolved, float charge)
        {
            var mousePoint = motor != null && motor.TryGetMouseGroundPoint(out var point)
                ? point
                : transform.position + transform.forward * 2f;
            var weaponRange = resolved.WeaponRange > 0f
                ? resolved.WeaponRange
                : CombatRangeRules.MeleeWeaponMaxRange;
            return AttackHitShape.Compute(resolved, transform, mousePoint, weaponRange, charge);
        }

        void DrawShape(AttackHitShape shape, float alpha)
        {
            SetActive(true);
            primaryLine.enabled = false;
            secondaryLine.enabled = false;
            fillDisc.SetActive(false);

            for (var i = 0; i < shape.RegionCount; i++)
            {
                var region = shape.GetRegion(i);
                var line = i == 0 ? primaryLine : secondaryLine;
                var color = region.Type switch
                {
                    HitRegionType.BodyCircle => bodyColor,
                    HitRegionType.AimCircle => aoeColor,
                    _ => meleeColor
                };
                color.a *= alpha;
                DrawRegion(line, region, color, i == 0 && region.Type == HitRegionType.AimCircle);
            }
        }

        void DrawRegion(LineRenderer line, HitRegion region, Color color, bool fill)
        {
            pointBuffer.Clear();
            switch (region.Type)
            {
                case HitRegionType.BodyCircle:
                case HitRegionType.AimCircle:
                    SampleCircle(region.Center, region.Radius, pointBuffer, 20);
                    line.loop = true;
                    break;
                case HitRegionType.SelfArc:
                    if (region.ArcAngle >= 359f)
                    {
                        SampleCircle(region.Origin, region.Radius, pointBuffer, ArcSegments);
                        line.loop = true;
                    }
                    else
                    {
                        pointBuffer.Add(region.Origin);
                        SampleArc(region.Origin, region.Radius, region.ArcAngle, region.Forward, pointBuffer, ArcSegments);
                        pointBuffer.Add(region.Origin);
                        line.loop = false;
                    }

                    break;
                case HitRegionType.ForwardRect:
                    SampleRect(region, pointBuffer);
                    line.loop = true;
                    break;
            }

            if (pointBuffer.Count == 0)
            {
                return;
            }

            line.enabled = true;
            line.startColor = line.endColor = color;
            line.positionCount = pointBuffer.Count;
            line.SetPositions(pointBuffer.ToArray());

            if (fill && fillDisc != null && region.Type == HitRegionType.AimCircle)
            {
                fillDisc.SetActive(true);
                fillDisc.transform.position = region.Center + Vector3.up * 0.04f;
                fillDisc.transform.localScale = new Vector3(region.Radius * 2f, 0.02f, region.Radius * 2f);
                fillDisc.GetComponent<Renderer>().sharedMaterial.color = new Color(color.r, color.g, color.b, color.a * 0.22f);
            }
        }

        static void SampleRect(HitRegion region, List<Vector3> output)
        {
            var forward = region.Forward;
            forward.y = 0f;
            forward.Normalize();
            var right = Vector3.Cross(Vector3.up, forward);
            var origin = region.Origin;
            var half = region.HalfWidth;
            var end = origin + forward * region.Length;
            output.Add(origin + right * half);
            output.Add(end + right * half);
            output.Add(end - right * half);
            output.Add(origin - right * half);
        }

        static void SampleCircle(Vector3 center, float radius, List<Vector3> output, int segments)
        {
            SampleArc(center, radius, 360f, Vector3.forward, output, segments);
        }

        static void SampleArc(
            Vector3 center,
            float radius,
            float arcAngle,
            Vector3 forward,
            List<Vector3> output,
            int segments)
        {
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }
            else
            {
                forward.Normalize();
            }

            var half = arcAngle * 0.5f;
            for (var i = 0; i <= segments; i++)
            {
                var t = Mathf.Lerp(-half, half, i / (float)segments);
                var dir = Quaternion.Euler(0f, t, 0f) * forward;
                output.Add(center + dir * radius);
            }
        }

        LineRenderer CreateLineRenderer(string childName)
        {
            var child = new GameObject(childName);
            child.transform.SetParent(transform, false);
            var line = child.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.widthMultiplier = lineWidth;
            line.numCapVertices = 4;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.positionCount = 64;
            return line;
        }

        GameObject CreateFillDisc()
        {
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "RangeFillDisc";
            disc.transform.SetParent(transform, false);
            Destroy(disc.GetComponent<Collider>());
            disc.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Sprites/Default"))
            {
                color = new Color(0.35f, 0.7f, 1f, 0.22f)
            };
            disc.SetActive(false);
            return disc;
        }

        void SetActive(bool active)
        {
            if (primaryLine != null)
            {
                primaryLine.enabled = active && primaryLine.positionCount > 0;
            }

            if (secondaryLine != null)
            {
                secondaryLine.enabled = active && secondaryLine.positionCount > 0;
            }

            if (fillDisc != null && !active)
            {
                fillDisc.SetActive(false);
            }
        }
    }
}
