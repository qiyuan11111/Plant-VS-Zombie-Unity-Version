using System;
using System.Collections;
using Script.Manager;
using Script.Model;
using Script.Util;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Prefab.Object.SeedCard.Script
{
    public class OnFieldSeedCard : OnFieldObject, IPointerEnterHandler, IPointerExitHandler
    {
        private OnFieldEntity _plant;

        // private bool _dirty = false;

        private float _cdTime;

        public OnFieldSeedCard SetCdTime(float time)
        {
            _cdTime = time;
            return this;
        }

        public float GetCdTime()
        {
            return _cdTime;
        }

        private int _sunPrice;

        public OnFieldSeedCard SetCardSunPrice(int price)
        {
            _sunPrice = price;
            GetEntity<SeedCard>().SetPriceText(price.ToString());
            return this;
        }

        public int GetSunPrice()
        {
            return _sunPrice;
        }

        private float _remainCdTime;

        public OnFieldSeedCard SetRemainCdTime(float time)
        {
            _remainCdTime = time;
            GetEntity<SeedCard>().SetCdMaskFill(Math.Max(time, 0f) / GetCdTime());
            UpdateState();
            return this;
        }

        public float GetRemainCdTime()
        {
            return _remainCdTime;
        }

        private bool _plantable;

        public OnFieldSeedCard SetPlantable(bool able)
        {
            _plantable = able;
            return this;
        }

        public bool IsPlantable()
        {
            return _plantable;
        }

        private IEnumerator StartColdDown(float time)
        {
            // _remainCdTime = time;
            SetRemainCdTime(time);
            
            var gapTime = Math.Max(0.05f, Time.deltaTime);
            var targetTime = TimeManager.Instance.globalTime + time;
            while (_remainCdTime > 0)
            {
                SetRemainCdTime(targetTime - TimeManager.Instance.globalTime);
                yield return WaitForSecondsPool.Create(gapTime);
            }
        }

        public void ColdDown(float time)
        {
            
            LightCoroutineManager.Instance.StartLightCoroutine("StartColdDown",
                StartColdDown(time));
        }

        private GameConfigObject.PlantType GetPlantType()
        {
            return GetEntity<SeedCard>().GetPlantType();
        }

        public OnFieldSeedCard CreateCardIcon()
        {
            var entity = Instantiate(MainGameManager.Instance.GetPlantByType(GetPlantType()),
                Transform, true).GetComponent<Entity>();
            _plant = entity.ToField().SetCardIconMode();
            return this;
        }

        public void OnChoose()
        {
            UpdateState();
            GetEntity<SeedCard>().SetCdMaskFill(1);
        }

        public void AfterPlace()
        {
            ColdDown(GetCdTime());
        }


        public void CancelChoose()
        {
            GetEntity<SeedCard>().SetCdMaskFill(0);
            UpdateState();
        }

        private void UpdateMaskColor(Color lackOfSunMaskColor, Color cdMaskColor)
        {
            GetEntity<SeedCard>().SetLackOfSunMaskColor(lackOfSunMaskColor).SetCdMaskColor(cdMaskColor);
        }

        public void UpdateState()
        {
            var currentSunlight = SunManager.Instance.GetCurrentSunLight();
            var currentChooseIndex = PlantingManager.Instance.GetCurrentChosenCardIndex();

            if (GetRemainCdTime() <= 0 && currentChooseIndex != Transform.GetSiblingIndex())
            {
                if (currentSunlight < GetSunPrice())
                {
                    SetPlantable(false);
                    UpdateMaskColor(new Color(0f, 0f, 0f, 0.47f),
                        new Color(0f, 0f, 0f, 0f));
                }
                else
                {
                    SetPlantable(true);
                    UpdateMaskColor(new Color(0f, 0f, 0f, 0f),
                        new Color(0f, 0f, 0f, 0f));
                }
            }
            else
            {
                SetPlantable(false);
                UpdateMaskColor(new Color(0f, 0f, 0f, 0.47f),
                    new Color(0f, 0f, 0f, 0.47f));
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            var index = Transform.GetSiblingIndex();
            PlantingManager.Instance.SetCurrentPutOnCardIndex(index);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            PlantingManager.Instance.SetCurrentPutOnCardIndex(-1);
        }
    }
}