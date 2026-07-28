using System.Collections;
using System.Collections.Generic;
using Script.Model;
using UnityEngine;
using Object = Script.Model.Object;

// using Object = UnityEngine.Object;

namespace Script.Util
{
    [CreateAssetMenu(fileName = "GameConfigObject", menuName = "GameConfigObject", order = 1)]
    public class GameConfigObject : ScriptableObject
    {
        [Header("植物")]
        [Tooltip("阳光菇")]
        public GameObject SunShroom;
    
        [Tooltip("豌豆射手")]
        public GameObject PeaShooterSingle;
    
        [Header("僵尸")]
        public GameObject ZombieNormal;

        [Header("物体")]
        [Tooltip("卡片")]
        public GameObject Card;
    
        [Tooltip("阳光")]
        public GameObject Sun;
    
        [Tooltip("植物影子")]
        public GameObject PlanteShadow;
    
        [Header("子弹")]
        [Tooltip("豌豆子弹")]
        public GameObject ProjectilePea;
    
        [Header("粒子特效")]
        [Tooltip("豌豆碎裂")]
        public GameObject PeaSplat;
    
        public class EntityType : IEqualityComparer<EntityType>
        {
            public string Order;
            public static EntityType None = new("None");

            protected EntityType(string order)
            {
                Order = order;
            }
            public bool Equals(EntityType x, EntityType y)
            {
                return x.Order == y.Order;
            }

            public int GetHashCode(EntityType obj)
            {
                return obj.Order.GetHashCode();
            }
        }
    
        public class ObjectType : EntityType
        {
            public static ObjectType Card = new("Card");
            public static ObjectType Sun = new("Sun");
            public static ObjectType PlanteShadow = new("PlanteShadow");

            protected ObjectType(string order) : base(order)
            {
            }
        }

        public class PlantType: EntityType
        {
            public static PlantType SunShroom = new("SunShroom");
            public static PlantType PeaShooterSingle = new("PeaShooterSingle");

            protected PlantType(string order) : base(order)
            {
            }
        }
    
        public class ZombieType: EntityType
        {
            public static ZombieType ZombieNormal = new("ZombieNormal");

            protected ZombieType(string order) : base(order)
            {
            }
        }
    
        public class BulletType: EntityType
        {
            public static BulletType ProjectilePea = new("ProjectilePea");

            protected BulletType(string order) : base(order)
            {
            }
        }
    
        public class ParticleType: EntityType
        {
            public static ParticleType PeaSplat = new("PeaSplat");

            protected ParticleType(string order) : base(order)
            {
            }
        }
    

        private readonly Dictionary<ObjectType, GameObject> _objectTypeToGameObject = new();
    
        private readonly Dictionary<PlantType, GameObject> _plantTypeToGameObject = new();
    
        private readonly Dictionary<ZombieType, GameObject> _zombieTypeToGameObject = new();

        private readonly Dictionary<BulletType, GameObject> _bulletTypeToGameObject = new();
    
        private readonly Dictionary<ParticleType, GameObject> _particleTypeToGameObject = new();
        
        
        private readonly Dictionary<ObjectType, Object> _objectTypeToObject = new();
    
        private readonly Dictionary<PlantType, Plant> _plantTypeToPlant = new();
    
        private readonly Dictionary<ZombieType, Zombie> _zombieTypeToZombie = new();

        // private Dictionary<BulletType, GameObject> bulletTypeToGameObject = new();
    
        // private Dictionary<ParticleType, GameObject> particleTypeToGameObject = new();

        public void Init()
        {
            InitPlante();
            InitBullet();
            InitZombie();
            InitParticle();
            InitObject();
        }

        private void InitPlante()
        {
            _plantTypeToPlant.Add(PlantType.PeaShooterSingle, PeaShooterSingle.GetComponent<Plant>());
            _plantTypeToGameObject.Add(PlantType.PeaShooterSingle, PeaShooterSingle);
            
            _plantTypeToPlant.Add(PlantType.SunShroom, SunShroom.GetComponent<Plant>());
            _plantTypeToGameObject.Add(PlantType.SunShroom, SunShroom);
        }

        private void InitBullet()
        {
            _bulletTypeToGameObject.Add(BulletType.ProjectilePea, ProjectilePea);
        }
    
        private void InitZombie()
        {
            _zombieTypeToGameObject.Add(ZombieType.ZombieNormal, ZombieNormal);
        }
    
        private void InitParticle()
        {
            _particleTypeToGameObject.Add(ParticleType.PeaSplat, PeaSplat);
        }

        private void InitObject()
        {
            _objectTypeToGameObject.Add(ObjectType.Card, Card);
            _objectTypeToGameObject.Add(ObjectType.Sun, Sun);
            _objectTypeToGameObject.Add(ObjectType.PlanteShadow, PlanteShadow);
        }

        public GameObject GetObjectByType(ObjectType objectType)
        {
            return _objectTypeToGameObject[objectType];
        }
    
        public GameObject GetPlantByType(PlantType objectType)
        {
            return _plantTypeToGameObject[objectType];
        }
    
        public GameObject GetZombieByType(ZombieType objectType)
        {
            return _zombieTypeToGameObject[objectType];
        }
    
        public GameObject GetBulletByType(BulletType objectType)
        {
            return _bulletTypeToGameObject[objectType];
        }
    
        public GameObject GetParticleByType(ParticleType objectType)
        {
            return _particleTypeToGameObject[objectType];
        }
        
        
        public Object GetObjectTypeByType(ObjectType objectType)
        {
            return _objectTypeToObject[objectType];
        }
    
        public Plant GetPlantTypeByType(PlantType objectType)
        {
            return _plantTypeToPlant[objectType];
        }
    
        public Zombie GetZombieTypeByType(ZombieType objectType)
        {
            return _zombieTypeToZombie[objectType];
        }
    }
}
