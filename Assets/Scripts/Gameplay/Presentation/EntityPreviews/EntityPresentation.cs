using System;
using PvZ.Config;
using PvZ.Gameplay.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace PvZ.Gameplay.Presentation.EntityPreviews
{
    /// <summary>
    /// Configures an entity instance for non-gameplay presentation contexts.
    /// Presentation instances never enter the field lifecycle and remain disabled.
    /// </summary>
    public static class EntityPresentation
    {
        private static readonly Vector3 PreviewScale = Vector3.one;

        public static GameEntity ConfigureCardIcon(GameEntity entity, PlantDefinition definition)
        {
            EnsureEntity(entity);
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            entity.GetComponentRoot()
                .SetSortingLayer("card")
                .SetColliderState(false);

            var scale = definition.SeedPacketScale;
            entity.SetLocalScale(new Vector3(scale, scale, 1f))
                .SetLocalPosition(definition.SeedPacketLocalPosition);

            Freeze(entity, definition.PresentationNormalizedTime);
            return entity;
        }

        public static GameEntity ConfigureCursorPreview(GameEntity entity, float normalizedTime = 0f)
        {
            EnsureEntity(entity);
            entity.GetComponentRoot()
                .SetSortingLayer("front")
                .SetColliderState(false);

            entity.SetLocalScale(PreviewScale)
                .SetName(entity.GetEnglishName() + "-Mouse");

            Freeze(entity, normalizedTime);
            return entity;
        }

        public static GameEntity ConfigureCellPreview(GameEntity entity, float normalizedTime = 0f)
        {
            EnsureEntity(entity);
            entity.GetComponentRoot()
                .SetTransparentMaterial()
                .SetColliderState(false);

            entity.SetLocalScale(PreviewScale)
                .SetName(entity.GetEnglishName() + "-Cell");

            Freeze(entity, normalizedTime);
            return entity;
        }

        private static void Freeze(GameEntity entity, float normalizedTime)
        {
            foreach (var animator in entity.GetComponentsInChildren<Animator>(true))
            {
                SampleAnimatorTime(animator, normalizedTime);
                animator.enabled = false;
            }

            var image = entity.GetComponent<Image>();
            if (image != null) image.raycastTarget = false;

            entity.GetComponentRoot().ApplyAndDisableSpriteTransforms();
            entity.enabled = false;
        }

        private static void SampleAnimatorTime(Animator animator, float normalizedTime)
        {
            if (animator.runtimeAnimatorController == null) return;

            // Initialize every controller layer at its default state. In the plant
            // controllers this is the idle state; prefab values remain the fallback
            // for properties that the active animation does not bind.
            animator.Update(0f);

            var sampleTime = Mathf.Clamp01(normalizedTime);
            if (sampleTime <= 0f) return;

            for (var layerIndex = 0; layerIndex < animator.layerCount; layerIndex++)
            {
                var state = animator.GetCurrentAnimatorStateInfo(layerIndex);
                animator.Play(state.fullPathHash, layerIndex, sampleTime);
            }

            animator.Update(0f);
        }

        private static void EnsureEntity(GameEntity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
        }

    }
}
