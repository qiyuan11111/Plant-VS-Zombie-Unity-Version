using UnityEngine;
using UnityEngine.Serialization;

namespace PvZ.Audio
{
    [CreateAssetMenu(fileName = "GameSound", menuName = "PvZ/Game Sound", order = 2)]
    public sealed class GameSound : ScriptableObject
    {
        [FormerlySerializedAs("BGM_day")]
        [SerializeField, Tooltip("背景音乐")]
        private AudioClip bgmDay;

        [FormerlySerializedAs("Plant")]
        [SerializeField, Tooltip("种下植物时的音效")]
        private AudioClip plant;

        [FormerlySerializedAs("SeedLift")]
        [SerializeField, Tooltip("从卡槽选择植物卡片的声音")]
        private AudioClip seedLift;

        [FormerlySerializedAs("Points")]
        [SerializeField, Tooltip("收集阳光的声音")]
        private AudioClip points;

        public AudioClip GetClip(SoundCue cue)
        {
            return cue switch
            {
                SoundCue.BgmDay => bgmDay,
                SoundCue.Plant => plant,
                SoundCue.SeedLift => seedLift,
                SoundCue.Points => points,
                _ => null
            };
        }
    }
}
