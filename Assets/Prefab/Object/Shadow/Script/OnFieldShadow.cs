using Script.Manager;
using Script.Model;

namespace Prefab.Object.Shadow.Script
{
    public class OnFieldShadow : OnFieldObject
    {
        public OnFieldShadow SetPlaceMode()
        {
            Sprite.GetComponentRoot()
                .SetSortingLayer("shadow");
            return this;
        }
    }
}
