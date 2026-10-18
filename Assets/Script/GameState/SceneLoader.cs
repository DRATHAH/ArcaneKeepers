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
        // Makes sure the player doesn't start a match with 0 towers equipped
        if (Inventory.instance && !Inventory.instance.CheckEquippedTowers())
        {
            return;
        }
        else if (Inventory.instance)
        {
            PlayerManager.instance.SaveTowers(Inventory.instance);
        }

        StartCoroutine(LoadSceneAsync(sceneToLoad));
    }

    IEnumerator LoadSceneAsync(string sceneToLoad)
    {
        yield return new WaitForSeconds(1);
        // Wait for transition to finish

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneToLoad);
        while (!operation.isDone)
        {
            // Progress bar code here if necessary
            yield return null;
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
