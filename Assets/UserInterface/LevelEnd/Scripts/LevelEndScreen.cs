using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The win / lose finale, played on top of the frozen level (no separate scene).
// WinLossManager loads it from Resources/LevelEndScreen and calls PlayWin / PlayLose, so every
// level gets it without scene edits.
//
// LOSE: the game slows to a stop and the camera leans in, a cartoon iris closes on the enemy that
//   broke through, holds on it, then shuts. The headless cat wizard drops in, his head lands a
//   moment later, bounces and rolls to a stop with dizzy stars, the neck gushes, the title drops
//   in letter by letter and the buttons pop up. Try Again closes the iris over the screen and
//   opens it again on the restarted level.
// WIN: the reward that appeared flashes, its light rays fly up behind the title, VICTORY!
//   bounces in, confetti cannons fire, the reward cards deal in and flip over one by one, then
//   the buttons pop up.
// Everything runs on unscaled time.
public class LevelEndScreen : MonoBehaviour
{
    public const string ResourcePath = "LevelEndScreen";

    [Header("Art")]
    public TMP_FontAsset font;
    public Material chunkyMaterial;
    public Sprite blueButton;
    public Sprite goldButton;
    [Tooltip("White + Ink outline button icons, shared with the pause menu (MenuBuilder.BuildLevelEndIcons)")]
    public Sprite playIcon, restartIcon, levelsIcon;
    [Tooltip("Dead_Cat split into layers that stack exactly (AgentScripts/SplitDeadCat.py)")]
    public Sprite catBody, catHead, catSpurt;
    [Tooltip("Points (0-1 of the dead cat picture) the head turns around and the spurt grows from")]
    public Vector2 headPivot = new Vector2(0.785f, 0.279f);
    public Vector2 spurtPivot = new Vector2(0.337f, 0.475f);

    [Header("Text")]
    [TextArea] public string loseTitle = "<size=56%><color=#FFF4DE>YOUR KEEPERS</color></size>\nLOST THEIR HEADS!";
    [TextArea] public string winTitle = "<size=40%><color=#FFF4DE>LEVEL COMPLETE</color></size>\nVICTORY!";
    public string levelSelectScene = "LevelSelect";

    [Header("Colours")]
    public Color ink = new Color32(29, 21, 51, 255);
    public Color cream = new Color32(255, 244, 222, 255);
    public Color gold = new Color32(255, 200, 60, 255);
    public Color blood = new Color32(236, 48, 48, 255);

    const float CatWidth = 800f;
    const float RefHeight = 1080f;

    Canvas canvas;
    RectTransform root;     // full screen, children anchored to its centre
    RectTransform content;  // safe area
    Image vignette;
    Iris iris;
    Vector3? focus;
    bool leaving;
    readonly List<Button> buttons = new List<Button>();

    // ------------------------------------------------------------------ entry points

    public void PlayLose(Vector3? culprit)
    {
        focus = culprit;
        Build();
        StartCoroutine(Lose());
    }

    public void PlayWin(Vector3? reward, IList<TowerEntry> rewards)
    {
        focus = reward;
        Build();
        StartCoroutine(Win(rewards));
    }

    // ------------------------------------------------------------------ lose

    IEnumerator Lose()
    {
        HideHud(0.35f);
        StartCoroutine(SlowTime(0.9f));
        StartCoroutine(CameraPush(0.16f, 1.5f, 0.35f));
        StartCoroutine(Pulse(vignette, blood, 0.38f, 0.7f));

        // The iris closes on the culprit, holds on it, then shuts
        iris.Show(true);
        iris.color = FxKit.WithAlpha(ink, 0.88f);
        float open = ScreenDiagonal();
        yield return FxKit.Wait(0.25f);
        yield return FxKit.Tween(0.8f, p =>
        {
            iris.center = FocusOnCanvas();
            iris.radius = Mathf.Lerp(open, 230f, FxKit.EaseInOut(p));
            iris.Apply();
        });
        yield return FxKit.Tween(0.45f, p =>
        {
            iris.center = FocusOnCanvas();
            iris.radius = 230f + Mathf.Sin(p * Mathf.PI) * 8f;
            iris.Apply();
        });
        yield return FxKit.Tween(0.3f, p =>
        {
            iris.radius = Mathf.Lerp(230f, 0f, p * p);
            iris.Apply();
        });
        // Let the frozen level show through a touch more while the cat drops
        StartCoroutine(FxKit.Tween(0.8f, p =>
        {
            iris.color = FxKit.WithAlpha(ink, Mathf.Lerp(0.88f, 0.8f, p));
            iris.Apply();
        }));

        yield return DeadCat();

        TMP_Text title = MakeTitle(loseTitle, 112f, blood, new Vector2(0, 345));
        StartCoroutine(Letters(title, 0.035f, 0.45f, false));
        yield return FxKit.Wait(0.55f);

        yield return PopButtons(
            ("TRY AGAIN", blueButton, restartIcon, () => StartCoroutine(Retry())),
            ("LEVELS", goldButton, levelsIcon, () => StartCoroutine(ToLevelSelect())));
    }

