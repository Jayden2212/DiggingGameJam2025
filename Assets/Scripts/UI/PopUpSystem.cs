using TMPro;
using UnityEngine;

public class PopUpSystem : MonoBehaviour
{
    public GameObject popUpBox;
    public TMP_Text popUpText;
    public PlayerCam cam;
    public PlayerController controller;

    float tempSensX, tempSensY, tempMovementSpeed, tempJumpForce;

    void GetTempValues()
    {
        tempSensX = cam.sensX;
        tempSensY = cam.sensY;
        tempMovementSpeed = controller.movementSpeed;
        tempJumpForce = controller.jumpForce;
    }

    public void PopUp(string text)
    {
        GetTempValues();
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        cam.sensX = 0f;
        cam.sensY = 0f;
        controller.movementSpeed = 0f;
        controller.jumpForce = 0f;

        popUpBox.SetActive(true);
        popUpText.text = text;
    }

    public void ClosePopUp()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        cam.sensX = tempSensX;
        cam.sensY = tempSensY;
        controller.movementSpeed = tempMovementSpeed;
        controller.jumpForce = tempJumpForce;

        popUpBox.SetActive(false);
    }
}
