using System;
using Script.Manager;
using Script.Model;
using Script.Util;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Prefab.Object.SeedCard.Script
{
    public class SeedCard : global::Script.Model.Object, IPointerClickHandler
    {
        [SerializeField] private SeedCardView view;

        private readonly SeedCardCooldown _cooldown = new();
        private PlantDefinition _plantDefinition;
        private int _currentSunlight;
        private bool _isSelected;

        public PlantDefinition Definition => _plantDefinition;
        public int SunPrice => _plantDefinition.SunPrice;
        public float Cooldown => _plantDefinition.Cooldown;
        public float CooldownRemaining => _cooldown.Remaining;
        public bool CanPlant =>
            _plantDefinition != null &&
            !_isSelected &&
            _cooldown.IsReady &&
            _currentSunlight >= SunPrice;

        public SeedCard Initialize(PlantDefinition plantDefinition)
        {
            _plantDefinition = plantDefinition ?? throw new ArgumentNullException(nameof(plantDefinition));
            ResolveView();
            view.Initialize(plantDefinition);

            SetLocalScale(Vector3.one);
            var cardRectTransform = GetComponent<RectTransform>();
            var position = cardRectTransform.anchoredPosition3D;
            cardRectTransform.anchoredPosition3D = new Vector3(position.x, position.y, -10f);

            RefreshView();
            return this;
        }

        public override string GetChineseName()
        {
            return "卡片";
        }

        public override string GetEnglishName()
        {
            return "Card";
        }

        public void SetCurrentSunlight(int sunlight)
        {
            if (_currentSunlight == sunlight) return;

            _currentSunlight = sunlight;
            RefreshView();
        }

        public void SetSelected(bool selected)
        {
            if (_isSelected == selected) return;

            _isSelected = selected;
            RefreshView();
        }

        public void StartCooldown()
        {
            _cooldown.Start(Cooldown);
            RefreshView();
        }

        public void OnChoose()
        {
            SetSelected(true);
        }

        public void AfterPlace()
        {
            _isSelected = false;
            StartCooldown();
        }

        public void CancelChoose()
        {
            SetSelected(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                PlantingManager.Instance.TryBegin(this);
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                PlantingManager.Instance.Cancel();
            }
        }

        private void ResolveView()
        {
            if (view == null) view = GetComponent<SeedCardView>();
            if (view == null) view = gameObject.AddComponent<SeedCardView>();
        }

        private void Update()
        {
            if (_cooldown.Tick(Time.deltaTime)) RefreshView();
        }

        private void RefreshView()
        {
            if (_plantDefinition == null || view == null) return;

            var state = GetVisualState();
            view.Render(state, _cooldown.Progress);
        }

        private SeedCardVisualState GetVisualState()
        {
            if (_isSelected) return SeedCardVisualState.Selected;
            if (!_cooldown.IsReady) return SeedCardVisualState.CoolingDown;
            return _currentSunlight < SunPrice
                ? SeedCardVisualState.InsufficientSun
                : SeedCardVisualState.Ready;
        }
    }
}
