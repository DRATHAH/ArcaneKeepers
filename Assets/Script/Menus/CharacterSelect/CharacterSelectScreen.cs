using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Character select: swipe through the lineup, tap keepers to add exactly four to the squad roster,
// tap a picked keeper (in the lineup or the roster) to remove them. DEPLOY unlocks at 4/4 and hands the
// team to the existing Inventory/PlayerManager so the level select and levels see it.
public class CharacterSelectScreen : MonoBehaviour
{
    public const int SquadSize = 4;

    public List<CharacterProfile> characters;
    public List<CharacterCard> cards;
    public SquadSlot[] slots;
    public LineupScroller scroller;

    [Header("Status")]
    public TextMeshProUGUI counterText;
    public RectTransform counterRoot;
    public TextMeshProUGUI hintText;
    public RectTransform rosterRoot;

    [Header("Play")]
    public Button playButton;
    public Image playImage;
    public TextMeshProUGUI playLabel;
    public Sprite playReadySprite;
    public Sprite playLockedSprite;
    public SceneLoader sceneLoader;
    public string playScene = "LevelSelect";
    public string backScene = "MainMenu";

    [Header("Name block (focused keeper)")]
    public CanvasGroup intelGroup;
    public TextMeshProUGUI intelName;
    [Tooltip("Slanted tag above the name: tinted with the keeper's colour, gold once they're in the squad")]
    public Image intelTag;
    public TextMeshProUGUI intelStatus;
    public Image intelGlow;
    [Tooltip("One dot per keeper under the name, showing where you are in the lineup")]
    public Image[] lineupDots;
    [Tooltip("Health, power and range pips")]
    public StatPips[] intelStats;

    [Header("Feedback")]
    public RectTransform flyLayer;
    [Tooltip("Optional: recruit / landing / deploy sparkles")]
    public CharacterSelectFx fx;
    public CanvasGroup screenGroup;
    public Color readyColor = new Color32(150, 240, 110, 255);
    public Color pendingColor = new Color32(255, 222, 99, 255);
    public Color warnColor = new Color32(255, 120, 96, 255);

    // Which character sits in each squad slot (-1 = empty)
    readonly int[] squad = { -1, -1, -1, -1 };
    readonly Coroutine[] flights = new Coroutine[SquadSize];
    readonly GameObject[] flyers = new GameObject[SquadSize];
    int shownIntel = -1;
    Coroutine intelSwap;
    Coroutine hintFlash;
    float counterPunch;
    float playPunch;
    bool wasFull;
    bool leaving;

    public int Count
    {
        get
        {
            int n = 0;
            foreach (int c in squad)
            {
                if (c >= 0)
                {
                    n++;
                }
            }
            return n;
        }
    }

    public void UpdateUI(List<CharacterCard> newCards)
    {
        characters.Clear();
        foreach(CharacterCard c in cards)
        {
            Destroy(c.gameObject);
        }
        cards.Clear();

        foreach (CharacterCard c in newCards)
        {
            cards.Add(c);
            characters.Add(c.tower.tower.profile);
        }

        for (int i = 0; i < cards.Count; i++)
        {
            int index = i;
            cards[i].Setup(i * 1.37f);
            cards[i].button.onClick.AddListener(() => Toggle(index));
            cards[i].Appear(0.06f * i);
        }
        for (int s = 0; s < slots.Length; s++)
        {
            int slot = s;
            slots[s].button.onClick.AddListener(() => RemoveFromSlot(slot));
            slots[s].Clear(false);
        }
        playButton.onClick.AddListener(Play);

        LoadSavedSquad();

        scroller.FocusChanged += ShowIntel;
        // Start in the middle of the lineup so keepers fill both sides
        int middle = characters.Count / 2;
        scroller.ScrollTo(middle, true);
        ShowIntel(middle);
        Refresh();
    }

    // Coming back from the level select ("Change Team"): start from the squad the player already chose
    void LoadSavedSquad()
    {
        if (PlayerManager.instance == null)
        {
            return;
        }
        int s = 0;
        foreach (TowerEntry entry in PlayerManager.instance.equippedTowers)
        {
            if (Inventory.IsEmpty(entry) || s >= SquadSize)
            {
                continue;
            }
            int index = IndexOf(entry.tower);
            if (index < 0 || SlotOf(index) >= 0)
            {
                continue;
            }
            squad[s] = index;
            cards[index].SetSelected(true, s + 1, false);
            slots[s].Fill(characters[index], false);
            s++;
        }
    }

