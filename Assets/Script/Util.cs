using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Util : MonoBehaviour
{
    public Transform _Transform;
    // Start is called before the first frame update
    void Start()
    {
        if (_Transform != null)
        {
            float lat = 0, lon = 0;
            int cnt = 0;
            foreach (Transform p in _Transform)
            {
                lat += p.transform.localPosition.x;// * Mathf.PI / 180;
                lon += p.transform.localPosition.y;// * Mathf.PI / 180;     
                cnt++;
            }

            lat /= cnt;
            lon /= cnt;
            
            Debug.Log(new Vector2(lat, lon));
        }
    }
}

/*Vector2 minxy = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 maxxy = new Vector2(float.MinValue, float.MinValue);
        foreach (SpriteRenderer p in GetComponentsInChildren<SpriteRenderer>())
        {
            // Debug.Log(p);
            var position1 = p.transform.position;
            minxy.x = Math.Min(minxy.x, position1.x - p.bounds.size.x / 6f);// * Mathf.PI / 180;
            minxy.y = Math.Min(minxy.y, position1.y - p.bounds.size.y / 6f);// * Mathf.PI / 180;
            
            maxxy.x = Math.Max(maxxy.x, position1.x + p.bounds.size.x / 6f);// * Mathf.PI / 180;
            maxxy.y = Math.Max(maxxy.y, position1.y + p.bounds.size.y / 4f);// * Mathf.PI / 180;
        }
        
        var position = transform.position;
        Debug.Log(minxy);
        Debug.Log(maxxy);*/
