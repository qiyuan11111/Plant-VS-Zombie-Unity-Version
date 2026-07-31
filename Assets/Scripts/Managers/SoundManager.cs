using Script.Manager;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;
    
    [SerializeField] private AudioSource bgm;
    [SerializeField] private AudioSource effect;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (bgm == null) return;

        bgm.clip = MainGameManager.Instance.GetAudioClipByType(GameSound.SoundType.BGM_day);
        if (bgm.clip == null) return;

        bgm.Play();
    }

    public void PlayEffect(GameSound.SoundType soundType)
    {
        if (effect == null) return;

        var audioClip = MainGameManager.Instance.GetAudioClipByType(soundType);
        if (audioClip != null)
        {
            effect.PlayOneShot(audioClip);
        }
    }
}
