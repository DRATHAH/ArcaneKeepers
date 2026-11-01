using UnityEngine;
using UnityEngine.UI;

// Music / Sound button: toggles the setting and greys itself out when off
[RequireComponent(typeof(Button))]
public class AudioToggleButton : MonoBehaviour
{
    public AudioChannel.Channel channel;
    public Color offColor = new Color(0.35f, 0.35f, 0.35f, 0.8f);
    [Tooltip("Optional: shown while the setting is off (e.g. a slash over the icon).")]
    public GameObject offMarker;

    Image image;

    private void Awake()
    {
        image = GetComponent<Image>();
        GetComponent<Button>().onClick.AddListener(Toggle);
    }

    private void OnEnable()
    {
        GameAudio.OnSettingsChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        GameAudio.OnSettingsChanged -= Refresh;
    }

    void Toggle()
    {
        if (channel == AudioChannel.Channel.Music)
        {
            GameAudio.ToggleMusic();
        }
        else
        {
            GameAudio.ToggleSound();
        }
    }

    void Refresh()
    {
        bool enabled = channel == AudioChannel.Channel.Music ? GameAudio.MusicEnabled : GameAudio.SoundEnabled;
        image.color = enabled ? Color.white : offColor;
        if (offMarker)
        {
            offMarker.SetActive(!enabled);
        }
    }
}
