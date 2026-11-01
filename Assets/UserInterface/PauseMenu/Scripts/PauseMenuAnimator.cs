using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// All motion for the pause screen. Runs on unscaled time (the game is frozen while it plays).
//  Open:  the pause button ducks away, the backdrop dims, the stone card pops, the banner swings down and lands with two sparkles,
//         then the buttons pop up one after another.
//  Close: everything folds away quickly, then the game resumes and the pause button pops back.
//  Buttons squash while held and bounce when released; the music/sound icons wiggle when toggled.
// One timeline drives every part, so a quick Continue can never leave a half-shown button behind.
public class PauseMenuAnimator : MonoBehaviour
{
    [Header("Parts (set by the builder; found by name if left empty)")]
    public Image overlay;
    public RectTransform card;
    public RectTransform banner;
    public RectTransform[] buttons;

    [Header("Feel")]
    public float closeTime = 0.18f;

    public bool Closing { get; private set; }

    class Part
    {
        public RectTransform rect;
        public CanvasGroup group;
        public Press press;
        public Vector2 rest;
        public float scale = 1;
        public Vector2 offset;
        public float angle;
        public float kick;
    }

    GameObject panel;
    float overlayAlpha;
    Part cardPart, bannerPart, pausePart;
    readonly List<Part> buttonParts = new List<Part>();
    readonly List<Part> all = new List<Part>();
    Coroutine running;

    public void Setup(GameObject pausePanel, GameObject pauseButton)
    {
        panel = pausePanel;
        FindParts();

        if (overlay)
        {
            overlayAlpha = overlay.color.a;
        }
        if (card) cardPart = Add(card);
        if (banner) bannerPart = Add(banner);
        foreach (RectTransform button in buttons)
        {
            if (button)
            {
                buttonParts.Add(Add(button));
            }
        }
        if (pauseButton)
        {
            pausePart = Make((RectTransform)pauseButton.transform);
        }
    }

    // Fallback for scenes that still hold an older hand-made pause panel
    void FindParts()
    {
        if (!panel)
        {
            return;
        }
        if (!overlay) overlay = panel.GetComponent<Image>();
        var found = new List<RectTransform>();
        foreach (RectTransform t in panel.GetComponentsInChildren<RectTransform>(true))
        {
            if (t.parent == null || t == panel.transform) continue;
            if (!card && t.name == "Card") card = t;
            else if (!banner && t.name.Contains("Banner")) banner = t;
            else if (t.GetComponent<Button>()) found.Add(t);
        }
        if (buttons == null || buttons.Length == 0) buttons = found.ToArray();
    }

    Part Add(RectTransform rect)
    {
        Part part = Make(rect);
        all.Add(part);
        return part;
    }

    Part Make(RectTransform rect)
    {
        Part part = new Part { rect = rect, rest = rect.anchoredPosition };
        part.group = rect.GetComponent<CanvasGroup>();
        if (!part.group)
        {
            part.group = rect.gameObject.AddComponent<CanvasGroup>();
        }
        Button button = rect.GetComponent<Button>();
        if (button)
        {
            part.press = rect.GetComponent<Press>();
            if (!part.press)
            {
                part.press = rect.gameObject.AddComponent<Press>();
            }
            part.press.released = () => StartCoroutine(Kick(part));
            if (rect.GetComponent<AudioToggleButton>())
            {
                Transform icon = rect.Find("Icon");
                if (icon)
                {
                    button.onClick.AddListener(() => StartCoroutine(Wiggle(icon)));
                }
            }
        }
        return part;
    }

    // ---- public API (called by PauseMenu) ----

    public void HideInstant()
    {
        Stop();
        Closing = false;
        panel.SetActive(false);
        if (pausePart != null)
        {
            pausePart.rect.gameObject.SetActive(true);
            pausePart.scale = 1;
            pausePart.group.alpha = 1;
            pausePart.group.interactable = true;
        }
    }

    public void Open()
    {
        Closing = false;
        Run(OpenRoutine());
    }

    // Folds the menu away, then calls done (which resumes the game)
    public void Close(Action done)
    {
        if (Closing)
        {
            return;
        }
        Closing = true;
        Run(CloseRoutine(done));
    }

