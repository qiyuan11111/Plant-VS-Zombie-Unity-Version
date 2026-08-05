using UnityEngine;

namespace PvZ.Audio
{

    [CreateAssetMenu(fileName = "GameSound", menuName = "GameSound", order = 2)]
    public class GameSound : ScriptableObject
    {
        [Tooltip("背景音乐")]
        public AudioClip BGM_day;

        [Tooltip("种下植物时的音效")]
        public AudioClip Plant;

        [Tooltip("从卡槽选择植物卡片的声音")]
        public AudioClip SeedLift;

        [Tooltip("收集阳光的声音")]
        public AudioClip Points;

        public enum SoundType
        {
            None,
            BGM_day,
            Plante,
            SeedLift,
            Points
        }

        public AudioClip GetAudioClipByType(SoundType soundType)
        {
            return soundType switch
            {
                SoundType.BGM_day => BGM_day,
                SoundType.Plante => Plant,
                SoundType.SeedLift => SeedLift,
                SoundType.Points => Points,
                _ => null
            };
        }
    }

}
