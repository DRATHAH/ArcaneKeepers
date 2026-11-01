using UnityEngine;

namespace ArcaneKeepers.Tutorial
{
    /// <summary>
    /// Glow drawn flat on a lane tile in the world (so it sits in the level's perspective): a soft
    /// rounded square that breathes, plus a ring that ripples outward every so often.
    /// </summary>
    public class TileHighlight : MonoBehaviour
    {
        SpriteRenderer glow;
        SpriteRenderer ripple;
        Color color;
        float t;
        float visible;
        bool showing;
        float size = 2f;

        public Tile Tile { get; private set; }

        public static TileHighlight Create(TutorialTheme theme, Tile tile, Color tint)
        {
            var go = new GameObject("TutorialTileGlow");
            var h = go.AddComponent<TileHighlight>();
            h.Build(theme, tile, tint);
            return h;
        }

        void Build(TutorialTheme theme, Tile tile, Color tint)
        {
            Tile = tile;
            color = tint;
            Transform tr = tile.transform;
            transform.SetParent(tr, false);
            // the grid is rotated flat (90 on X): local -Z is "up" off the ground
            transform.localPosition = new Vector3(0f, 0f, -0.03f);
            transform.localRotation = Quaternion.identity;

            if (tr is RectTransform rt && rt.rect.width > 0.01f)
            {
                size = Mathf.Min(rt.rect.width, rt.rect.height);
            }
            SpriteRenderer tileSr = tile.GetComponent<SpriteRenderer>();
            int order = tileSr ? tileSr.sortingOrder + 1 : 1;
            int layer = tileSr ? tileSr.sortingLayerID : 0;

            glow = MakeSr("Glow", theme.tileGlow, order, layer);
            ripple = MakeSr("Ripple", theme.tileGlow, order, layer);
            showing = true;
            Update();
        }

        SpriteRenderer MakeSr(string n, Sprite s, int order, int layer)
        {
            var go = new GameObject(n);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.sortingOrder = order;
            sr.sortingLayerID = layer;
            sr.color = Color.clear;
            return sr;
        }

        public void Hide()
        {
            showing = false;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            t += dt;
            visible = Mathf.MoveTowards(visible, showing ? 1f : 0f, dt * 4f);
            if (!showing && visible <= 0f)
            {
                Destroy(gameObject);
                return;
            }
            float spriteUnits = glow.sprite ? glow.sprite.bounds.size.x : 2.56f;
            float baseScale = size / spriteUnits * 1.04f;
            float pop = TutorialKit.EaseOutBack(Mathf.Clamp01(t / 0.4f), 2f);
            float breathe = 1f + Mathf.Sin(t * 3.4f) * 0.03f;
            glow.transform.localScale = Vector3.one * baseScale * pop * breathe;
            float v = TutorialKit.Smooth(visible);
            glow.color = TutorialKit.A(Color.Lerp(color, Color.white, 0.25f), (0.85f + Mathf.Sin(t * 3.4f) * 0.15f) * v);

            float rp = (t % 1.3f) / 1.3f;
            ripple.transform.localScale = Vector3.one * baseScale * Mathf.Lerp(1f, 1.45f, TutorialKit.EaseOut(rp));
            ripple.color = TutorialKit.A(color, (1f - rp) * 0.8f * v);
        }
    }
}
