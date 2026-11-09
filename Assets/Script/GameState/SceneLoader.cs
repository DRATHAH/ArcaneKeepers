using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (Time.timeScale == 0)
        {
            Time.timeScale = 1;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void LoadScene(string sceneToLoad)
    {
        LoadScene(sceneToLoad, false);
        if (PlayerManager.instance != null)
        {
            PlayerManager.instance.SaveToFile();
        }
    }

    // DEPLOY on the character select: the push slide, but longer and gentler
    public void LoadSceneGentle(string sceneToLoad)
    {
        LoadScene(sceneToLoad, false, true);
        if (PlayerManager.instance != null)
        {
            PlayerManager.instance.SaveToFile();
        }
    }

    // Like LoadScene, but with the cloud wipe instead of the push slide
    public void LoadSceneWithClouds(string sceneToLoad)
    {
        LoadScene(sceneToLoad, true);
        if (PlayerManager.instance != null)
        {
            PlayerManager.instance.SaveToFile();
        }
    }

    void LoadScene(string sceneToLoad, bool clouds, bool gentle = false)
    {
        // Makes sure the player doesn't start a match with 0 towers equipped
        if (Inventory.instance && !Inventory.instance.CheckEquippedTowers())
        {
            return;
        }
        else if (Inventory.instance)
        {
            PlayerManager.instance.SaveTowers(Inventory.instance);
        }

        Time.timeScale = 1;
        // The transition keeps this screen up for 0.2s so the button sound plays (0.42s for DEPLOY, so the
        // squad's send-off hop finishes), loading the next scene in the background meanwhile, then slides
        // (or clouds) the new screen in
        SceneTransition.Load(sceneToLoad, clouds, gentle ? 0.42f : 0.2f, gentle);
        if (PlayerManager.instance != null)
        {
            PlayerManager.instance.SaveToFile();
        }
    }

    public void Restart()
    {
        string sceneToLoad = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(sceneToLoad);
    }

    public void Quit()
    {
        Application.Quit();
    }
}
