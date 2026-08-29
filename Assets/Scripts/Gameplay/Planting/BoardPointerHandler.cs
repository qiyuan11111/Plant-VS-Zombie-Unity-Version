using UnityEngine;
using UnityEngine.EventSystems;

namespace PvZ.Gameplay.Planting
{
    /// <summary>Translates board pointer clicks into planting commands.</summary>
    public sealed class BoardPointerHandler : MonoBehaviour, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
            if (PlantingController.Instance == null) return;

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                PlantingController.Instance.TryPlace();
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                PlantingController.Instance.Cancel();
            }
        }
    }
}
