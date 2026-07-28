using System;
using System.Collections;
using System.Collections.Generic;
using Script.Manager;
using Script.Model;
using Script.Util;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Prefab.Object.SeedCard.Script
{
    public class SeedCard : global::Script.Model.Object, IToField, IPointerEnterHandler, IPointerExitHandler
    {
        private Image _lackOfSunMask;
        private Image _cdMask;

        private GameConfigObject.PlantType _entityType;

        public GameConfigObject.PlantType GetPlantType()
        {
            return _entityType;
        }
        
        public SeedCard InitPlantType(GameConfigObject.PlantType type)
        {
            _entityType = type;
            return this;
        }

        private int GetDefaultSunPrice()
        {
            var plant = MainGameManager.Instance.GetPlantTypeByType(_entityType);
            return plant != null ? plant.GetDefaultSunPrice() : 0;
        }

        private float GetDefaultCdTime()
        {
            var plant = MainGameManager.Instance.GetPlantTypeByType(_entityType);
            return plant != null ? plant.GetDefaultCdTime() : 0;
        }

        public override string GetChineseName()
        {
            return "卡片";
        }

        public override string GetEnglishName()
        {
            return "Card";
        }

        public SeedCard SetCdMaskFill(float fillAmount)
        {
            _cdMask.fillAmount = fillAmount;
            return this;
        }

        public SeedCard SetCdMaskColor(Color color)
        {
            _cdMask.color = color;
            return this;
        }

        public SeedCard SetLackOfSunMaskColor(Color color)
        {
            _lackOfSunMask.color = color;
            return this;
        }

        public Text _priceText;

        public SeedCard SetPriceText(string priceText)
        {
            _priceText.text = priceText;
            return this;
        }

        public string GetPriceText()
        {
            return _priceText.text;
        }

        // public override OnFieldSprite ToField()
        // {
        //     var onFieldEntity = gameObject.AddComponent<OnFieldSeedCard>();
        //     onFieldEntity.SetEntity(this);
        //     return onFieldEntity;
        // }

        
        private new void Awake()
        {
            base.Awake();
            _lackOfSunMask = Transform.Find("NoSunMask").GetComponent<Image>();
            _cdMask = Transform.Find("CDMask").GetComponent<Image>();
            _priceText = Transform.Find("Price").GetComponent<Text>();

            _lackOfSunMask.GetComponent<Canvas>().sortingLayerName = "cardmask";
            _cdMask.GetComponent<Canvas>().sortingLayerName = "cardmask";
        }

        public override void AfterCreate(Dictionary<string, object> param)
        {
            var plantType = param["entityType"] as GameConfigObject.PlantType;
            InitPlantType(plantType);
        }

        public override OnFieldEntity ToField(Dictionary<string, object> param = null)
        {
            return ToField<OnFieldSeedCard>(param);
        }

        public T ToField<T>(Dictionary<string, object> param = null) where T : OnFieldEntity
        {
            var onFieldSeedCard = PrePareToField<OnFieldSeedCard>()
                .CreateCardIcon()
                .SetPlantable(false)
                .SetCardSunPrice(GetDefaultSunPrice())
                .SetCdTime(GetDefaultCdTime())

                .SetLocalScale(new Vector3(1, 1, 1));
            
            if (param != null)
            {
                var name = param["Name"] as string;
                onFieldSeedCard.SetName(name);
            }
            
            var cardRectTransform = onFieldSeedCard.GetComponent<RectTransform>();
            var anchoredPosition = cardRectTransform.anchoredPosition3D;
            cardRectTransform.anchoredPosition3D = new Vector3(anchoredPosition.x, anchoredPosition.y, -10);
            return onFieldSeedCard as T;
        }
        
        
        
        
        
        /*******/
        private OnFieldEntity _plant;

        // private bool _dirty = false;

        private float _cdTime;

        public SeedCard SetCdTime(float time)
        {
            _cdTime = time;
            return this;
        }

        public float GetCdTime()
        {
            return _cdTime;
        }

        private int _sunPrice;

        public SeedCard SetCardSunPrice(int price)
        {
            _sunPrice = price;
            SetPriceText(price.ToString());
            return this;
        }

        public int GetSunPrice()
        {
            return _sunPrice;
        }

        private float _remainCdTime;

        public SeedCard SetRemainCdTime(float time)
        {
            _remainCdTime = time;
            SetCdMaskFill(Math.Max(time, 0f) / GetCdTime());
            UpdateState();
            return this;
        }

        public float GetRemainCdTime()
        {
            return _remainCdTime;
        }

        private bool _plantable;

        public SeedCard SetPlantable(bool able)
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

        // private GameConfigObject.PlantType GetPlantType()
        // {
        //     return GetEntity<SeedCard>().GetPlantType();
        // }

        public SeedCard CreateCardIcon()
        {
            var entity = Instantiate(MainGameManager.Instance.GetPlantByType(GetPlantType()),
                Transform, true).GetComponent<Entity>();
            _plant = entity.ToField().SetCardIconMode();
            return this;
        }

        public void OnChoose()
        {
            UpdateState();
            SetCdMaskFill(1);
        }

        public void AfterPlace()
        {
            ColdDown(GetCdTime());
        }


        public void CancelChoose()
        {
            SetCdMaskFill(0);
            UpdateState();
        }

        private void UpdateMaskColor(Color lackOfSunMaskColor, Color cdMaskColor)
        {
            this.SetLackOfSunMaskColor(lackOfSunMaskColor)
                .SetCdMaskColor(cdMaskColor);
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