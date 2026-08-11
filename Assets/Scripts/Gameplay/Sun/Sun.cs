using System.Collections;
using System.Collections.Generic;
using PvZ.Audio;
using PvZ.Core.Entities;
using UnityEngine;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;

namespace PvZ.Gameplay.Sun
{
    public class Sun : WorldObject, IPointerClickHandler
    {
        private const float LifetimeSeconds = 100f;
        private const float JumpDuration = 0.5f;
        private const float InitialJumpSpeed = 400f;
        private const float CollectSpeed = 6f;
        private const float CollectDisappearDistance = 5f;
        private const float CollectFinishSqrDistance = 0.1f;

        private static readonly int DisappearProperty = Animator.StringToHash("disappear");

        private SunManager.SunType _sunType;
        private Coroutine _jumpCoroutine;
        private Coroutine _lifetimeCoroutine;
        private Coroutine _collectCoroutine;
        private bool _isCollected;

        public override string GetChineseName()
        {
            return "阳光";
        }

        public override string GetEnglishName()
        {
            return "Sun";
        }

        private void Disappear()
        {
            if (!Animator.GetBool(DisappearProperty))
            {
                Animator.SetBool(DisappearProperty, true);
            }
        }

        private IEnumerator DisappearAfterLifetime()
        {
            yield return new WaitForSeconds(LifetimeSeconds);
            _lifetimeCoroutine = null;
            Disappear();
        }

        private IEnumerator JumpDown()
        {
            var offsetX = Random.Range(-28f, 28f);
            var offsetY = Random.Range(-23f, -33f);
            var startPosition = Transform.localPosition;
            var startScale = Transform.localScale;
            var acceleration = 2f * (offsetY - InitialJumpSpeed * JumpDuration) /
                               (JumpDuration * JumpDuration);
            var horizontalSpeed = offsetX / JumpDuration;
            var elapsed = 0f;

            while (elapsed < JumpDuration)
            {
                ApplyJumpFrame(Mathf.Min(elapsed, JumpDuration), startPosition, startScale,
                    horizontalSpeed, acceleration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            ApplyJumpFrame(JumpDuration, startPosition, startScale, horizontalSpeed, acceleration);
            _jumpCoroutine = null;
        }

        private void ApplyJumpFrame(float elapsed, Vector3 startPosition, Vector3 startScale,
            float horizontalSpeed, float acceleration)
        {
            Transform.localPosition = startPosition + new Vector3(
                horizontalSpeed * elapsed,
                InitialJumpSpeed * elapsed + elapsed * elapsed * acceleration * 0.5f,
                10f);

            var scaleMultiplier = 1f + elapsed / JumpDuration;
            Transform.localScale = new Vector3(
                startScale.x * scaleMultiplier,
                startScale.y * scaleMultiplier,
                1f);
        }

        private IEnumerator CollectTo(Vector3 targetPosition)
        {
            var startPosition = Transform.localPosition;
            var startScale = Transform.localScale;
            var startTime = Time.time;
            var initialDistance = Vector3.Distance(startPosition, targetPosition);
            var shrinkStartTime = initialDistance > 0f
                ? Mathf.Log(initialDistance / CollectDisappearDistance) / CollectSpeed
                : 0f;

            while ((targetPosition - Transform.localPosition).sqrMagnitude > CollectFinishSqrDistance)
            {
                var elapsed = Time.time - startTime;
                var distanceMultiplier = Mathf.Exp(-CollectSpeed * elapsed);
                Transform.localPosition = targetPosition + (startPosition - targetPosition) * distanceMultiplier;

                if (elapsed > shrinkStartTime)
                {
                    Transform.localScale = startScale * Mathf.Max(0f, 1f - (elapsed - shrinkStartTime));
                    Disappear();
                }

                yield return null;
            }

            Transform.localPosition = targetPosition;
            _collectCoroutine = null;
            Disappear();
        }

        public Sun SetSunType(SunManager.SunType type)
        {
            _sunType = type;
            return this;
        }

        public Sun Initialize(SunManager.SunType type, Vector3 localPosition)
        {
            ResetRuntimeState();
            _isCollected = false;
            SetComponentState(true);

            var scale = SunManager.Instance.GetSunScaleBySunType(type);
            SetSunType(type);
            SetLocalPosition(localPosition);
            SetLocalScale(new Vector3(scale, scale, 1f));
            SetSortingLayer("sun");
            StartSunLifecycle();
            return this;
        }

        private bool CanCollect()
        {
            return !_isCollected && _jumpCoroutine == null;
        }

        private Sun StartSunLifecycle()
        {
            StopRunningCoroutines();
            Transform.localScale = Vector3.Scale(Transform.localScale, new Vector3(0.5f, 0.5f, 1f));
            _jumpCoroutine = StartCoroutine(JumpDown());
            _lifetimeCoroutine = StartCoroutine(DisappearAfterLifetime());
            return this;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData == null ||
                eventData.button != PointerEventData.InputButton.Left ||
                !CanCollect())
            {
                return;
            }

            _isCollected = true;
            SetComponentState(false);
            StopRoutine(ref _lifetimeCoroutine);

            SunManager.Instance.AddCurrentSunLight(SunManager.Instance.GetSunLightBySunType(_sunType));
            SoundManager.Instance.PlayEffect(GameSound.SoundType.Points);
            _collectCoroutine = StartCoroutine(CollectTo(SunManager.Instance.sunPointPosition));
        }

        private void StopRunningCoroutines()
        {
            StopRoutine(ref _jumpCoroutine);
            StopRoutine(ref _lifetimeCoroutine);
            StopRoutine(ref _collectCoroutine);
        }

        private void StopRoutine(ref Coroutine coroutine)
        {
            if (coroutine == null) return;

            StopCoroutine(coroutine);
            coroutine = null;
        }

    }
}
