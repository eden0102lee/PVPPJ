using UnityEngine;

namespace GamePJ.Player
{
    [DisallowMultipleComponent]
    public sealed class HeroAnimatorBridge : MonoBehaviour
    {
        static readonly int SpeedHash = Animator.StringToHash("Speed");
        static readonly int AttackHash = Animator.StringToHash("Attack");

        [SerializeField] HeroMotor motor;
        [SerializeField] HeroInputController input;
        [SerializeField] Animator animator;
        [SerializeField] float walkThreshold = 0.1f;

        bool combatDriven;

        public void SetCombatDriven(bool value) => combatDriven = value;

        public void PlayAttack()
        {
            if (animator != null)
            {
                animator.SetTrigger(AttackHash);
            }
        }

        void Awake()
        {
            motor ??= GetComponent<HeroMotor>();
            input ??= GetComponent<HeroInputController>();
            animator ??= GetComponentInChildren<Animator>();
        }

        void Update()
        {
            if (animator == null)
            {
                return;
            }

            var speed = motor != null ? motor.CurrentSpeed : 0f;
            animator.SetFloat(SpeedHash, speed > walkThreshold ? speed : 0f);

            if (!combatDriven && input != null && input.AttackPressedThisFrame)
            {
                animator.SetTrigger(AttackHash);
            }
        }
    }
}
