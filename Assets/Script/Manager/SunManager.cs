using System.Collections;
using System.Collections.Generic;
using Prefab.Object.Sun.Script;
using Script.Util;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Script.Manager
{
    public class SunManager : MonoBehaviour
    {
        public static SunManager Instance;

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
                sunLightText.text = _currentSunLight.ToString();
                SeedCardManager.Instance.UpdateCardGroupState();
                // SeedCardBankManager.Instance.UpdateCardGroupState();
            }
        }

        private IEnumerator SpawnNaturalSuns()
        {
            var wait = new WaitForSeconds(naturalSunInterval);
            while (enableNaturalSun)
            {
                if (transform.childCount < maxNaturalSunCount)
                {
                    ProduceSun(new Vector3(
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

        private void ProduceSun(Vector3 position, SunType type)
        {
            var sunObject = Instantiate(MainGameManager.Instance.GetObjectByType(GameConfigObject.ObjectType.Sun),
                transform, true);
            var sun = sunObject.GetComponent<Sun>();
            sun.Reset();

            sun.ToField(new Dictionary<string, object>
            {
                { "LocalPosition", transform.InverseTransformPoint(position) },
                { "sunType", type }
            });
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

        private void Awake()
        {
            Instance = this;
            sunPointPosition = transform.InverseTransformPoint(sunPoint.position);
        }

        private void Start()
        {
            SetCurrentSunLight(100);
            if (enableNaturalSun)
            {
                StartCoroutine(SpawnNaturalSuns());
            }
        }
    }
}
