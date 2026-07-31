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

        public PlantDefinition GetPlantDefinition(GameConfigObject.PlantType plantType)
        {
            return gameConfigObject.GetPlantDefinition(plantType);
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
    }
}