    // Restart / Give Up: lock the buttons; the screen change covers the rest
    public void Leave()
    {
        foreach (Part part in all)
        {
            part.group.interactable = false;
        }
    }

    // ---- timelines ----

    void Run(IEnumerator routine)
    {
        Stop();
        running = StartCoroutine(routine);
    }

    void Stop()
    {
        if (running != null)
        {
            StopCoroutine(running);
            running = null;
        }
    }

    IEnumerator OpenRoutine()
    {
        panel.SetActive(true);
        foreach (Part part in all)
        {
            part.group.alpha = 0;
            part.group.interactable = true;
            part.kick = 0;
        }
        SetOverlay(0);

        // Pause button ducks away as the menu comes in
        if (pausePart != null)
        {
            pausePart.group.interactable = false;
        }
        float pauseFrom = pausePart != null ? pausePart.scale : 0;

        float buttonsStart = 0.2f, buttonGap = 0.05f, buttonTime = 0.3f;
        float total = buttonsStart + buttonGap * Mathf.Max(0, buttonParts.Count - 1) + buttonTime;
        bool sparkled = false;

        yield return FxKit.Tween(total, p =>
        {
            float t = p * total;

            if (pausePart != null)
            {
                float k = Local(t, 0, 0.12f);
                pausePart.scale = Mathf.Lerp(pauseFrom, 0.4f, k * k);
                pausePart.group.alpha = 1 - k;
            }

            SetOverlay(FxKit.EaseOut(Local(t, 0, 0.22f)));

            if (cardPart != null)
            {
                float k = Local(t, 0.02f, 0.34f);
                cardPart.scale = Mathf.LerpUnclamped(0.55f, 1, BackOut(k, 0.5f));
                cardPart.group.alpha = Mathf.Clamp01(k * 5);
            }

            if (bannerPart != null)
            {
                // Drops in from above and swings to rest like a hanging sign
                float k = Local(t, 0.1f, 0.45f);
                bannerPart.offset = new Vector2(0, 150 * (1 - BackOut(k, 0.55f)));
                bannerPart.angle = k <= 0 ? -8 : -8 * Mathf.Exp(-5 * k) * Mathf.Cos(13 * k) * (1 - k);
                bannerPart.group.alpha = Mathf.Clamp01(k * 6);
                if (!sparkled && k >= 0.45f)
                {
                    sparkled = true;
                    StartCoroutine(BannerSparkles());
                }
            }

            for (int i = 0; i < buttonParts.Count; i++)
            {
                float k = Local(t, buttonsStart + i * buttonGap, buttonTime);
                buttonParts[i].scale = Mathf.LerpUnclamped(0.4f, 1, BackOut(k, 0.6f));
                buttonParts[i].group.alpha = Mathf.Clamp01(k * 4);
            }
        });

        if (pausePart != null)
        {
            pausePart.rect.gameObject.SetActive(false);
        }
        running = null;
    }

    IEnumerator CloseRoutine(Action done)
    {
        int n = all.Count;
        float[] alpha = new float[n], scale = new float[n];
        Vector2[] offset = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            alpha[i] = all[i].group.alpha;
            scale[i] = all[i].scale;
            offset[i] = all[i].offset;
            all[i].group.interactable = false;
        }
        float overlayFrom = overlay ? overlay.color.a / Mathf.Max(overlayAlpha, 0.001f) : 1;

        yield return FxKit.Tween(closeTime, p =>
        {
            float e = p * p;
            for (int i = 0; i < n; i++)
            {
                Part part = all[i];
                part.group.alpha = alpha[i] * (1 - e);
                part.scale = scale[i] * (1 - 0.12f * e);
                part.offset = offset[i] + (part == bannerPart ? new Vector2(0, 50 * e) : Vector2.zero);
            }
            SetOverlay(overlayFrom * (1 - e));
        });

        panel.SetActive(false);
        Closing = false;
        running = null;
        done();

