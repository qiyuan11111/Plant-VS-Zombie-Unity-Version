using Script.Manager;
using UnityEngine;

public class SoundManager : SceneSingleton<SoundManager>
{
    [SerializeField] private AudioSource bgm;
    [SerializeField] private AudioSource effect;

    protected override bool ValidateReferences()
    {
        var isValid = true;
        isValid &= RequireReference(bgm, nameof(bgm));
        isValid &= RequireReference(effect, nameof(effect));
        return isValid;
    }

    protected override bool ValidateDependencies()
    {
        return RequireManager(MainGameManager.Instance);
    }

    protected override void OnSingletonStart()
    {
        bgm.clip = MainGameManager.Instance.GetAudioClipByType(GameSound.SoundType.BGM_day);
        if (bgm.clip == null) return;

        bgm.Play();
    }

    public void PlayEffect(GameSound.SoundType soundType)
    {
        var audioClip = MainGameManager.Instance.GetAudioClipByType(soundType);
        if (audioClip != null)
        {
            effect.PlayOneShot(audioClip);
        }
    }
}
