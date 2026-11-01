using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Small glowing magic motes drifting up across the menu.
/// Fixed pool, nothing allocated after start.
/// </summary>
public class MagicMotes : MenuFxLayer
{
    [SerializeField] private int count = 12;
    [SerializeField] private Color[] colors =
    {
        new Color(1f, 0.86f, 0.4f),   // gold
        new Color(1f, 0.95f, 0.7f),   // pale gold
        new Color(0.55f, 0.85f, 1f),  // arcane blue
    };

    private class Mote
    {
        public RectTransform rt;
        public Image img;
        public Color color;
        public float x, y, speed, sway, phase, size, life, age;
    }

    private const float MaxAlpha = 0.85f;
    private Mote[] motes;

    protected override void Awake()
    {
        base.Awake();
        motes = new Mote[count];
        for (int i = 0; i < motes.Length; i++)
        {
            var img = FxKit.NewImage("Mote", transform, FxKit.Glow, Color.clear);
            var m = new Mote { img = img, rt = img.rectTransform };
            Respawn(m);
            m.age = Random.Range(0f, m.life); // start mid-flight so the screen isn't empty
            motes[i] = m;
        }
    }

    private void Respawn(Mote m)
    {
        m.color = colors[Random.Range(0, colors.Length)];
        m.x = Random.Range(0.02f, 0.98f);
        m.y = Random.Range(-0.05f, 0.5f);
        m.phase = Random.Range(0f, 10f);
        m.age = 0f;
        m.speed = Random.Range(0.03f, 0.06f);    // screen heights per second
        m.sway = Random.Range(0.006f, 0.018f);
        m.size = Random.Range(0.035f, 0.065f);   // screen heights
        m.life = Random.Range(6f, 10f);
    }

    protected override void Tick(float dt)
    {
        Rect r = Layer.rect;
        foreach (var m in motes)
        {
            m.age += dt;
            if (m.age >= m.life) Respawn(m);
            float x = m.x + Mathf.Sin(m.age * 1.3f + m.phase) * m.sway * r.height / r.width;
            float y = m.y + m.speed * m.age;
            m.rt.anchoredPosition = new Vector2(x * r.width, y * r.height);
            float twinkle = 0.75f + 0.25f * Mathf.Sin(m.age * 6f + m.phase);
            m.rt.sizeDelta = Vector2.one * m.size * r.height * twinkle;
            m.img.color = FxKit.WithAlpha(m.color, Mathf.Sin(m.age / m.life * Mathf.PI) * MaxAlpha);
        }
    }
}