        if (pausePart != null && !IsGameOverHidden())
        {
            pausePart.rect.gameObject.SetActive(true);
            pausePart.group.interactable = true;
            yield return FxKit.Tween(0.3f, p =>
            {
                pausePart.scale = Mathf.LerpUnclamped(0.4f, 1, BackOut(p, 0.6f));
                pausePart.group.alpha = Mathf.Clamp01(p * 4);
            });
        }
    }

    bool IsGameOverHidden()
    {
        return WinLossManager.instance && (WinLossManager.instance.winner || WinLossManager.instance.loser);
    }

    // Two small gold sparkles flash at the ribbon's tails as it lands
    IEnumerator BannerSparkles()
    {
        if (!banner)
        {
            yield break;
        }
        float half = banner.rect.width * 0.5f;
        var sparks = new List<Image>();
        foreach (float side in new[] { -1f, 1f })
        {
            Image s = FxKit.NewImage("Sparkle", banner, FxKit.Sparkle, new Color(1f, 0.86f, 0.4f, 0));
            s.rectTransform.anchorMin = s.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            s.rectTransform.anchoredPosition = new Vector2(side * (half - 60), 50);
            s.rectTransform.sizeDelta = new Vector2(70, 70);
            sparks.Add(s);
        }
        yield return FxKit.Tween(0.4f, p =>
        {
            float a = Mathf.Sin(p * Mathf.PI);
            for (int i = 0; i < sparks.Count; i++)
            {
                if (!sparks[i]) continue;
                sparks[i].color = FxKit.WithAlpha(sparks[i].color, a);
                sparks[i].rectTransform.localScale = Vector3.one * (0.4f + 0.8f * a);
                sparks[i].rectTransform.localRotation = Quaternion.Euler(0, 0, (i == 0 ? -1 : 1) * 90 * p);
            }
        });
        foreach (Image s in sparks)
        {
            if (s) Destroy(s.gameObject);
        }
    }

    // Little bounce when a held button is let go
    IEnumerator Kick(Part part)
    {
        yield return FxKit.Tween(0.25f, t => part.kick = Mathf.Sin(t * Mathf.PI) * (1 - t) * 0.12f);
        part.kick = 0;
    }

    // Music / sound icon shakes when toggled
    IEnumerator Wiggle(Transform icon)
    {
        yield return FxKit.Tween(0.35f, t =>
        {
            if (icon) icon.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * Mathf.PI * 4) * 16 * (1 - t));
        });
        if (icon) icon.localRotation = Quaternion.identity;
    }

    // ---- per-frame ----

    private void LateUpdate()
    {
        if (!panel)
        {
            return;
        }
        if (panel.activeInHierarchy)
        {
            foreach (Part part in all)
            {
                Apply(part);
            }
        }
        if (pausePart != null)
        {
            Apply(pausePart);
        }
    }

    void Apply(Part part)
    {
        float squash = part.press ? part.press.Amount : 0;
        part.rect.localScale = Vector3.one * (part.scale * (1 + part.kick) * (1 - 0.07f * squash));
        part.rect.anchoredPosition = part.rest + part.offset;
        part.rect.localRotation = Quaternion.Euler(0, 0, part.angle);
    }

    void SetOverlay(float amount)
    {
        if (overlay)
        {
            overlay.color = FxKit.WithAlpha(overlay.color, overlayAlpha * amount);
        }
    }

    // 0..1 progress of a step that starts at `start` and lasts `length` seconds
    static float Local(float t, float start, float length)
    {
        return Mathf.Clamp01((t - start) / length);
    }

    // Ease out that overshoots past 1 then settles
    static float BackOut(float t, float overshoot)
    {
        float s = 1.70158f * overshoot * 2;
        t -= 1;
        return t * t * ((s + 1) * t + s) + 1;
    }

    // Tracks a finger held on a button (eased 0..1)
    public class Press : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public Action released;
        public float Amount { get; private set; }
        bool held;

        public void OnPointerDown(PointerEventData eventData) { held = true; }
        public void OnPointerExit(PointerEventData eventData) { held = false; }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (held && isActiveAndEnabled)
            {
                released?.Invoke();
            }
            held = false;
        }

        private void Update()
        {
            Amount = Mathf.Lerp(Amount, held ? 1 : 0, 1 - Mathf.Exp(-Time.unscaledDeltaTime * 25f));
        }

        private void OnDisable()
        {
            held = false;
            Amount = 0;
        }
    }
}
