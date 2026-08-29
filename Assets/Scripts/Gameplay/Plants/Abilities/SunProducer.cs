using System.Collections;
using PvZ.Gameplay.Plants.Presentation;
using PvZ.Gameplay.Sun;
using UnityEngine;

namespace PvZ.Gameplay.Plants.Abilities
{
    [DisallowMultipleComponent]
    public sealed class SunProducer : MonoBehaviour
    {
        private static readonly string[] ProductionAnchorPaths =
        {
            "component/anchors/Sun_Anchor",
            "component/basic/head/SunShroom_head/Sun_Anchor"
        };

        private static readonly int ProduceTrigger = Animator.StringToHash("produce");

        [Header("Production")]
        [SerializeField] private Transform productionAnchor;
        [SerializeField, Min(0f)] private float initialDelaySeconds = 10f;
        [SerializeField, Min(0f)] private float initialDelayVariationSeconds = 2f;
        [SerializeField, Min(0.1f)] private float intervalSeconds = 24f;
        [SerializeField, Min(0f)] private float intervalVariationSeconds = 3f;
        [SerializeField] private SunType fallbackSunType = SunType.Small;

        private Animator _animator;
        private Coroutine _productionRoutine;
        private Coroutine _fallbackProductionRoutine;
        private SunProductionGlow _fallbackGlow;

        public bool IsProducing => _productionRoutine != null;

        public void StartProducing()
        {
            if (IsProducing)
            {
                return;
            }

            ResolveReferences();
            if (SunSpawner.Instance == null)
            {
                throw new MissingReferenceException(
                    $"{nameof(SunProducer)} requires an active {nameof(SunSpawner)} in the scene.");
            }

            enabled = true;
            _productionRoutine = StartCoroutine(ProductionLoop());
        }

        public void StopProducing()
        {
            if (_productionRoutine != null)
            {
                StopCoroutine(_productionRoutine);
                _productionRoutine = null;
            }

            if (_animator != null)
            {
                _animator.ResetTrigger(ProduceTrigger);
            }

            StopFallbackProductionAnimation();
        }

        // Called by the ProduceSun animation event on sun.anim.
        public void ProduceSun(int sunTypeNumber)
        {
            if (!IsProducing)
            {
                return;
            }

            var sunSpawner = SunSpawner.Instance;
            if (sunSpawner == null)
            {
                Debug.LogError($"Cannot produce sun because {nameof(SunSpawner)} is unavailable.", this);
                StopProducing();
                return;
            }

            SpawnSun(sunSpawner, SunTypeCatalog.FromAnimationEvent(sunTypeNumber));
        }

        private IEnumerator ProductionLoop()
        {
            var initialDelay = GetRandomizedDelay(
                initialDelaySeconds,
                initialDelayVariationSeconds,
                0f);
            if (initialDelay > 0f)
            {
                yield return new WaitForSeconds(initialDelay);
            }

            while (true)
            {
                RequestProduction();
                var interval = GetRandomizedDelay(
                    intervalSeconds,
                    intervalVariationSeconds,
                    0.1f);
                yield return new WaitForSeconds(interval);
            }
        }

        private static float GetRandomizedDelay(float baseSeconds, float variationSeconds, float minimumSeconds)
        {
            var variation = Mathf.Max(0f, variationSeconds);
            return Mathf.Max(
                minimumSeconds,
                baseSeconds + Random.Range(-variation, variation));
        }

        private void RequestProduction()
        {
            if (CanUseProductionAnimation())
            {
                _animator.SetTrigger(ProduceTrigger);
                return;
            }

            if (_fallbackProductionRoutine == null)
            {
                _fallbackProductionRoutine = StartCoroutine(FallbackProductionAnimation());
            }
        }

        private IEnumerator FallbackProductionAnimation()
        {
            _fallbackGlow = SunProductionGlow.Capture(transform);

            const float glowDuration = 2f;
            const float peakTime = glowDuration * 0.5f;
            var elapsed = 0f;
            var produced = false;

            while (elapsed < glowDuration)
            {
                var glow = elapsed <= peakTime
                    ? Mathf.SmoothStep(1f, 2f, elapsed / peakTime)
                    : Mathf.SmoothStep(2f, 1f, (elapsed - peakTime) / peakTime);
                _fallbackGlow.Apply(glow);

                if (!produced && elapsed >= peakTime)
                {
                    SpawnSun(SunSpawner.Instance, fallbackSunType);
                    produced = true;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!produced && IsProducing)
            {
                SpawnSun(SunSpawner.Instance, fallbackSunType);
            }

            RestoreFallbackBrightness();
            _fallbackProductionRoutine = null;
        }

        private void StopFallbackProductionAnimation()
        {
            if (_fallbackProductionRoutine != null)
            {
                StopCoroutine(_fallbackProductionRoutine);
                _fallbackProductionRoutine = null;
            }

            RestoreFallbackBrightness();
        }

        private void RestoreFallbackBrightness()
        {
            _fallbackGlow?.Restore();
            _fallbackGlow = null;
        }

        private void SpawnSun(SunSpawner sunSpawner, SunType type)
        {
            if (sunSpawner == null)
            {
                Debug.LogError($"Cannot produce sun because {nameof(SunSpawner)} is unavailable.", this);
                StopProducing();
                return;
            }

            sunSpawner.SpawnSun(productionAnchor.position, type);
        }

        private bool CanUseProductionAnimation()
        {
            if (_animator == null || !_animator.isActiveAndEnabled) return false;

            foreach (var parameter in _animator.parameters)
            {
                if (parameter.nameHash == ProduceTrigger &&
                    parameter.type == AnimatorControllerParameterType.Trigger)
                {
                    return true;
                }
            }

            return false;
        }

        private void ResolveReferences()
        {
            if (productionAnchor == null)
            {
                foreach (var path in ProductionAnchorPaths)
                {
                    productionAnchor = transform.Find(path);
                    if (productionAnchor != null) break;
                }
            }

            if (productionAnchor == null)
            {
                foreach (var child in GetComponentsInChildren<Transform>(true))
                {
                    if (child.name != "Sun_Anchor") continue;

                    productionAnchor = child;
                    break;
                }
            }

            if (productionAnchor == null)
            {
                productionAnchor = transform;
                Debug.LogWarning(
                    $"{name} has no production anchor; sun will spawn at the plant origin.",
                    this);
            }

            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
            }
        }

        private void OnDisable()
        {
            StopProducing();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            initialDelaySeconds = Mathf.Max(0f, initialDelaySeconds);
            initialDelayVariationSeconds = Mathf.Max(0f, initialDelayVariationSeconds);
            intervalSeconds = Mathf.Max(0.1f, intervalSeconds);
            intervalVariationSeconds = Mathf.Max(0f, intervalVariationSeconds);

            if (productionAnchor == null)
            {
                foreach (var path in ProductionAnchorPaths)
                {
                    productionAnchor = transform.Find(path);
                    if (productionAnchor != null) break;
                }
            }
        }
#endif
    }
}
