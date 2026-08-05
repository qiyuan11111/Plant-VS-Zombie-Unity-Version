using System.Collections;
using UnityEngine;

namespace PvZ.Gameplay.Plants.Abilities
{
    /// <summary>
    /// Periodically requests the shared "blink" animation used by plant visuals.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Blink : MonoBehaviour
    {
        private static readonly int BlinkTrigger = Animator.StringToHash("blink");

        [SerializeField] private Animator animator;
        [SerializeField, Min(0.1f)] private float minimumIntervalSeconds = 2.5f;
        [SerializeField, Min(0.1f)] private float maximumIntervalSeconds = 5.5f;

        private Coroutine _blinkRoutine;

        public bool IsBlinking => _blinkRoutine != null;

        public void StartBlinking()
        {
            if (IsBlinking) return;

            ResolveAnimator();
            enabled = true;
            _blinkRoutine = StartCoroutine(BlinkLoop());
        }

        public void StopBlinking()
        {
            if (_blinkRoutine != null)
            {
                StopCoroutine(_blinkRoutine);
                _blinkRoutine = null;
            }

            if (animator != null)
            {
                animator.ResetTrigger(BlinkTrigger);
            }
        }

        private IEnumerator BlinkLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(
                    Random.Range(minimumIntervalSeconds, maximumIntervalSeconds));

                if (animator.isActiveAndEnabled)
                {
                    animator.SetTrigger(BlinkTrigger);
                }
            }
        }

        private void ResolveAnimator()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (animator == null)
            {
                throw new MissingComponentException(
                    $"{name} requires an {nameof(Animator)} for blink animation.");
            }
        }

        private void OnDisable()
        {
            StopBlinking();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            minimumIntervalSeconds = Mathf.Max(0.1f, minimumIntervalSeconds);
            maximumIntervalSeconds = Mathf.Max(minimumIntervalSeconds, maximumIntervalSeconds);

            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
        }
#endif
    }
}
