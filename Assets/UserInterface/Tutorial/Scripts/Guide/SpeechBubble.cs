using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArcaneKeepers.Tutorial
{
    /// <summary>
    /// Dog_Hero's speech bubble: pops out of his head, types each line in with a little bob per word,
    /// resizes smoothly between lines and shows a bouncing arrow when the player can tap to continue.
    /// Keywords can be wrapped in &lt;k&gt;...&lt;/k&gt; to colour them.
    /// </summary>
    public class SpeechBubble : MonoBehaviour
    {
        public const float Width = 660f;
        const float PadX = 46f, PadTop = 34f, PadBottom = 40f;
        const float MinHeight = 140f;

        TutorialTheme theme;
        DogGuide dog;
        RectTransform root;      // anchored like the dog (bottom-right), pivot at the tail
        RectTransform panel;     // scales for pop in / out
        Image shadow, face, tail;
        TextMeshProUGUI text;
        Image nextArrow;

        float height = MinHeight;
        float heightTarget = MinHeight;
        bool skipTyping;
        float shown;             // 0..1 pop
        float arrowAlpha;
        bool arrowOn;
        int version;

        public bool IsTyping { get; private set; }
        public bool IsOpen => shown > 0.01f;

        public void Init(TutorialTheme t, DogGuide guide, RectTransform parent)
        {
            theme = t;
            dog = guide;
            root = (RectTransform)transform;
            root.SetParent(parent, false);
            root.anchorMin = root.anchorMax = new Vector2(1f, 0f);
            root.pivot = new Vector2(1f, 0f);
            root.sizeDelta = new Vector2(Width, MinHeight);

            panel = TutorialKit.Rect("Panel", root, Vector2.zero);
            TutorialKit.Stretch(panel);
            // grow out of the tail's corner
            panel.pivot = new Vector2(0.86f, 0f);

            shadow = TutorialKit.Img("Shadow", panel, theme.bubbleShadow, TutorialKit.A(theme.ink, 0.28f), Vector2.zero, true);
            TutorialKit.Stretch(shadow.rectTransform);
            shadow.rectTransform.anchoredPosition = new Vector2(0f, -10f);

            face = TutorialKit.Img("Bubble", panel, theme.bubble, Color.white, Vector2.zero, true);
            TutorialKit.Stretch(face.rectTransform);

            tail = TutorialKit.Img("Tail", panel, theme.bubbleTail, Color.white, new Vector2(128f, 112f));
            tail.preserveAspect = true;
            RectTransform tr = tail.rectTransform;
            tr.anchorMin = tr.anchorMax = new Vector2(1f, 0f);
            tr.pivot = new Vector2(0f, 1f);
            tr.anchoredPosition = new Vector2(-150f, 14f);

            text = TutorialKit.Text("Text", panel, theme, "", 44f, theme.ink);
            text.alignment = TextAlignmentOptions.TopLeft;
            text.lineSpacing = -6f;
            RectTransform tx = text.rectTransform;
            tx.anchorMin = new Vector2(0f, 0f);
            tx.anchorMax = new Vector2(1f, 1f);
            tx.offsetMin = new Vector2(PadX, PadBottom * 0.6f);
            tx.offsetMax = new Vector2(-PadX, -PadTop);

            nextArrow = TutorialKit.Img("Next", panel, theme.nextArrow, Color.white, new Vector2(36f, 30f));
            nextArrow.rectTransform.anchorMin = nextArrow.rectTransform.anchorMax = new Vector2(1f, 0f);
            nextArrow.rectTransform.anchoredPosition = new Vector2(-46f, 34f);

            panel.localScale = Vector3.zero;
            shown = 0f;
        }

        /// <summary>Places the bubble so its tail points down at <paramref name="tip"/> (dog-parent space).</summary>
        public void PointTailAt(Vector2 tip)
        {
            // the tail's tip sits 36px left of / 96px under the bubble's bottom-right corner
            root.anchoredPosition = tip + new Vector2(36f, 96f);
        }

        // ------------------------------------------------------------ dialogue

        /// <summary>Pops open (if needed) and types the line. Finishes when the whole line is visible.</summary>
        public IEnumerator Say(string line, bool canContinue)
        {
            int my = ++version;
            arrowOn = false;
            string rich = Format(line);
            text.text = rich;
            text.maxVisibleCharacters = 0;
            text.ForceMeshUpdate();
            heightTarget = Mathf.Max(MinHeight, text.GetPreferredValues(rich, Width - PadX * 2f, 0f).y + PadTop + PadBottom);

            if (!IsOpen)
            {
                height = heightTarget;
                ApplySize();
                yield return Pop(true);
            }

            IsTyping = true;
            skipTyping = false;
            int total = text.textInfo.characterCount;
            float clock = 0f;
            int visible = 0;
            const float cps = 52f;
            while (visible < total)
            {
                if (my != version)
                {
                    yield break; // a newer line took over
                }
                if (skipTyping)
                {
                    visible = total;
                    break;
                }
                clock += Time.unscaledDeltaTime * cps;
                while (clock >= 1f && visible < total)
                {
                    clock -= 1f;
                    char c = text.textInfo.characterInfo[visible].character;
                    visible++;
                    if (c == ' ' || visible == 1)
                    {
                        dog.TalkBeat();
                        TutorialKit.Play(theme, theme.talkBlip, 0.5f, Random.Range(0.92f, 1.12f));
                    }
                    // tiny pauses on punctuation make the lines read naturally
                    if (c == '.' || c == '!' || c == '?')
                    {
                        clock -= 7f;
                    }
                    else if (c == ',')
                    {
                        clock -= 3.5f;
                    }
                }
                text.maxVisibleCharacters = visible;
                yield return null;
            }
            if (my != version)
            {
                yield break;
            }
            text.maxVisibleCharacters = total;
            IsTyping = false;
            arrowOn = canContinue;
        }

        /// <summary>Completes the current line instantly (first tap while typing).</summary>
        public void SkipTyping()
        {
            skipTyping = true;
        }

        public IEnumerator Close()
        {
            version++;
            arrowOn = false;
            if (IsOpen)
            {
                yield return Pop(false);
            }
        }

        public void SetContinueArrow(bool on) => arrowOn = on;

        IEnumerator Pop(bool open)
        {
            if (open)
            {
                TutorialKit.Play(theme, theme.popSound, 0.6f, 1.15f);
                yield return TutorialKit.Tween(0.3f, p => { shown = p; panel.localScale = Vector3.one * TutorialKit.EaseOutBack(p, 2.2f); });
            }
            else
            {
                yield return TutorialKit.Tween(0.16f, p => { shown = 1f - p; panel.localScale = Vector3.one * Mathf.Lerp(1f, 0.6f, TutorialKit.EaseIn(p)); });
                panel.localScale = Vector3.zero;
                shown = 0f;
            }
        }

        string Format(string line)
        {
            string hex = ColorUtility.ToHtmlStringRGB(theme.keyword);
            return line.Replace("<k>", "<color=#" + hex + ">").Replace("</k>", "</color>");
        }

        void ApplySize()
        {
            root.sizeDelta = new Vector2(Width, height);
        }

        void Update()
        {
            if (!root)
            {
                return;
            }
            // smooth resize between lines (with a hint of overshoot)
            if (Mathf.Abs(height - heightTarget) > 0.5f)
            {
                height = Mathf.Lerp(height, heightTarget, TutorialKit.Damp(14f));
                ApplySize();
            }

            arrowAlpha = Mathf.MoveTowards(arrowAlpha, arrowOn ? 1f : 0f, Time.unscaledDeltaTime * 6f);
            float bob = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4.2f)) * 8f;
            nextArrow.rectTransform.anchoredPosition = new Vector2(-46f, 30f + bob);
            nextArrow.color = TutorialKit.A(Color.white, arrowAlpha);
        }
    }
}
