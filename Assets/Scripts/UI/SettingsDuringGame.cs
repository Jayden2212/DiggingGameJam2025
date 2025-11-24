using UnityEngine;
using UnityEngine.SceneManagement;

public class SettingsDuringGame : MonoBehaviour
{
    GameObject SettingScreenCanvas;

    void Awake()
    {
        SettingScreenCanvas = GameObject.Find("SettingsScreenCanvas");
    }
    public void OpenSettingsScreen()
    {
        SettingScreenCanvas = GameObject.Find("SettingsScreenCanvas");
        SettingScreenCanvas.SetActive(true);
    }

    public void CloseSettingsScreen()
    {
        SettingScreenCanvas.SetActive(false);
    }
    
}
