using System;
using Script.Model;
using UnityEngine;
using UnityEngine.UI;

namespace Script.Presentation
{
    /// <summary>
    /// Configures an entity instance for non-gameplay presentation contexts.
    /// Presentation instances never enter the field lifecycle and remain disabled.
    /// </summary>
    public static class EntityPresentation
    {
        private static readonly Vector3 PreviewScale = new(1.025f, 1.025f, 1f);

        public static Entity ConfigureCardIcon(Entity entity)
        {
            EnsureEntity(entity);
            entity.GetComponentRoot()
                .SetSortingLayer("card")
                .SetColliderState(false);

            entity.SetLocalScale(new Vector3(0.54f, 0.54f, 1f))
                .SetLocalPosition(new Vector3(0f, 1.1f, 0f));

            Freeze(entity);
            return entity;
        }

        public static Entity ConfigureCursorPreview(Entity entity)
        {
            EnsureEntity(entity);
            entity.GetComponentRoot()
                .SetSortingLayer("front")
                .SetColliderState(false);

            entity.SetLocalScale(PreviewScale)
                .SetName(entity.GetEnglishName() + "-Mouse");

            Freeze(entity);
            return entity;
        }

        public static Entity ConfigureGridPreview(Entity entity)
        {
            EnsureEntity(entity);
            entity.GetComponentRoot()
                .SetTransparentMaterial()
                .SetColliderState(false);

            entity.SetLocalScale(PreviewScale)
                .SetName(entity.GetEnglishName() + "-Grid");

            Freeze(entity);
            return entity;
        }

        private static void Freeze(Entity entity)
        {
            var animator = entity.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                // Presentation instances are frozen immediately after Instantiate,
                // before Animator has had a frame to populate SpriteTransform values.
                animator.Update(0f);
                animator.enabled = false;
            }

            var image = entity.GetComponent<Image>();
            if (image != null) image.raycastTarget = false;

            entity.GetComponentRoot().ApplyAndDisableSpriteTransforms();
            entity.enabled = false;
        }

        private static void EnsureEntity(Entity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
        }
    }
}
