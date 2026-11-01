using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ArcaneKeepers.Tutorial.EditorTools
{
    /// <summary>
    /// One-click setup for the tutorial (menu: Arcane Keepers > Tutorial):
    ///  - imports the art in Assets/UserInterface/Tutorial/Art as UI sprites (9-slice borders where needed),
    ///  - fills Assets/UserInterface/Tutorial/Data/TutorialTheme.asset,
    ///  - makes the Tutorial + WaveMeter prefabs in Assets/UserInterface/Tutorial/Prefabs,
    ///  - drops both into the first level (DemoLevel).
    /// Every step is safe to run again.
    /// </summary>
    public static class TutorialSetup
    {
        const string Root = "Assets/UserInterface/Tutorial";
        const string Art = Root + "/Art";
        const string ThemePath = Root + "/Data/TutorialTheme.asset";
        const string TutorialPrefab = Root + "/Prefabs/Tutorial.prefab";
        const string MeterPrefab = Root + "/Prefabs/WaveMeter.prefab";
        const string FirstLevel = "Assets/Scenes/DemoLevel.unity";
        const string Generated = "Assets/UserInterface/Shared/Style";

        [MenuItem("Arcane Keepers/Tutorial/Set Up Everything")]
        public static void InstallAll()
        {
            ImportArt();
            TutorialTheme theme = BuildTheme();
            BuildPrefabs(theme);
            InstallInScene(FirstLevel);
            Debug.Log("Tutorial set up: art imported, theme + prefabs built, installed in " + FirstLevel);
        }

        [MenuItem("Arcane Keepers/Tutorial/Reset Tutorial Progress")]
        public static void ResetProgress()
        {
            TutorialDirector.ResetProgress();
            Debug.Log("Tutorial progress reset: it will play again next time the first level starts.");
        }

        // ------------------------------------------------------------ art

        public static void ImportArt()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Art }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var imp = (TextureImporter)AssetImporter.GetAtPath(path);
                if (imp == null) continue;
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.alphaIsTransparency = true;
                imp.mipmapEnabled = false;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.filterMode = FilterMode.Bilinear;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.spritePixelsPerUnit = 100f;

                string file = Path.GetFileNameWithoutExtension(path);
                float b = file switch
                {
                    "Bubble" => 80f,
                    "BubbleShadow" => 80f,
                    "GlowFrame" => 78f,
                    "SpotHole" => 112f,
                    _ => 0f,
                };
                var settings = new TextureImporterSettings();
                imp.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.spriteBorder = new Vector4(b, b, b, b);
                if (file == "Wand")
                {
                    // pivot at the crystal so positioning the wand = positioning its tip
                    settings.spriteAlignment = (int)SpriteAlignment.Custom;
                    settings.spritePivot = new Vector2(0.492f, 0.85f);
                }
                imp.SetTextureSettings(settings);
                imp.SaveAndReimport();
            }
        }

        static Sprite S(string sub, string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Art}/{sub}/{name}.png");
        static Sprite G(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Generated}/{name}.png");

        // ------------------------------------------------------------ theme

        public static TutorialTheme BuildTheme()
        {
            var theme = AssetDatabase.LoadAssetAtPath<TutorialTheme>(ThemePath);
            if (!theme)
            {
                theme = ScriptableObject.CreateInstance<TutorialTheme>();
                AssetDatabase.CreateAsset(theme, ThemePath);
            }

            theme.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{Generated}/LilitaOne SDF.asset");
            theme.chunkyMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{Generated}/LilitaOne SDF Chunky.mat");

            theme.dog = S("Dog", "DogGuide");
            theme.dogShadow = S("Dog", "DogShadow");
            theme.bubble = S("UI", "Bubble");
            theme.bubbleShadow = S("UI", "BubbleShadow");
            theme.bubbleTail = S("UI", "BubbleTail");
            theme.nextArrow = S("UI", "NextArrow");
            theme.wand = S("UI", "Wand");
            theme.glowFrame = S("UI", "GlowFrame");
            theme.tileGlow = S("UI", "TileGlow");
            theme.spotHole = S("UI", "SpotHole");
            theme.chevron = S("UI", "Chevron");
            theme.emoteExclaim = S("UI", "EmoteExclaim");
            theme.emoteQuestion = S("UI", "EmoteQuestion");
            theme.emoteHeart = S("UI", "EmoteHeart");
            theme.emoteSweat = S("UI", "EmoteSweat");
            theme.emoteSparkle = S("UI", "EmoteSparkle");
            theme.skullToken = S("Hud", "SkullToken");
            theme.waveFlag = S("Hud", "WaveFlag");
            theme.meterBack = S("Hud", "MeterFrame");
            theme.meterFill = S("Hud", "MeterFill");
            theme.wandTip = new Vector2(0.492f, 0.85f);
            theme.softGlow = S("Fx", "SoftGlow");
            theme.sparkle = S("Fx", "Sparkle");
            theme.ring = S("Fx", "Ring");
            theme.puff = S("Fx", "Puff");
            theme.starChunk = S("Fx", "StarChunk");

            // shared with the rest of the game's chunky UI
            theme.skipButton = G("CC_Dark");

            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            return theme;
        }

        // ------------------------------------------------------------ prefabs

        public static void BuildPrefabs(TutorialTheme theme)
        {
            var go = new GameObject("Tutorial");
            go.AddComponent<TutorialDirector>().theme = theme;
            PrefabUtility.SaveAsPrefabAsset(go, TutorialPrefab);
            Object.DestroyImmediate(go);

            var meter = new GameObject("WaveMeter", typeof(RectTransform));
            meter.AddComponent<WaveProgressMeter>().theme = theme;
            PrefabUtility.SaveAsPrefabAsset(meter, MeterPrefab);
            Object.DestroyImmediate(meter);
            AssetDatabase.SaveAssets();
        }

        // ------------------------------------------------------------ scene

        public static void InstallInScene(string scenePath)
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != scenePath)
            {
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                scene = EditorSceneManager.OpenScene(scenePath);
            }
            // load after opening the scene (opening a scene can unload earlier references)
            var tutorial = AssetDatabase.LoadAssetAtPath<GameObject>(TutorialPrefab);
            var meter = AssetDatabase.LoadAssetAtPath<GameObject>(MeterPrefab);

            if (!Object.FindAnyObjectByType<TutorialDirector>(FindObjectsInactive.Include))
            {
                PrefabUtility.InstantiatePrefab(tutorial, scene);
            }
            if (!Object.FindAnyObjectByType<WaveProgressMeter>(FindObjectsInactive.Include))
            {
                PrefabUtility.InstantiatePrefab(meter, scene);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
