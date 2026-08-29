using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PvZ.UI.Settings
{
    [RequireComponent(typeof(Dropdown))]
    public sealed class ResolutionDropdown : MonoBehaviour
    {
        [SerializeField] private Dropdown dropdown;
        [SerializeField] private Vector2Int[] supportedResolutions =
        {
            new(1600, 1200),
            new(2160, 1440)
        };
        [SerializeField] private bool fullscreen = true;

        private void Awake()
        {
            if (dropdown == null) dropdown = GetComponent<Dropdown>();
            RenderOptions();
        }

        private void OnEnable()
        {
            dropdown.onValueChanged.AddListener(ApplyResolution);
        }

        private void OnDisable()
        {
            if (dropdown != null)
            {
                dropdown.onValueChanged.RemoveListener(ApplyResolution);
            }
        }

        private void RenderOptions()
        {
            var options = new List<Dropdown.OptionData>(supportedResolutions.Length);
            foreach (var resolution in supportedResolutions)
            {
                options.Add(new Dropdown.OptionData($"{resolution.x} × {resolution.y}"));
            }

            dropdown.ClearOptions();
            dropdown.AddOptions(options);
        }

        private void ApplyResolution(int index)
        {
            if (index < 0 || index >= supportedResolutions.Length) return;

            var resolution = supportedResolutions[index];
            Screen.SetResolution(resolution.x, resolution.y, fullscreen);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (dropdown == null) dropdown = GetComponent<Dropdown>();
        }
#endif
    }
}
