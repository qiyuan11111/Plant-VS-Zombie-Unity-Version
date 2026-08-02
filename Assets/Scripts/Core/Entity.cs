using UnityEngine;

namespace Script.Model
{
    public abstract class Entity : Sprite
    {
        // private readonly Dictionary<string, Task> _functions = new();
    
        protected Animator Animator; //动画

        public abstract string GetChineseName();
        public abstract string GetEnglishName();
        
        

        protected sealed override void Awake()
        {
            base.Awake();
            Animator = GetComponentInChildren<Animator>();
        } 
        
        public Entity SetParent(Transform parentTransform)
        {
            Transform.SetParent(parentTransform);
            return this;
        }
    
        public Entity SetPosition(Vector3 position)
        {
            Transform.position = position;
            return this;
        }
    
        public Entity SetLocalPosition(Vector3 position)
        {
            Transform.localPosition = position;
            return this;
        }
    
        public Entity SetLocalScale(Vector3 localScale)
        {
            Transform.localScale = localScale;
            return this;
        }

        public Entity SetName(string name)
        {
            Transform.name = name;
            return this;
        }
        
    }
}
