using System;
using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

public class SoundManager : MonoBehaviour
{
    public Sound[] sounds;
    public bool musicEnabled = true;
    public GameObject soundObject;

    #region Singleton
    private static SoundManager instance;
    public static SoundManager Instance
    {
        get => instance;
        private set
        {
            if (instance == null)
            {
                instance = value;
            }
            else if (instance != value)
            {
                Debug.Log($"{nameof(SoundManager)} instance already exists, destoying duplicate");
            }
        }
    }
    #endregion

    void Awake()
    {
        Instance = this;

        foreach (Sound s in sounds)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;

            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
            s.source.loop = s.loop;
        }
    }

    private void Start()
    {

    }

    public void PlaySound(string name)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s != null)
        {
            GameObject newSound = Instantiate(soundObject);
            newSound.GetComponent<SoundObject>().Initialize(s);
        }
        else
        {
            Debug.LogWarning("Sound: " + name + " not found!");
        }
    }

    public void StopSound(string name)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s != null && s.source.isPlaying)
        {
            s.source.Stop();
        }
        else
        {
            Debug.LogWarning("Sound: " + name + " is being stopped!");
        }
    }
}
