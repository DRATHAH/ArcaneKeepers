using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Motion for the pause screen (added by PauseMenu at runtime, so every level gets it):
// the overlay fades in, the card settles, the banner drops in and the buttons pop up one after another.
// Continue plays a quick fold-away before the game resumes, then the pause button pops back.
// Buttons squash while held and bounce on release. Runs on unscaled time (the game is frozen).
public class PauseMenuAnimator : MonoBehaviour
{
    public float stagger = 0.04f;
    public float popTime = 0.3f;
    public float closeTime = 0.16f;

    public bool Closing { get; private set; }

    class Item
    {
        public RectTransform rect;
        public CanvasGroup group;
        public Press press;
        public Vector2 rest;
        public bool banner;
        public bool backdrop;   // the card behind the buttons: settles instead of popping
        public float scale = 1;
        public float offset;
        public float kick;
    }

    Image overlay;
    float overlayAlpha;
    GameObject panel;
    Item pauseButton;
    readonly List<Item> items = new List<Item>();
    Coroutine running;

    public void Setup(GameObject pausePanel, GameObject button)
    {
        panel = pausePanel;
        overlay = panel.GetComponent<Image>();
        if (overlay)
        {
            overlayAlpha = overlay.color.a;
        }

        // Items are the children of the panel's safe area (or of the panel itself)
        Transform content = panel.transform.childCount == 1 && panel.transform.GetChild(0).GetComponent<SafeArea>()
            ? panel.transform.GetChild(0)
            : panel.transform;
        foreach (RectTransform child in content)
        {
            Item item = MakeItem(child);
            item.banner = child.name.Contains("Banner");
            item.backdrop = items.Count == 0 && !child.GetComponent<Button>() && !item.banner;
            items.Add(item);
        }
        if (button)
        {
            pauseButton = MakeItem((RectTransform)button.transform);
        }
    }

    Item MakeItem(RectTransform rect)
    {
        Item item = new Item { rect = rect, rest = rect.anchoredPosition };
        item.group = rect.GetComponent<CanvasGroup>();
        if (!item.group)
        {
            item.group = rect.gameObject.AddComponent<CanvasGroup>();
        }
        if (rect.GetComponent<Button>())
        {
            item.press = rect.gameObject.AddComponent<Press>();
            item.press.released = () => StartCoroutine(Kick(item));
        }
        return item;
    }

    public void Open()
    {
        Closing = false;
        Restart(OpenRoutine());
    }

    // Folds the menu away, then calls done (which hides the panel and resumes the game)
    public void Close(Action done)
    {
        if (Closing)
        {
            return;
        }
        Closing = true;
        Restart(CloseRoutine(done));
    }

    void Restart(IEnumerator routine)
    {
        if (running != null)
        {
            StopCoroutine(running);
        }
        running = StartCoroutine(routine);
    }

    IEnumerator OpenRoutine()
    {
        SetOverlay(0);
        foreach (Item item in items)
        {
            item.group.alpha = 0;
            item.group.interactable = true;
            item.scale = item.backdrop ? 0.9f : item.banner ? 1 : 0.5f;
            item.offset = item.banner ? 90 : 0;
        }

        for (int i = 0; i < items.Count; i++)
        {
            StartCoroutine(Enter(items[i], i * stagger));
        }
        yield return FxKit.Tween(0.2f, t => SetOverlay(FxKit.EaseOut(t)));
        running = null;
    }

    IEnumerator Enter(Item item, float delay)
    {
        yield return FxKit.Wait(delay);
        float from = item.scale;
        yield return FxKit.Tween(popTime, t =>
        {
            if (Closing)
            {
                return;
            }
            item.group.alpha = Mathf.Clamp01(t * 4);
            if (item.banner)
            {
                item.offset = 90 * (1 - BackOut(t, 0.7f));
            }
            else
            {
                item.scale = Mathf.LerpUnclamped(from, 1, BackOut(t, item.backdrop ? 0.3f : 0.6f));
            }
        });
    }

    IEnumerator CloseRoutine(Action done)
    {
        float[] startAlpha = new float[items.Count];
        float[] startScale = new float[items.Count];
        for (int i = 0; i < items.Count; i++)
        {
            startAlpha[i] = items[i].group.alpha;
            startScale[i] = items[i].scale;
            items[i].group.interactable = false;
        }
        float startOverlay = overlay ? overlay.color.a / Mathf.Max(overlayAlpha, 0.001f) : 1;

        yield return FxKit.Tween(closeTime, t =>
        {
            float e = t * t;
            for (int i = 0; i < items.Count; i++)
            {
                items[i].group.alpha = startAlpha[i] * (1 - e);
                items[i].scale = startScale[i] * (1 - 0.1f * e);
            }
            SetOverlay(startOverlay * (1 - e));
        });

        running = null;
        Closing = false;
        done();
        if (pauseButton != null && pauseButton.rect.gameObject.activeInHierarchy)
        {
            StartCoroutine(PopPauseButton());
        }
    }

    IEnumerator PopPauseButton()
    {
        yield return FxKit.Tween(0.3f, t =>
        {
            pauseButton.scale = BackOut(t, 0.6f);
            pauseButton.group.alpha = Mathf.Clamp01(t * 4);
        });
    }

    // Little bounce when a held button is let go
    IEnumerator Kick(Item item)
    {
        yield return FxKit.Tween(0.25f, t => item.kick = Mathf.Sin(t * Mathf.PI) * (1 - t) * 0.12f);
        item.kick = 0;
    }

    void SetOverlay(float amount)
    {
        if (overlay)
        {
            overlay.color = FxKit.WithAlpha(overlay.color, overlayAlpha * amount);
        }
    }

    private void Update()
    {
        if (!panel)
        {
            return;
        }
        float time = Time.unscaledTime;
        if (panel.activeInHierarchy)
        {
            foreach (Item item in items)
            {
                float bob = item.banner && !Closing ? Mathf.Sin(time * 2f) * 5f : 0;
                Apply(item, bob);
            }
        }
        if (pauseButton != null)
        {
            Apply(pauseButton, 0);
        }
    }

    void Apply(Item item, float bob)
    {
        float squash = item.press ? item.press.Amount : 0;
        item.rect.localScale = Vector3.one * (item.scale * (1 + item.kick) * (1 - 0.07f * squash));
        item.rect.anchoredPosition = item.rest + Vector2.up * (item.offset + bob);
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
