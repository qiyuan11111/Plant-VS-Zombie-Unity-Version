using PvZ.Core;
using PvZ.Gameplay.Sun;
using TMPro;
using UnityEngine;

namespace PvZ.UI.HUD
{
    /// <summary>Displays sunlight and exposes the collection animation target.</summary>
    public sealed class SunHud : SceneSingleton<SunHud>
    {
        [SerializeField] private Transform collectionTarget;
        [SerializeField] private TextMeshProUGUI sunlightText;

        public Vector3 CollectionTargetLocalPosition { get; private set; }

        protected override bool ValidateReferences()
        {
            var valid = true;
            valid &= RequireReference(collectionTarget, nameof(collectionTarget));
            valid &= RequireReference(sunlightText, nameof(sunlightText));
            return valid;
        }

        protected override void OnReferencesValidated()
        {
            CollectionTargetLocalPosition = transform.InverseTransformPoint(collectionTarget.position);
        }

        protected override bool ValidateDependencies()
        {
            return RequireManager(SunWallet.Instance);
        }

        protected override void OnSingletonStart()
        {
            SunWallet.Instance.BalanceChanged += Render;
            Render(SunWallet.Instance.Balance);
        }

        protected override void OnSingletonDestroy()
        {
            if (SunWallet.Instance != null)
            {
                SunWallet.Instance.BalanceChanged -= Render;
            }
        }

        private void Render(int sunlight)
        {
            sunlightText.text = sunlight.ToString();
        }
    }
}
