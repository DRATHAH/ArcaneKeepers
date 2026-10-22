using UnityEngine;
using UnityEngine.UI;

// One stat row under the keeper's name: a short label and five chunky pips.
// When the value changes the filled pips pop in one after another.
public class StatPips : MonoBehaviour
{
    [Tooltip("The gold fill inside each pip, left to right")]
    public Image[] fills;
    public float popDelay = 0.05f;

    int value;
    float sinceChange = 10f;

    public void Set(int newValue, bool animate)
    {
        value = Mathf.Clamp(newValue, 0, fills.Length);
        sinceChange = animate ? 0 : 10f;
        Apply();
    }

    private void Update()
    {
        if (sinceChange < 2f)
        {
            sinceChange += Time.unscaledDeltaTime;
            Apply();
        }
    }

    void Apply()
    {
        for (int i = 0; i < fills.Length; i++)
        {
            bool on = i < value;
            fills[i].enabled = on;
            if (!on)
            {
                continue;
            }
            // 0 -> overshoot -> 1, staggered per pip
            float t = Mathf.Clamp01((sinceChange - i * popDelay) / 0.22f);
            float s = t >= 1 ? 1 : Mathf.Sin(t * Mathf.PI * 0.5f) + Mathf.Sin(t * Mathf.PI) * 0.25f;
            fills[i].rectTransform.localScale = new Vector3(s, s, 1);
        }
    }
}
