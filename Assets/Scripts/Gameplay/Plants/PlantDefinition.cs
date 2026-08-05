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
        [SerializeField, Min(1)] private int cardIconFrame = 1;

        public GameConfigObject.PlantType Type => type;
        public GameObject Prefab => prefab;
        public GameObject PresentationPrefab => presentationPrefab != null ? presentationPrefab : prefab;
        public int SunPrice => sunPrice;
        public float Cooldown => cooldown;
        public int CardIconFrame => Mathf.Max(1, cardIconFrame);
    }
}
