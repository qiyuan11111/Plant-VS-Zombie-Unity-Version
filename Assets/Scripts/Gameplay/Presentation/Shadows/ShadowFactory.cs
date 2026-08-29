using PvZ.Bootstrap;
using PvZ.Config;
using UnityEngine;

namespace PvZ.Gameplay.Presentation.Shadows
{
    public static class ShadowFactory
    {
        public static Shadow Create(
            Transform parent,
            Vector3 localPosition,
            float scale,
            bool useNightSprite,
            string objectName = null)
        {
            if (GameBootstrap.Instance == null)
            {
                throw new MissingReferenceException(
                    $"An active {nameof(GameBootstrap)} is required to create a shadow.");
            }

            var prefab = GameBootstrap.Instance.Catalog.GetObjectByType(ObjectType.PlantShadow);
            var instance = Object.Instantiate(prefab, parent, false);
            if (!string.IsNullOrEmpty(objectName)) instance.name = objectName;
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = Quaternion.identity;

            var shadow = instance.GetComponent<Shadow>();
            if (shadow != null)
            {
                return shadow.SetNight(useNightSprite).Initialize(scale);
            }

            Object.Destroy(instance);
            throw new MissingComponentException($"{prefab.name} requires a {nameof(Shadow)} component.");
        }
    }
}
