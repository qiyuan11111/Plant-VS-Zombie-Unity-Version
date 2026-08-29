using UnityEngine;

namespace PvZ.Gameplay.Sun.Presentation.Animation
{
    public sealed class SunDisappearStateBehaviour : StateMachineBehaviour
    {
        public override void OnStateExit(
            Animator animator,
            AnimatorStateInfo stateInfo,
            int layerIndex)
        {
            Destroy(animator.gameObject);
        }
    }
}
