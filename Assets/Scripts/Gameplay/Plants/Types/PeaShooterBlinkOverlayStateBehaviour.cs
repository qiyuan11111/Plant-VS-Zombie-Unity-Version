using UnityEngine;

namespace PvZ.Gameplay.Plants.Types
{
    /// <summary>
    /// Enables the blink overlay only while its one-shot clip is playing, so
    /// the continuously evaluated body and head idle layers are never held by
    /// an otherwise empty override layer.
    /// </summary>
    public sealed class PeaShooterBlinkOverlayStateBehaviour : StateMachineBehaviour
    {
        private static readonly int BlinkTrigger = Animator.StringToHash("blink");

        public override void OnStateEnter(
            Animator animator,
            AnimatorStateInfo stateInfo,
            int layerIndex)
        {
            animator.ResetTrigger(BlinkTrigger);
            animator.SetLayerWeight(layerIndex, 1f);
        }

        public override void OnStateExit(
            Animator animator,
            AnimatorStateInfo stateInfo,
            int layerIndex)
        {
            animator.ResetTrigger(BlinkTrigger);
            animator.SetLayerWeight(layerIndex, 0f);
        }
    }
}
