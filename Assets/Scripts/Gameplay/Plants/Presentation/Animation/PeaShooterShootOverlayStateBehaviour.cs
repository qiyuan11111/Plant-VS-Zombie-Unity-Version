using UnityEngine;

namespace PvZ.Gameplay.Plants.Presentation.Animation
{
    /// <summary>
    /// Fades the one-shot head overlay in and out while the lower head_idle
    /// layer keeps advancing. No body or idle animation time is modified.
    /// </summary>
    public sealed class PeaShooterShootOverlayStateBehaviour : StateMachineBehaviour
    {
        private static readonly int ShootTrigger = Animator.StringToHash("shoot");
        private static readonly int OverlayIdleState =
            Animator.StringToHash("PeaShooterSingle_head_shoot.shoot_idle");

        private const float ShootClipLengthSeconds = 2f;
        private const float ShootStateSpeed = 2.8f;
        private const float FadeDurationSeconds = 0.2f;
        private const float FadeNormalizedDuration =
            FadeDurationSeconds / (ShootClipLengthSeconds / ShootStateSpeed);

        private bool _returnedToIdle;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            _returnedToIdle = false;
            animator.ResetTrigger(ShootTrigger);
            animator.SetLayerWeight(layerIndex, 0f);
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (_returnedToIdle) return;

            float normalizedTime = stateInfo.normalizedTime;
            float weight;
            if (normalizedTime < FadeNormalizedDuration)
            {
                weight = normalizedTime / FadeNormalizedDuration;
            }
            else if (normalizedTime <= 1f)
            {
                weight = 1f;
            }
            else
            {
                weight = 1f - (normalizedTime - 1f) / FadeNormalizedDuration;
            }

            if (weight > 0f)
            {
                animator.SetLayerWeight(layerIndex, Mathf.Clamp01(weight));
                return;
            }

            _returnedToIdle = true;
            animator.SetLayerWeight(layerIndex, 0f);
            animator.Play(OverlayIdleState, layerIndex, 0f);
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            animator.ResetTrigger(ShootTrigger);
            animator.SetLayerWeight(layerIndex, 0f);
        }
    }
}
