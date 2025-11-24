using UnityEngine;

public class Settings : MonoBehaviour
{
    public GameObject SettingScreenCanvas;
    public GameObject TitleScreenCanvas;
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
