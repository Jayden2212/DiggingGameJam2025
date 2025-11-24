using TMPro;
using UnityEngine;

public class PopUpSystem : MonoBehaviour
{
    public GameObject popUpBox;
    public TMP_Text popUpText;
    public PlayerController controller;
    public PlayerCam playerCam;
    public Swing swing;
    public DigTool digTool;

    float tempSensX, tempSensY, tempMovementSpeed, tempJumpForce;
    bool valuesStored = false;

    void GetTempValues()
    {
        // Only store values if not already stored (prevent overwriting with zeroed values)
        if (!valuesStored)
        {
            tempSensX = playerCam.sensX;
            tempSensY = playerCam.sensY;
            tempMovementSpeed = controller.movementSpeed;
            tempJumpForce = controller.jumpForce;
            valuesStored = true;
        }
    }

    public void PopUp(string text)
    {
        GetTempValues();
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        playerCam.sensX = 0f;
        playerCam.sensY = 0f;
        controller.movementSpeed = 0f;
        controller.jumpForce = 0f;
        
        // Disable digging animation and tool
        if (swing != null)
            swing.enabled = false;
        if (digTool != null)
            digTool.enabled = false;

        popUpBox.SetActive(true);
        popUpText.text = text;
    }

    public void ClosePopUp()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        playerCam.sensX = tempSensX;
        playerCam.sensY = tempSensY;
        controller.movementSpeed = tempMovementSpeed;
        controller.jumpForce = tempJumpForce;
        
        // Re-enable digging animation and tool
        if (swing != null)
            swing.enabled = true;
        if (digTool != null)
            digTool.enabled = true;

        popUpBox.SetActive(false);
        
        // Reset flag so values can be stored again next time
        valuesStored = false;
    }
}
