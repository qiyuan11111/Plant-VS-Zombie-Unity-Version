namespace Script.Model
{
    public abstract class OnFieldPlant : OnFieldCharacter
    {
        public new void SetNormalMode()
        {
            SetSortingLayer("plant-"+Row);
        }
    }
}