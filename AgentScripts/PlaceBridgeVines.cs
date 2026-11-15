using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Imports the ivy art from GenerateVines.py and dresses DemoLevel's bridge.
// run_script AgentScripts/PlaceBridgeVines.cs PlaceBridgeVines.Run  (edit mode)
// Idempotent: rebuilds Level/BridgeVines each time.
public static class PlaceBridgeVines
{
    const string Dir = "Assets/Sprites/";

    public static string Run()
    {
        string[] deco = { "Vines/bridge_vine_floor.png", "Vines/bridge_vine_face.png", "Vines/bridge_vine_back.png", "Vines/bridge_vine_leg.png", "Vines/castle_vine_wall_near.png", "Vines/castle_vine_wall_far.png", "Vines/castle_vine_frame_far.png" };
        foreach (var d in deco) ImportDeco(Dir + d, 110, d.Contains("face") ? new Vector2(0, 1) : d.Contains("leg") ? new Vector2(0.5f, 1) : new Vector2(0, 0));

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UserInterface/LevelHUD/Prefabs/Tile.prefab");
        var root = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(prefab));
        var sr = root.GetComponent<SpriteRenderer>();
        // no per-tile vine outline any more: the floor ivy is one organic overlay (below)
        var tileMat = sr.sharedMaterial;
        sr.sprite = null;
        var tileComp = root.GetComponent<Tile>();
        if (tileComp) tileComp.floorVariants = new Sprite[0];
        PrefabUtility.SaveAsPrefabAsset(root, AssetDatabase.GetAssetPath(prefab));
        PrefabUtility.UnloadPrefabContents(root);

        // ---- scene dressing
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/DemoLevel.unity", OpenSceneMode.Single);
        var old = GameObject.Find("BridgeVines");
        if (old) Object.DestroyImmediate(old);
        var holder = new GameObject("BridgeVines");
        var level = GameObject.Find("Level");
        if (level) holder.transform.SetParent(level.transform, true);
        var mat = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat");

        // re-laid castle brick inside the placement grid (GenerateDeckPaving.py) so mortar seams sit on
        // the grid lines; a Lit quad with a copy of the bridge material so it shades like the deck
        {
            const string pavPath = Dir + "Vines/bridge_deck_paving.png";
            AssetDatabase.ImportAsset(pavPath);
            var ti = (TextureImporter)AssetImporter.GetAtPath(pavPath);
            ti.textureType = TextureImporterType.Default;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.mipmapEnabled = true;
            ti.maxTextureSize = 2048;
            ti.SaveAndReimport();
            var slab = GameObject.Find("bridgeSlab");
            var src = slab ? slab.GetComponent<MeshRenderer>().sharedMaterial : null;
            const string matPath = "Assets/Materials/DeckPaving.mat";
            var pm = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (!pm) { pm = new Material(src); AssetDatabase.CreateAsset(pm, matPath); }
            else if (src) pm.CopyPropertiesFromMaterial(src);
            var pt = AssetDatabase.LoadAssetAtPath<Texture2D>(pavPath);
            pm.SetTexture("_BaseMap", pt); pm.SetTexture("_MainTex", pt);
            pm.SetTextureScale("_BaseMap", Vector2.one); pm.SetTextureOffset("_BaseMap", Vector2.zero);
            EditorUtility.SetDirty(pm);
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "Paving";
            Object.DestroyImmediate(q.GetComponent<Collider>());
            q.transform.SetParent(holder.transform, false);
            // grid: 10x5 cells of 2 units from (-9.16, -5.04)
            q.transform.position = new Vector3(-9.16f + 10f, -0.245f, -5.04f + 5f);
            q.transform.rotation = Quaternion.Euler(90, 0, 0);
            q.transform.localScale = new Vector3(20f, 10f, 1f);
            q.GetComponent<MeshRenderer>().sharedMaterial = pm;
        }

        // deck floor: ivy creeping in from the wall feet and the gate, lying flat (deck top is y -0.25)
        var fl = Add(holder, "Floor", Dir + "Vines/bridge_vine_floor.png", new Vector3(-11.45f, -0.235f, -5.87f), tileMat);
        fl.transform.rotation = Quaternion.Euler(90, 0, 0);
        // near (camera-facing) wall of the bridge: garland + strands hanging off the edge
        Add(holder, "NearWall", Dir + "Vines/bridge_vine_face.png", new Vector3(-9.7f, 0.36f, -6.86f), tileMat);
        // inner face of the far parapet: low creeper along its foot
        Add(holder, "FarParapet", Dir + "Vines/bridge_vine_back.png", new Vector3(-10f, -0.25f, 5.77f), tileMat);
        // legs: ivy wrapping round each leg's front face, the east one mirrored
        Add(holder, "LegWest", Dir + "Vines/bridge_vine_leg.png", new Vector3(-5.66f, -0.74f, -5.76f), tileMat);
        var east = Add(holder, "LegEast", Dir + "Vines/bridge_vine_leg.png", new Vector3(5.66f, -0.74f, -5.76f), tileMat);
        east.flipX = true;
        // castle wall either side of the gate: outer face x=-9.72 facing +x (toward the deck);
        // rotated so the image's left edge = its z0, roots on the deck (y -0.25)
        var wn = Add(holder, "CastleWallNear", Dir + "Vines/castle_vine_wall_near.png", new Vector3(-9.70f, -0.25f, -7.5f), tileMat);
        wn.transform.rotation = Quaternion.Euler(0, -90, 0);
        var wf = Add(holder, "CastleWallFar", Dir + "Vines/castle_vine_wall_far.png", new Vector3(-9.70f, -0.25f, 5.2f), tileMat);
        wf.transform.rotation = Quaternion.Euler(0, -90, 0);
        // door frame chamfer right of the gate: from (-10.55, z 4.36) to (-9.72, z 5.19), normal (0.7, 0, -0.7)
        var ff = Add(holder, "CastleFrameFar", Dir + "Vines/castle_vine_frame_far.png", new Vector3(-10.55f + 0.015f, -0.25f, 4.36f - 0.015f), tileMat);
        ff.transform.rotation = Quaternion.Euler(0, -45, 0);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return "ok";
    }

    static SpriteRenderer Add(GameObject parent, string name, string path, Vector3 pos, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.transform.position = pos;
        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (mat) r.sharedMaterial = mat;
        r.sortingLayerName = "Default";
        return r;
    }

    static void ImportDeco(string path, float ppu, Vector2 pivot)
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