    public void Toggle(int index)
    {
        if (leaving)
        {
            return;
        }
        int slot = SlotOf(index);
        if (slot >= 0)
        {
            RemoveFromSlot(slot);
            return;
        }

        scroller.ScrollTo(index);
        slot = FirstEmpty();
        if (slot < 0)
        {
            // No more than four: shake the keeper and the roster, explain how to swap
            cards[index].Reject();
            foreach (SquadSlot s in slots)
            {
                s.Shake();
            }
            counterPunch = 1;
            Flash("Squad full! Tap a squad member to remove them", warnColor);
            return;
        }

        // Adds card to squad, which allows it to be saved
        squad[slot] = index;
        cards[index].SetSelected(true, slot + 1, true);
        slots[slot].Reserve();
        if (fx)
        {
            fx.Recruit(cards[index], characters[index].accent);
        }
        flights[slot] = StartCoroutine(FlyToSlot(index, slot));
        counterPunch = 1;
        Refresh();
    }

    public void RemoveFromSlot(int slot)
    {
        if (leaving || squad[slot] < 0)
        {
            return;
        }
        int index = squad[slot];
        squad[slot] = -1;
        if (flights[slot] != null)
        {
            StopCoroutine(flights[slot]);
            flights[slot] = null;
        }
        if (flyers[slot])
        {
            Destroy(flyers[slot]);
        }
        cards[index].SetSelected(false, 0, true);
        if (fx && slots[slot].IsFilled)
        {
            fx.Puff(slots[slot].disc.rectTransform);
        }
        slots[slot].Clear(true);
        counterPunch = 1;
        Refresh();
    }

