using System.Collections;
using Prefab.Object.Sun.Script;
using Script.Util;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Script.Manager
{
    public class SunManager : SceneSingleton<SunManager>
    {
        [Header("Sun References")]
        [SerializeField] private Transform sunPoint;
        [SerializeField] private TextMeshProUGUI sunLightText;

        [Header("Natural Sun Spawn")]
        [SerializeField] private bool enableNaturalSun = true;
        [SerializeField, Min(0.1f)] private float naturalSunInterval = 0.8f;
        [SerializeField, Min(1)] private int maxNaturalSunCount = 30;
        [SerializeField] private Vector2 naturalSunXRange = new(-600f, 600f);
        [SerializeField] private Vector2 naturalSunYRange = new(-500f, 500f);

        public Vector3 sunPointPosition;

        private int _currentSunLight;
        private int CurrentSunLight
        {
            get => _currentSunLight;
            set
            {
                _currentSunLight = value;
                if (sunLightText != null)
                {
                    sunLightText.text = _currentSunLight.ToString();
                }

                if (SeedCardManager.Instance != null)
                {
                    SeedCardManager.Instance.SetCurrentSunlight(_currentSunLight);
                }
            }
        }

        private IEnumerator SpawnNaturalSuns()
        {
            var wait = new WaitForSeconds(naturalSunInterval);
            while (enableNaturalSun)
            {
                if (transform.childCount < maxNaturalSunCount)
                {
                    SpawnSun(new Vector3(
                        Random.Range(naturalSunXRange.x, naturalSunXRange.y),
                        Random.Range(naturalSunYRange.x, naturalSunYRange.y),
                        0f), SunType.Normal);
                }

                yield return wait;
            }
        }

        public enum SunType
        {
            Small,
            Normal
        }

        public float GetSunScaleBySunType(SunType sunType)
        {
            return sunType switch
            {
                SunType.Small => 0.5f,
                SunType.Normal => 1f,
                _ => 1f
            };
        }

        public int GetSunLightBySunType(SunType sunType)
        {
            return sunType switch
            {
                SunType.Small => 15,
                SunType.Normal => 25,
                _ => 25
            };
        }

        public SunType GetSunTypeByTypeNum(int sunTypeNum)
        {
            return sunTypeNum switch
            {
                0 => SunType.Small,
                1 => SunType.Normal,
                _ => SunType.Normal
            };
        }

        public Sun SpawnSun(Vector3 worldPosition, SunType type)
        {
            if (MainGameManager.Instance == null)
            {
                throw new MissingReferenceException(
                    $"{nameof(SunManager)} requires an active {nameof(MainGameManager)} in the scene.");
            }

            var sunPrefab = MainGameManager.Instance.GetObjectByType(GameConfigObject.ObjectType.Sun);
            var sunObject = Instantiate(sunPrefab, transform, true);
            var sun = sunObject.GetComponent<Sun>();
            if (sun == null)
            {
                Destroy(sunObject);
                throw new MissingComponentException($"{sunPrefab.name} requires a {nameof(Sun)} component.");
            }

            return sun.Initialize(type, transform.InverseTransformPoint(worldPosition));
        }

        public int GetCurrentSunLight()
        {
            return CurrentSunLight;
        }

        public void SetCurrentSunLight(int sunLight)
        {
            CurrentSunLight = sunLight;
        }

        public void SubCurrentSunLight(int sunLight)
        {
            CurrentSunLight -= sunLight;
        }

        public void AddCurrentSunLight(int sunLight)
        {
            CurrentSunLight += sunLight;
        }

        protected override bool ValidateReferences()
        {
            var isValid = true;
            isValid &= RequireReference(sunPoint, nameof(sunPoint));
            isValid &= RequireReference(sunLightText, nameof(sunLightText));
            return isValid;
        }

        protected override void OnReferencesValidated()
        {
            sunPointPosition = transform.InverseTransformPoint(sunPoint.position);
            SetCurrentSunLight(100);
        }

        protected override bool ValidateDependencies()
        {
            return RequireManager(MainGameManager.Instance);
        }

        protected override void OnSingletonStart()
        {
            if (enableNaturalSun)
            {
                StartCoroutine(SpawnNaturalSuns());
            }
        }
    }
}
