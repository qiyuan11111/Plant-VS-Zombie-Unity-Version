using System;
using System.Collections;
using System.Collections.Generic;
using Script.Manager;
using Script.Model;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Prefab.Object.Sun.Script
{
    public class Sun : global::Script.Model.Object, IToField
    {
        private static readonly int DisappearProperty = Animator.StringToHash("disappear");

        // // 是否可以被点击
        // public bool canBeClick;
        //
        // public void SetCanBeClick(bool click) => canBeClick = click;
        // public bool GetCanBeClick() => canBeClick;

        private new void Awake()
        {
            base.Awake();
        }

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
            var disappear = Animator.GetBool(DisappearProperty);
            if (disappear) return;
            Animator.SetBool(DisappearProperty, true);
        }

        public IEnumerator StartDisappear()
        {
            yield return WaitForSecondsPool.Create(100f);
            Disappear();
        }

        // 阳光跳动
        // public IEnumerator StartJumpDown()
        // {
        //     float T = 0.4f, offsetT = 0.02f;
        //     var offsetx = Random.Range(-28f, 28f);
        //     var offsety = Random.Range(-23f, -33f);
        //
        //     var speed = 400f;
        //     var a = 2 * (offsety - speed * T) / (T * T);
        //     var t = T;
        //
        //     var scale = transform.localScale;
        //     var scalex = scale.x;
        //     var scaley = scale.y;
        //
        //     while (t > 0)
        //     {
        //         var localScale = transform.localScale;
        //         localScale = new Vector3(localScale.x + scalex * offsetT / T, localScale.y + scaley * offsetT / T, 1f);
        //         transform.localScale = localScale;
        //         transform.localPosition += new Vector3(offsetT * offsetx / T, speed * offsetT + 0.5f * a * offsetT * offsetT, 0);
        //         speed += a * offsetT;
        //         t -= offsetT;
        //         yield return WaitForSecondsPool.Create(offsetT);
        //     }
        // }

        public IEnumerator StartJumpDown()
        {
            const float T = 0.5f;
            const float offsetT = 0.02f;
            var offsetX = Random.Range(-28f, 28f);
            var offsetY = Random.Range(-23f, -33f);

            var targetT = TimeManager.Instance.globalTime + T;
            var startT = TimeManager.Instance.globalTime;
            var startPosition = Transform.localPosition;

            var t = T;
            var scale = transform.localScale;

            var a = 2f * (offsetY - 400f * T) / T / T;
            var v0 = offsetX / T;
            while (t < targetT)
            {
                transform.localPosition = startPosition + new Vector3(v0 * (t - startT),
                    400f * (t - startT) + (t - startT) * (t - startT) * a / 2f, 10f);
                transform.localScale =
                    new Vector3(scale.x * (1f + (t - startT) / T), scale.y * (1f + (t - startT) / T), 1f);
                t = TimeManager.Instance.globalTime;
                yield return WaitForSecondsPool.Create(offsetT);
            }

            transform.localPosition = startPosition + new Vector3(v0 * T, 400f * T + T * T * a / 2f, 10f);
            transform.localScale = new Vector3(scale.x * (1f + T / T), scale.y * (1f + T / T), 1f);
        }

        // IEnumerator StartClickSun(Vector3 position)
        // {
        //     const float T = 0.5f;
        //     var offsetT = 0.004f;
        //     var scale = transform.localScale / (2 * T);
        //     var startTime = TimeManager.Instance.globalTime;
        //     while ((position - transform.localPosition).sqrMagnitude > 0.1f)
        //     {
        //         var localPosition = transform.localPosition;
        //
        //         var speed = 3 * (position - localPosition) / T;
        //
        //         // localPosition += speed * offsetT;
        //         transform.localPosition = localPosition;
        //
        //         if ((position - localPosition).sqrMagnitude < 25f)
        //         {
        //             transform.localScale -= scale * offsetT;
        //             Disappear();
        //         }
        //         yield return new WaitForSeconds(offsetT);
        //     }
        //
        //     yield return null;
        // }

        public IEnumerator StartClickSun(Vector3 position)
        {
            const float T = 0.5f;
            var offsetT = Math.Max(0.01f, Time.deltaTime);
            // var scale = transform.localScale / (2 * T);
            var startTime = TimeManager.Instance.globalTime;
            var startPosition = transform.localPosition;
            var startScale = transform.localScale;

            var triggerTime = Math.Log(Vector3.Distance(startPosition, position) / 5f) / 6f;
            while ((position - transform.localPosition).sqrMagnitude > 0.1f)
            {
                var nowTime = TimeManager.Instance.globalTime - startTime;
                var k = Math.Pow(Math.E, -6 * nowTime);
                var nowPosition = (startPosition - position) * (float)k;
                transform.localPosition = position + nowPosition;

                if (nowTime > triggerTime)
                {
                    transform.localScale = startScale * (float)(1f - (nowTime - triggerTime));
                    Disappear();
                }

                yield return new WaitForSeconds(offsetT);
            }
        }


        // public IEnumerator StartClickSun(Vector3 position)
        // {
        //     const float T = 0.5f;
        //     const float offsetT = 0.004f;
        //     const float speedFactor = 3f / T; // 6f
        //     const float positionUpdateFactor = speedFactor * offsetT; // 0.024f
        //     const float shrinkFactor = 1 - positionUpdateFactor; // 0.976f
        //     const float shrinkFactorSquared = shrinkFactor * shrinkFactor; // 0.952576f
        //     
        //     var scale = Transform.localScale / (2 * T);
        //     var scaleOffset = scale * offsetT;
        //
        //     while ((position - Transform.localPosition).sqrMagnitude > 0.1f)
        //     {
        //         var currentLocalPos = Transform.localPosition;
        //         var diff = position - currentLocalPos;
        //         var diffSqrMag = diff.sqrMagnitude;
        //
        //         // 计算新位置
        //         var positionDelta = diff * positionUpdateFactor;
        //         var newLocalPos = currentLocalPos + positionDelta;
        //         Transform.localPosition = newLocalPos;
        //
        //         // 计算新平方距离
        //         var newSqrMag = diffSqrMag * shrinkFactorSquared;
        //         if (newSqrMag < 25f)
        //         {
        //             Transform.localScale -= scaleOffset;
        //             Disappear();
        //         }
        //
        //         yield return WaitForSecondsPool.Create(offsetT);
        //     }
        // }
        //
        // public IEnumerator StartClickSun(Vector3 targetPosition)
        // {
        //     const float T = 0.5f;
        //     const float minSqrDistance = 0.1f;
        //     const float disappearSqrDistance = 25f;
        //     const float speedFactor = 3f / T;
        //
        //     Vector3 initialScale = Transform.localScale;
        //     Vector3 startPosition = Transform.localPosition;
        //
        //     // 计算总缩放变化量（根据原始逻辑，最终会缩小一半）
        //     Vector3 totalScaleChange = initialScale * 0.5f;
        //
        //     float startTime = TimeManager.Instance.globalTime;
        //     float lastUpdateTime = startTime;
        //
        //     while (true)
        //     {
        //         float currentTime = TimeManager.Instance.globalTime;
        //         float deltaTime = currentTime - lastUpdateTime;
        //         lastUpdateTime = currentTime;
        //
        //         // 计算当前目标方向
        //         Vector3 currentPosition = Transform.localPosition;
        //         Vector3 toTarget = targetPosition - currentPosition;
        //         float sqrMag = toTarget.sqrMagnitude;
        //
        //         // 检查终止条件
        //         if (sqrMag <= minSqrDistance)
        //         {
        //             Transform.localPosition = targetPosition;
        //             break;
        //         }
        //
        //         // 基于实际时间差计算移动比例
        //         float moveRatio = speedFactor * deltaTime;
        //
        //         // 更新位置（保持原始指数衰减逻辑）
        //         Transform.localPosition = Vector3.Lerp(currentPosition, targetPosition, moveRatio);
        //
        //         // 计算已用时间比例
        //         float elapsed = currentTime - startTime;
        //         float progress = Mathf.Clamp01(elapsed / T);
        //
        //         // 更新缩放（线性缩小到原始尺寸的一半）
        //         Transform.localScale = Vector3.Lerp(initialScale, initialScale * 0.5f, progress);
        //
        //         // 处理消失条件
        //         if (sqrMag < disappearSqrDistance)
        //         {
        //             Disappear();
        //             break;
        //         }
        //
        //         // 使用Unity协程等待，但实际逻辑由TimeManager控制
        //         yield return null;
        //     }
        // }

        public T ToField<T>(Dictionary<string, object> param = null) where T : OnFieldEntity
        {
            var onFieldSun = PrePareToField<OnFieldSun>();
            if (param != null)
            {
                var type = param["sunType"] is SunManager.SunType ? (SunManager.SunType)param["sunType"] : SunManager.SunType.Small;
                var localPosition = param["LocalPosition"] as Vector3? ?? default;

                var scale = SunManager.Instance.GetSunScaleBySunType(type);
                var localScale = new Vector3(scale, scale, 1f);

                onFieldSun.SetSunType(type)
                    .SetLocalPosition(localPosition)
                    .SetLocalScale(localScale);
            }

            onFieldSun.SetSortingLayer("sun");
            onFieldSun.SetJumpMode();

            return onFieldSun as T;
        }

        public override void AfterCreate(Dictionary<string, object> param)
        {
        }

        public override OnFieldEntity ToField(Dictionary<string, object> param = null)
        {
            return ToField<OnFieldSun>(param);
        }
    }
}