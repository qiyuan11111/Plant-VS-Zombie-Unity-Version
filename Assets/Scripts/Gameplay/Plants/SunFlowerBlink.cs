using System.Collections;
using Script;
using UnityEngine;

namespace Prefab.Plant.SunFlower.Script
{
    [DisallowMultipleComponent]
    public sealed class SunFlowerBlink : MonoBehaviour
    {
        private static readonly int BlinkTrigger = Animator.StringToHash("blink");

        [SerializeField] private Animator animator;
        [SerializeField] private Sprite blink1Sprite;
        [SerializeField] private Sprite blink2Sprite;
        [SerializeField, Min(0.1f)] private float minimumIntervalSeconds = 2.5f;
        [SerializeField, Min(0.1f)] private float maximumIntervalSeconds = 5.5f;

        private Coroutine _blinkRoutine;
        private GameObject _blink1;
        private GameObject _blink2;

        public bool IsBlinking => _blinkRoutine != null;

        public void StartBlinking()
        {
            if (IsBlinking) return;

            ResolveAnimator();
            ResolveVisuals();
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

            SetBlinkFrame(false, false);
        }

        private IEnumerator BlinkLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(
                    Random.Range(minimumIntervalSeconds, maximumIntervalSeconds));

                if (CanUseBlinkAnimation())
                {
                    animator.SetTrigger(BlinkTrigger);
                }
                else
                {
                    yield return PlayFallbackBlink();
                }
            }
        }

        private IEnumerator PlayFallbackBlink()
        {
            const float frameDuration = 1f / 12f;
            SetBlinkFrame(false, false);
            yield return new WaitForSeconds(frameDuration);
            SetBlinkFrame(false, true);
            yield return new WaitForSeconds(frameDuration);
            SetBlinkFrame(true, false);
            yield return new WaitForSeconds(frameDuration);
            SetBlinkFrame(false, true);
            yield return new WaitForSeconds(frameDuration);
            SetBlinkFrame(false, false);
        }

        private bool CanUseBlinkAnimation()
        {
            if (animator == null || !animator.isActiveAndEnabled) return false;

            foreach (var parameter in animator.parameters)
            {
                if (parameter.nameHash == BlinkTrigger &&
                    parameter.type == AnimatorControllerParameterType.Trigger)
                {
                    return true;
                }
            }

            return false;
        }

        private void ResolveVisuals()
        {
            const string headPath = "component/basic/head/SunFlower_head";
            var head = transform.Find(headPath) ??
                       transform.Find("component/basic/head/face/SunFlower_head");
            if (head == null) return;

            _blink1 = head.Find("blink/SunFlower_blink1")?.gameObject ??
                      head.Find("SunFlower_blink1")?.gameObject;
            _blink2 = head.Find("blink/SunFlower_blink2")?.gameObject ??
                      head.Find("SunFlower_blink2")?.gameObject;
            if (_blink1 != null && _blink2 != null) return;
            if (blink1Sprite == null || blink2Sprite == null) return;

            var headTransform = head.GetComponent<SpriteTransform>();
            if (headTransform != null)
            {
                headTransform.providesChildSpritePosition = true;
                headTransform.childSpritePosition = headTransform.position;
            }

            var blinkRoot = head.Find("blink");
            if (blinkRoot == null)
            {
                var blinkRootObject = new GameObject("blink");
                blinkRoot = blinkRootObject.transform;
                blinkRoot.SetParent(head, false);
            }

            var templateRenderer = head.GetComponent<SpriteRenderer>();
            var blinkPosition = new Vector2(39.1f, 31.5f);
            _blink1 = CreateBlinkVisual(
                blinkRoot,
                "SunFlower_blink1",
                blink1Sprite,
                blinkPosition,
                templateRenderer);
            _blink2 = CreateBlinkVisual(
                blinkRoot,
                "SunFlower_blink2",
                blink2Sprite,
                blinkPosition,
                templateRenderer);
        }

        private GameObject CreateBlinkVisual(
            Transform parent,
            string objectName,
            Sprite sprite,
            Vector2 animationPosition,
            SpriteRenderer templateRenderer)
        {
            var visual = new GameObject(objectName)
            {
                layer = gameObject.layer
            };
            visual.transform.SetParent(parent, false);

            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 10;
            if (templateRenderer != null)
            {
                renderer.sharedMaterial = templateRenderer.sharedMaterial;
                renderer.sortingLayerID = templateRenderer.sortingLayerID;
            }

            var spriteTransform = visual.AddComponent<SpriteTransform>();
            spriteTransform.position = animationPosition;
            spriteTransform.scale = new Vector2(100f, 100f);
            spriteTransform.skew = Vector2.zero;
            spriteTransform.brightness = 1f;
            spriteTransform.alpha = 1f;
            spriteTransform.alphaCoef = 1f;
            spriteTransform.updatePosition = true;
            spriteTransform.Apply();

            visual.SetActive(false);
            return visual;
        }

        private void SetBlinkFrame(bool showBlink1, bool showBlink2)
        {
            if (_blink1 != null) _blink1.SetActive(showBlink1);
            if (_blink2 != null) _blink2.SetActive(showBlink2);
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
