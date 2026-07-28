using System;
using System.Collections;
using System.Collections.Generic;
using Prefab.Object.Sun.Script;
using Script.Util;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Script.Manager
{
    public class SunManager : MonoBehaviour
    {
        public static SunManager Instance;
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

        public TextMeshProUGUI sunLightText; // 阳光文本
        // Start is called before the first frame update

        IEnumerator DelayedLoop()
        {
            for (int i = 0; i < 20000; i++)
            {
                ProduceSun(new Vector3(Random.Range(-600f, 600f), Random.Range(-500f, 500f), 0), SunType.Normal);
                // 等待 0.5 秒
                yield return new WaitForSeconds(0.8f);
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

            sun.ToField<OnFieldSun>(new Dictionary<string, object>
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

            sunPointPosition =
                transform.InverseTransformPoint(GameObject.Find("/UI/SeedBank/SunPoint").transform.position);
            sunLightText = GameObject.Find("/UI/SeedBank/SunLight").GetComponent<TextMeshProUGUI>();
        }

        private void Start()
        {
            SetCurrentSunLight(100);
            StartCoroutine(DelayedLoop());

            // ProduceSun(Vector3.zero, Sun.SunType.Normal);
            // ProduceSun(Vector3.zero, Sun.SunType.Normal);
            // ProduceSun(Vector3.zero, Sun.SunType.Normal);
            // ProduceSun(Vector3.zero, Sun.SunType.Normal);
            // ProduceSun(Vector3.zero, Sun.SunType.Normal);
            // ProduceSun(Vector3.zero, Sun.SunType.Normal);
            // ProduceSun(Vector3.zero, Sun.SunType.Normal);
        }
    }
}