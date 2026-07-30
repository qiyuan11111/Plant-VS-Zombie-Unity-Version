using Script.Model;
using Script.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Prefab.Object.SeedCard.Script
{
    public enum SeedCardVisualState
    {
        Ready,
        InsufficientSun,
        CoolingDown,
        Selected
    }

    [DisallowMultipleComponent]
    public sealed class SeedCardView : MonoBehaviour
    {
        private static readonly Color ClearMask = new(0f, 0f, 0f, 0f);
        private static readonly Color BlockedMask = new(0f, 0f, 0f, 0.47f);

        [SerializeField] private Image lackOfSunMask;
        [SerializeField] private Image cooldownMask;
        [SerializeField] private Text priceText;
        [SerializeField] private Transform iconRoot;

        private Entity _plantIcon;

        public void Initialize(PlantDefinition definition)
        {
            ResolveReferences();
            priceText.text = definition.SunPrice.ToString();
            CreatePlantIcon(definition);
        }

        public void Render(SeedCardVisualState state, float cooldownProgress)
        {
            ResolveReferences();

            switch (state)
            {
                case SeedCardVisualState.Ready:
                    ApplyMasks(ClearMask, ClearMask, 0f);
                    break;
                case SeedCardVisualState.InsufficientSun:
                    ApplyMasks(BlockedMask, ClearMask, 0f);
                    break;
                case SeedCardVisualState.CoolingDown:
                    ApplyMasks(BlockedMask, BlockedMask, Mathf.Clamp01(cooldownProgress));
                    break;
                case SeedCardVisualState.Selected:
                    ApplyMasks(BlockedMask, BlockedMask, 1f);
                    break;
            }
        }

        private void Awake()
        {
            ResolveReferences();
            lackOfSunMask.GetComponent<Canvas>().sortingLayerName = "cardmask";
            cooldownMask.GetComponent<Canvas>().sortingLayerName = "cardmask";
        }

        private void ApplyMasks(Color lackOfSunColor, Color cooldownColor, float cooldownFill)
        {
            lackOfSunMask.color = lackOfSunColor;
            cooldownMask.color = cooldownColor;
            cooldownMask.fillAmount = cooldownFill;
        }

        private void CreatePlantIcon(PlantDefinition definition)
        {
            if (_plantIcon != null) Destroy(_plantIcon.gameObject);

            var iconObject = Instantiate(definition.Prefab, iconRoot, true);
            _plantIcon = iconObject.GetComponent<Entity>();
            if (_plantIcon == null)
            {
                Destroy(iconObject);
                throw new MissingComponentException(
                    $"Plant prefab {definition.Prefab.name} requires an Entity component.");
            }

            _plantIcon.ToField().SetCardIconMode();
        }

        private void ResolveReferences()
        {
            if (lackOfSunMask == null)
                lackOfSunMask = transform.Find("NoSunMask")?.GetComponent<Image>();
            if (cooldownMask == null)
                cooldownMask = transform.Find("CDMask")?.GetComponent<Image>();
            if (priceText == null)
                priceText = transform.Find("Price")?.GetComponent<Text>();
            if (iconRoot == null) iconRoot = transform;

            if (lackOfSunMask == null || cooldownMask == null || priceText == null)
            {
                throw new MissingReferenceException("SeedCardView UI references are incomplete.");
            }
        }
    }
}
