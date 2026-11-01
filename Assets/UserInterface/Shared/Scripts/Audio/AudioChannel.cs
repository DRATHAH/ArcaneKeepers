using UnityEngine;

// Put this on any AudioSource so the pause menu's Music / Sound buttons can mute it
[RequireComponent(typeof(AudioSource))]
public class AudioChannel : MonoBehaviour
{
    public enum Channel { Music, Sound }

    public Channel channel = Channel.Sound;

    AudioSource source;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        GameAudio.OnSettingsChanged += Apply;
        Apply();
    }

    private void OnDisable()
    {
        GameAudio.OnSettingsChanged -= Apply;
    }

    void Apply()
    {
        bool enabled = channel == Channel.Music ? GameAudio.MusicEnabled : GameAudio.SoundEnabled;
        source.mute = !enabled;
    }
}
