using PvZ.Core;
using UnityEngine;

namespace PvZ.Audio
{

    public sealed class SoundManager : SceneSingleton<SoundManager>
    {
        [SerializeField] private AudioSource bgm;
        [SerializeField] private AudioSource effect;
        [SerializeField] private GameSound sounds;

        protected override void OnSingletonAwake()
        {
            if (sounds == null)
            {
                sounds = Resources.Load<GameSound>(nameof(GameSound));
            }
        }

        protected override bool ValidateReferences()
        {
            var isValid = true;
            isValid &= RequireReference(bgm, nameof(bgm));
            isValid &= RequireReference(effect, nameof(effect));
            isValid &= RequireReference(sounds, $"Resources/{nameof(GameSound)}");
            return isValid;
        }

        protected override void OnSingletonStart()
        {
            bgm.clip = sounds.GetClip(SoundCue.BgmDay);
            if (bgm.clip == null) return;

            bgm.Play();
        }

        public void PlayEffect(SoundCue cue)
        {
            var audioClip = sounds.GetClip(cue);
            if (audioClip != null)
            {
                effect.PlayOneShot(audioClip);
            }
        }
    }

}
