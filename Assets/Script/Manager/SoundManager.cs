using System;
using System.Collections;
using System.Collections.Generic;
using Script.Manager;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;
    
    public AudioSource bgm;
    public AudioSource effect;

    public AudioClip clip;
    // Start is called before the first frame update

    void Awake()
    {
        Instance = this;
        
        bgm = GetComponents<AudioSource>()[0];
        effect = GetComponents<AudioSource>()[1];
    }

    void Start()
    {
        bgm.clip = MainGameManager.Instance.GetAudioClipByType(GameSound.SoundType.BGM_day);
        bgm.Play();
    }

    public void PlayEffect(GameSound.SoundType soundType)
    {
        effect.PlayOneShot(MainGameManager.Instance.GetAudioClipByType(soundType));
    }
}
