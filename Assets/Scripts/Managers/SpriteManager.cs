using UnityEngine;

namespace Script.Manager
{
    public class SpriteManager : MonoBehaviour
    {
        public static SpriteManager Instance;
        private void Awake()
        {
            Instance = this;
        }
    }
}
