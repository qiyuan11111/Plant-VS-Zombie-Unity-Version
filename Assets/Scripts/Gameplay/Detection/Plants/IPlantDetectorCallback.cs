using PvZ.Core.Detection;
using PvZ.Gameplay.Plants;

namespace PvZ.Gameplay.Detection.Plants
{
    public interface IPlantDetectorCallback : IDetectorCallback
    {
        void OnLoad(PlantDetector detector);
        void OnPlantEnter(PlantDetector detector, PlantEntity plant);
        void OnPlantStay(PlantDetector detector, PlantEntity plant);
        void OnPlantExit(PlantDetector detector, PlantEntity plant);
    }
}
