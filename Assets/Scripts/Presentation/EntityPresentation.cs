using System;
using PvZ.Core.Entities;
using PvZ.Gameplay.Plants;
using UnityEngine;
using UnityEngine.UI;

namespace PvZ.Presentation
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

            var drawOrigin = GetSeedPacketDrawOrigin(entity, definition.SeedPacketDrawOffset);
            var scale = definition.SeedPacketScale;
            entity.SetLocalScale(new Vector3(scale, scale, 1f))
                .SetLocalPosition(drawOrigin);

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

        public static GameEntity ConfigureGridPreview(GameEntity entity, float normalizedTime = 0f)
        {
            EnsureEntity(entity);
            entity.GetComponentRoot()
                .SetTransparentMaterial()
                .SetColliderState(false);

            entity.SetLocalScale(PreviewScale)
                .SetName(entity.GetEnglishName() + "-Grid");

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

        private static Vector3 GetSeedPacketDrawOrigin(GameEntity entity, Vector2 sourceOffset)
        {
            if (entity.transform.parent is RectTransform packet)
            {
                return new Vector3(
                    packet.rect.xMin + sourceOffset.x,
                    packet.rect.yMax - sourceOffset.y,
                    0f);
            }

            // Non-UI callers can define their parent origin as the packet's
            // top-left. This also keeps isolated tests deterministic.
            return new Vector3(sourceOffset.x, -sourceOffset.y, 0f);
        }
    }
}
