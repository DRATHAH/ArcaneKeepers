using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Night-forest placement grid (Level4, Level5): root net from GenerateRoots.py lying on the dirt path,
// glowing mushroom clusters standing at some root crossings (unlit, facing the camera) with a soft
// glow pool on the ground under each.
// run_script AgentScripts/PlaceForestRoots.cs PlaceForestRoots.RunAll   (both levels)
// PlaceForestRoots.Place(scenePath, "Level4", seed) for one. Idempotent: rebuilds Level/ForestRoots.
public static class PlaceForestRoots
{
    const string Dir = "Assets/Sprites/Roots/";
    // grid (same as the bridge levels): 10x5 cells of 2 units from (-9.16, -5.04); art margin 0.45
    const float GX0 = -9.16f, GZ0 = -5.04f, Cell = 2f, Margin = 0.45f, Left = 1.95f;
    const int Cols = 10, Rows = 5;
    const float Ground = -0.25f;   // terrain is flat at -0.25 over the whole grid

    public static string RunAll()
    {
        Place("Assets/Scenes/Levels/World1/Level4.unity", "Level4", 4);
        Place("Assets/Scenes/Levels/World1/Level5.unity", "Level5", 5);
        return "ok";
    }

    public static string Place(string scenePath, string level, int seed)
    {
        string art = Dir + level + "/";
        ImportSprite(art + "forest_roots_floor.png", 110, Vector2.zero);
        AssetDatabase.Refresh();
        ImportSprite(art + "forest_shroom_glow.png", 128, new Vector2(0.5f, 0.5f));

        var tilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UserInterface/LevelHUD/Prefabs/Tile.prefab");
        var litMat = tilePrefab.GetComponent<SpriteRenderer>().sharedMaterial;

        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        var unlitMat = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
        var old = GameObject.Find("ForestRoots");
        if (old) Object.DestroyImmediate(old);
        var holder = new GameObject("ForestRoots");
        var level0 = GameObject.Find("Background/Level");
        if (level0) holder.transform.SetParent(level0.transform, true);

        // root net: a mesh draped over the terrain (flat on the path, up the grass banks to the trees),
        // textured with the floor art; same Sprite-Lit material as the sprites so it shades the same
        BuildRootMesh(holder, level, art, litMat);

        // roots climbing the castle: real low-poly 3D roots (flat-shaded 5-sided tubes, the trees'
        // ForestBark material) so they match the scene's low-poly trees and stumps
        BuildCastleRoots(holder, level, seed);

        // mushrooms: copies of the scene's own low-poly GlowingMushrooms clusters (emissive teal /
        // violet), at ~22% of the crossings (never two side by side) + one at each castle wall foot.
        // Their point lights are stripped (the scene already has many); a soft ground glow stands in.
        var rnd = new System.Random(seed);
        var glowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(art + "forest_shroom_glow.png");
        var src = GameObject.Find("GlowingMushrooms").transform.Cast<Transform>()
            .Where(t => t.name.StartsWith("Cluster") && t.GetComponentInChildren<MeshRenderer>()).ToList();
        System.Func<Transform, bool> isTeal = t => t.GetComponentInChildren<MeshRenderer>().sharedMaterial.name.Contains("Teal");
        var teal = src.Where(isTeal).ToList();
        var violet = src.Where(t => !isTeal(t)).ToList();
        float tealShare = level == "Level4" ? 1f : 0f;   // one colour per level: calmer
        var cluster = new GameObject("Mushrooms");
        cluster.transform.SetParent(holder.transform, false);
        System.Action<Vector3, string, float> place = (pos, name, scale) =>
        {
            bool t = rnd.NextDouble() < tealShare;
            var pool = t ? teal : violet;
            var go = Object.Instantiate(pool[rnd.Next(pool.Count)].gameObject, cluster.transform);
            go.name = name;
            foreach (var l in go.GetComponentsInChildren<Light>(true)) Object.DestroyImmediate(l);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0, rnd.Next(360), 0);
            go.transform.localScale = Vector3.one * scale;
            var g = new GameObject("Glow");
            g.transform.SetParent(go.transform, false);
            g.transform.position = new Vector3(pos.x, Ground + 0.02f, pos.z);
            g.transform.rotation = Quaternion.Euler(90, 0, 0);
            g.transform.localScale = Vector3.one * 1.1f / scale;
            var gr = g.AddComponent<SpriteRenderer>();
            gr.sprite = glowSprite;
            gr.sharedMaterial = unlitMat;
            gr.color = t ? new Color(0.35f, 1f, 0.85f, 0.22f) : new Color(0.75f, 0.45f, 1f, 0.22f);
        };
        // only along the grid's top and bottom edge (never inside the play area), every 3rd crossing,
        // all one size: reads as a tidy border instead of clutter
        int n = 0;
        foreach (int j in new[] { 0, Rows })
        {
            int start = rnd.Next(1, 3);
            for (int i = start; i < Cols; i += 3)
            {
                float x = GX0 + i * Cell + Rf(rnd, -0.1f, 0.1f);
                float z = GZ0 + j * Cell + (j == 0 ? -0.25f : 0.25f);   // just outside the grid line
                place(new Vector3(x, Ground, z), "Shroom_" + i + "_" + j, 0.55f);
                n++;
            }
        }
        place(new Vector3(-9.95f, Ground, -5.45f), "Shroom_wall0", 0.6f);
        place(new Vector3(-10.45f, Ground, 4.62f), "Shroom_wall1", 0.6f);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return "ok " + scenePath + " shrooms=" + n;
    }

    // ------------------------------------------------------------------ 3D castle roots
    // Level4/5 castle faces: origin at the face's left foot, tangent along the face, outward normal.
    static readonly (Vector3 origin, Vector3 tangent, Vector3 normal, float width, float[] heights)[] Faces =
    {
        (new Vector3(-10.21f, 0, -7.41f), Vector3.forward, Vector3.right, 2.48f, new[] { 1.7f, 2.4f, 1.3f, 2.0f }),      // near jamb
        (new Vector3(-11.04f, 0, 4.42f), new Vector3(0.7071f, 0, 0.7071f), new Vector3(0.7071f, 0, -0.7071f), 1.17f, new[] { 1.5f, 1.0f, 1.3f }), // door-frame chamfer
        (new Vector3(-10.21f, 0, 5.25f), Vector3.forward, Vector3.right, 2.48f, new[] { 1.3f, 1.8f, 1.0f, 1.6f, 1.1f }), // far jamb
    };

    class RootPath { public List<Vector2> pts = new List<Vector2>(); public List<float> r = new List<float>(); }

    static void BuildCastleRoots(GameObject holder, string level, int seed)
    {
        var rnd = new System.Random(seed * 97 + 13);
        var terrain = Terrain.activeTerrain;
        System.Func<Vector3, float> groundAt = p => terrain ? terrain.SampleHeight(p) + terrain.transform.position.y : Ground;
        var verts = new List<Vector3>(); var norms = new List<Vector3>(); var tris = new List<int>();

        foreach (var f in Faces)
        {
            var paths = new List<RootPath>();
            float lo = 0.1f, hi = f.width - 0.1f;
            int n = f.heights.Length;
            for (int i = 0; i < n; i++)
            {
                float s = lo + (hi - lo) * (i + Rf(rnd, 0.3f, 0.7f)) / n;
                Grow(rnd, paths, new Vector2(s, -0.15f), f.heights[i] * Rf(rnd, 0.85f, 1.15f), Rf(rnd, 0.12f, 0.15f), lo + 0.1f, hi - 0.1f, 0, Rf(rnd, -0.08f, 0.08f));
            }
            // runner along the wall foot, half sunk into the ground
            var run = new RootPath();
            for (float s = 0.02f; s <= f.width - 0.02f; s += 0.12f)
            {
                run.pts.Add(new Vector2(s, 0.02f + 0.025f * Mathf.Sin(s * 4.1f + seed)));
                run.r.Add(0.13f * (1 + 0.2f * Mathf.Sin(s * 2.7f + seed * 0.7f)));
            }
            paths.Add(run);

            // roots crawling from the wall foot across the ground toward the grid (lying flat, half buried)
            int ng = Mathf.Max(2, n);
            for (int i = 0; i < ng; i++)
            {
                float s = f.width * (i + Rf(rnd, 0.25f, 0.75f)) / ng;
                var start = f.origin + f.tangent * s + f.normal * 0.06f;
                var dir = (f.normal + f.tangent * Rf(rnd, -0.6f, 0.6f)).normalized;
                Crawl(rnd, start, dir, Rf(rnd, 0.9f, 1.7f), Rf(rnd, 0.1f, 0.13f), 0, groundAt, verts, norms, tris);
            }

            foreach (var p in paths)
            {
                // face coords -> world; v is height above the ground at that spot; tube centre sits
                // 0.45 r out from the stone so it hugs (half sunk into) the wall
                var w = new List<Vector3>();
                for (int k = 0; k < p.pts.Count; k++)
                {
                    var foot = f.origin + f.tangent * p.pts[k].x;
                    float gy = groundAt(foot);
                    w.Add(foot + Vector3.up * (gy + p.pts[k].y) + f.normal * (p.r[k] * 0.2f + 0.005f));
                }
                Tube(w, p.r, f.normal, verts, norms, tris);
            }
        }

        var mesh = new Mesh { name = "CastleRoots_" + level, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.SetVertices(verts); mesh.SetNormals(norms); mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        string meshPath = "Assets/Materials/CastleRoots_" + level + ".asset";
        AssetDatabase.DeleteAsset(meshPath);
        AssetDatabase.CreateAsset(mesh, meshPath);
        var go = new GameObject("CastleRoots");
        go.transform.SetParent(holder.transform, true);
        go.transform.position = Vector3.zero; go.transform.rotation = Quaternion.identity;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        // the trees' ForestBark, a bit lighter so the roots read against the night-lit stone
        const string barkPath = "Assets/Materials/ForestRootBark.mat";
        var bark = AssetDatabase.LoadAssetAtPath<Material>(barkPath);
        if (!bark)
        {
            bark = new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefabs/Environment/Forest/ForestBark.mat"));
            AssetDatabase.CreateAsset(bark, barkPath);
        }
        bark.SetColor("_BaseColor", new Color(0.55f, 0.30f, 0.14f));
        bark.SetFloat("_SpecularHighlights", 0); bark.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
        bark.SetFloat("_EnvironmentReflections", 0); bark.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
        bark.SetFloat("_Smoothness", 0);
        bark.SetColor("_EmissionColor", new Color(0.10f, 0.05f, 0.02f)); bark.EnableKeyword("_EMISSION");
        EditorUtility.SetDirty(bark);
        go.AddComponent<MeshRenderer>().sharedMaterial = bark;
    }

    // A root lying on the ground from start heading dir: wanders, tapers, forks once or twice.
    static void Crawl(System.Random rnd, Vector3 start, Vector3 dir, float len, float r0, int depth,
                      System.Func<Vector3, float> groundAt, List<Vector3> V, List<Vector3> N, List<int> T)
    {
        const float step = 0.08f;
        int n = Mathf.Max(4, Mathf.RoundToInt(len / step));
        var c = new List<Vector3>(); var r = new List<float>();
        float ang = Mathf.Atan2(dir.z, dir.x), turn = 0;
        var p = start;
        for (int k = 0; k <= n; k++)
        {
            float t = k / (float)n, rad = r0 * (1 - 0.75f * Mathf.Pow(t, 1.2f));
            // centre slightly below the ground so it reads as half buried
            c.Add(new Vector3(p.x, groundAt(p) + rad * 0.15f, p.z)); r.Add(rad);
            turn = Mathf.Clamp(turn + Rf(rnd, -0.35f, 0.35f), -1, 1);
            ang += turn * 0.08f;
            p += new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)) * step;
        }
        Tube(c, r, Vector3.up, V, N, T);
        if (depth >= 2) return;
        int forks = depth == 0 ? rnd.Next(1, 3) : rnd.Next(0, 2);
        for (int i = 0; i < forks; i++)
        {
            int kf = rnd.Next(n / 4, Mathf.Max(n / 4 + 1, n * 2 / 3));
            var d0 = (c[Mathf.Min(n, kf + 1)] - c[kf]); d0.y = 0;
            float a = Mathf.Atan2(d0.z, d0.x) + (rnd.Next(2) == 0 ? -1 : 1) * Rf(rnd, 0.45f, 0.8f);
            Crawl(rnd, c[kf], new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)), len * (1 - kf / (float)n) * Rf(rnd, 0.6f, 0.9f),
                  r[kf] * 0.75f, depth + 1, groundAt, V, N, T);
        }
    }

    static float Rf(System.Random r, float a, float b) => a + (float)r.NextDouble() * (b - a);

    // A root climbing the face from start (face coords: x along, y up, world units) in smooth S-curves,
    // like ivy / roots growing up a wall; tapers a little; at most one fork that sweeps off sideways.
    static void Grow(System.Random rnd, List<RootPath> outPaths, Vector2 start, float height, float r0,
                     float lo, float hi, int depth, float lean)
    {
        const float step = 0.08f;
        var p = new RootPath();
        int n = Mathf.Max(4, Mathf.RoundToInt(height / step));
        float amp = Rf(rnd, 0.12f, 0.22f), freq = Rf(rnd, 1.6f, 2.3f), ph = Rf(rnd, 0, 6.28f);
        for (int k = 0; k <= n; k++)
        {
            float t = k / (float)n, v = t * height;
            // S-curve around a gently leaning line; the wave fades in from the foot
            float x = start.x + lean * v + amp * Mathf.Sin(ph + freq * v) * Mathf.Min(1, v / 0.5f) - amp * Mathf.Sin(ph) * 0;
            x = Mathf.Clamp(x, lo, hi);
            p.pts.Add(new Vector2(x, start.y + v));
            p.r.Add(r0 * (1 - 0.5f * Mathf.Pow(t, 1.3f)));
        }
        outPaths.Add(p);
        if (depth >= 2) return;
        int forks = depth == 0 ? 1 : 0;   // one soft fork; more read as spiky twigs
        for (int i = 0; i < forks; i++)
        {
            int kf = rnd.Next(n / 4, Mathf.Max(n / 4 + 1, n * 2 / 3));
            float side = i == 0 ? (p.pts[kf].x < (lo + hi) / 2 ? 1 : -1) : (rnd.Next(2) == 0 ? -1 : 1);
            Grow(rnd, outPaths, p.pts[kf], height * (1 - kf / (float)n) * Rf(rnd, 0.5f, 0.8f), p.r[kf] * 0.72f,
                 lo, hi, depth + 1, side * Rf(rnd, 0.3f, 0.45f));
        }
    }

    // Flat-shaded 5-sided tube along world points c with radii r; ends closed (tip comes to a point).
    static void Tube(List<Vector3> c, List<float> r, Vector3 wallN, List<Vector3> V, List<Vector3> N, List<int> T)
    {
        const int sides = 6;
        int n = c.Count;
        if (n < 2) return;
        // rounded tip: two extra shrinking rings past the end
        {
            var dir = (c[n - 1] - c[n - 2]).normalized;
            float re = r[n - 1];
            c = new List<Vector3>(c) { c[n - 1] + dir * re * 0.5f, c[n - 1] + dir * re * 0.85f };
            r = new List<float>(r) { re * 0.75f, re * 0.3f };
            n = c.Count;
        }
        var rings = new Vector3[n][];
        for (int k = 0; k < n; k++)
        {
            var tan = (c[Mathf.Min(n - 1, k + 1)] - c[Mathf.Max(0, k - 1)]).normalized;
            var a = Vector3.ProjectOnPlane(wallN, tan).normalized;
            if (a.sqrMagnitude < 1e-4f) a = Vector3.up;
            var b = Vector3.Cross(tan, a);
            rings[k] = new Vector3[sides];
            float rad = r[k];
            for (int s = 0; s < sides; s++)
            {
                float th = 2 * Mathf.PI * s / sides + 0.3f;
                rings[k][s] = c[k] + (a * Mathf.Cos(th) * 0.5f + b * Mathf.Sin(th)) * rad;
            }
        }
        for (int k = 0; k < n - 1; k++)
            for (int s = 0; s < sides; s++)
            {
                var p0 = rings[k][s]; var p1 = rings[k][(s + 1) % sides];
                var p2 = rings[k + 1][s]; var p3 = rings[k + 1][(s + 1) % sides];
                var nrm = Vector3.Cross(p2 - p0, p1 - p0).normalized;
                // make sure it faces outward
                var mid = (p0 + p1 + p2 + p3) * 0.25f;
                if (Vector3.Dot(nrm, mid - (c[k] + c[k + 1]) * 0.5f) < 0) nrm = -nrm;
                int i0 = V.Count;
                V.Add(p0); V.Add(p1); V.Add(p2); V.Add(p3);
                N.Add(nrm); N.Add(nrm); N.Add(nrm); N.Add(nrm);
                // winding chosen so the face points along nrm
                if (Vector3.Dot(Vector3.Cross(p1 - p0, p2 - p0), nrm) > 0) T.AddRange(new[] { i0, i0 + 1, i0 + 2, i0 + 1, i0 + 3, i0 + 2 });
                else T.AddRange(new[] { i0, i0 + 2, i0 + 1, i0 + 1, i0 + 2, i0 + 3 });
            }
        // cap the root's start (usually underground)
        var capN = (c[0] - c[1]).normalized;
        int ci = V.Count;
        V.Add(c[0]); N.Add(capN);
        for (int s = 0; s < sides; s++) { V.Add(rings[0][s]); N.Add(capN); }
        for (int s = 0; s < sides; s++)
        {
            int a1 = ci + 1 + s, b1 = ci + 1 + (s + 1) % sides;
            if (Vector3.Dot(Vector3.Cross(V[a1] - V[ci], V[b1] - V[ci]), capN) > 0) T.AddRange(new[] { ci, a1, b1 });
            else T.AddRange(new[] { ci, b1, a1 });
        }
    }

    static void BuildRootMesh(GameObject holder, string level, string art, Material litMat)
    {
        var rect = System.IO.File.ReadAllText(art + "forest_roots_floor.rect.txt").Trim().Split(' ')
            .Select(v => float.Parse(v, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        float x0 = rect[0], z0 = rect[1], x1 = rect[2], z1 = rect[3];
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(art + "forest_roots_floor.png");
        string matPath = "Assets/Materials/ForestRoots_" + level + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (!m) { m = new Material(litMat); AssetDatabase.CreateAsset(m, matPath); }
        m.mainTexture = tex;
        EditorUtility.SetDirty(m);

        var terrain = Terrain.activeTerrain;
        const float stepW = 0.2f;
        int nx = Mathf.CeilToInt((x1 - x0) / stepW) + 1, nz = Mathf.CeilToInt((z1 - z0) / stepW) + 1;
        var verts = new Vector3[nx * nz];
        var uvs = new Vector2[nx * nz];
        var cols = new Color[nx * nz];
        for (int j = 0; j < nz; j++)
            for (int i = 0; i < nx; i++)
            {
                float x = Mathf.Min(x1, x0 + i * stepW), z = Mathf.Min(z1, z0 + j * stepW);
                float y = Ground;
                if (terrain)
                {
                    // highest of a few samples round the vertex so no triangle sinks into a curved bank
                    y = float.MinValue;
                    for (int s = 0; s < 5; s++)
                    {
                        var o = s == 0 ? Vector2.zero : new Vector2(s == 1 ? 0.1f : s == 2 ? -0.1f : 0, s == 3 ? 0.1f : s == 4 ? -0.1f : 0);
                        y = Mathf.Max(y, terrain.SampleHeight(new Vector3(x + o.x, 0, z + o.y)) + terrain.transform.position.y);
                    }
                }
                bool flat = y < Ground + 0.005f;
                verts[j * nx + i] = new Vector3(x, (flat ? Ground : y) + (flat ? 0.012f : 0.03f), z);
                uvs[j * nx + i] = new Vector2((x - x0) / (x1 - x0), (z - z0) / (z1 - z0));
                cols[j * nx + i] = Color.white;
            }
        var tris = new List<int>();
        for (int j = 0; j < nz - 1; j++)
            for (int i = 0; i < nx - 1; i++)
            {
                int a = j * nx + i, b = a + 1, c = a + nx, d = c + 1;
                tris.AddRange(new[] { a, c, b, b, c, d });
            }
        var mesh = new Mesh { name = "ForestRootNet_" + level, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.vertices = verts; mesh.uv = uvs; mesh.colors = cols; mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        string meshPath = "Assets/Materials/ForestRootNet_" + level + ".asset";
        AssetDatabase.DeleteAsset(meshPath);
        AssetDatabase.CreateAsset(mesh, meshPath);

        var go = new GameObject("RootNet");
        go.transform.SetParent(holder.transform, false);
        go.transform.position = Vector3.zero; go.transform.rotation = Quaternion.identity; go.transform.localScale = Vector3.one;
        // holder may sit under a scaled/moved parent: keep the mesh in world space
        go.transform.SetParent(holder.transform, true);
        go.transform.position = Vector3.zero; go.transform.rotation = Quaternion.identity;
        var mf = go.AddComponent<MeshFilter>(); mf.sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = m;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    static SpriteRenderer Add(GameObject parent, string name, string path, Vector3 pos, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.transform.position = pos;
        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (mat) r.sharedMaterial = mat;
        return r;
    }

    static void ImportSprite(string path, float ppu, Vector2 pivot)
    {
        AssetDatabase.ImportAsset(path);
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = ppu;
        var s = new TextureImporterSettings();
        imp.ReadTextureSettings(s);
        s.spriteAlignment = (int)SpriteAlignment.Custom;
        s.spritePivot = pivot;
        s.spriteMeshType = SpriteMeshType.FullRect;
        imp.SetTextureSettings(s);
        imp.mipmapEnabled = true;
        imp.alphaIsTransparency = true;
        imp.maxTextureSize = 4096;
        imp.SaveAndReimport();
    }
}
