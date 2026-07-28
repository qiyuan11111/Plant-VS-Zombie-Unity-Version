using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Serialization;
using Sprite = Script.Model.Sprite;

namespace Script
{
    public class SpriteTransform : MonoBehaviour
    {
        public Material material;
        public bool hasMaterial;
    
        //private Transform _transform;
        private Vector3 _positionOffset;
    
        [FormerlySerializedAs("Position")] public Vector2 position;
        
        [FormerlySerializedAs("Float")]
        
        
        

        [FormerlySerializedAs("Scale")] public Vector2 scale;
    
        [FormerlySerializedAs("Skew")] public Vector2 skew;

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
        
        private static readonly int SkewX = Shader.PropertyToID("_SkewX");
        private static readonly int SkewY = Shader.PropertyToID("_SkewY");
        private static readonly int ScaleX = Shader.PropertyToID("_ScaleX");
        private static readonly int ScaleY = Shader.PropertyToID("_ScaleY");
        private static readonly int Brightness = Shader.PropertyToID("_Brightness");
        private static readonly int Alpha = Shader.PropertyToID("_Alpha");

        public bool updatePosition;

        void Awake()
        {
            InitializeParm();
        }

        private bool InitializeMaterial()
        {
            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null) return false;
            var mat = spriteRenderer.material;
            var shader = mat.shader;
            var m = new Material(shader);
            m.CopyPropertiesFromMaterial(mat);
            GetComponent<SpriteRenderer>().material = m;
            return true;

        }

        private bool InitializeSpriteType()
        {
            transform.parent.TryGetComponent<Sprite>(out var sprite);
            if (sprite == null) return false;
            _positionOffset = sprite.SpritePosition;
            return true;
        }

        private void InitializeParm()
        {
            // skew = new Vector2(0f, 0f);
            // scale = new Vector2(100f, 100f);
            // position = new Vector2(0f, 0f);
            // brightness = alpha = alphaCoef = 1f;
            //
            // updatePosition= true;
        
            hasMaterial = false;
            if (InitializeMaterial())
            {
                material = GetComponent<SpriteRenderer>().material;
                hasMaterial = true;
            }
            else if (GetComponentsInChildren<Transform>(true).Length <= 1)
            {
                Debug.Log(transform.name+"None material!");
            }

            if (!InitializeSpriteType())
            {
                _positionOffset = Vector3.zero;
                // Debug.Log("None Sprite!");
            }
        
        }

        // Update is called once per frame
        void Update()
        {
            if (hasMaterial)
            {
                material.SetFloat(SkewX, skew.x);
                material.SetFloat(SkewY, skew.y);
                material.SetFloat(ScaleX, scale.x);
                material.SetFloat(ScaleY, scale.y);
                material.SetFloat(Brightness, brightness);
                material.SetFloat(Alpha, alpha * alphaCoef);
            }

            if (updatePosition)
            {
                transform.localPosition = new Vector3(position.x - _positionOffset.x, -position.y - _positionOffset.y, 0);
            }

            
        }
    }
}