using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Plants vs Zombies style idle: bends a UI image like jelly while its feet stay planted.
// The image is split into a grid; columns sway and breathe out of phase, so each
// character in a group sprite moves a little on its own.
[RequireComponent(typeof(Graphic))]
public class IdleWobble : BaseMeshEffect
{
    [Header("Sway (side to side, strongest at the top)")]
    public float swayAmount = 0.025f;   // fraction of the image height
    public float swaySpeed = 1.6f;

    [Header("Breathe (squash and stretch)")]
    public float breatheAmount = 0.03f;
    public float breatheSpeed = 2.4f;

    [Header("Lurch (whole group leans forward and back)")]
    public float lurchAmount = 0f;
    public float lurchSpeed = 1f;

    [Header("Bounce (whole image hops up and lands)")]
    public float bounceAmount = 0f;     // fraction of the image height
    public float bounceSpeed = 3f;

    [Tooltip("How out of step the left and right sides of the image move (radians across its width)")]
    public float phaseSpread = 9f;
    public float timeOffset;

    const int Columns = 32;
    const int Rows = 12;

    readonly List<UIVertex> quad = new List<UIVertex>();

    private void Update()
    {
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
        foreach (UIVertex v in quad)
        {
            min = Vector2.Min(min, v.position);
            max = Vector2.Max(max, v.position);
            uvMin = Vector2.Min(uvMin, v.uv0);
            uvMax = Vector2.Max(uvMax, v.uv0);
        }
        Color32 color = quad[0].color;
        Vector2 size = max - min;
        float t = (Application.isPlaying ? Time.unscaledTime : 0) + timeOffset;
        float hop = Mathf.Abs(Mathf.Sin(t * bounceSpeed)) * bounceAmount * size.y;

        vh.Clear();
        for (int y = 0; y <= Rows; y++)
        {
            float v = (float)y / Rows;
            for (int x = 0; x <= Columns; x++)
            {
                float u = (float)x / Columns;
                float phase = u * phaseSpread;

                // Feet (v = 0) never move; the higher up, the more it bends
                float bend = v * v;
                float sway = Mathf.Sin(t * swaySpeed + phase) * swayAmount * bend
                    + Mathf.Sin(t * lurchSpeed) * lurchAmount * bend;
                float breathe = Mathf.Sin(t * breatheSpeed + phase * 0.7f) * breatheAmount * v;

                Vector3 pos = new Vector3(
                    min.x + u * size.x + sway * size.y,
                    min.y + v * size.y * (1 + breathe) + hop,
                    0);
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
