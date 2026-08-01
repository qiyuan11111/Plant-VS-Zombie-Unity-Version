using UnityEngine;

namespace Script.Model
{
    public abstract class GameEntity : EntitySprite
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
        
        public GameEntity SetParent(Transform parentTransform)
        {
            Transform.SetParent(parentTransform);
            return this;
        }
    
        public GameEntity SetPosition(Vector3 position)
        {
            Transform.position = position;
            return this;
        }
    
        public GameEntity SetLocalPosition(Vector3 position)
        {
            Transform.localPosition = position;
            return this;
        }
    
        public GameEntity SetLocalScale(Vector3 localScale)
        {
            Transform.localScale = localScale;
            return this;
        }

        public GameEntity SetName(string name)
        {
            Transform.name = name;
            return this;
        }
        
    }
}
