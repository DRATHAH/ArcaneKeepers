using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// One HUD for every level.
//   LevelHUD/Prefabs/TowerCanvas.prefab = tower-card column (left) + round MOVE button bottom-right (MoveDock, MoveButton.cs).
//   Every level scene (and Prefabs/LevelTemplate.prefab) gets that prefab + the WaveMeter prefab; the old
//   per-scene TowerCanvas and the round RelocateCanvas are removed, and scene references to them
//   (RelocationManager, InventoryUI, ...) are re-pointed to the same-named objects in the prefab.
// Menu: Arcane Keepers > Level HUD > Rebuild + Apply To All Levels
// Agent:  run_script Assets/UserInterface/LevelHUD/Editor/LevelHudBuilder.cs entry LevelHudBuilder.RunAll
public static class LevelHudBuilder
{
    public const string Root = "Assets/UserInterface/LevelHUD/";
    public const string PrefabPath = Root + "Prefabs/TowerCanvas.prefab";
    const string ArtFolder = Root + "Art/MoveButton/";
    const string StyleFolder = "Assets/UserInterface/Shared/Style/";
    const string WaveMeterPrefab = "Assets/UserInterface/Tutorial/Prefabs/WaveMeter.prefab";
    const string TemplatePrefab = "Assets/Prefabs/LevelTemplate.prefab";
    const string SourceScene = "Assets/Scenes/DemoLevel.unity";

    public static readonly string[] Levels =
    {
        "Assets/Scenes/DemoLevel.unity",
        "Assets/Scenes/Levels/World1/Level2.unity",
        "Assets/Scenes/Levels/World1/Level3.unity",
        "Assets/Scenes/Levels/World1/Level4.unity",
    };

    // MOVE button (1080-tall canvas): round gold disc in the bottom-right corner, "MOVE" across its bottom rim
    const float DiscSize = 170f, BtnW = 210f, BtnH = 222f, BtnMargin = 22f;
    const float DiscCenterFromTop = 22f + DiscSize / 2f;

    [MenuItem("Arcane Keepers/Level HUD/Rebuild + Apply To All Levels")]
    static void MenuRun() => Debug.Log(RunAll());

    public static string RunAll()
    {
        var log = new StringBuilder();
        ImportArt();
        log.AppendLine(BuildPrefab());
        foreach (string scene in Levels)
        {
            log.AppendLine(ApplyToScene(scene));
        }
        log.AppendLine(ApplyToTemplate());
        EditorSceneManager.OpenScene(SourceScene, OpenSceneMode.Single);
        return log.ToString();
    }

    // ---------------------------------------------------------------- art

    public static void ImportArt()
    {
        foreach (string name in new[] { "MoveIcon" })
        {
            string path = ArtFolder + name + ".png";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();
        }
    }

    // ---------------------------------------------------------------- prefab

    /// <summary>Creates the prefab from DemoLevel's canvas the first time; after that edits it in place
    /// (same objects/fileIDs, so scene references into the prefab keep working).</summary>
    public static string BuildPrefab()
    {
        bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        GameObject canvas;
        if (exists)
        {
            canvas = PrefabUtility.LoadPrefabContents(PrefabPath);
        }
        else
        {
            EditorSceneManager.OpenScene(SourceScene, OpenSceneMode.Single);
            GameObject source = FindRoot("TowerCanvas");
            if (!source)
            {
                return "no TowerCanvas in " + SourceScene;
            }
            canvas = Object.Instantiate(source);
            canvas.name = "TowerCanvas";
            foreach (Transform t in canvas.GetComponentsInChildren<Transform>(true).Where(t => t.name == "RelocateButton").ToList())
            {
                Object.DestroyImmediate(t.gameObject);
            }
        }

        Transform panel = canvas.transform.Find("TowerPanel");
        var icons = (RectTransform)panel.Find("IconParent");
        icons.anchorMin = new Vector2(0f, 0f);
        icons.anchorMax = new Vector2(1f, 1f);
        icons.offsetMin = Vector2.zero; // cards get the whole column again
        icons.offsetMax = Vector2.zero;
        var grid = icons.GetComponent<GridLayoutGroup>();
        grid.spacing = new Vector2(grid.spacing.x, 12f);
        grid.childAlignment = TextAnchor.MiddleCenter;

        BuildMoveButton(canvas.transform);

        System.IO.Directory.CreateDirectory(Root + "Prefabs");
        PrefabUtility.SaveAsPrefabAsset(canvas, PrefabPath);
        if (exists)
        {
            PrefabUtility.UnloadPrefabContents(canvas);
        }
        else
        {
            Object.DestroyImmediate(canvas);
        }
        return (exists ? "updated " : "created ") + PrefabPath;
    }

