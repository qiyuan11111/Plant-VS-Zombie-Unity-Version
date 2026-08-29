using System;
using System.Collections.Generic;
using PvZ.Config;

namespace PvZ.Gameplay.SeedCards
{
    [Serializable]
    public sealed class SeedCardLoadout
    {
        [UnityEngine.SerializeField]
        private List<PlantType> plantTypes = new()
        {
            PlantType.SunFlower,
            PlantType.SunShroom,
            PlantType.SunShroom,
            PlantType.SunShroom,
            PlantType.SunShroom,
            PlantType.SunShroom,
            PlantType.PeaShooterSingle
        };

        public IReadOnlyList<PlantType> PlantTypes => plantTypes;
    }
}
