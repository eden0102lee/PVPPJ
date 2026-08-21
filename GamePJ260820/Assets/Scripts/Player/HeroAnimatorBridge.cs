using GamePJ.Modules;
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
        [SerializeField] float actionCrossFade = 0.08f;

        bool combatDriven;
        AnimatorOverrideController overrideController;
        AnimationClip attackSlotClip;

        public void SetCombatDriven(bool value) => combatDriven = value;

        public void PlayAttack()
        {
            PlayAction("Attack");
        }

        public void PlayAction(ResolvedAction resolved)
        {
            var definition = ActionAnimationCatalog.Get(resolved?.AnimationId);
            if (TryPlayProtofactorClip(definition))
            {
                return;
            }

            var stateName = resolved?.AnimationStateName;
            if (string.IsNullOrEmpty(stateName))
            {
                stateName = "Attack";
            }

            PlayAction(stateName);
        }

        public void PlayAction(string stateName)
        {
            if (animator == null || string.IsNullOrEmpty(stateName))
            {
                return;
            }

            if (HasState(stateName))
            {
                animator.CrossFadeInFixedTime(stateName, actionCrossFade, 0);
                return;
            }

            animator.SetTrigger(AttackHash);
        }

        bool TryPlayProtofactorClip(ActionAnimationDefinition definition)
        {
            if (definition == null || !definition.HasProtofactorClip)
            {
                return false;
            }

            EnsureOverrideController();
            if (overrideController == null || attackSlotClip == null)
            {
                return false;
            }

            var clip = ActionAnimationClipCache.GetClip(definition);
            if (clip == null)
            {
                return false;
            }

            overrideController[attackSlotClip] = clip;
            animator.ResetTrigger(AttackHash);
            animator.SetTrigger(AttackHash);
            return true;
        }

        void EnsureOverrideController()
        {
            if (overrideController != null || animator == null)
            {
                return;
            }

            var baseController = animator.runtimeAnimatorController;
            if (baseController == null)
            {
                return;
            }

            overrideController = new AnimatorOverrideController(baseController);
            animator.runtimeAnimatorController = overrideController;

            foreach (var pair in overrideController.clips)
            {
                if (pair.originalClip == null)
                {
                    continue;
                }

                if (pair.originalClip.name == "Hero_Attack" ||
                    pair.originalClip.name.Contains("Attack"))
                {
                    attackSlotClip = pair.originalClip;
                    break;
                }
            }

            if (attackSlotClip == null && overrideController.clips.Length > 0)
            {
                attackSlotClip = overrideController.clips[0].originalClip;
            }
        }

        bool HasState(string stateName)
        {
            if (animator.runtimeAnimatorController == null)
            {
                return false;
            }

            var hash = Animator.StringToHash(stateName);
            return animator.HasState(0, hash);
        }

        void Awake()
        {
            motor ??= GetComponent<HeroMotor>();
            input ??= GetComponent<HeroInputController>();
            animator ??= GetComponentInChildren<Animator>();
            EnsureOverrideController();
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
