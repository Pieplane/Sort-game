using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadScene(string sceneName)
    {
        Debug.Log($"[SceneLoader] LoadScene: {sceneName}");
        SceneManager.LoadScene(sceneName);
    }

    public void LoadHome()
    {
        LoadScene("Menu");
    }

    public void LoadCollection()
    {
        LoadScene("Collection");
    }
    public void LoadGame()
    {
        LoadScene("SampleScene");
    }
    public void LoadShop()
    {
        LoadScene("Shop");
    }
}
