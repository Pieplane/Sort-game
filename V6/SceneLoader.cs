using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadScene(string sceneName)
    {
        //Debug.Log($"[SceneLoader] LoadScene: {sceneName}");
        SceneManager.LoadScene(sceneName);
    }

    public void LoadHome()
    {
        if (AudioManager.Instance != null)
        {
            // ================================== 🎧 AUDIO MANAGER CALL ==================================
            AudioManager.Instance.Play("Click");
        }
        LoadScene("Menu");
    }

    public void LoadCollection()
    {
        if (AudioManager.Instance != null)
        {
            // ================================== 🎧 AUDIO MANAGER CALL ==================================
            AudioManager.Instance.Play("Click");
        }
        AdController.Instance.ShowAd(() =>
        {
            LoadScene("Collection");
        });
        
    }
    public void LoadGame()
    {
        if (AudioManager.Instance != null)
        {
            // ================================== 🎧 AUDIO MANAGER CALL ==================================
            AudioManager.Instance.Play("Click");
        }
        LoadScene("SampleScene");
    }
    public void LoadShop()
    {
        if (AudioManager.Instance != null)
        {
            // ================================== 🎧 AUDIO MANAGER CALL ==================================
            AudioManager.Instance.Play("Click");
        }
        AdController.Instance.ShowAd(() =>
        {
            LoadScene("Shop");
        });
        
    }
}
