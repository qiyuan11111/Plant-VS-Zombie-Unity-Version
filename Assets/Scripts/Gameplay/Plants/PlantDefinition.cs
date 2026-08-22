using System;
using PvZ.Config;
using UnityEngine;
using UnityEngine.Serialization;

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
        [SerializeField, Min(0.01f)] private float seedPacketScale = 0.5f;
        [FormerlySerializedAs("seedPacketDrawOffset")]
        [SerializeField] private Vector2 seedPacketLocalPosition = Vector2.zero;

        public GameConfigObject.PlantType Type => type;
        public GameObject Prefab => prefab;
        public GameObject PresentationPrefab => presentationPrefab != null ? presentationPrefab : prefab;
        public int SunPrice => sunPrice;
        public float Cooldown => cooldown;
        public float PresentationNormalizedTime => Mathf.Clamp01(presentationNormalizedTime);
        public float SeedPacketScale => Mathf.Max(0.01f, seedPacketScale);
        public Vector2 SeedPacketLocalPosition => seedPacketLocalPosition;
    }
}
