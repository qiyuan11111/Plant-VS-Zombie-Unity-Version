using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace PvZ.Config
{
    [CreateAssetMenu(fileName = "GameConfigObject", menuName = "GameConfigObject", order = 1)]
    public sealed class GameConfigObject : ScriptableObject
    {
        [Header("Plants")]
        [SerializeField] private List<PlantDefinition> plantDefinitions = new();

        [Header("Zombies")]
        public GameObject ZombieNormal;

        [Header("Objects")]
        public GameObject Card;
        public GameObject Sun;
        [FormerlySerializedAs("PlanteShadow")]
        public GameObject PlantShadow;

        [Header("Bullets")]
        public GameObject ProjectilePea;

        [Header("Particles")]
        public GameObject PeaSplat;

        private readonly Dictionary<PlantType, PlantDefinition> _plants = new();
        private readonly Dictionary<ZombieType, GameObject> _zombies = new();
        private readonly Dictionary<ObjectType, GameObject> _objects = new();
        private readonly Dictionary<BulletType, GameObject> _bullets = new();
        private readonly Dictionary<ParticleType, GameObject> _particles = new();

        public void Init()
        {
            _plants.Clear();
            _zombies.Clear();
            _objects.Clear();
            _bullets.Clear();
            _particles.Clear();

            foreach (var definition in plantDefinitions)
            {
                if (definition == null || definition.Prefab == null)
                {
                    Debug.LogError("GameConfigObject contains an incomplete plant definition.", this);
                    continue;
                }

                if (_plants.ContainsKey(definition.Type))
                {
                    Debug.LogError($"Duplicate plant definition: {definition.Type}.", this);
                    continue;
                }

                _plants.Add(definition.Type, definition);
            }

            Register(_zombies, ZombieType.ZombieNormal, ZombieNormal);
            Register(_objects, ObjectType.Card, Card);
            Register(_objects, ObjectType.Sun, Sun);
            Register(_objects, ObjectType.PlantShadow, PlantShadow);
            Register(_bullets, BulletType.ProjectilePea, ProjectilePea);
            Register(_particles, ParticleType.PeaSplat, PeaSplat);
        }

        public PlantDefinition GetPlantDefinition(PlantType type)
        {
            return GetRequired(_plants, type);
        }

        public GameObject GetPlantByType(PlantType type)
        {
            return GetPlantDefinition(type).Prefab;
        }

        public GameObject GetZombieByType(ZombieType type)
        {
            return GetRequired(_zombies, type);
        }

        public GameObject GetObjectByType(ObjectType type)
        {
            return GetRequired(_objects, type);
        }

        public GameObject GetBulletByType(BulletType type)
        {
            return GetRequired(_bullets, type);
        }

        public GameObject GetParticleByType(ParticleType type)
        {
            return GetRequired(_particles, type);
        }

        private void Register<TType>(Dictionary<TType, GameObject> target, TType type, GameObject prefab)
        {
            if (prefab == null)
            {
                Debug.LogError($"No prefab configured for {typeof(TType).Name}.{type}.", this);
                return;
            }

            target.Add(type, prefab);
        }

        private static TValue GetRequired<TKey, TValue>(Dictionary<TKey, TValue> source, TKey key)
        {
            if (source.TryGetValue(key, out var value)) return value;

            throw new KeyNotFoundException($"No configuration found for {typeof(TKey).Name}.{key}.");
        }
    }
}
