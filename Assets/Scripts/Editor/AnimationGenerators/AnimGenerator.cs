using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using LitJson;
using Script;
using UnityEditor;
using UnityEngine;

public abstract class AnimGenerator : MonoBehaviour
{
    protected abstract string[] getClipNames();

    public abstract Type getPropertyType(string prop);

    public GameObject gameObject;

    protected abstract string getPrefabName();

    protected virtual float getFrameRate()
    {
        return 12f;
    }
    // Start is called before the first frame update
    void Start()
    {
        var streamreader = new StreamReader(Application.dataPath + "/StreamingAssets/" + getPrefabName() + ".json");//读取数据，转换成数据流
        var jsonDatas = JsonMapper.ToObject(streamreader);
        
        var clipNames = getClipNames();
        
        // Debug.Log(clipNames);

        // foreach (var tt in gameObject.transform)
        // {
        //     Debug.Log(tt);
        // }
        // Debug.Log(gameObject.transform.Find("Zombie_jaw "));

        foreach (string clipName in clipNames)
        {
            var clip = new AnimationClip()
            {
                name = clipName,
            };
            
            var jsonData = jsonDatas[clipName];
        
            clip.frameRate = getFrameRate();
        
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            for (int i = 0; i < jsonData.Count; i++)
            {
                var name = jsonData[i]["obj"].ToString();
                // 获得gameObject的一个名叫aaa的子物体
                var child = name.Equals(gameObject.transform.name) ? gameObject.transform : gameObject.transform.Find(name);
                var ani = jsonData[i]["ani"];
                var props = jsonData[i]["ani_list"];
                
                // Debug.Log(name);
                // Debug.Log(child);
                // Debug.Log(transform.name);

                for (int j = 0; j < props.Count; j++)
                {
                    var curve = new AnimationCurve();
                    var prop = props[j].ToString();

                    for (int k = 0; k < ani.Count; k++)
                    {
                        float fram = Convert.ToSingle(ani[k]["fram"].ToString()) / getFrameRate();
                        float data = Convert.ToSingle(ani[k][prop].ToString());
                        curve.AddKey(new Keyframe(fram, data));
                    }

                    clip.SetCurve(name.Equals(gameObject.transform.name) ? "" : name, getPropertyType(prop), prop,
                        curve);
                }
            }

            string dstclippath = "Assets/generator/" + clipName + ".anim";
            
            AssetDatabase.CreateAsset(clip, dstclippath);
        }
    }

    
}
