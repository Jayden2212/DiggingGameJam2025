using UnityEngine;
using UnityEngine.SceneManagement;

public class Settings : MonoBehaviour
{
    public GameObject TitleScreenCanvas;
    public GameObject SettingScreenCanvas;
    public void OpenSettingsScreen()
    {
        TitleScreenCanvas.SetActive(false);
        SettingScreenCanvas.SetActive(true);
    }

    public void CloseSettingsScreen()
    {
        TitleScreenCanvas.SetActive(true);
        SettingScreenCanvas.SetActive(false);
    }
    
}
