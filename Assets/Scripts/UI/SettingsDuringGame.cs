using UnityEngine;
using UnityEngine.SceneManagement;

public class SettingsDuringGame : MonoBehaviour
{
    [SerializeField] GameObject SettingScreenCanvas;
    
    [Header("Player References")]
    public PlayerController controller;
    public PlayerCam playerCam;
    public Swing swing;
    public DigTool digTool;

    void Awake()
    {
        // Only search if not already assigned in Inspector
        if (SettingScreenCanvas == null)
        {
            SettingScreenCanvas = FindSettingsCanvas();
        }
        
        // Find player components if not assigned
        FindPlayerComponents();
    }
    
    void FindPlayerComponents()
    {
        if (controller == null)
            controller = FindFirstObjectByType<PlayerController>();
        if (playerCam == null)
            playerCam = FindFirstObjectByType<PlayerCam>();
        if (swing == null)
            swing = FindFirstObjectByType<Swing>();
        if (digTool == null)
            digTool = FindFirstObjectByType<DigTool>();
            
        Debug.Log($"SettingsDuringGame - Found Components: Controller={controller != null}, Cam={playerCam != null}, Swing={swing != null}, DigTool={digTool != null}");
    }
    
    GameObject FindSettingsCanvas()
    {
        // First try standard Find (works for active objects in active scene)
        GameObject canvas = GameObject.Find("SettingsScreenCanvas");
        if (canvas != null) return canvas;
        
        // Search all Canvas objects (including inactive ones)
        Canvas[] allCanvases = Resources.FindObjectsOfTypeAll<Canvas>();
        foreach (Canvas c in allCanvases)
        {
            if (c.gameObject.name == "SettingsScreenCanvas")
            {
                return c.gameObject;
            }
        }
        
        // Last resort: search all GameObjects (including inactive)
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            // Exclude prefabs and assets
            if (obj.scene.name != null && obj.name == "SettingsScreenCanvas")
            {
                return obj;
            }
        }
        
        return null;
    }
    
    void DisablePlayerControls()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        if (playerCam != null)
            playerCam.isEnabled = false;
        
        if (controller != null)
            controller.isEnabled = false;
        
        if (swing != null)
            swing.enabled = false;
        if (digTool != null)
            digTool.enabled = false;
    }
    
    void RestorePlayerControls()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        if (playerCam != null)
            playerCam.isEnabled = true;
        
        if (controller != null)
            controller.isEnabled = true;
        
        if (swing != null)
            swing.enabled = true;
        if (digTool != null)
            digTool.enabled = true;
    }
    
    public void OpenSettingsScreen()
    {
        Debug.Log("OpenSettingsScreen called");
        
        // Try to find if still null
        if (SettingScreenCanvas == null)
        {
            SettingScreenCanvas = FindSettingsCanvas();
        }
        
        if (SettingScreenCanvas != null)
        {
            // Re-find player components in case they weren't available at Awake
            FindPlayerComponents();
            
            DisablePlayerControls();
            SettingScreenCanvas.SetActive(true);
            Debug.Log("Settings screen opened successfully");
        }
        else
        {
            Debug.LogError("SettingsScreenCanvas not found! Make sure it exists in the scene or assign it in the Inspector.");
        }
    }

    public void CloseSettingsScreen()
    {
        Debug.Log("CloseSettingsScreen called");
        
        if (SettingScreenCanvas != null)
        {
            SettingScreenCanvas.SetActive(false);
            RestorePlayerControls();
            Debug.Log("Settings screen closed successfully");
        }
    }
    
    public void ExitGame()
    {
        Debug.Log("ExitGame called");
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
    
}
