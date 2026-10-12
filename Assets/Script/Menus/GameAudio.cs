using System;
using UnityEngine;

// Global music / sound on-off settings, saved between sessions
public static class GameAudio
{
    const string MusicKey = "MusicEnabled";
    const string SoundKey = "SoundEnabled";

    public static event Action OnSettingsChanged;

    public static bool MusicEnabled
    {
        get { return PlayerPrefs.GetInt(MusicKey, 1) == 1; }
        set { Save(MusicKey, value); }
    }

    public static bool SoundEnabled
    {
        get { return PlayerPrefs.GetInt(SoundKey, 1) == 1; }
        set { Save(SoundKey, value); }
    }

    public static void ToggleMusic() { MusicEnabled = !MusicEnabled; }
    public static void ToggleSound() { SoundEnabled = !SoundEnabled; }

    static void Save(string key, bool enabled)
    {
        PlayerPrefs.SetInt(key, enabled ? 1 : 0);
        PlayerPrefs.Save();
        OnSettingsChanged?.Invoke();
    }

    // Domain reload is disabled in the editor, so clear subscribers between play sessions
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        OnSettingsChanged = null;
    }
}
