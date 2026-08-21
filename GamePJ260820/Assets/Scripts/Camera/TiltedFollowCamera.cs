using UnityEngine;

namespace GamePJ.CameraSystems
{
    [DisallowMultipleComponent]
    public sealed class TiltedFollowCamera : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] Vector3 offset = new(0f, 6f, -5f);
        [SerializeField] float followSmooth = 8f;
        [SerializeField] float lookAhead = 0.35f;

        void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            var desiredPosition = target.position + offset;
            transform.position = Vector3.Lerp(
                transform.position,
                desiredPosition,
                1f - Mathf.Exp(-followSmooth * Time.deltaTime));

            var lookTarget = target.position + target.forward * lookAhead;
            transform.LookAt(lookTarget);
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        public void Configure(Transform newTarget, Vector3 newOffset)
        {
            target = newTarget;
            offset = newOffset;
        }
    }
}
