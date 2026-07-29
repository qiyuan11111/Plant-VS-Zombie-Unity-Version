using System;
using Script.Model;
using Script.Util;
using UnityEngine;

namespace Script.Manager
{
    public class MainGameManager : MonoBehaviour
    {
        public static MainGameManager Instance;
        public GameConfigObject gameConfigObject{get; private set;}
        public GameSound gameSound{get; private set;}

        private Camera _camera;

        private void Awake()
        {
            Instance = this;
            gameConfigObject = Resources.Load<GameConfigObject>("GameConfigObject");
            gameConfigObject.Init();

            gameSound = Resources.Load<GameSound>("GameSound");
            gameSound.Init();

            _camera = Camera.main;
        }

        public Vector3 GetNowMouseScreenToWorldPoint(float z)
        {
            var position = _camera.ScreenToWorldPoint(Input.mousePosition);
            position.z = z;
            return position;
        }
    
        public AudioClip GetAudioClipByType(GameSound.SoundType soundType)
        {
            return gameSound.GetAudioClipByType(soundType);
        }
    
        public GameObject GetPlantByType(GameConfigObject.PlantType objectType)
        {
            return gameConfigObject.GetPlantByType(objectType);
        }
        
        public Plant GetPlantTypeByType(GameConfigObject.PlantType objectType)
        {
            return gameConfigObject.GetPlantTypeByType(objectType);
        }
    
        public GameObject GetZombieByType(GameConfigObject.ZombieType objectType)
        {
            return gameConfigObject.GetZombieByType(objectType);
        }
    
        public GameObject GetBulletByType(GameConfigObject.BulletType objectType)
        {
            return gameConfigObject.GetBulletByType(objectType);
        }
    
        public GameObject GetParticleByType(GameConfigObject.ParticleType objectType)
        {
            return gameConfigObject.GetParticleByType(objectType);
        }
    
        public GameObject GetObjectByType(GameConfigObject.ObjectType objectType)
        {
            return gameConfigObject.GetObjectByType(objectType);
        }
    
        public GameObject GetEntityByType(GameConfigObject.EntityType entityType)
        {
            return entityType switch
            {
                GameConfigObject.PlantType plantType => GetPlantByType(plantType),
                GameConfigObject.ZombieType zombieType => GetZombieByType(zombieType),
                _ => throw new Exception("No type of plant and zombie")
            };
        }

        // public static void DestoryIfNotNull(GameObject gameObject)
        // {
        //     if (gameObject != null)
        //     {
        //         Destroy(gameObject);
        //     }
        // }
        //
        // public static bool IsExist(Entity sprite)
        // {
        //     var o = sprite.gameObject;
        //     return sprite != null && o != null && o.activeSelf;
        // }
        // void Start()
        // {
        //
        // }
        //
        // // Update is called once per frame
        // void Update()
        // {
        //
        // }
    }
}
