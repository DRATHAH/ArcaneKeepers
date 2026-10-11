using UnityEngine;
using UnityEngine.SceneManagement;

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
        // Makes sure the player doesn't start a match with 0 towers equipped
        if (Inventory.instance && !Inventory.instance.CheckEquippedTowers())
        {
            return;
        }
        else if (Inventory.instance)
        {
            PlayerManager.instance.SaveTowers(Inventory.instance);
        }
        SceneManager.LoadScene(sceneToLoad, LoadSceneMode.Single);
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
