using System;
using UnityEngine;
using UnityEngine.Serialization;
using Script.Model;

namespace Script
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class SpriteTransform : MonoBehaviour
    {
        private static readonly int SkewX = Shader.PropertyToID("_SkewX");
        private static readonly int SkewY = Shader.PropertyToID("_SkewY");
        private static readonly int ScaleX = Shader.PropertyToID("_ScaleX");
        private static readonly int ScaleY = Shader.PropertyToID("_ScaleY");
        private static readonly int Brightness = Shader.PropertyToID("_Brightness");
        private static readonly int Alpha = Shader.PropertyToID("_Alpha");

        [NonSerialized] public Material material;
        [NonSerialized] public bool hasMaterial;

        [FormerlySerializedAs("Position")]
        public Vector2 position;

        [FormerlySerializedAs("Scale")]
        public Vector2 scale;

        [FormerlySerializedAs("Skew")]
        public Vector2 skew;

        [FormerlySerializedAs("Brightness")]
        [Range(0f, 2f)]
        public float brightness;

        public bool isBright;

        [FormerlySerializedAs("Alpha")]
        [Range(0f, 1f)]
        public float alpha;

        [FormerlySerializedAs("AlphaCoef")]
        [Range(0f, 1f)]
        public float alphaCoef;

        public bool updatePosition;
        public bool updateHierarchyScale;
        public Vector2 hierarchyScaleReference;

        private Transform _cachedTransform;
        private SpriteRenderer _spriteRenderer;
        private MaterialPropertyBlock _materialProperties;
        private Vector3 _positionOffset;

        private bool _hasSkewX;
        private bool _hasSkewY;
        private bool _hasScaleX;
        private bool _hasScaleY;
        private bool _hasBrightness;
        private bool _hasAlpha;

        private bool _materialStateApplied;
        private Vector2 _appliedSkew;
        private Vector2 _appliedScale;
        private float _appliedBrightness;
        private float _appliedAlpha;

        private void Awake()
        {
            EnsureInitialized();

            if (!hasMaterial && !updatePosition && !updateHierarchyScale)
            {
                enabled = false;
            }
        }

        private bool InitializeMaterial()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer == null || _spriteRenderer.sharedMaterial == null) return false;

            var sharedMaterial = _spriteRenderer.sharedMaterial;
            _hasSkewX = sharedMaterial.HasProperty(SkewX);
            _hasSkewY = sharedMaterial.HasProperty(SkewY);
            _hasScaleX = sharedMaterial.HasProperty(ScaleX);
            _hasScaleY = sharedMaterial.HasProperty(ScaleY);
            _hasBrightness = sharedMaterial.HasProperty(Brightness);
            _hasAlpha = sharedMaterial.HasProperty(Alpha);

            var hasSupportedProperty = _hasSkewX || _hasSkewY ||
                                       _hasScaleX || _hasScaleY ||
                                       _hasBrightness || _hasAlpha;
            if (!hasSupportedProperty) return false;

            material = sharedMaterial;
            _materialProperties = new MaterialPropertyBlock();
            return true;
        }

        private void InitializePositionOffset()
        {
            var parent = _cachedTransform.parent;
            while (parent != null)
            {
                if (parent.TryGetComponent<EntitySprite>(out var sprite))
                {
                    _positionOffset = sprite.SpritePosition;
                    return;
                }

                // An animated parent establishes a local animation coordinate system.
                // Pure organizational transforms can be skipped safely, but crossing
                // another SpriteTransform would apply the plant offset more than once.
                if (parent.TryGetComponent<SpriteTransform>(out var animatedParent) &&
                    animatedParent.updatePosition)
                {
                    return;
                }

                parent = parent.parent;
            }
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                Apply();
            }
        }

        private void RestoreLegacyMaterialDefaults()
        {
            if (!hasMaterial || material == null) return;

            var hasUninitializedValues = scale == Vector2.zero &&
                                         brightness == 0f &&
                                         alpha == 0f &&
                                         alphaCoef == 0f;
            if (!hasUninitializedValues) return;

            if (_hasScaleX) scale.x = material.GetFloat(ScaleX);
            if (_hasScaleY) scale.y = material.GetFloat(ScaleY);
            if (_hasSkewX) skew.x = material.GetFloat(SkewX);
            if (_hasSkewY) skew.y = material.GetFloat(SkewY);
            if (_hasBrightness) brightness = material.GetFloat(Brightness);
            if (_hasAlpha) alpha = material.GetFloat(Alpha);
            alphaCoef = 1f;
        }

        private void Update()
        {
            Apply();
        }

        private void OnDidApplyAnimationProperties()
        {
            Apply();
        }

        public void Apply()
        {
            EnsureInitialized();
            ApplyMaterialChanges();
            ApplyPositionChange();
            ApplyHierarchyScaleChange();
        }

        private void EnsureInitialized()
        {
            if (_cachedTransform != null) return;

            _cachedTransform = transform;
            hasMaterial = InitializeMaterial();
            RestoreLegacyMaterialDefaults();
            InitializePositionOffset();
        }

        private void InvalidateInitialization()
        {
            _cachedTransform = null;
            _spriteRenderer = null;
            _materialProperties = null;
            _positionOffset = Vector3.zero;

            _hasSkewX = false;
            _hasSkewY = false;
            _hasScaleX = false;
            _hasScaleY = false;
            _hasBrightness = false;
            _hasAlpha = false;
            _materialStateApplied = false;
        }

        public void ApplyAndDisable()
        {
            Apply();
            enabled = false;
        }

        private void ApplyMaterialChanges()
        {
            if (!hasMaterial || material == null || _spriteRenderer == null) return;

            // MaterialPropertyBlock is transient Unity state. It can be cleared
            // by an editor validation/domain-reload cycle while the cached
            // renderer and material are still valid, so always create it lazily
            // at the point of use.
            if (_materialProperties == null)
            {
                _materialProperties = new MaterialPropertyBlock();
                _materialStateApplied = false;
            }

            var skewXChanged = _hasSkewX && (!_materialStateApplied || _appliedSkew.x != skew.x);
            var skewYChanged = _hasSkewY && (!_materialStateApplied || _appliedSkew.y != skew.y);
            var scaleXChanged = _hasScaleX && (!_materialStateApplied || _appliedScale.x != scale.x);
            var scaleYChanged = _hasScaleY && (!_materialStateApplied || _appliedScale.y != scale.y);
            var brightnessChanged = _hasBrightness && (!_materialStateApplied || _appliedBrightness != brightness);
            var combinedAlpha = alpha * alphaCoef;
            var alphaChanged = _hasAlpha && (!_materialStateApplied || _appliedAlpha != combinedAlpha);

            if (!skewXChanged && !skewYChanged &&
                !scaleXChanged && !scaleYChanged &&
                !brightnessChanged && !alphaChanged)
            {
                return;
            }

            _spriteRenderer.GetPropertyBlock(_materialProperties);

            if (skewXChanged)
            {
                _materialProperties.SetFloat(SkewX, skew.x);
                _appliedSkew.x = skew.x;
            }

            if (skewYChanged)
            {
                _materialProperties.SetFloat(SkewY, skew.y);
                _appliedSkew.y = skew.y;
            }

            if (scaleXChanged)
            {
                _materialProperties.SetFloat(ScaleX, scale.x);
                _appliedScale.x = scale.x;
            }

            if (scaleYChanged)
            {
                _materialProperties.SetFloat(ScaleY, scale.y);
                _appliedScale.y = scale.y;
            }

            if (brightnessChanged)
            {
                _materialProperties.SetFloat(Brightness, brightness);
                _appliedBrightness = brightness;
            }

            if (alphaChanged)
            {
                _materialProperties.SetFloat(Alpha, combinedAlpha);
                _appliedAlpha = combinedAlpha;
            }

            _spriteRenderer.SetPropertyBlock(_materialProperties);
            _materialStateApplied = true;
        }

        private void ApplyPositionChange()
        {
            if (!updatePosition) return;

            var targetPosition = new Vector3(
                position.x - _positionOffset.x,
                -position.y - _positionOffset.y,
                0f);

            if (_cachedTransform.localPosition != targetPosition)
            {
                _cachedTransform.localPosition = targetPosition;
            }
        }

        private void ApplyHierarchyScaleChange()
        {
            if (!updateHierarchyScale) return;

            var referenceX = Mathf.Approximately(hierarchyScaleReference.x, 0f)
                ? 100f
                : hierarchyScaleReference.x;
            var referenceY = Mathf.Approximately(hierarchyScaleReference.y, 0f)
                ? 100f
                : hierarchyScaleReference.y;
            var targetScale = new Vector3(
                scale.x / referenceX,
                scale.y / referenceY,
                1f);

            if (_cachedTransform.localScale != targetScale)
            {
                _cachedTransform.localScale = targetScale;
            }
        }

#if UNITY_EDITOR
        private void OnTransformParentChanged()
        {
            if (Application.isPlaying) return;

            InvalidateInitialization();
            Apply();
        }

        private void OnValidate()
        {
            if (Application.isPlaying) return;

            // Preview both per-renderer shader properties and animation-space
            // position changes while editing a prefab. Position remains gated by
            // updatePosition, exactly as it is at runtime.
            InvalidateInitialization();
            EnsureInitialized();
            ApplyMaterialChanges();
            ApplyPositionChange();
            ApplyHierarchyScaleChange();
        }
#endif

    }
}
