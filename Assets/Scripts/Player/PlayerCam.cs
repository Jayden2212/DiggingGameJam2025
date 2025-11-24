using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCam : MonoBehaviour
{
    public Transform orientation;

    [Header("Camera Settings")]
    public float sensX = 5f;
    public float sensY = 5f;
    private Vector2 mouseInput;
    float xRotation, yRotation;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Initialize camera rotation to match SpawnPoint
        GameObject spawnPoint = GameObject.Find("SpawnPoint");
        if (spawnPoint != null)
        {
            Vector3 spawnRot = spawnPoint.transform.rotation.eulerAngles;
            xRotation = spawnRot.x;
            yRotation = spawnRot.y;
            transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
            if (orientation != null)
            {
                orientation.rotation = Quaternion.Euler(0, yRotation, 0);
            }
        }
    }

    void Update()
    {
        float mouseX = mouseInput.x * Time.deltaTime * sensX;
        float mouseY = mouseInput.y * Time.deltaTime * sensY;

        yRotation += mouseX;
        xRotation -= mouseY;

        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
        orientation.rotation = Quaternion.Euler(0, yRotation, 0);
    }

    public void OnLook(InputValue val)
    {
        mouseInput = val.Get<Vector2>();
    }
    
    // Call this method to reset camera rotation to SpawnPoint (e.g., when teleporting)
    public void ResetToSpawnPoint()
    {
        GameObject spawnPoint = GameObject.Find("SpawnPoint");
        if (spawnPoint != null)
        {
            Vector3 spawnRot = spawnPoint.transform.rotation.eulerAngles;
            xRotation = spawnRot.x;
            yRotation = spawnRot.y;
            transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
            if (orientation != null)
            {
                orientation.rotation = Quaternion.Euler(0, yRotation, 0);
            }
        }
    }
}
