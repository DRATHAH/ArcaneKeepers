using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Phone-app style "push" between scenes. Going forward, the new screen slides in from the right
// over the old one, which drifts left and darkens; going back plays it in reverse.
// It renders the old screen to a texture, loads the new scene behind it, renders the new scene's
// first frame the same way, then slides the two pictures and hands over to the live scene.
// (Rendered by the scene's camera rather than ScreenCapture, which gives a blank white picture in
// the editor's Device Simulator.)
// Use SceneTransition.Load("SceneName") instead of SceneManager.LoadScene.
public class SceneTransition : MonoBehaviour
{
    public static float slideTime = 0.38f;
    // How far the screen underneath moves, as a share of the width (iOS uses about a third)
    const float Parallax = 0.3f;
    const float UnderDim = 0.55f;

    // Menu order: moving to an earlier screen counts as going back. Anything else slides forward.
    static readonly string[] MenuOrder = { "MainMenu", "TowerSelect", "LevelSelect" };

    static SceneTransition instance;

    Canvas canvas;
    RawImage oldScreen;
    RawImage newScreen;
    bool busy;

    public static bool IsTransitioning => instance && instance.busy;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        instance = null;
    }

    public static void Load(string sceneName)
    {
        Time.timeScale = 1;
        if (!instance)
        {
            instance = Create();
        }
        if (!instance.busy)
        {
            instance.StartCoroutine(instance.Run(sceneName));
        }
    }

    static SceneTransition Create()
    {
        GameObject go = new GameObject("SceneTransition");
        DontDestroyOnLoad(go);
        SceneTransition transition = go.AddComponent<SceneTransition>();

        transition.canvas = go.AddComponent<Canvas>();
        transition.canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        transition.canvas.sortingOrder = 1000;
        transition.canvas.enabled = false;
        // Swallows taps while the screen changes
        go.AddComponent<GraphicRaycaster>();

        transition.oldScreen = CreateScreen("OldScreen", go.transform);
        transition.newScreen = CreateScreen("NewScreen", go.transform);
        return transition;
    }

    static RawImage CreateScreen(string name, Transform parent)
    {
        GameObject image = new GameObject(name, typeof(RectTransform));
        image.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)image.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return image.AddComponent<RawImage>();
    }

    IEnumerator Run(string sceneName)
    {
        busy = true;
        if (SceneManager.GetActiveScene().name == "MainMenu" && sceneName == "TowerSelect")
        {
            yield return RunClouds(sceneName);
            busy = false;
            yield break;
        }
        bool back = System.Array.IndexOf(MenuOrder, sceneName) >= 0
            && System.Array.IndexOf(MenuOrder, sceneName) < System.Array.IndexOf(MenuOrder, SceneManager.GetActiveScene().name);

        // Freeze the current screen on top
        RenderTexture oldShot = RenderActiveScene();
        ShowScreen(oldScreen, oldShot);
        ShowScreen(newScreen, null);
        SetSlide(0, back);
        canvas.enabled = true;

        // Load behind it; any hitch while the scene activates is hidden by the frozen frame
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone)
        {
            yield return null;
        }
        // Let the new scene lay itself out, then take a picture of it to slide in
        yield return null;
        yield return null;
        RenderTexture newShot = RenderActiveScene();
        ShowScreen(newScreen, newShot);

        for (float t = 0; t < 1; t += Mathf.Min(Time.unscaledDeltaTime, 1f / 45f) / slideTime)
        {
            // Capped step: a slow frame slows the slide a touch instead of making it jump
            SetSlide(EaseOut(t), back);
            yield return null;
        }
        SetSlide(1, back);
        yield return null;

        canvas.enabled = false;
        ShowScreen(oldScreen, null);
        ShowScreen(newScreen, null);
        oldShot.Release();
        Destroy(oldShot);
        newShot.Release();
        Destroy(newShot);
        busy = false;
    }

    // Main menu -> character select: clouds roll over the live menu, the scene loads behind them,
    // then they part over the live character select (no pictures needed, nothing shows through)
    IEnumerator RunClouds(string sceneName)
    {
        CloudWipe clouds = GetComponent<CloudWipe>();
        if (!clouds)
        {
            clouds = gameObject.AddComponent<CloudWipe>();
        }
        ShowScreen(oldScreen, null);
        ShowScreen(newScreen, null);
        canvas.enabled = true;
        yield return clouds.Close();

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone)
        {
            yield return null;
        }
        yield return null;
        yield return null;

        yield return clouds.Open();
        canvas.enabled = false;
    }

    // A RawImage with no texture draws solid white, so hide it until it has a picture
    static void ShowScreen(RawImage screen, Texture picture)
    {
        screen.texture = picture;
        screen.enabled = picture != null;
    }

    // p = 0: old screen showing, p = 1: new screen showing
    void SetSlide(float p, bool back)
    {
        float width = ((RectTransform)canvas.transform).rect.width;
        RectTransform oldRect = oldScreen.rectTransform;
        RectTransform newRect = newScreen.rectTransform;
        if (!back)
        {
            // New screen slides in on top from the right; the old one drifts left underneath
            newRect.SetAsLastSibling();
            newRect.anchoredPosition = new Vector2((1 - p) * width, 0);
            oldRect.anchoredPosition = new Vector2(-p * Parallax * width, 0);
            oldScreen.color = Dim(p);
            newScreen.color = Color.white;
        }
        else
        {
            // Old screen slides off to the right on top, revealing the previous one underneath
            oldRect.SetAsLastSibling();
            oldRect.anchoredPosition = new Vector2(p * width, 0);
            newRect.anchoredPosition = new Vector2(-(1 - p) * Parallax * width, 0);
            newScreen.color = Dim(1 - p);
            oldScreen.color = Color.white;
        }
    }

    static Color Dim(float amount)
    {
        float v = 1 - amount * (1 - UnderDim);
        return new Color(v, v, v, 1);
    }

    static float EaseOut(float t)
    {
        t = Mathf.Clamp01(t);
        return 1 - (1 - t) * (1 - t) * (1 - t);
    }

    // Renders the active scene (cameras + its overlay canvases) into a texture, without showing it
    RenderTexture RenderActiveScene()
    {
        RenderTexture rt = new RenderTexture(Screen.width, Screen.height, 24);
        Camera cam = Camera.main;
        GameObject tempCamera = null;
        if (!cam)
        {
            tempCamera = new GameObject("TransitionCamera");
            cam = tempCamera.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.orthographic = true;
        }
        RenderTexture previousTarget = cam.targetTexture;
        cam.targetTexture = rt;

        // Overlay canvases don't draw into cameras, so point them at this camera for one render
        List<Canvas> moved = new List<Canvas>();
        Scene scene = SceneManager.GetActiveScene();
        foreach (Canvas c in FindObjectsByType<Canvas>(FindObjectsInactive.Exclude))
        {
            if (c != canvas && c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay && c.gameObject.scene == scene)
            {
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = cam;
                c.planeDistance = Mathf.Min(10, cam.farClipPlane * 0.5f);
                moved.Add(c);
            }
        }
        Canvas.ForceUpdateCanvases();
        cam.Render();

        foreach (Canvas c in moved)
        {
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.worldCamera = null;
        }
        Canvas.ForceUpdateCanvases();
        cam.targetTexture = previousTarget;
        if (tempCamera)
        {
            Destroy(tempCamera);
        }
        return rt;
    }
}