    IEnumerator DeadCat()
    {
        float h = CatWidth * catBody.rect.height / catBody.rect.width;
        RectTransform cat = Rect("DeadCat", content, new Vector2(CatWidth, h));
        cat.anchoredPosition = new Vector2(0, -50);

        RectTransform shadow = Img("Shadow", cat, FxKit.Glow, FxKit.WithAlpha(Color.black, 0f), new Vector2(CatWidth * 0.95f, 70)).rectTransform;
        shadow.anchorMin = shadow.anchorMax = new Vector2(0.53f, 0.03f);
        shadow.anchoredPosition = Vector2.zero;

        // Body (pivot at its feet so it squashes on the ground) with the spurt as a child
        Image body = Img("Body", cat, catBody, Color.white, Vector2.zero);
        Stretch(body.rectTransform);
        body.rectTransform.pivot = new Vector2(0.4f, 0.02f);
        Image spurt = Img("Spurt", body.transform, catSpurt, Color.white, new Vector2(CatWidth, h));
        Pin(spurt.rectTransform, spurtPivot);
        spurt.rectTransform.localScale = new Vector3(0.6f, 0f, 1f);

        Image head = Img("Head", cat, catHead, Color.white, new Vector2(CatWidth, h));
        Pin(head.rectTransform, headPivot);
        Vector2 neck = new Vector2((spurtPivot.x - 0.5f) * CatWidth, (spurtPivot.y - 0.5f) * h);
        Vector2 headRest = new Vector2((headPivot.x - 0.5f) * CatWidth, (headPivot.y - 0.5f) * h);

        body.gameObject.SetActive(false);
        head.gameObject.SetActive(false);

        // Body falls and thuds
        body.gameObject.SetActive(true);
        RectTransform bodyRt = body.rectTransform;
        yield return FxKit.Tween(0.3f, p =>
        {
            bodyRt.anchoredPosition = new Vector2(0, Mathf.Lerp(760f, 0f, p * p));
            bodyRt.localScale = new Vector3(0.94f, 1.08f, 1f);
            shadow.GetComponent<Image>().color = FxKit.WithAlpha(Color.black, 0.35f * p);
        });
        StartCoroutine(Squash(bodyRt, 0.14f, 0.32f));
        Dust(new Vector2(-40, -h * 0.5f + 8), 7, 1f);
        StartCoroutine(Spurt(spurt.rectTransform, neck));

        // Head follows a beat later: lands, bounces twice and rolls to a stop
        yield return FxKit.Wait(0.1f);
        head.gameObject.SetActive(true);
        StartCoroutine(HeadTumble(head.rectTransform, headRest, h));
        yield return FxKit.Wait(0.75f);
    }

    IEnumerator HeadTumble(RectTransform head, Vector2 rest, float catHeight)
    {
        Vector2 startOffset = new Vector2(-140f, 820f);
        float total = 1.15f;
        float t = 0f;
        bool hit1 = false, hit2 = false;
        while (t < total)
        {
            float x = Mathf.Lerp(startOffset.x, 0f, FxKit.EaseOut(Mathf.Clamp01(t / 0.95f)));
            float y, rot;
            if (t < 0.36f)
            {
                float p = t / 0.36f;
                y = Mathf.Lerp(startOffset.y, 0f, p * p);
                rot = Mathf.Lerp(220f, 18f, p);
            }
            else if (t < 0.66f)
            {
                float p = (t - 0.36f) / 0.3f;
                y = 75f * 4f * p * (1f - p);
                rot = Mathf.Lerp(18f, -12f, p);
            }
            else if (t < 0.84f)
            {
                float p = (t - 0.66f) / 0.18f;
                y = 22f * 4f * p * (1f - p);
                rot = Mathf.Lerp(-12f, 5f, p);
            }
            else
            {
                float p = (t - 0.84f) / (total - 0.84f);
                y = 0f;
                rot = Mathf.Lerp(5f, 0f, FxKit.Smooth(p));
            }
            if (!hit1 && t >= 0.36f)
            {
                hit1 = true;
                StartCoroutine(Squash(head, 0.16f, 0.22f));
                Dust(rest + new Vector2(0, -catHeight * 0.26f), 5, 0.8f);
            }
            if (!hit2 && t >= 0.66f)
            {
                hit2 = true;
                StartCoroutine(Squash(head, 0.08f, 0.16f));
            }
            head.anchoredPosition = new Vector2(x, y);
            head.localEulerAngles = new Vector3(0, 0, rot);
            yield return null;
            t += Time.unscaledDeltaTime;
        }
        head.anchoredPosition = Vector2.zero;
        head.localEulerAngles = Vector3.zero;

        StartCoroutine(DizzyStars(head));
        // Every few seconds the head gives a little twitch
        while (true)
        {
            yield return FxKit.Wait(UnityEngine.Random.Range(2.6f, 4f));
            yield return FxKit.Tween(0.35f, p =>
                head.localEulerAngles = new Vector3(0, 0, Mathf.Sin(p * Mathf.PI * 3f) * 6f * (1f - p)));
        }
    }

    IEnumerator Spurt(RectTransform spurt, Vector2 neck)
    {
        yield return FxKit.Wait(0.04f);
        yield return FxKit.Tween(0.32f, p =>
        {
            float s = OutBack(p, 2.2f);
            spurt.localScale = new Vector3(Mathf.Lerp(0.6f, 1f, p), s, 1f);
        });
        float t = 0f, nextDrop = 0.3f;
        while (true)
        {
            t += Time.unscaledDeltaTime;
            float beat = Mathf.Pow(Mathf.Abs(Mathf.Sin(t * 3.2f)), 4f);
            spurt.localScale = new Vector3(1f - beat * 0.03f, 1f + beat * 0.08f, 1f);
            if (t >= nextDrop)
            {
                nextDrop = t + UnityEngine.Random.Range(0.9f, 1.4f);
                StartCoroutine(Droplet(neck + new Vector2(10, 40)));
            }
            yield return null;
        }
    }

