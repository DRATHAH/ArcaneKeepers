using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Everything about the keeper in the middle, right under their name: the card bar, a line saying what
// it's for (or an Upgrade button once enough cards are collected), three icon chips (health, damage or
// healing, range) and a short description. Upgrading spends the cards, counts the numbers up and bursts
// over the level badge.
public class TowerInfoBlock : MonoBehaviour
{
    public TowerProgressBar progress;
    [Tooltip("Line under the card bar: cards still needed, or how to level up")]
    public TextMeshProUGUI cardsHint;
    [Tooltip("On the card bar; only interactable while an upgrade is affordable")]
    public Button upgradeButton;

    [Header("Stats")]
    public TextMeshProUGUI healthValue;
    public TextMeshProUGUI powerValue;
    [Tooltip("Chip title over the power number: DAMAGE or HEALING")]
    public TextMeshProUGUI powerLabel;
    public Image powerIcon;
    [Tooltip("Green \"+n\" tag on the power chip while an upgrade is affordable")]
    public TextMeshProUGUI powerGain;
    public Sprite damageIcon;
    public Sprite healIcon;
    [Tooltip("FAR / MID / CLOSE")]
    public TextMeshProUGUI rangeValue;
    public TextMeshProUGUI description;

    [Tooltip("Optional: burst on upgrade")]
    public CharacterSelectFx fx;

    // Fired after a successful upgrade
    public event Action<TowerEntry> Upgraded;

    [Tooltip("Full size of the stat values; long words shrink below it to fit their tile")]
    public float valueSize = 64;

    TowerEntry entry;
    Color accent = Color.white;

    private void Awake()
    {
        upgradeButton.onClick.AddListener(Upgrade);
    }

    public void Show(TowerEntry tower, CharacterProfile profile, bool animate)
    {
        if (Inventory.IsEmpty(tower))
        {
            return;
        }
        entry = tower;
        accent = profile != null ? profile.accent : Color.white;
        description.text = tower.tower.description;
        Fill(animate);
    }

    // Numbers for the current level; the bar and the row under it follow the card count
    void Fill(bool animate)
    {
        Tower tower = entry.tower;
        healthValue.text = tower.health.ToString();
        bool heals = Heals(tower);
        powerLabel.text = heals ? "HEALING" : "DAMAGE";
        powerIcon.sprite = heals ? healIcon : damageIcon;
        progress.Set(entry, animate);
        powerValue.text = Power(tower, entry.level).ToString();
        int gain = Gain();
        powerGain.transform.parent.gameObject.SetActive(gain > 0);
        powerGain.text = "+" + gain;
        rangeValue.text = RangeWord(tower);

        FitValues();

        // The card bar itself is the upgrade control; it only takes taps while an upgrade is affordable
        bool ready = progress.Ready;
        upgradeButton.interactable = ready;
        if (RarityCalculation.IsMaxLevel(entry))
        {
            cardsHint.text = "Fully upgraded!";
            cardsHint.color = new Color32(255, 214, 90, 255);
        }
        else if (ready)
        {
            cardsHint.text = "Tap the bar to reach level " + (entry.level + 1) + "!";
            cardsHint.color = new Color32(170, 245, 120, 255);
        }
        else
        {
            int missing = RarityCalculation.GetCardReqs(entry) - entry.amount;
            cardsHint.text = "Collect " + missing + " more" + (missing == 1 ? " card" : " cards") + " to upgrade";
            cardsHint.color = new Color(1, 1, 1, 0.85f);
        }
    }

    // Each tile's icon + value keeps its full size unless the word is too long for the tile ("CLOSE"),
    // then the value shrinks just enough to fit inside it
    void FitValues()
    {
        foreach (TextMeshProUGUI value in new[] { healthValue, powerValue, rangeValue })
        {
            RectTransform row = (RectTransform)value.transform.parent;
            HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
            float icon = 0;
            foreach (Transform child in row)
            {
                if (child != value.transform && child.gameObject.activeSelf)
                {
                    LayoutElement e = child.GetComponent<LayoutElement>();
                    icon += (e ? e.preferredWidth : 0) + (layout ? layout.spacing : 0);
                }
            }
            float room = ((RectTransform)row.parent).rect.width - 24 - icon;
            value.fontSize = valueSize;
            float width = value.GetPreferredValues(value.text, float.PositiveInfinity, float.PositiveInfinity).x;
            if (width > room && width > 0)
            {
                value.fontSize = Mathf.Floor(valueSize * room / width);
            }
        }
    }

    // What the next upgrade adds to the power number, while it's affordable (0 otherwise)
    int Gain()
    {
        if (!progress.Ready || RarityCalculation.IsMaxLevel(entry))
        {
            return 0;
        }
        return Power(entry.tower, entry.level + 1) - Power(entry.tower, entry.level);
    }

    // Healers (Cleric, Knight) have no damaging attack
    public static bool Heals(Tower tower)
    {
        bool damage = (tower.closeAttack != null && tower.closeAttack.attackType == Attack.AttackType.Damage)
            || (tower.farAttack != null && tower.farAttack.attackType == Attack.AttackType.Damage);
        return !damage;
    }

    // How far the tower reaches with its main kind of attack (damage, or healing for healers)
    public static string RangeWord(Tower tower)
    {
        Attack.AttackType main = Heals(tower) ? Attack.AttackType.Healing : Attack.AttackType.Damage;
        float range = 0;
        foreach (Attack attack in new[] { tower.closeAttack, tower.farAttack })
        {
            if (attack != null && attack.attackType == main)
            {
                range = Mathf.Max(range, attack.range);
            }
        }
        return range >= 8 ? "FAR" : range >= 5 ? "MID" : "CLOSE";
    }

    // Same scaling the towers use in a level: base damage (or healing) x (1 + levelScaling per level)
    public static int Power(Tower tower, int level)
    {
        int damage = 0;
        if (tower.closeAttack != null)
        {
            damage = tower.closeAttack.damage;
        }
        if (tower.farAttack != null)
        {
            damage = Mathf.Max(damage, tower.farAttack.damage);
        }
        return Mathf.RoundToInt(damage * Mathf.Max(1, 1 + tower.levelScaling * (level - 1)));
    }

    public void Upgrade()
    {
        if (entry == null || !Inventory.instance || !progress.Ready)
        {
            return;
        }
        int oldHealth = entry.tower.health;
        int oldPower = Power(entry.tower, entry.level);
        if (!Inventory.instance.TryUpgrade(entry))
        {
            return;
        }
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound("ButtonPop");
        }

        progress.LevelUp();
        Fill(true);
        StartCoroutine(CountUp(healthValue, oldHealth, entry.tower.health));
        StartCoroutine(CountUp(powerValue, oldPower, Power(entry.tower, entry.level)));
        if (fx && progress.levelBadge)
        {
            fx.Land(progress.levelBadge, accent);
        }
        Upgraded?.Invoke(entry);
    }

    IEnumerator CountUp(TextMeshProUGUI text, int from, int to)
    {
        if (from == to)
        {
            yield break;
        }
        RectTransform rt = text.rectTransform;
        yield return FxKit.Tween(0.5f, t =>
        {
            text.text = Mathf.RoundToInt(Mathf.Lerp(from, to, FxKit.EaseOut(t))).ToString();
            float s = 1 + Mathf.Sin(t * Mathf.PI) * 0.25f;
            rt.localScale = new Vector3(s, s, 1);
        });
        rt.localScale = Vector3.one;
    }
}
