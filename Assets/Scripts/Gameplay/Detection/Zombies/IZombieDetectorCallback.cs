using PvZ.Core.Detection;
using PvZ.Gameplay.Zombies;

namespace PvZ.Gameplay.Detection.Zombies
{
    public interface IZombieDetectorCallback : IDetectorCallback
    {
        void OnLoad(ZombieDetector detector);
        void OnZombieEnter(ZombieDetector detector, ZombieEntity zombie);
        void OnZombieStay(ZombieDetector detector, ZombieEntity zombie);
        void OnZombieExit(ZombieDetector detector, ZombieEntity zombie);
    }
}