    IEnumerator Droplet(Vector2 from)
    {
        Image d = Img("Drop", content.Find("DeadCat"), FxKit.Glow, blood, new Vector2(20, 20));
        d.transform.SetSiblingIndex(1);
        RectTransform rt = d.rectTransform;
        Vector2 v = new Vector2(UnityEngine.Random.Range(-60f, 170f), UnityEngine.Random.Range(380f, 520f));
        Vector2 pos = from;
        float t = 0f;
        while (t < 0.85f)
        {
            float dt = Time.unscaledDeltaTime;
            v.y -= 1700f * dt;
            pos += v * dt;
            rt.anchoredPosition = pos;
            rt.localScale = Vector3.one * Mathf.Lerp(1f, 0.5f, t / 0.85f);
            d.color = FxKit.WithAlpha(blood, Mathf.Clamp01((0.85f - t) / 0.25f));
            yield return null;
            t += dt;
        }
        Destroy(d.gameObject);
    }

    IEnumerator DizzyStars(RectTransform head)
    {
        const int count = 3;
        var stars = new Image[count];
        for (int i = 0; i < count; i++)
        {
            stars[i] = Img("Star", head, FxKit.Sparkle, gold, new Vector2(34, 34));
            Pin(stars[i].rectTransform, headPivot);
        }
        float t = 0f;
        while (true)
        {
            float appear = Mathf.Clamp01(t / 0.4f);
            for (int i = 0; i < count; i++)
            {
                float a = t * 2.6f + i * Mathf.PI * 2f / count;
                float depth = Mathf.Sin(a);
                RectTransform rt = stars[i].rectTransform;
                rt.anchoredPosition = new Vector2(Mathf.Cos(a) * 85f, 120f + depth * 18f);
                rt.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, depth * 0.5f + 0.5f) * appear;
                rt.localEulerAngles = new Vector3(0, 0, t * 90f);
                stars[i].color = FxKit.WithAlpha(gold, Mathf.Lerp(0.55f, 1f, depth * 0.5f + 0.5f));
            }
            yield return null;
            t += Time.unscaledDeltaTime;
        }
    }

    // ------------------------------------------------------------------ win

    IEnumerator Win(IList<TowerEntry> rewards)
    {
        HideHud(0.3f);
        StartCoroutine(SlowTime(0.5f));
        StartCoroutine(CameraPush(0.08f, 1.2f, 0f));

        Vector2 from = FocusOnCanvas();
        Burst(from, gold, 1.3f);

        // Dim the level (the iris stays wide open, it is just the backdrop here)
        iris.Show(true);
        iris.radius = 0f;
        StartCoroutine(FxKit.Tween(0.5f, p =>
        {
            iris.color = FxKit.WithAlpha(ink, 0.74f * FxKit.Smooth(p));
            iris.Apply();
        }));
        StartCoroutine(Pulse(vignette, gold, 0.22f, 0.8f));

        // The reward's light flies up behind the title and keeps turning
        Vector2 titlePos = new Vector2(0, 330);
        Image rays = Img("Glory", content, FxKit.Sunburst, FxKit.WithAlpha(gold, 0f), new Vector2(1150, 1150));
        rays.transform.SetAsFirstSibling();
        StartCoroutine(Spin(rays.rectTransform, 10f));
        yield return FxKit.Wait(0.15f);
        yield return FxKit.Tween(0.6f, p =>
        {
            float e = FxKit.EaseInOut(p);
            rays.rectTransform.anchoredPosition = Vector2.Lerp(from, titlePos, e);
            rays.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.15f, 1f, e);
            rays.color = FxKit.WithAlpha(gold, Mathf.Lerp(0.9f, 0.4f, e));
        });

        TMP_Text title = MakeTitle(winTitle, 150f, gold, titlePos + new Vector2(0, 10));
        StartCoroutine(Letters(title, 0.05f, 0.45f, true));
        Burst(titlePos, cream, 1.6f);
        yield return FxKit.Wait(0.2f);
        StartCoroutine(Confetti());
        yield return FxKit.Wait(0.4f);

        yield return RewardCards(rewards);

        yield return PopButtons(
            ("CONTINUE", blueButton, playIcon, () => StartCoroutine(ToLevelSelect())),
            ("REPLAY", goldButton, restartIcon, () => StartCoroutine(Retry())));
    }

    IEnumerator RewardCards(IList<TowerEntry> rewards)
    {
        // One card per tower, amounts added up
        var merged = new List<TowerEntry>();
        if (rewards != null)
        {
            foreach (TowerEntry r in rewards)
            {
                if (r == null || !r.tower) continue;
                TowerEntry same = merged.Find(m => m.tower == r.tower);
                if (same != null) same.amount += r.amount;
                else merged.Add(new TowerEntry { tower = r.tower, amount = r.amount });
            }
        }
        if (merged.Count == 0) yield break;

        const float spacing = 390f;
        Vector2 cardSize = new Vector2(340, 215);
        for (int i = 0; i < merged.Count; i++)
        {
            Vector2 rest = new Vector2((i - (merged.Count - 1) * 0.5f) * spacing, -40f);
            StartCoroutine(DealCard(merged[i], rest, cardSize));
            yield return FxKit.Wait(0.28f);
        }
        yield return FxKit.Wait(0.55f);
    }

    IEnumerator DealCard(TowerEntry entry, Vector2 rest, Vector2 size)
    {
        RectTransform card = Rect("Card_" + entry.tower.towerName, content, size);
        Image glow = Img("Glow", card, FxKit.Glow, FxKit.WithAlpha(gold, 0f), size * 1.9f);

        // Back: blue chunky tile with a "?" (shown while it flies in)
        Image back = Img("Back", card, blueButton, Color.white, Vector2.zero);
        Chunky(back, size.y);
        Stretch(back.rectTransform);
        TMP_Text q = Label("Q", back.transform, "?", 110f, Color.white);
        Stretch(q.rectTransform);

        Sprite art = entry.tower.cardArt ? entry.tower.cardArt : entry.tower.towerIcon;
        Image front = Img("Art", card, art, Color.white, Vector2.zero);
        front.preserveAspect = true;
        Stretch(front.rectTransform);
        front.gameObject.SetActive(false);

        TMP_Text name = Label("Name", card, entry.tower.towerName.ToUpperInvariant(), 36f, cream);
        name.rectTransform.anchorMin = name.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        name.rectTransform.sizeDelta = new Vector2(size.x + 60, 50);
        name.rectTransform.anchoredPosition = new Vector2(0, -38);
        name.gameObject.SetActive(false);

        Image badge = Img("Amount", card, goldButton, Color.white, new Vector2(124, 74));
        Chunky(badge, 74f);
        badge.rectTransform.anchorMin = badge.rectTransform.anchorMax = new Vector2(1f, 1f);
        badge.rectTransform.anchoredPosition = new Vector2(-14, -6);
        badge.rectTransform.localEulerAngles = new Vector3(0, 0, -8);
        TMP_Text amount = Label("Text", badge.transform, "+" + entry.amount, 44f, Color.white);
        FitChunky(amount.rectTransform, 74f);
        badge.gameObject.SetActive(false);

        // Deal in from below, spinning down to rest
        float tilt = UnityEngine.Random.Range(-14f, 14f);
        yield return FxKit.Tween(0.34f, p =>
        {
            float e = FxKit.EaseOut(p);
            card.anchoredPosition = Vector2.Lerp(rest + new Vector2(0, -620f), rest, e);
            card.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(tilt, 0f, e));
        });
        // Flip
        yield return FxKit.Tween(0.11f, p => card.localScale = new Vector3(1f - p, 1f + p * 0.06f, 1f));
        back.gameObject.SetActive(false);
        front.gameObject.SetActive(true);
        yield return FxKit.Tween(0.22f, p => card.localScale = new Vector3(OutBack(p, 2.4f), 1.06f - p * 0.06f, 1f));
        card.localScale = Vector3.one;
        Burst(rest, gold, 0.9f);
        StartCoroutine(FxKit.Tween(0.6f, p => glow.color = FxKit.WithAlpha(gold, Mathf.Sin(p * Mathf.PI * 0.5f) * 0.55f)));

        name.gameObject.SetActive(true);
        StartCoroutine(Pop(name.rectTransform, 0.25f, 1.6f));
        yield return FxKit.Wait(0.08f);
        badge.gameObject.SetActive(true);
        yield return Pop(badge.rectTransform, 0.3f, 3f);
    }

    IEnumerator Confetti()
    {
        Color[] palette =
        {
            gold, new Color32(70, 150, 255, 255), new Color32(255, 92, 92, 255),
            new Color32(108, 214, 108, 255), cream, new Color32(186, 120, 255, 255)
        };
        Vector2 half = root.rect.size * 0.5f;
        RectTransform layer = Rect("Confetti", root, Vector2.zero);
        layer.SetSiblingIndex(content.GetSiblingIndex()); // behind the title, cards and buttons

        const int perSide = 34;
        var bits = new List<(RectTransform rt, Vector2 v, float spin, float flap, float phase)>();
        for (int side = -1; side <= 1; side += 2)
        {
            for (int i = 0; i < perSide; i++)
            {
                Image img = Img("Bit", layer, null, palette[UnityEngine.Random.Range(0, palette.Length)],
                    new Vector2(UnityEngine.Random.Range(12f, 18f), UnityEngine.Random.Range(20f, 30f)));
                img.rectTransform.anchoredPosition = new Vector2(side * (half.x + 20f), -half.y - 20f);
                float angle = UnityEngine.Random.Range(58f, 78f) * Mathf.Deg2Rad;
                float speed = UnityEngine.Random.Range(1700f, 2500f) * half.y / 540f;
                Vector2 v = new Vector2(-side * Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
                bits.Add((img.rectTransform, v, UnityEngine.Random.Range(-540f, 540f),
                    UnityEngine.Random.Range(6f, 12f), UnityEngine.Random.Range(0f, 6f)));
            }
        }
        float t = 0f;
        while (t < 4.5f)
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < bits.Count; i++)
            {
                var b = bits[i];
                b.v.y -= 1500f * dt;
                b.v *= 1f - 0.9f * dt;                       // air drag
                if (b.v.y < -230f) b.v.y = -230f;            // flutter down slowly
                Vector2 sway = new Vector2(Mathf.Sin(t * 3f + b.phase) * 60f * dt, 0f);
                b.rt.anchoredPosition += b.v * dt + sway;
                b.rt.localEulerAngles = new Vector3(0, 0, b.rt.localEulerAngles.z + b.spin * dt);
                b.rt.localScale = new Vector3(Mathf.Cos(t * b.flap + b.phase), 1f, 1f); // paper flipping
                bits[i] = b;
            }
            yield return null;
            t += dt;
        }
        Destroy(layer.gameObject);
    }

    // ------------------------------------------------------------------ buttons & leaving

    // Same look as the pause menu: the first button is the big main action, the rest a size down,
    // all with a white Ink-outlined icon left of the label, bottoms on one line, on a soft ink shadow.
    IEnumerator PopButtons(params (string label, Sprite sprite, Sprite icon, Action onClick)[] defs)
    {
        Vector2 main = new Vector2(540, 140), side = new Vector2(460, 120);
        float gap = 44f;
        float bottom = -475f;

        float total = gap * (defs.Length - 1);
        for (int i = 0; i < defs.Length; i++) total += (i == 0 ? main : side).x;

        Image shadow = Img("ButtonShadow", content, ShadowSprite, Color.clear, new Vector2(total + 260f, main.y + 120f));
        shadow.rectTransform.anchoredPosition = new Vector2(0, bottom + main.y * 0.5f - 6f);
        StartCoroutine(FxKit.Tween(0.35f, p => shadow.color = FxKit.WithAlpha(ink, 0.6f * p)));

        float x = -total * 0.5f;
        for (int i = 0; i < defs.Length; i++)
        {
            Vector2 size = i == 0 ? main : side;
            Button b = MakeButton(defs[i].label, defs[i].sprite, defs[i].icon, size, i == 0 ? 66f : 56f, defs[i].onClick);
            RectTransform rt = (RectTransform)b.transform;
            rt.anchoredPosition = new Vector2(x + size.x * 0.5f, bottom + size.y * 0.5f);
            x += size.x + gap;
            StartCoroutine(Pop(rt, 0.34f, 2.2f));
            yield return FxKit.Wait(0.09f);
        }
    }

    Button MakeButton(string label, Sprite sprite, Sprite icon, Vector2 size, float fontSize, Action onClick)
    {
        RectTransform rt = Rect(label + "Button", content, size);
        Image face = Img("Face", rt, sprite, Color.white, Vector2.zero);
        Stretch(face.rectTransform);
        Chunky(face, size.y);
        face.raycastTarget = true;
        TMP_Text text = Label("Text", face.transform, label, fontSize, Color.white);
        FitChunky(text.rectTransform, size.y);

        // Icon on the left, label centred in the space beside it (as CreateIconButton in the pause menu)
        if (icon)
        {
            float lip = 20f / Mathf.Clamp(160f / size.y, 0.9f, 2f);
            float iconSize = size.y * 0.52f;
            Image iconImage = Img("Icon", face.transform, icon, Color.white, new Vector2(iconSize, iconSize));
            RectTransform ir = iconImage.rectTransform;
            ir.anchorMin = ir.anchorMax = new Vector2(0f, 0.5f);
            ir.anchoredPosition = new Vector2(30f + iconSize * 0.5f, lip * 0.5f);
            text.rectTransform.offsetMin = new Vector2(40f + iconSize, text.rectTransform.offsetMin.y);
            text.rectTransform.offsetMax = new Vector2(-30f, text.rectTransform.offsetMax.y);
        }

        Button button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = face;
        ColorBlock colors = button.colors;
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
        colors.selectedColor = colors.highlightedColor = Color.white;
        button.colors = colors;
        face.gameObject.AddComponent<Squish>();
        button.onClick.AddListener(() =>
        {
            if (leaving) return;
            leaving = true;
            foreach (Button other in buttons) other.interactable = false;
            if (SoundManager.Instance) SoundManager.Instance.PlaySound("ButtonClick");
            lastPressed = rt;
            onClick();
        });
        buttons.Add(button);
        return button;
    }

    RectTransform lastPressed;

    IEnumerator Retry()
    {
        yield return FxKit.Wait(0.12f);
        Vector2 screen = lastPressed
            ? RectTransformUtility.WorldToScreenPoint(null, lastPressed.position)
            : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        IrisWipe.Reload(ink, screen);
    }

    IEnumerator ToLevelSelect()
    {
        yield return FxKit.Wait(0.12f);
        Time.timeScale = 1f;
        SceneTransition.Load(levelSelectScene);
    }

    // ------------------------------------------------------------------ building

    void Build()
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60; // above the HUD and the pause menu
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, RefHeight);
        scaler.matchWidthOrHeight = 1f;
        gameObject.AddComponent<GraphicRaycaster>();

        root = (RectTransform)transform;

        // Swallows every tap on the level from now on
        Image blocker = Img("Blocker", root, null, Color.clear, Vector2.zero);
        Stretch(blocker.rectTransform);
        blocker.raycastTarget = true;

        iris = new Iris(root);
        vignette = Img("Vignette", root, VignetteSprite, Color.clear, Vector2.zero);
        Stretch(vignette.rectTransform);

        content = Rect("Safe", root, Vector2.zero);
        Stretch(content);
        content.gameObject.AddComponent<SafeArea>();
    }

    TMP_Text MakeTitle(string text, float size, Color color, Vector2 pos)
    {
        TMP_Text t = Label("Title", content, text, size, color);
        t.enableAutoSizing = false;
        t.lineSpacing = -12f;
        t.rectTransform.sizeDelta = new Vector2(1700, 300);
        t.rectTransform.anchoredPosition = pos;
        return t;
    }

    void HideHud(float duration)
    {
        foreach (Canvas c in FindObjectsByType<Canvas>(FindObjectsInactive.Exclude))
        {
            if (!c.isRootCanvas || c == canvas || c.renderMode == RenderMode.WorldSpace) continue;
            CanvasGroup g = c.GetComponent<CanvasGroup>();
            if (!g) g = c.gameObject.AddComponent<CanvasGroup>();
            g.interactable = false;
            g.blocksRaycasts = false;
            float from = g.alpha;
            StartCoroutine(FxKit.Tween(duration, p => { if (g) g.alpha = Mathf.Lerp(from, 0f, p); }));
        }
    }

    // Time eases to a stop instead of snapping, so the last moment plays out in slow motion
    IEnumerator SlowTime(float duration)
    {
        float from = Time.timeScale;
        yield return FxKit.Tween(duration, p => Time.timeScale = Mathf.Lerp(from, 0f, FxKit.EaseOut(p)));
        Time.timeScale = 0f;
    }

    IEnumerator CameraPush(float amount, float duration, float shake)
    {
        Camera cam = Camera.main;
        if (!cam) yield break;
        Transform t = cam.transform;
        Vector3 start = t.position;
        Vector3 target = focus.HasValue ? Vector3.Lerp(start, focus.Value, amount) : start + t.forward * amount * 10f;
        yield return FxKit.Tween(duration, p =>
        {
            if (!t) return;
            Vector3 jolt = Vector3.zero;
            float shakeTime = 0.3f / duration;
            if (shake > 0f && p < shakeTime)
            {
                float k = 1f - p / shakeTime;
                jolt = (t.right * Mathf.Sin(p * 190f) + t.up * Mathf.Cos(p * 150f)) * shake * k * k;
            }
            t.position = Vector3.Lerp(start, target, FxKit.EaseInOut(p)) + jolt;
        });
    }

    IEnumerator Pulse(Image img, Color color, float peak, float duration)
    {
        yield return FxKit.Tween(duration, p =>
            img.color = FxKit.WithAlpha(color, peak * Mathf.Sin(Mathf.Min(1f, p * 3f) * Mathf.PI * 0.5f) * (1f - p)));
    }

    IEnumerator Squash(RectTransform rt, float amount, float duration)
    {
        yield return FxKit.Tween(duration, p =>
        {
            float w = Mathf.Sin(p * Mathf.PI * 2f) * Mathf.Exp(-p * 3f);
            float s = amount * Mathf.Cos(p * Mathf.PI * 1.5f) * (1f - p);
            rt.localScale = new Vector3(1f + s, 1f - s + w * 0.02f, 1f);
        });
        rt.localScale = Vector3.one;
    }

    IEnumerator Pop(RectTransform rt, float duration, float overshoot)
    {
        rt.localScale = Vector3.zero;
        yield return FxKit.Tween(duration, p => rt.localScale = Vector3.one * OutBack(p, overshoot));
        rt.localScale = Vector3.one;
    }

    IEnumerator Spin(RectTransform rt, float degreesPerSecond)
    {
        while (rt)
        {
            rt.localEulerAngles += new Vector3(0, 0, -degreesPerSecond * Time.unscaledDeltaTime);
            yield return null;
        }
    }

    // Flash + ring + sparks
    void Burst(Vector2 at, Color color, float size)
    {
        Image flash = Img("Flash", content, FxKit.Glow, color, new Vector2(260, 260) * size);
        flash.rectTransform.anchoredPosition = at;
        StartCoroutine(Tween(0.45f, p =>
        {
            flash.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.4f, 1.4f, FxKit.EaseOut(p));
            flash.color = FxKit.WithAlpha(color, 0.9f * (1f - p));
        }, () => Destroy(flash.gameObject)));

        Image ring = Img("Ring", content, FxKit.Ring, color, new Vector2(300, 300) * size);
        ring.rectTransform.anchoredPosition = at;
        StartCoroutine(Tween(0.5f, p =>
        {
            ring.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.2f, 1.3f, FxKit.EaseOut(p));
            ring.color = FxKit.WithAlpha(color, 0.8f * (1f - p));
        }, () => Destroy(ring.gameObject)));

        int sparks = 8;
        for (int i = 0; i < sparks; i++)
        {
            float a = (i + UnityEngine.Random.value * 0.5f) / sparks * Mathf.PI * 2f;
            Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            float dist = UnityEngine.Random.Range(110f, 170f) * size;
            Image s = Img("Spark", content, FxKit.Sparkle, color, new Vector2(40, 40) * Mathf.Sqrt(size));
            StartCoroutine(Tween(0.55f, p =>
            {
                float e = FxKit.EaseOut(p);
                s.rectTransform.anchoredPosition = at + dir * dist * e;
                s.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 0.2f, p);
                s.color = FxKit.WithAlpha(color, 1f - p * p);
            }, () => Destroy(s.gameObject)));
        }
    }

    // Soft dust puffs pushed out sideways along the ground
    void Dust(Vector2 at, int count, float size)
    {
        Transform parent = content.Find("DeadCat");
        for (int i = 0; i < count; i++)
        {
            float side = i % 2 == 0 ? 1f : -1f;
            float speed = UnityEngine.Random.Range(110f, 260f) * size;
            float rise = UnityEngine.Random.Range(10f, 45f);
            Image puff = Img("Dust", parent, FxKit.Glow, FxKit.WithAlpha(cream, 0.7f), new Vector2(90, 90) * size);
            puff.transform.SetSiblingIndex(1);
            Vector2 start = at + new Vector2(side * UnityEngine.Random.Range(0f, 60f), 0f);
            StartCoroutine(Tween(0.55f, p =>
            {
                float e = FxKit.EaseOut(p);
                puff.rectTransform.anchoredPosition = start + new Vector2(side * speed * e, rise * e);
                puff.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.4f, 1.1f, e);
                puff.color = FxKit.WithAlpha(cream, 0.55f * (1f - p));
            }, () => Destroy(puff.gameObject)));
        }
    }

    // Per-letter entrance on a TMP text: drop (lose) or bounce-pop (win)
    IEnumerator Letters(TMP_Text text, float stagger, float duration, bool pop)
    {
        text.ForceMeshUpdate();
        TMP_TextInfo info = text.textInfo;
        TMP_MeshInfo[] source = info.CopyMeshInfoVertexData();
        int count = info.characterCount;
        var tilt = new float[count];
        for (int i = 0; i < count; i++) tilt[i] = UnityEngine.Random.Range(-22f, 22f);

        float total = stagger * count + duration;
        float t = 0f;
        while (true)
        {
            for (int i = 0; i < count; i++)
            {
                TMP_CharacterInfo ch = info.characterInfo[i];
                if (!ch.isVisible) continue;
                float p = Mathf.Clamp01((t - i * stagger) / duration);
                Vector3 offset;
                float scale, rot;
                if (pop)
                {
                    scale = OutBack(p, 3f);
                    offset = new Vector3(0, (1f - FxKit.EaseOut(p)) * -40f, 0);
                    rot = (1f - p) * tilt[i];
                }
                else
                {
                    scale = 1f;
                    offset = new Vector3(0, (1f - OutBack(p, 1.6f)) * 220f, 0);
                    rot = (1f - FxKit.EaseOut(p)) * tilt[i];
                }
                byte alpha = (byte)(255 * Mathf.Clamp01(p * 4f));

                int mi = ch.materialReferenceIndex, vi = ch.vertexIndex;
                Vector3[] src = source[mi].vertices;
                Vector3[] dst = info.meshInfo[mi].vertices;
                Vector3 mid = (src[vi] + src[vi + 2]) * 0.5f;
                Matrix4x4 m = Matrix4x4.TRS(offset, Quaternion.Euler(0, 0, rot), Vector3.one * scale);
                Color32[] colors = info.meshInfo[mi].colors32;
                Color32[] srcColors = source[mi].colors32;
                for (int j = 0; j < 4; j++)
                {
                    dst[vi + j] = m.MultiplyPoint3x4(src[vi + j] - mid) + mid;
                    Color32 c = srcColors[vi + j];
                    c.a = (byte)(c.a * alpha / 255);
                    colors[vi + j] = c;
                }
            }
            text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
            if (t >= total) break;
            yield return null;
            t += Time.unscaledDeltaTime;
        }
    }

    Vector2 FocusOnCanvas()
    {
        Camera cam = Camera.main;
        if (!focus.HasValue || !cam) return Vector2.zero;
        Vector3 screen = cam.WorldToScreenPoint(focus.Value);
        if (screen.z < 0f) return Vector2.zero;
        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, uiCamera, out Vector2 local);
        return local;
    }

    float ScreenDiagonal() => root.rect.size.magnitude * 0.5f + 40f;

    static IEnumerator Tween(float duration, Action<float> step, Action done)
    {
        yield return FxKit.Tween(duration, step);
        done();
    }

    static float OutBack(float p, float s)
    {
        p -= 1f;
        return 1f + p * p * ((s + 1f) * p + s);
    }

    // ------------------------------------------------------------------ small UI factory

    static RectTransform Rect(string name, Transform parent, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.sizeDelta = size;
        return rt;
    }

    static Image Img(string name, Transform parent, Sprite sprite, Color color, Vector2 size)
    {
        RectTransform rt = Rect(name, parent, size);
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    TMP_Text Label(string name, Transform parent, string text, float size, Color color)
    {
        RectTransform rt = Rect(name, parent, Vector2.zero);
        TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font) t.font = font;
        if (chunkyMaterial) t.fontSharedMaterial = chunkyMaterial;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.enableAutoSizing = true;
        t.fontSizeMin = size * 0.5f;
        t.fontSizeMax = size;
        t.raycastTarget = false;
        return t;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    // A layer the same size as its parent picture, turning/scaling around the given point of it
    static void Pin(RectTransform rt, Vector2 point)
    {
        rt.anchorMin = rt.anchorMax = point;
        rt.pivot = point;
        rt.anchoredPosition = Vector2.zero;
    }

    // 9-sliced chunky sprite with the border scaled like MenuBuilder's buttons
    static void Chunky(Image img, float height)
    {
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = Mathf.Clamp(160f / height, 0.9f, 2f);
    }

    // Keeps a label off the chunky sprite's thick bottom lip
    static void FitChunky(RectTransform rt, float height)
    {
        Stretch(rt);
        float lip = 20f / Mathf.Clamp(160f / height, 0.9f, 2f);
        rt.offsetMin = new Vector2(14, lip);
        rt.offsetMax = new Vector2(-14, -4);
    }

    // ------------------------------------------------------------------ generated sprites

    static Sprite vignetteSprite, holeSprite, shadowSprite;

    // Soft blob (stretched into a wide oval) that grounds the button row on the level
    static Sprite ShadowSprite => shadowSprite ? shadowSprite : shadowSprite = Keep(FxKit.MakeSprite(128, (x, y) =>
    {
        float d = new Vector2(x - 0.5f, y - 0.5f).magnitude * 2f;
        return 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 1f, d));
    }));

    static Sprite VignetteSprite => vignetteSprite ? vignetteSprite : vignetteSprite = Keep(FxKit.MakeSprite(128, (x, y) =>
    {
        float d = new Vector2(x - 0.5f, y - 0.5f).magnitude * 2f;
        return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1.35f, d));
    }));

    // Opaque square with a round hole half its width across (sharp, anti-aliased edge)
    static Sprite HoleSprite => holeSprite ? holeSprite : holeSprite = Keep(FxKit.MakeSprite(512, (x, y) =>
    {
        float d = new Vector2(x - 0.5f, y - 0.5f).magnitude * 2f;
        return Mathf.Clamp01((d - 0.5f) / 0.006f + 0.5f);
    }));

    static Sprite Keep(Sprite s)
    {
        s.texture.hideFlags = HideFlags.DontUnloadUnusedAsset;
        s.hideFlags = HideFlags.DontUnloadUnusedAsset;
        return s;
    }

    // ------------------------------------------------------------------ helpers

    // Cartoon iris: a dark screen with a round hole. The hole is one image; four plain quads
    // cover the rest of the screen around it.
    class Iris
    {
        public Vector2 center;
        public float radius;
        public Color color;
        readonly RectTransform rootRt;
        readonly Image hole, left, right, top, bottom;

        public Iris(Transform parent)
        {
            rootRt = Rect("Iris", parent, Vector2.zero);
            Stretch(rootRt);
            hole = Img("Hole", rootRt, HoleSprite, Color.clear, Vector2.zero);
            left = Img("L", rootRt, null, Color.clear, Vector2.zero);
            right = Img("R", rootRt, null, Color.clear, Vector2.zero);
            top = Img("T", rootRt, null, Color.clear, Vector2.zero);
            bottom = Img("B", rootRt, null, Color.clear, Vector2.zero);
            left.rectTransform.pivot = new Vector2(1f, 0.5f);
            right.rectTransform.pivot = new Vector2(0f, 0.5f);
            top.rectTransform.pivot = new Vector2(0.5f, 0f);
            bottom.rectTransform.pivot = new Vector2(0.5f, 1f);
            Show(false);
        }

        public void Show(bool on) => rootRt.gameObject.SetActive(on);

        public void Apply()
        {
            const float big = 20000f;
            float s = Mathf.Max(0f, radius) * 4f;
            float h = s * 0.5f;
            hole.rectTransform.anchoredPosition = center;
            hole.rectTransform.sizeDelta = new Vector2(s, s);
            hole.enabled = s > 0.5f;
            left.rectTransform.anchoredPosition = new Vector2(center.x - h, 0f);
            left.rectTransform.sizeDelta = new Vector2(big, big);
            right.rectTransform.anchoredPosition = new Vector2(center.x + h, 0f);
            right.rectTransform.sizeDelta = new Vector2(big, big);
            top.rectTransform.anchoredPosition = new Vector2(center.x, center.y + h);
            top.rectTransform.sizeDelta = new Vector2(s, big);
            bottom.rectTransform.anchoredPosition = new Vector2(center.x, center.y - h);
            bottom.rectTransform.sizeDelta = new Vector2(s, big);
            hole.color = left.color = right.color = top.color = bottom.color = color;
        }
    }

    // Buttons squash while held and spring back on release
    class Squish : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        float target = 1f, scale = 1f, velocity;

        public void OnPointerDown(PointerEventData e) => target = 0.9f;
        public void OnPointerUp(PointerEventData e) => target = 1f;

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            velocity += (target - scale) * 900f * dt;
            velocity *= Mathf.Exp(-22f * dt);
            scale += velocity * dt;
            transform.localScale = new Vector3(scale + (1f - scale) * 0.3f, scale, 1f);
        }
    }

    // Try Again / Replay: the iris shuts over the button, the level reloads behind it,
    // then the iris opens on the fresh level. Lives across the scene load.
    public class IrisWipe : MonoBehaviour
    {
        public static void Reload(Color color, Vector2 screenPoint)
        {
            var go = new GameObject("IrisWipe", typeof(RectTransform));
            DontDestroyOnLoad(go);
            var wipe = go.AddComponent<IrisWipe>();
            wipe.StartCoroutine(wipe.Run(color, screenPoint));
        }

        IEnumerator Run(Color color, Vector2 screenPoint)
        {
            Canvas c = gameObject.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 1001;
            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, RefHeight);
            scaler.matchWidthOrHeight = 1f;
            gameObject.AddComponent<GraphicRaycaster>();
            RectTransform rt = (RectTransform)transform;
            Image blocker = Img("Blocker", rt, null, Color.clear, Vector2.zero);
            Stretch(blocker.rectTransform);
            blocker.raycastTarget = true;
            yield return null; // let the canvas size itself

            var iris = new Iris(rt) { color = color };
            iris.Show(true);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screenPoint, null, out Vector2 at);
            float open = FarthestCorner(rt, at);
            yield return FxKit.Tween(0.45f, p =>
            {
                iris.center = at;
                iris.radius = Mathf.Lerp(open, 0f, FxKit.EaseInOut(p));
                iris.Apply();
            });

            Time.timeScale = 1f;
            AsyncOperation op = SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name);
            while (!op.isDone) yield return null;
            yield return null;
            yield return null;

            open = FarthestCorner(rt, Vector2.zero);
            yield return FxKit.Tween(0.55f, p =>
            {
                iris.center = Vector2.zero;
                iris.radius = Mathf.Lerp(0f, open, FxKit.EaseInOut(p));
                iris.Apply();
            });
            Destroy(gameObject);
        }

        static float FarthestCorner(RectTransform rt, Vector2 from)
        {
            Vector2 half = rt.rect.size * 0.5f;
            float best = 0f;
            for (int i = 0; i < 4; i++)
            {
                Vector2 corner = new Vector2(i % 2 == 0 ? -half.x : half.x, i < 2 ? -half.y : half.y);
                best = Mathf.Max(best, Vector2.Distance(from, corner));
            }
            return best + 20f;
        }
    }
}
