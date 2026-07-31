using System.Collections;
using Script.Manager;
using UnityEngine;

namespace Prefab.Plant.SunShroom.Script
{
    [DisallowMultipleComponent]
    public sealed class SunProducer : MonoBehaviour
    {
        private const string ProductionAnchorPath =
            "component/basic/head/SunShroom_head/Sun_Anchor";

        private static readonly int ProduceTrigger = Animator.StringToHash("produce");

        [Header("Production")]
        [SerializeField] private Transform productionAnchor;
        [SerializeField, Min(0f)] private float initialDelaySeconds = 10f;
        [SerializeField, Min(0f)] private float initialDelayVariationSeconds = 2f;
        [SerializeField, Min(0.1f)] private float intervalSeconds = 24f;
        [SerializeField, Min(0f)] private float intervalVariationSeconds = 3f;
        [SerializeField] private SunManager.SunType fallbackSunType = SunManager.SunType.Small;

        private Animator _animator;
        private Coroutine _productionRoutine;

        public bool IsProducing => _productionRoutine != null;

        public void StartProducing()
        {
            if (IsProducing)
            {
                return;
            }

            ResolveReferences();
            if (SunManager.Instance == null)
            {
                throw new MissingReferenceException(
                    $"{nameof(SunProducer)} requires an active {nameof(SunManager)} in the scene.");
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
        }

        // Called by the ProduceSun animation event on sun.anim.
        public void ProduceSun(int sunTypeNumber)
        {
            if (!IsProducing)
            {
                return;
            }

            var sunManager = SunManager.Instance;
            if (sunManager == null)
            {
                Debug.LogError($"Cannot produce sun because {nameof(SunManager)} is unavailable.", this);
                StopProducing();
                return;
            }

            SpawnSun(sunManager, sunManager.GetSunTypeByTypeNum(sunTypeNumber));
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
            if (_animator != null && _animator.isActiveAndEnabled)
            {
                _animator.SetTrigger(ProduceTrigger);
                return;
            }

            SpawnSun(SunManager.Instance, fallbackSunType);
        }

        private void SpawnSun(SunManager sunManager, SunManager.SunType type)
        {
            if (sunManager == null)
            {
                Debug.LogError($"Cannot produce sun because {nameof(SunManager)} is unavailable.", this);
                StopProducing();
                return;
            }

            sunManager.SpawnSun(productionAnchor.position, type);
        }

        private void ResolveReferences()
        {
            if (productionAnchor == null)
            {
                productionAnchor = transform.Find(ProductionAnchorPath);
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
                productionAnchor = transform.Find(ProductionAnchorPath);
            }
        }
#endif
    }
}