    // The keeper's art lifts off the platform and arcs down into the roster slot
    IEnumerator FlyToSlot(int index, int slot)
    {
        RectTransform from = cards[index].hero.rectTransform;
        RectTransform to = slots[slot].disc.rectTransform;

        GameObject go = new GameObject("Flyer", typeof(RectTransform));
        go.transform.SetParent(flyLayer, false);
        flyers[slot] = go;
        Image image = go.AddComponent<Image>();
        image.sprite = characters[index].heroSprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        RectTransform rt = image.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        GetRect(from, out Vector2 startPos, out Vector2 startSize);
        const float duration = 0.42f;
        bool landed = false;
        for (float t = 0; t < 1; t += Time.unscaledDeltaTime / duration)
        {
            // The target can move (layout), so measure it every frame
            GetRect(to, out Vector2 endPos, out Vector2 endSize);
            float e = t < 0.5f ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) / 2;
            Vector2 p = Vector2.Lerp(startPos, endPos, e);
            p.y += Mathf.Sin(e * Mathf.PI) * 70f;
            rt.anchoredPosition = p;
            rt.sizeDelta = Vector2.Lerp(startSize, endSize, e);
            image.color = new Color(1, 1, 1, t < 0.75f ? 1 : Mathf.InverseLerp(1f, 0.75f, t));
            if (fx && t < 0.75f)
            {
                fx.Trail(rt);
            }
            if (!landed && t >= 0.75f)
            {
                landed = true;
                Land(index, slot);
            }
            yield return null;
        }
        if (!landed)
        {
            Land(index, slot);
        }
        Destroy(go);
        flyers[slot] = null;
        flights[slot] = null;
    }

    void Land(int index, int slot)
    {
        slots[slot].Fill(characters[index], true);
        if (fx)
        {
            fx.Land(slots[slot].disc.rectTransform, characters[index].accent);
        }
    }

    void GetRect(RectTransform target, out Vector2 centre, out Vector2 size)
    {
        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);
        Vector2 a = flyLayer.InverseTransformPoint(corners[0]);
        Vector2 b = flyLayer.InverseTransformPoint(corners[2]);
        centre = (a + b) / 2 - flyLayer.rect.center;
        size = b - a;
    }

    void Refresh()
    {
        int count = Count;
        bool full = count == SquadSize;

        counterText.text = count + "/" + SquadSize;
        counterText.color = full ? readyColor : pendingColor;

        if (full && !wasFull)
        {
            playPunch = 1; // DEPLOY just unlocked: pop it so the eye goes there
        }
        if (fx && full != wasFull)
        {
            fx.SetDeployReady((RectTransform)playButton.transform, full, full);
        }
        wasFull = full;
        playButton.interactable = full;
        playImage.sprite = full ? playReadySprite : playLockedSprite;
        playLabel.color = full ? Color.white : new Color(1, 1, 1, 0.75f);

        int next = FirstEmpty();
        for (int s = 0; s < slots.Length; s++)
        {
            slots[s].SetNext(s == next);
        }
        foreach (CharacterCard card in cards)
        {
            card.SetTeamFull(full);
        }

        if (hintFlash == null)
        {
            SetHint();
        }
        UpdateIntelStatus();
    }

    void SetHint()
    {
        int count = Count;
        if (count == 0)
        {
            hintText.text = "Swipe and tap keepers\nto add them";
            hintText.color = Color.white;
        }
        else if (count < SquadSize)
        {
            int left = SquadSize - count;
            hintText.text = "Add " + left + " more keeper" + (left == 1 ? "" : "s");
            hintText.color = pendingColor;
        }
        else
        {
            hintText.text = "Squad ready! Tap Deploy";
            hintText.color = readyColor;
        }
    }

    void Flash(string message, Color color)
    {
        if (hintFlash != null)
        {
            StopCoroutine(hintFlash);
        }
        hintFlash = StartCoroutine(FlashRoutine(message, color));
    }

    IEnumerator FlashRoutine(string message, Color color)
    {
        hintText.text = message;
        hintText.color = color;
        yield return new WaitForSecondsRealtime(1.8f);
        hintFlash = null;
        SetHint();
    }

    void ShowIntel(int index)
    {
        if (index == shownIntel || index < 0 || index >= characters.Count)
        {
            return;
        }
        int previous = shownIntel;
        shownIntel = index;
        if (intelSwap != null)
        {
            StopCoroutine(intelSwap);
        }
        if (previous < 0)
        {
            FillIntel(index);
            intelGroup.alpha = 1;
        }
        else
        {
            intelSwap = StartCoroutine(SwapIntel(index, index > previous ? 1 : -1));
        }
    }

    // The name slides out and the next one slides in from the side you swiped from
    IEnumerator SwapIntel(int index, float direction)
    {
        RectTransform rt = (RectTransform)intelGroup.transform;
        Vector2 home = Vector2.zero;
        for (float t = 1 - intelGroup.alpha; t < 1; t += Time.unscaledDeltaTime / 0.09f)
        {
            intelGroup.alpha = 1 - t;
            rt.anchoredPosition = home + new Vector2(-40 * direction * t, 0);
            yield return null;
        }
        FillIntel(index);
        for (float t = 0; t < 1; t += Time.unscaledDeltaTime / 0.22f)
        {
            float e = 1 - Mathf.Pow(1 - t, 3);
            intelGroup.alpha = e;
            rt.anchoredPosition = home + new Vector2(60 * direction * (1 - e), 0);
            yield return null;
        }
        intelGroup.alpha = 1;
        rt.anchoredPosition = home;
        intelSwap = null;
    }

    void FillIntel(int index)
    {
        CharacterProfile p = characters[index];
        intelName.text = p.tower.name;
        if (intelStats != null && intelStats.Length >= 3)
        {
            intelStats[0].Set(p.health, true);
            intelStats[1].Set(p.power, true);
            intelStats[2].Set(p.range, true);
        }
        UpdateIntelStatus();
    }

    void UpdateIntelStatus()
    {
        if (shownIntel < 0)
        {
            return;
        }
        CharacterProfile p = characters[shownIntel];
        int slot = SlotOf(shownIntel);
        if (slot >= 0)
        {
            intelStatus.text = "IN SQUAD  #" + (slot + 1);
            intelTag.color = new Color32(255, 196, 60, 255);
        }
        else if (Count >= SquadSize)
        {
            intelStatus.text = "SQUAD FULL";
            intelTag.color = new Color32(120, 112, 160, 255);
        }
        else
        {
            intelStatus.text = "TAP TO ADD";
            intelTag.color = p.accent;
        }
        Color glow = p.accent;
        glow.a = intelGlow.color.a;
        intelGlow.color = glow;

        for (int i = 0; i < lineupDots.Length; i++)
        {
            lineupDots[i].color = SlotOf(i) >= 0 ? (Color)new Color32(255, 214, 90, 255) : new Color(1, 1, 1, i == shownIntel ? 1f : 0.35f);
        }
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;

        // Counter gives a small punch whenever the squad changes
        counterPunch = Mathf.Max(0, counterPunch - dt * 4f);
        float s = 1 + Mathf.Sin((1 - counterPunch) * Mathf.PI) * 0.18f * (counterPunch > 0 ? 1 : 0);
        counterRoot.localScale = new Vector3(s, s, 1);

        // DEPLOY pops when it unlocks, then gently breathes while the squad is full
        playPunch = Mathf.Max(0, playPunch - dt * 2.5f);
        float pop = playPunch > 0 ? Mathf.Sin((1 - playPunch) * Mathf.PI * 2f) * playPunch * 0.18f : 0;
        float p = Count == SquadSize && !leaving ? 1 + Mathf.Sin(Time.unscaledTime * 3f) * 0.025f + pop : 1;
        playButton.transform.localScale = new Vector3(p, p, 1);

        // The dot for the keeper in the middle grows; the soft colour glow behind the name breathes
        for (int i = 0; i < lineupDots.Length; i++)
        {
            float target = i == shownIntel ? 1.35f : 1f;
            Transform dot = lineupDots[i].transform;
            dot.localScale = Vector3.one * Mathf.Lerp(dot.localScale.x, target, 1 - Mathf.Exp(-dt * 14f));
        }
        Color g = intelGlow.color;
        g.a = 0.32f + 0.08f * Mathf.Sin(Time.unscaledTime * 1.6f);
        intelGlow.color = g;
    }

    public void Play()
    {
        if (leaving || Count < SquadSize)
        {
            return;
        }
        leaving = true;
        if (fx)
        {
            fx.SetDeployReady((RectTransform)playButton.transform, false, false);
        }
        SaveSquad();
        Leave(playScene, true);
    }

    public void Back()
    {
        if (leaving)
        {
            return;
        }
        // Back cancels edits; the last squad confirmed with Play stays saved
        leaving = true;
        Leave(backScene, false);
    }

    // Puts the squad into the existing Inventory (slot order) so SceneLoader saves it to PlayerManager
    void SaveSquad()
    {
        Inventory inventory = Inventory.instance;
        inventory.ClearInventory();
        foreach (CharacterProfile p in characters)
        {
            inventory.Add(new TowerEntry { tower = p.tower, amount = 1 });
        }
        foreach (int index in squad)
        {
            if (index >= 0)
            {
                inventory.EquipTower(new TowerEntry { tower = characters[index].tower, amount = 1 });
            }
        }
    }

    // SceneTransition cross-fades this screen into the next one
    void Leave(string scene, bool saveTeam)
    {
        Time.timeScale = 1;
        if (saveTeam && sceneLoader)
        {
            // Same path the old team screen used: saves Inventory -> PlayerManager, then loads
            sceneLoader.LoadScene(scene);
        }
        else
        {
            SceneTransition.Load(scene);
        }
    }

    int SlotOf(int index)
    {
        for (int s = 0; s < squad.Length; s++)
        {
            if (squad[s] == index)
            {
                return s;
            }
        }
        return -1;
    }

    int FirstEmpty()
    {
        for (int s = 0; s < squad.Length; s++)
        {
            if (squad[s] < 0)
            {
                return s;
            }
        }
        return -1;
    }

    int IndexOf(Tower tower)
    {
        for (int i = 0; i < characters.Count; i++)
        {
            if (characters[i].tower == tower || (characters[i].tower && tower && characters[i].tower.towerId == tower.towerId))
            {
                return i;
            }
        }
        return -1;
    }
}
