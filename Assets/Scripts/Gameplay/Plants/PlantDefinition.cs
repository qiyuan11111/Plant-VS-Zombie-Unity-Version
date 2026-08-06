using System;
using PvZ.Config;
using UnityEngine;

namespace PvZ.Gameplay.Plants
{
    [Serializable]
    public sealed class PlantDefinition
    {
        [SerializeField] private GameConfigObject.PlantType type;
        [SerializeField] private GameObject prefab;
        [SerializeField] private GameObject presentationPrefab;
        [SerializeField, Min(0)] private int sunPrice;
        [SerializeField, Min(0f)] private float cooldown;
        [SerializeField, Range(0f, 1f)] private float presentationNormalizedTime;
        [SerializeField, Min(0.01f)] private float cardIconScale = 0.5f;
        [SerializeField] private Vector2 cardIconAnchorPosition = new(0f, -11f);

        public GameConfigObject.PlantType Type => type;
        public GameObject Prefab => prefab;
        public GameObject PresentationPrefab => presentationPrefab != null ? presentationPrefab : prefab;
        public int SunPrice => sunPrice;
        public float Cooldown => cooldown;
        public float PresentationNormalizedTime => Mathf.Clamp01(presentationNormalizedTime);
        public float CardIconScale => Mathf.Max(0.01f, cardIconScale);
        public Vector2 CardIconAnchorPosition => cardIconAnchorPosition;
    }
}
