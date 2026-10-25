using TMPro;
using UnityEngine;

/// <summary>
/// "Tap to play" breathes: a gentle grow + brighten loop, so the call to action reads as alive.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class PromptPulse : MonoBehaviour
{
    [SerializeField] private float speed = 2.6f;
    [SerializeField] private float grow = 0.06f;
    [SerializeField] private float minAlpha = 0.7f;

    private TMP_Text text;

    private void Awake() => text = GetComponent<TMP_Text>();

    private void Update()
    {
        float b = Mathf.Sin(Time.unscaledTime * speed) * 0.5f + 0.5f;
        float s = 1f + grow * FxKit.Smooth(b);
        text.rectTransform.localScale = new Vector3(s, s, 1f);
        text.alpha = Mathf.Lerp(minAlpha, 1f, b);
    }
}
