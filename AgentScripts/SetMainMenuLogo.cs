using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Swaps the main menu title art for ArcaneKeepers-Logo.png (cut out from ArcaneKeepers-LOGO-TEST.png)
// and pins it to the sky area of the background, between the heroes and skeletons.
public static class SetMainMenuLogo
{
    const string LogoPath = "Assets/UserInterface/ArcaneKeepers-Logo.png";

    // Background-relative band the logo lives in (the background is an envelope fitter,
    // so anchoring to it keeps the logo in the same place relative to the characters).
    public static string Run(float bottom, float top)
    {
        AssetDatabase.ImportAsset(LogoPath, ImportAssetOptions.ForceSynchronousImport);
        var imp = (TextureImporter)AssetImporter.GetAtPath(LogoPath);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.alphaIsTransparency = true;
        imp.mipmapEnabled = false;
        imp.maxTextureSize = 2048;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.SaveAndReimport();
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(LogoPath);

        var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
        var canvas = GameObject.Find("MainMenuCanvas").transform;
        var background = (RectTransform)canvas.Find("Background");
        var logo = (RectTransform)canvas.Find("Logo") ?? (RectTransform)background.Find("Logo");
        var title = (RectTransform)logo.Find("Title");

        logo.SetParent(background, false);
        logo.SetAsLastSibling();
        logo.anchorMin = new Vector2(0.5f, bottom);
        logo.anchorMax = new Vector2(0.5f, top);
        logo.pivot = new Vector2(0.5f, 0.5f);
        logo.anchoredPosition = Vector2.zero;
        logo.sizeDelta = Vector2.zero;
        logo.localScale = Vector3.one;

        var fitter = logo.GetComponent<AspectRatioFitter>() ?? logo.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
        fitter.aspectRatio = (float)sprite.texture.width / sprite.texture.height;

        title.anchorMin = Vector2.zero;
        title.anchorMax = Vector2.one;
        title.pivot = new Vector2(0.5f, 0.5f);
        title.anchoredPosition = Vector2.zero;
        title.sizeDelta = Vector2.zero;
        var img = title.GetComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;

        EditorUtility.SetDirty(logo);
        EditorUtility.SetDirty(title);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return "logo " + sprite.texture.width + "x" + sprite.texture.height;
    }
}
