using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Character select: the tower info block under the keeper's name (in place of the stat pips): card bar,
// a line saying what it's for or the Upgrade button, icon stat chips and a description.
public static partial class MenuBuilder
{
    static readonly Color GreenTop = new Color32(150, 236, 96, 255);
    static readonly Color GreenBottom = new Color32(58, 176, 52, 255);
    static readonly Color HeartRed = new Color32(255, 92, 110, 255);
    static readonly Color RangeGold = new Color32(255, 206, 80, 255);
    static readonly Color CardBlue = new Color32(150, 200, 255, 255);
    const float ChipValueSize = 64;

    // Adds the info block to the existing character select scene without rebuilding it (the scene now
    // spawns its cards from the inventory and has teammates' changes). Removes the old (i) button and
    // info popup. Safe to re-run.
    public static void ApplyTowerInfo()
    {
        LoadStyleAssets();
        EnsureTowerInfoAssets();
        Scene scene = EditorSceneManager.OpenScene(ScenesFolder + CharacterScene + ".unity", OpenSceneMode.Single);
        CharacterSelectScreen screen = Object.FindAnyObjectByType<CharacterSelectScreen>(FindObjectsInactive.Include);

        Transform content = screen.intelGroup.transform;
        foreach (string old in new[] { "StatHEALTH", "StatPOWER", "StatRANGE", "CardBar", "InfoButton", "TowerInfo" })
        {
            Transform t = content.Find(old);
            if (t)
            {
                Object.DestroyImmediate(t.gameObject);
            }
        }
        Transform popup = screen.transform.Find("TowerInfo");
        if (popup)
        {
            Object.DestroyImmediate(popup.gameObject);
        }

        CharacterSelectLayout layout = screen.GetComponentInChildren<CharacterSelectLayout>(true);
        if (layout)
        {
            layout.intelSize = IntelSize;
            layout.intelFooter = IntelFooter;
            layout.maxIntelShare = IntelMaxShare;
            layout.intelPanel.sizeDelta = IntelSize;
            Transform shadow = layout.intelPanel.Find("Shadow");
            if (shadow)
            {
                ((RectTransform)shadow).sizeDelta = IntelShadowSize;
            }
            EditorUtility.SetDirty(layout);
        }
        BuildTowerInfoBlock(content, screen);

        EditorUtility.SetDirty(screen);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    static void EnsureTowerInfoAssets()
    {
        if (!StyleSprite("CC_BarFillGreen"))
        {
            WriteBar("CC_BarFillGreen", GreenTop, GreenBottom, 0);
        }
        if (!StyleSprite("CC_IconHeart"))
        {
            WriteHeartIcon("CC_IconHeart");
        }
        if (!StyleSprite("CC_IconSword"))
        {
            WriteSwordIcon("CC_IconSword");
        }
        if (!StyleSprite("CC_IconHeal"))
        {
            WriteHealIcon("CC_IconHeal");
        }
        if (!StyleSprite("CC_IconRange"))
        {
            WriteRangeIcon("CC_IconRange");
        }
        if (!StyleSprite("CC_IconCard"))
        {
            WriteCardIcon("CC_IconCard");
        }
        if (!StyleSprite("CC_IconUp"))
        {
            WriteUpIcon("CC_IconUp");
        }
    }

    // Under the name, all on the same left edge:
    //   [level badge | owned / needed | card]
    //   "Collect n more cards to upgrade"  or  [Upgrade]
    //   [heart HEALTH] [sword DAMAGE] [arrow RANGE]
    //   description
    static void BuildTowerInfoBlock(Transform content, CharacterSelectScreen screen)
    {
        const float left = 24;
        GameObject root = new GameObject("TowerInfo", typeof(RectTransform));
        root.transform.SetParent(content, false);
        Stretch((RectTransform)root.transform);
        TowerInfoBlock block = root.AddComponent<TowerInfoBlock>();
        Transform b = root.transform;

        // Every row spans the same column
        const float column = 760;

        // The badge hangs over the bar's left end and the card over its right, so the bar is inset by both.
        // The bar is also the upgrade control (see TowerProgressBar).
        Vector2 barSize = new Vector2(column - 54 - 50, 72);
        block.progress = BuildProgressBar("CardBar", b, barSize, true, true);
        TopLeft((RectTransform)block.progress.transform, new Vector2(left + 54, -280), barSize);
        block.upgradeButton = block.progress.GetComponent<Button>();

        block.cardsHint = CreateChunkyText("CardsHint", b, "Collect 1 more card to upgrade", 34, Color.white);
        block.cardsHint.alignment = TextAlignmentOptions.Left;
        block.cardsHint.fontSizeMin = 24;
        TopLeft(block.cardsHint.rectTransform, new Vector2(left, -390), new Vector2(column, 46));

        const float chipTop = -466, chipGap = 18;
        Vector2 chipSize = new Vector2((column - chipGap * 2) / 3, 180);
        float chipY = chipTop - chipSize.y / 2;
        block.healthValue = CreateStatChip("Health", b, "HEALTH", StyleSprite("CC_IconHeart"), HeartRed, new Vector2(left + chipSize.x / 2, chipY), chipSize, out _, out _);
        block.powerValue = CreateStatChip("Power", b, "DAMAGE", StyleSprite("CC_IconSword"), Color.white, new Vector2(left + chipSize.x * 1.5f + chipGap, chipY), chipSize, out block.powerLabel, out block.powerIcon);
        block.rangeValue = CreateStatChip("Range", b, "RANGE", StyleSprite("CC_IconRange"), RangeGold, new Vector2(left + chipSize.x * 2.5f + chipGap * 2, chipY), chipSize, out _, out _);
        block.damageIcon = StyleSprite("CC_IconSword");
        block.healIcon = StyleSprite("CC_IconHeal");

        // "+n" the next upgrade adds, as a green tag pinned to the power chip's top-right corner
        Image gain = CreateImage("Gain", block.powerValue.transform.parent.parent, StyleSprite("CC_BarFillGreen"));
        gain.type = Image.Type.Sliced;
        gain.preserveAspect = false;
        gain.pixelsPerUnitMultiplier = 1.4f;
        Place(gain.rectTransform, new Vector2(1f, 1f), new Vector2(-38, 2), new Vector2(96, 48));
        block.powerGain = CreateChunkyText("Text", gain.transform, "+50", 32, Color.white);
        Stretch(block.powerGain.rectTransform);
        block.powerGain.rectTransform.offsetMin = new Vector2(6, 2);
        block.powerGain.rectTransform.offsetMax = new Vector2(-6, 0);
        gain.gameObject.SetActive(false);

        block.description = CreateChunkyText("Description", b, "Heals nearby keepers.", 38, new Color(1, 1, 1, 0.9f));
        block.description.alignment = TextAlignmentOptions.TopLeft;
        block.description.textWrappingMode = TextWrappingModes.Normal;
        block.description.fontSizeMin = 28;
        block.description.lineSpacing = 4;
        TopLeft(block.description.rectTransform, new Vector2(left, -678), new Vector2(column, 112));

        // Only the card bar (and its arrow gem, which hangs off the end) takes touches; the rest of the name block lets swipes through
        foreach (Graphic g in b.GetComponentsInChildren<Graphic>(true))
        {
            g.raycastTarget = g.GetComponent<Button>() != null || g.name == "UpGem";
        }
        block.valueSize = ChipValueSize;
        block.fx = screen.fx;
        screen.intelInfo = block;
        screen.intelGroup.blocksRaycasts = true;
    }

    // Dark well, gold fill (green once an upgrade is affordable), "owned/needed" on top,
    // optionally a round level badge over the left end and a little card over the right end
    static TowerProgressBar BuildProgressBar(string name, Transform parent, Vector2 size, bool withBadge, bool withCard)
    {
        GameObject root = new GameObject(name, typeof(RectTransform));
        root.transform.SetParent(parent, false);
        ((RectTransform)root.transform).sizeDelta = size;
        TowerProgressBar bar = root.AddComponent<TowerProgressBar>();

        Image glow = CreateImage("ReadyGlow", root.transform, StyleSprite("CS_Glow"));
        glow.preserveAspect = false;
        glow.color = new Color(OkGreen.r, OkGreen.g, OkGreen.b, 0);
        Stretch(glow.rectTransform);
        glow.rectTransform.offsetMin = new Vector2(-70, -50);
        glow.rectTransform.offsetMax = new Vector2(70, 50);
        bar.readyGlow = glow;

        Image well = CreateImage("Well", root.transform, StyleSprite("CC_BarBg"));
        well.type = Image.Type.Sliced;
        well.preserveAspect = false;
        well.pixelsPerUnitMultiplier = 0.6f;
        Stretch(well.rectTransform);

        Image fill = CreateImage("Fill", well.transform, StyleSprite("CC_BarFill"));
        fill.type = Image.Type.Sliced;
        fill.preserveAspect = false;
        fill.pixelsPerUnitMultiplier = 0.75f;
        Stretch(fill.rectTransform);
        fill.rectTransform.offsetMin = new Vector2(6, 6);
        fill.rectTransform.offsetMax = new Vector2(-6, -6);
        bar.fill = fill.rectTransform;
        bar.fillImage = fill;
        bar.fillSprite = StyleSprite("CC_BarFill");
        bar.readySprite = StyleSprite("CC_BarFillGreen");

        TextMeshProUGUI count = CreateChunkyText("Count", root.transform, "1/2", Mathf.Round(size.y * 0.66f), Color.white);
        Stretch(count.rectTransform);
        count.rectTransform.offsetMin = new Vector2(withBadge ? size.y * 0.8f : 0, 2);
        count.rectTransform.offsetMax = new Vector2(withCard ? -size.y * 0.5f : 0, 0);
        bar.countText = count;

        if (withBadge)
        {
            Image badge = CreateImage("LevelBadge", root.transform, StyleSprite("CC_CircleBlue"));
            Place(badge.rectTransform, new Vector2(0f, 0.5f), new Vector2(0, 2), Vector2.one * size.y * 1.5f);
            TextMeshProUGUI level = CreateChunkyText("Level", badge.transform, "1", Mathf.Round(size.y * 0.75f), Color.white);
            StretchLabel(level, size.y * 1.5f);
            bar.levelBadge = badge.rectTransform;
            bar.levelText = level;
        }
        if (withCard)
        {
            // Says "this bar counts cards" without words
            Image card = CreateImage("CardIcon", root.transform, StyleSprite("CC_IconCard"));
            card.color = CardBlue;
            Place(card.rectTransform, new Vector2(1f, 0.5f), new Vector2(0, 4), Vector2.one * size.y * 1.35f);
            card.rectTransform.localRotation = Quaternion.Euler(0, 0, -12);
            bar.cardIcon = card.gameObject;
        }
        if (withBadge && withCard)
        {
            AddLevelUpParts(bar, root.transform, well, count, size);
        }
        return bar;
    }

    // The card bar doubles as the level-up control: the whole bar is the tap target; while an upgrade
    // is affordable it reads LEVEL UP!, a shine sweeps through the well and a gold arrow gem replaces the card
    static void AddLevelUpParts(TowerProgressBar bar, Transform root, Image well, TextMeshProUGUI count, Vector2 size)
    {
        Image hit = root.gameObject.AddComponent<Image>();
        hit.color = new Color(1, 1, 1, 0);
        Button button = root.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = hit;
        Navigation nav = button.navigation;
        nav.mode = Navigation.Mode.None;
        button.navigation = nav;

        // The well masks the shine to the bar
        well.gameObject.AddComponent<RectMask2D>();
        Image shine = CreateImage("Shine", well.transform, StyleSprite("CS_Glow"));
        shine.preserveAspect = false;
        shine.color = new Color(1, 1, 1, 0.7f);
        Place(shine.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(70, size.y * 2.2f));
        shine.rectTransform.localRotation = Quaternion.Euler(0, 0, -22);
        shine.gameObject.SetActive(false);
        bar.shine = shine.rectTransform;

        TextMeshProUGUI ready = CreateChunkyText("LevelUp", root, "LEVEL UP!", Mathf.Round(size.y * 0.66f), Color.white);
        Stretch(ready.rectTransform);
        ready.rectTransform.offsetMin = count.rectTransform.offsetMin;
        ready.rectTransform.offsetMax = count.rectTransform.offsetMax;
        ready.gameObject.SetActive(false);
        bar.readyText = ready;

        Image gem = CreateImage("UpGem", root, StyleSprite("CC_CircleGold"));
        Place(gem.rectTransform, new Vector2(1f, 0.5f), new Vector2(0, 4), Vector2.one * size.y * 1.4f);
        Image up = CreateImage("Arrow", gem.transform, StyleSprite("CC_IconUp"));
        Stretch(up.rectTransform);
        up.rectTransform.offsetMin = Vector2.one * size.y * 0.32f;
        up.rectTransform.offsetMax = -Vector2.one * size.y * 0.32f;
        gem.gameObject.SetActive(false);
        bar.upGem = gem.rectTransform;

        bar.arrowSprite = StyleSprite("CC_IconUp");
    }

    // Dark tile: label centred along the top, icon + big value centred under it; returns the value
    static TextMeshProUGUI CreateStatChip(string name, Transform parent, string label, Sprite icon, Color iconTint, Vector2 pos, Vector2 size,
        out TextMeshProUGUI labelText, out Image iconImage)
    {
        Image chip = CreateImage(name, parent, StyleSprite("CC_BarBg"));
        chip.type = Image.Type.Sliced;
        chip.preserveAspect = false;
        chip.pixelsPerUnitMultiplier = 0.9f;
        Place(chip.rectTransform, new Vector2(0f, 1f), pos, size);

        labelText = CreateChunkyText("Label", chip.transform, label, 32, new Color(1, 1, 1, 0.7f));
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.fontSizeMin = 20;
        TopLeft(labelText.rectTransform, new Vector2(12, -18), new Vector2(size.x - 24, 40));

        // Icon and value as one centred group, whatever the value's width
        GameObject row = new GameObject("Row", typeof(RectTransform));
        row.transform.SetParent(chip.transform, false);
        RectTransform rowRect = (RectTransform)row.transform;
        rowRect.anchorMin = new Vector2(0f, 0f);
        rowRect.anchorMax = new Vector2(1f, 0f);
        rowRect.pivot = new Vector2(0.5f, 0f);
        rowRect.anchoredPosition = new Vector2(0, 28);
        rowRect.sizeDelta = new Vector2(-16, 80);
        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 10;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        iconImage = CreateImage("Icon", row.transform, icon);
        iconImage.color = iconTint;
        LayoutElement iconSize = iconImage.gameObject.AddComponent<LayoutElement>();
        iconSize.preferredWidth = iconSize.preferredHeight = 62;

        TextMeshProUGUI value = CreateChunkyText("Value", row.transform, "10", ChipValueSize, Color.white);
        value.alignment = TextAlignmentOptions.Left;
        // Auto-size fights the layout group; TowerInfoBlock.FitValues shrinks long words instead
        value.enableAutoSizing = false;
        value.fontSize = ChipValueSize;
        return value;
    }

    // Heart (health)
    static void WriteHeartIcon(string name)
    {
        WriteIcon(name, (px, py) =>
        {
            float x = (px - 64) / 46f, y = (py - 58) / 46f + 0.15f;
            float a = x * x + y * y - 1;
            return a * a * a - x * x * y * y * y < 0;
        });
    }

    // Sword pointing up-right (damage)
    static void WriteSwordIcon(string name)
    {
        WriteIcon(name, (px, py) =>
        {
            bool blade = SegmentDistance(px, py, 44, 44, 102, 102) < 10;
            bool tip = px + py > 196 && px + py < 222 && Mathf.Abs(px - py) < (222 - px - py) * 0.5f;
            bool guard = SegmentDistance(px, py, 24, 60, 60, 24) < 8;
            bool grip = SegmentDistance(px, py, 42, 42, 24, 24) < 7;
            bool pommel = new Vector2(px - 20, py - 20).magnitude < 11;
            return blade || tip || guard || grip || pommel;
        });
    }

    // Plus cross (healing)
    static void WriteHealIcon(string name)
    {
        WriteIcon(name, (px, py) =>
        {
            float dx = Mathf.Abs(px - 64), dy = Mathf.Abs(py - 64);
            return (dx < 17 && dy < 46) || (dy < 17 && dx < 46);
        });
    }

    // Arrow flying right (range)
    static void WriteRangeIcon(string name)
    {
        WriteIcon(name, (px, py) =>
        {
            bool shaft = SegmentDistance(px, py, 22, 64, 88, 64) < 7;
            bool head = px >= 80 && px <= 118 && Mathf.Abs(py - 64) < (118 - px) * 0.75f;
            bool fletchTop = SegmentDistance(px, py, 16, 88, 38, 64) < 7;
            bool fletchBottom = SegmentDistance(px, py, 16, 40, 38, 64) < 7;
            return shaft || head || fletchTop || fletchBottom;
        });
    }

    // Upright card with a diamond in the middle (cards)
    static void WriteCardIcon(string name)
    {
        WriteIcon(name, (px, py) =>
        {
            bool card = RoundedRectDistance(px, py, 64, 64, 38, 52, 10) < 0;
            bool diamond = Mathf.Abs(px - 64) / 16f + Mathf.Abs(py - 64) / 22f < 1;
            return card && !diamond;
        });
    }

    // Chunky up arrow (upgrade ready)
    static void WriteUpIcon(string name)
    {
        WriteIcon(name, (px, py) =>
        {
            bool head = py >= 60 && py <= 112 && Mathf.Abs(px - 64) < (112 - py) * 0.95f;
            bool stem = Mathf.Abs(px - 64) < 17 && py > 18 && py < 64;
            return head || stem;
        });
    }
}
