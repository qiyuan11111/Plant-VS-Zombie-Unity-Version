using System.Collections;
using System.Collections.Generic;
using Script;
using Script.Model;
using Script.Util;
using UnityEngine;
using UnityEngine.EventSystems;

public class PeaShooterSingle : Plant, IPointerClickHandler
{
    private static readonly int Shoot = Animator.StringToHash("shoot");
    // public override Vector3 spritePosition => new(40.2f, -52.25f, 0f);

    // public override int sunlight => 100;
    // public override float cdTime => 0.5f;

    // public Vector3 shadowPosition => new(0, -17f, 0);
    // public Vector3 shadowScale => new(1, 1, 1);

    // public class DetectZombieCallback : DetectRegion.DetectCallback
    // {
    //     public override void OnEnterDetect(Collider2D col)
    //     {
    //         var peaShooterSingle = (PeaShooterSingle)sprite;
    //         peaShooterSingle.attack = true;
    //     }
    //     
    //     public override void OnExitDetect(Collider2D col)
    //     {
    //         var peaShooterSingle = (PeaShooterSingle)sprite;
    //         if(sprite.detectRegions[name].IsAnyDetect())    return;
    //         peaShooterSingle.attack = false;
    //     }
    //
    //     public override bool IsGoal(Entity sprite)
    //     {
    //         if (sprite.row != this.sprite.row) return false;
    //         return true;
    //     }
    //
    //     public DetectZombieCallback(Entity sprite) : base(sprite)
    //     {
    //     }
    // }

    // public class HandDetectZombieRegion : DetectRegion.HandDetectRegion
    // {
    //     public override void hand(DetectRegion region)
    //     {
    //         GameObject detectRegionGameObject = region.gameObject;
    //     
    //         var right = 304.3585f;
    //         var left = sprite._transform.localPosition.x + 40;
    //
    //         BoxCollider2D collider2D = detectRegionGameObject.AddComponent<BoxCollider2D>();
    //         collider2D.size = new Vector2(right - left, 34.5f);
    //         collider2D.offset = new Vector2((right - left) / 2 + 40, 27.6f);
    //         collider2D.isTrigger = true;
    //     }
    //
    //     public HandDetectZombieRegion(Entity sprite, string name, string layer) : base(sprite, name, layer)
    //     {
    //     }
    // }

    public override int GetDefaultSunPrice()
    {
        throw new System.NotImplementedException();
    }

    public override float GetDefaultCdTime()
    {
        throw new System.NotImplementedException();
    }

    public new void Awake()
    {
        base.Awake();
        // animator = GetComponent<Animator>();
    }

    public override void AfterCreate(Dictionary<string, object> param)
    {
        throw new System.NotImplementedException();
    }

    public override Entity ToField(Dictionary<string, object> param = null)
    {
        return this;
    }

    public void ShootProjectilePea()
    {
        Vector3 position = Transform.Find("PeaShooterSingle_head/PeaShooterSingle_mouth").transform.position;
        // BulletManager.instance.InstantiateBullet(this, position + new Vector3(30, 6, 0), 
        //     GameConfigObject.BulletType.ProjectilePea);
    }

    // public override void SetDetectRegions()
    // {
    //     SetDetectRegion<Zombie>(new DetectZombieCallback(this), new HandDetectZombieRegion(this, "DetectZombieRegion", "DetectZombieRegion"));
    // }
    //
    // public override List<Task> SetFunction()
    // {
    //     List<Task> tasks = new List<Task>();
    //     tasks.Add(coroutine.StartCoroutineTask(StartShootColdDown()));
    //     return tasks;
    // }


    // IEnumerator StartShootColdDown()
    // {
    //     while (true)
    //     {
    //         if (attack)
    //         {
    //             animator.SetTrigger(Shoot);
    //             yield return new TimeWait(Random.Range(1.45f, 1.55f));
    //         }
    //         else
    //         {
    //             yield return new TimeWait();
    //         }
    //         
    //     }
    // }
    
    public override string GetChineseName()
    {
        return "豌豆射手";
    }

    public override string GetEnglishName()
    {
        return "PeaShooterSingle";
    }

    // public override void SetNormalModeAnimatioSpeed()
    // {
    //     SetAnimationSpeed(1f);
    // }


    public void OnPointerClick(PointerEventData eventData)
    {
    }
    
    
}
