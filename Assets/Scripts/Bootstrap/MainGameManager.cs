using PvZ.Audio;
using PvZ.Config;
using PvZ.Core;
using PvZ.Gameplay.Plants;
using UnityEngine;

namespace PvZ.Bootstrap
{
    public class MainGameManager : SceneSingleton<MainGameManager>
    {
        public GameConfigObject gameConfigObject{get; private set;}
        public GameSound gameSound{get; private set;}

        private Camera _camera;

        protected override void OnSingletonAwake()
        {
            gameConfigObject = Resources.Load<GameConfigObject>("GameConfigObject");
            gameSound = Resources.Load<GameSound>("GameSound");
            _camera = Camera.main;
        }

        protected override bool ValidateReferences()
        {
            var isValid = true;
            isValid &= RequireReference(gameConfigObject, "Resources/GameConfigObject");
            isValid &= RequireReference(gameSound, "Resources/GameSound");
            isValid &= RequireReference(_camera, "a Camera tagged MainCamera");
            return isValid;
        }

        protected override void OnReferencesValidated()
        {
            gameConfigObject.Init();
        }

        public Vector3 GetNowMouseScreenToWorldPoint(float z)
        {
            var position = _camera.ScreenToWorldPoint(UnityEngine.Input.mousePosition);
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
