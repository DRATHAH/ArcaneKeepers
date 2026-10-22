using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Understated "standing around" idle for a full-body UI image: slow breathing in the chest,
// small head turns and nods, and a weight shift from hip to hip. Feet stay planted.
// Motion comes from smooth noise rather than one sine wave, so it never looks looped.
[RequireComponent(typeof(Graphic))]
public class HeroIdle : BaseMeshEffect
{
    [Header("Breathing")]
    [Tooltip("Seconds per breath")]
    public float breathPeriod = 3.8f;
    [Tooltip("How much the upper body rises, as a fraction of the image height")]
    public float breathRise = 0.010f;
    [Tooltip("How much the chest widens, as a fraction of the image width")]
    public float breathWiden = 0.008f;

    [Header("Weight shift")]
    [Tooltip("Seconds for a full left-right-left shift")]
    public float shiftPeriod = 8f;
    [Tooltip("How far the hips move sideways, as a fraction of the image width")]
    public float hipShift = 0.012f;
    [Tooltip("How much the shoulders lean back the other way (0 = none)")]
    public float counterLean = 0.5f;

    [Header("Head")]
    [Tooltip("Where the neck is, as a fraction of the image height")]
    [Range(0, 1)] public float neckHeight = 0.66f;
    [Range(0, 1)] public float neckX = 0.5f;
    [Tooltip("Largest head tilt in degrees")]
    public float headTurn = 2.2f;
    [Tooltip("Head nod in degrees that follows the breath")]
    public float headNod = 0.5f;

    [Tooltip("Desynchronises characters standing next to each other")]
    public float timeOffset;

    const int Columns = 12;
    const int Rows = 16;

    readonly List<UIVertex> quad = new List<UIVertex>();
    float perk;

    // A brief, small "stand up straight" when this character is picked
    public void Perk()
    {
        perk = 1f;
    }

    private void Update()
    {
        if (perk > 0)
        {
            perk = Mathf.Max(0, perk - Time.unscaledDeltaTime * 2.2f);
        }
        if (graphic)
        {
            graphic.SetVerticesDirty();
        }
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount != 4)
        {
            return;
        }

        quad.Clear();
        vh.GetUIVertexStream(quad);
        Vector2 min = quad[0].position, max = quad[0].position;
        Vector2 uvMin = quad[0].uv0, uvMax = quad[0].uv0;
        foreach (UIVertex vert in quad)
        {
            min = Vector2.Min(min, vert.position);
            max = Vector2.Max(max, vert.position);
            uvMin = Vector2.Min(uvMin, vert.uv0);
            uvMax = Vector2.Max(uvMax, vert.uv0);
        }
        Color32 color = quad[0].color;
        Vector2 size = max - min;

        float t = (Application.isPlaying ? Time.unscaledTime : 0) + timeOffset;

        // Breath: slightly quicker in than out, with a little noise in depth
        float breathPhase = t * Mathf.PI * 2 / Mathf.Max(0.5f, breathPeriod);
        float breath = Mathf.Sin(breathPhase) + 0.25f * Mathf.Sin(breathPhase * 2 + 1.3f);
        breath = breath * 0.4f + 0.5f; // roughly 0..1
        breath *= 0.85f + 0.3f * Mathf.PerlinNoise(t * 0.15f, timeOffset);

        // Weight shift: slow and uneven
        float shiftPhase = t * Mathf.PI * 2 / Mathf.Max(1f, shiftPeriod);
        float shift = (Mathf.Sin(shiftPhase) + 0.35f * Mathf.Sin(shiftPhase * 2.3f + 0.7f)) / 1.35f;

        // Head: drifts between small glances, plus a nod with each breath
        float glance = (Mathf.PerlinNoise(t * 0.22f, 7.3f + timeOffset) - 0.5f) * 2f;
        float angle = glance * headTurn + (breath - 0.5f) * headNod;
        float perkEase = perk * perk * (3 - 2 * perk);

        Vector2 neck = new Vector2(min.x + neckX * size.x, min.y + neckHeight * size.y);

        vh.Clear();
        for (int y = 0; y <= Rows; y++)
        {
            float v = (float)y / Rows;
            for (int x = 0; x <= Columns; x++)
            {
                float u = (float)x / Columns;
                Vector2 pos = new Vector2(min.x + u * size.x, min.y + v * size.y);

                // Hips carry the weight shift; shoulders lean back a little the other way
                float hips = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.02f, 0.42f, v));
                float upper = Mathf.Max(0, v - 0.42f) / 0.58f;
                pos.x += shift * hipShift * size.x * (hips - counterLean * upper * 0.6f);
                // Settling onto one leg lowers the body a touch
                pos.y -= Mathf.Abs(shift) * 0.004f * size.y * hips;

                // Chest rises and widens; everything above it rides along
                float rise = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.25f, 0.65f, v));
                pos.y += (breath * breathRise + perkEase * 0.02f) * size.y * rise;
                float chest = Mathf.Exp(-Mathf.Pow((v - 0.5f) / 0.18f, 2));
                pos.x += (u - 0.5f) * breath * breathWiden * size.x * chest * 2f;

                // Head rotates around the neck, blended in so the neck doesn't tear
                float head = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(neckHeight - 0.1f, neckHeight + 0.08f, v));
                if (head > 0)
                {
                    float a = angle * head * Mathf.Deg2Rad;
                    Vector2 pivot = neck + new Vector2(0, (breath * breathRise + perkEase * 0.02f) * size.y);
                    Vector2 d = pos - pivot;
                    float cos = Mathf.Cos(a), sin = Mathf.Sin(a);
                    pos = pivot + new Vector2(d.x * cos - d.y * sin, d.x * sin + d.y * cos);
                }

                Vector2 uv = new Vector2(Mathf.Lerp(uvMin.x, uvMax.x, u), Mathf.Lerp(uvMin.y, uvMax.y, v));
                vh.AddVert(pos, color, uv);
            }
        }

        int stride = Columns + 1;
        for (int y = 0; y < Rows; y++)
        {
            for (int x = 0; x < Columns; x++)
            {
                int i = y * stride + x;
                vh.AddTriangle(i, i + stride, i + stride + 1);
                vh.AddTriangle(i, i + stride + 1, i + 1);
            }
        }
    }
}
