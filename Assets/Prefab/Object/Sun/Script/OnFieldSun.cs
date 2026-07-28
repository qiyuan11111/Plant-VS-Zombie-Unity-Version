using System;
using Script.Manager;
using Script.Model;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Prefab.Object.Sun.Script
{
    public class OnFieldSun : OnFieldObject, IPointerClickHandler
    {
        public SunManager.SunType sunType;

        private string _jumpDownTask;

        public bool IsCanBeClick()
        {
            return string.IsNullOrEmpty(_jumpDownTask) || !LightCoroutineManager.Instance.Exist(_jumpDownTask);
        }
        //
        // public void ShiftClickStatus(bool canBeClick)
        // {
        //     GetEntity<Sun>().canBeClick = canBeClick;
        // }

        public OnFieldSun SetSunType(SunManager.SunType type)
        {
            sunType = type;
            return this;
        }
 
        public OnFieldSun SetJumpMode()
        {
            Transform.localScale = Vector3.Scale(Transform.localScale, new Vector3(0.5f, 0.5f, 1f));
            _jumpDownTask = LightCoroutineManager.Instance.StartLightCoroutine("JumpDown", GetEntity<Sun>().StartJumpDown());
            LightCoroutineManager.Instance.StartLightCoroutine("Disappear", GetEntity<Sun>().StartDisappear());
            return this;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!IsCanBeClick()) return;

            SunManager.Instance.AddCurrentSunLight(SunManager.Instance.GetSunLightBySunType(sunType));
            SoundManager.Instance.PlayEffect(GameSound.SoundType.Points);

            // 启动协程
            LightCoroutineManager.Instance.StartLightCoroutine("ClickSun",
                GetEntity<Sun>().StartClickSun(SunManager.Instance.sunPointPosition));
        }

        private void Start()
        {
            // for (int i = 1; i <= 10; i++)
            // {
            //     
            // }
        }
    }
}