    static void BuildMoveButton(Transform canvasRoot)
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(StyleFolder + "LilitaOne SDF.asset");
        var chunky = AssetDatabase.LoadAssetAtPath<Material>(StyleFolder + "LilitaOne SDF Chunky.mat");
        Sprite Style(string n) => AssetDatabase.LoadAssetAtPath<Sprite>(StyleFolder + n + ".png");

        // Bottom-right dock inside the safe area (the cards own the left edge, pause the top-right)
        RectTransform dock = Stretch(Rect("MoveDock", canvasRoot));
        Get<SafeArea>(dock.gameObject);

        // Root = hit area + Button, named RelocateButton (the tutorial finds it by that name).
        // Re-parented (not recreated) so scene refs to its Button/CooldownSlider survive.
        Transform existing = canvasRoot.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "RelocateButton");
        RectTransform root = existing ? (RectTransform)existing : Rect("RelocateButton", dock);
        root.SetParent(dock, false);
        foreach (string gone in new[] { "Tab", "Glow" })
        {
            Transform t = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(c => c.name == gone);
            if (t)
            {
                Object.DestroyImmediate(t.gameObject);
            }
        }
        root.anchorMin = root.anchorMax = new Vector2(1f, 0f);
        root.pivot = new Vector2(1f, 0f);
        root.sizeDelta = new Vector2(BtnW, BtnH);
        root.anchoredPosition = new Vector2(-BtnMargin, BtnMargin);
        root.localScale = Vector3.one;
        root.localRotation = Quaternion.identity;
        var hit = Get<Image>(root.gameObject);
        hit.sprite = null;
        hit.color = new Color(1f, 1f, 1f, 0f);
        var button = Get<Button>(root.gameObject);
        button.transition = Selectable.Transition.None;
        button.targetGraphic = hit;
        button.navigation = new Navigation { mode = Navigation.Mode.None };

        // "Drag a Keeper" pill, left of the disc (outside Body so it doesn't scale with the presses)
        RectTransform hintRt = Rect("Hint", root);
        hintRt.anchorMin = hintRt.anchorMax = new Vector2(0f, 1f);
        hintRt.pivot = new Vector2(1f, 0.5f);
        hintRt.sizeDelta = new Vector2(290f, 74f);
        hintRt.anchoredPosition = new Vector2(4f, -DiscCenterFromTop);
        var hintImg = Get<Image>(hintRt.gameObject);
        hintImg.sprite = Style("CC_Dark");
        hintImg.type = Image.Type.Sliced;
        hintImg.pixelsPerUnitMultiplier = 2f;
        hintImg.color = new Color(1f, 1f, 1f, 0.92f);
        hintImg.raycastTarget = false;
        var hintGroup = Get<CanvasGroup>(hintRt.gameObject);
        hintGroup.interactable = false;
        hintGroup.blocksRaycasts = false;
        RectTransform hintTextRt = Stretch(Rect("Text", hintRt));
        hintTextRt.offsetMin = new Vector2(18f, 4f);
        hintTextRt.offsetMax = new Vector2(-18f, -4f);
        var hintText = Get<TextMeshProUGUI>(hintTextRt.gameObject);
        hintText.font = font;
        hintText.text = "Drag a Keeper";
        hintText.fontSize = 34f;
        hintText.color = new Color(1f, 0.93f, 0.7f);
        hintText.alignment = TextAlignmentOptions.Center;
        hintText.textWrappingMode = TextWrappingModes.NoWrap;
        hintText.raycastTarget = false;
        hintRt.SetAsFirstSibling();

        RectTransform body = Stretch(Rect("Body", root));
        body.pivot = new Vector2(0.5f, 0.5f);
        body.localRotation = Quaternion.identity;

        Image Circle(string name, Sprite sprite, float size)
        {
            RectTransform rt = Rect(name, body);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = new Vector2(0f, -DiscCenterFromTop);
            rt.localRotation = Quaternion.identity;
            var img = Get<Image>(rt.gameObject);
            img.sprite = sprite;
            img.raycastTarget = false;
            img.preserveAspect = true;
            return img;
        }

        Image ring = Circle("Ring", Style("CS_DashRing"), DiscSize + 34f);
        ring.color = new Color(1f, 0.84f, 0.3f, 0f);
        Image disc = Circle("Disc", Style("CC_CircleGold"), DiscSize);
        disc.color = Color.white;
        Image icon = Circle("Icon", AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "MoveIcon.png"), DiscSize * 0.6f);
        icon.rectTransform.anchoredPosition += new Vector2(0f, 4f);
        icon.color = Color.white;
        Image shade = Circle("Shade", Style("CS_Disc"), DiscSize * 0.9f);
        shade.color = new Color(0.08f, 0.06f, 0.14f, 0.6f);
        shade.type = Image.Type.Filled;
        shade.fillMethod = Image.FillMethod.Radial360;
        shade.fillOrigin = (int)Image.Origin360.Top;
        shade.fillClockwise = true;
        shade.fillAmount = 0f;
        shade.rectTransform.anchoredPosition += new Vector2(0f, 3f);
        foreach (string n in new[] { "Ring", "Disc", "Icon", "Shade" })
        {
            body.Find(n).SetSiblingIndex(body.childCount - 1);
        }

        // "MOVE" sits across the bottom rim of the disc
        RectTransform labelRt = Rect("Label", body);
        labelRt.anchorMin = new Vector2(0f, 0f);
        labelRt.anchorMax = new Vector2(1f, 0f);
        labelRt.pivot = new Vector2(0.5f, 0f);
        labelRt.sizeDelta = new Vector2(0f, 64f);
        labelRt.anchoredPosition = Vector2.zero;
        labelRt.SetAsLastSibling();
        var label = Get<TextMeshProUGUI>(labelRt.gameObject);
        label.font = font;
        if (chunky)
        {
            label.fontSharedMaterial = chunky;
        }
        label.text = "MOVE";
        label.fontSize = 50f;
        label.characterSpacing = 2f;
        label.alignment = TextAlignmentOptions.Bottom;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;

        // Data-only slider: RelocationManager writes the cooldown here, MoveButton reads it
        RectTransform sliderRt = Stretch(Rect("CooldownSlider", root));
        var slider = Get<Slider>(sliderRt.gameObject);
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;
        slider.navigation = new Navigation { mode = Navigation.Mode.None };
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0f;

        var move = Get<MoveButton>(root.gameObject);
        move.body = body;
        move.disc = disc;
        move.icon = icon;
        move.label = label;
        move.shade = shade;
        move.ring = ring;
        move.hint = hintGroup;
        move.cooldownSlider = slider;
    }

    // ---------------------------------------------------------------- scenes

    public static string ApplyToScene(string path)
    {
        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var log = new StringBuilder(path + ": ");

        List<GameObject> old = scene.GetRootGameObjects().Where(g => (g.name == "TowerCanvas" && !IsHud(g)) || g.name == "RelocateCanvas").ToList();
        if (old.Count == 0 && scene.GetRootGameObjects().Any(IsHud))
        {
            log.Append("HUD prefab already in place; ");
            return EnsureWaveMeterAndSave(scene, log);
        }
        int sibling = old.Count > 0 ? old.Min(g => g.transform.GetSiblingIndex()) : scene.rootCount;

        var hud = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        hud.name = "TowerCanvas";
        hud.transform.SetSiblingIndex(sibling);

        int fixedRefs = Remap(scene.GetRootGameObjects(), old, hud, log);
        foreach (GameObject g in old)
        {
            Object.DestroyImmediate(g);
        }
        log.Append($"replaced {old.Count} old canvases, {fixedRefs} refs re-pointed; ");
        return EnsureWaveMeterAndSave(scene, log);
    }

    static bool IsHud(GameObject g) =>
        PrefabUtility.IsAnyPrefabInstanceRoot(g) && AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(g)) == PrefabPath;

    static string EnsureWaveMeterAndSave(UnityEngine.SceneManagement.Scene scene, StringBuilder log)
    {
        if (!scene.GetRootGameObjects().Any(g => g.name == "WaveMeter"))
        {
            var meter = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(WaveMeterPrefab), scene);
            meter.name = "WaveMeter";
            log.Append("added WaveMeter; ");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return log.ToString();
    }

    public static string ApplyToTemplate()
    {
        GameObject contents = PrefabUtility.LoadPrefabContents(TemplatePrefab);
        var log = new StringBuilder(TemplatePrefab + ": ");
        try
        {
            List<GameObject> old = contents.transform.Cast<Transform>().Select(t => t.gameObject)
                .Where(g => (g.name == "TowerCanvas" && !IsHud(g)) || g.name == "RelocateCanvas").ToList();
            if (old.Count == 0)
            {
                log.Append("HUD prefab already in place; ");
                return log.ToString();
            }
            var hud = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), contents.transform);
            hud.name = "TowerCanvas";
            if (old.Count > 0)
            {
                hud.transform.SetSiblingIndex(old.Min(g => g.transform.GetSiblingIndex()));
            }
            int fixedRefs = Remap(new[] { contents }, old, hud, log);
            foreach (GameObject g in old)
            {
                Object.DestroyImmediate(g);
            }
            log.Append($"replaced {old.Count}, {fixedRefs} refs; ");
            if (!contents.transform.Cast<Transform>().Any(t => t.name == "WaveMeter"))
            {
                var meter = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(WaveMeterPrefab), contents.transform);
                meter.name = "WaveMeter";
                log.Append("added WaveMeter; ");
            }
            PrefabUtility.SaveAsPrefabAsset(contents, TemplatePrefab);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
        return log.ToString();
    }

    /// <summary>Points every reference into the old canvases at the same-named object (same type) in the new HUD.</summary>
    static int Remap(IEnumerable<GameObject> roots, List<GameObject> old, GameObject hud, StringBuilder log)
    {
        var oldSet = new HashSet<Transform>(old.SelectMany(g => g.GetComponentsInChildren<Transform>(true)));
        var hudByName = hud.GetComponentsInChildren<Transform>(true).GroupBy(t => t.name).ToDictionary(g => g.Key, g => g.First());
        int count = 0;
        foreach (GameObject r in roots)
        {
            foreach (Component c in r.GetComponentsInChildren<Component>(true))
            {
                if (!c || c is Transform || oldSet.Contains(c.transform) || c.transform.IsChildOf(hud.transform))
                {
                    continue;
                }
                var so = new SerializedObject(c);
                SerializedProperty p = so.GetIterator();
                bool changed = false;
                while (p.Next(true))
                {
                    if (p.propertyType != SerializedPropertyType.ObjectReference || !p.objectReferenceValue)
                    {
                        continue;
                    }
                    Object target = p.objectReferenceValue;
                    Transform tt = target is Component tc ? tc.transform : target is GameObject tg ? tg.transform : null;
                    if (!tt || !oldSet.Contains(tt))
                    {
                        continue;
                    }
                    string name = tt.name == "RelocateCanvas" ? "TowerCanvas" : tt.name;
                    Object replacement = null;
                    if (hudByName.TryGetValue(name, out Transform nt) || (name == hud.name && (nt = hud.transform)))
                    {
                        replacement = target is GameObject ? nt.gameObject : (Object)nt.GetComponent(target.GetType());
                    }
                    if (replacement)
                    {
                        p.objectReferenceValue = replacement;
                        changed = true;
                        count++;
                    }
                    else
                    {
                        // Persistent onClick targets of the old round button etc. are expected to drop
                        log.Append($"[dropped {c.GetType().Name}.{p.propertyPath} -> {tt.name}] ");
                    }
                }
                if (changed)
                {
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }
        return count;
    }

    // ---------------------------------------------------------------- helpers

    static GameObject FindRoot(string name) =>
        EditorSceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g => g.name == name);

    static T Get<T>(GameObject go) where T : Component => go.GetComponent<T>() ? go.GetComponent<T>() : go.AddComponent<T>();

    static RectTransform Rect(string name, Transform parent)
    {
        Transform existing = parent.Find(name);
        if (existing)
        {
            return (RectTransform)existing;
        }
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return rt;
    }

    static RectTransform Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }
}
