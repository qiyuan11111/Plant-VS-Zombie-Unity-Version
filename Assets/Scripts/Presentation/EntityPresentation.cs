using System;
using PvZ.Core.Entities;
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
        private static readonly Vector3 PreviewScale = new(1.025f, 1.025f, 1f);

        public static GameEntity ConfigureCardIcon(GameEntity entity, int animationFrame = 1)
        {
            EnsureEntity(entity);
            entity.GetComponentRoot()
                .SetSortingLayer("card")
                .SetColliderState(false);

            entity.SetLocalScale(new Vector3(0.5f, 0.5f, 1f))
                .SetLocalPosition(new Vector3(0f, 5f, 0f));

            Freeze(entity, animationFrame);
            return entity;
        }

        public static GameEntity ConfigureCursorPreview(GameEntity entity, int animationFrame = 1)
        {
            EnsureEntity(entity);
            entity.GetComponentRoot()
                .SetSortingLayer("front")
                .SetColliderState(false);

            entity.SetLocalScale(PreviewScale)
                .SetName(entity.GetEnglishName() + "-Mouse");

            Freeze(entity, animationFrame);
            return entity;
        }

        public static GameEntity ConfigureGridPreview(GameEntity entity, int animationFrame = 1)
        {
            EnsureEntity(entity);
            entity.GetComponentRoot()
                .SetTransparentMaterial()
                .SetColliderState(false);

            entity.SetLocalScale(PreviewScale)
                .SetName(entity.GetEnglishName() + "-Grid");

            Freeze(entity, animationFrame);
            return entity;
        }

        private static void Freeze(GameEntity entity, int animationFrame = 1)
        {
            foreach (var animator in entity.GetComponentsInChildren<Animator>(true))
            {
                SampleAnimatorFrame(animator, animationFrame);
                animator.enabled = false;
            }

            var image = entity.GetComponent<Image>();
            if (image != null) image.raycastTarget = false;

            entity.GetComponentRoot().ApplyAndDisableSpriteTransforms();
            entity.enabled = false;
        }

        private static void SampleAnimatorFrame(Animator animator, int animationFrame)
        {
            if (animator.runtimeAnimatorController == null) return;

            // Initialize every controller layer at its default state. In the plant
            // controllers this is the idle state; prefab values remain the fallback
            // for properties that the active animation does not bind.
            animator.Update(0f);

            var zeroBasedFrame = Mathf.Max(0, animationFrame - 1);
            if (zeroBasedFrame == 0) return;

            for (var layerIndex = 0; layerIndex < animator.layerCount; layerIndex++)
            {
                var clipInfo = animator.GetCurrentAnimatorClipInfo(layerIndex);
                var clip = GetDominantClip(clipInfo);
                if (clip == null || clip.frameRate <= 0f || clip.length <= 0f) continue;

                // Looping clips contain length * frameRate distinct display frames;
                // clamping below 1 normalized time avoids wrapping an oversized
                // configured frame back to the first pose.
                var lastFrame = Mathf.Max(0, Mathf.CeilToInt(clip.length * clip.frameRate) - 1);
                var frame = Mathf.Min(zeroBasedFrame, lastFrame);
                var normalizedTime = frame / clip.frameRate / clip.length;
                var state = animator.GetCurrentAnimatorStateInfo(layerIndex);
                animator.Play(state.fullPathHash, layerIndex, normalizedTime);
            }

            animator.Update(0f);
        }

        private static AnimationClip GetDominantClip(AnimatorClipInfo[] clipInfo)
        {
            AnimationClip dominantClip = null;
            var dominantWeight = float.MinValue;

            foreach (var candidate in clipInfo)
            {
                if (candidate.clip == null || candidate.weight <= dominantWeight) continue;

                dominantClip = candidate.clip;
                dominantWeight = candidate.weight;
            }

            return dominantClip;
        }

        private static void EnsureEntity(GameEntity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
        }
    }
}
