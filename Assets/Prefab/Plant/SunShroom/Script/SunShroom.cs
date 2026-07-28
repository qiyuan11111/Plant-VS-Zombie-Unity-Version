using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Script.Manager;
using Script.Model;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace Prefab.Plant.SunShroom.Script
{
    public class SunShroom : global::Script.Model.Plant
    {
        // Start is called before the first frame update
        // public override Vector3 SpritePosition => new(41.975f, -47.87f, 0f);
        public override Vector3 SpritePosition => new(42.275f, -45.875f, 0f);

        // public int defaultSunPrice = 25;
        // public float defaultSunPrice = 0f;
    
        // public override Vector3 shadowPosition => new(-0.6f, -12.7f, 0);
        // public override Vector3 shadowScale => new(0.6f, 0.6f, 1f);

        public override string GetChineseName()
        {
            return "阳光菇";
        }

        public override string GetEnglishName()
        {
            return "SunShroom";
        }
        
        public override int GetDefaultSunPrice()
        {
            return 25;
        }

        public override float GetDefaultCdTime()
        {
            return 5f;
        }

        // public override void SetNormalModeAnimatioSpeed()
        // {
        //     SetAnimationSpeed(1.4f);
        // }
        //
        // public override List<Task> SetFunction()
        // {
        //     List<Task> tasks = new List<Task>();
        //     tasks.Add(coroutine.StartCoroutineTask(StartProduceSunColdDown()));
        //     return tasks;
        // }
        //
        //
        // public void ProduceSun(int type)
        // {
        //     SunManager.instance.ProduceSun(transform.Find("SunShroom_head").position, Sun.GetSunTypeByType(type));
        // }
    
        public new void Awake()
        {
            base.Awake();
        }

        public T ToField<T>(Dictionary<string, object> param = null) where T : OnFieldEntity
        {
            return PrePareToField<OnFieldSunShroom>() as T;
        }

        public override void AfterCreate(Dictionary<string, object> param)
        {
            throw new System.NotImplementedException();
        }

        public override OnFieldEntity ToField(Dictionary<string, object> param = null)
        {
            return ToField<OnFieldSunShroom>(param);
        }

        public void Start()
        {
            // 
        }

        public void Update()
        {
        
        }
    }
}
