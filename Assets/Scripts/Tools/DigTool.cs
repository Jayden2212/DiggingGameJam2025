using UnityEngine;
using UnityEngine.InputSystem;

public class DigTool : MonoBehaviour
{
    [Header("Digging Settings")]
    public float digRadius = 2f;
    public float digStrength = 5f;
    public float digDistance = 10f;
    
    [Header("Visual Feedback")]
    public GameObject digCursorPrefab; // Optional: sphere to show dig location
    private GameObject digCursor;
    
    private Camera mainCamera;
    
    [Header("Input")]
    // Assign these to actions from your InputSystem_Actions.inputactions asset in the Inspector
    public InputActionReference pointAction; // Vector2 (pointer position)
    public InputActionReference digAction;   // Button
    
    public enum AimMode { Pointer, CameraCenter, CameraForward }
    [Header("Aim")]
    public AimMode aimMode = AimMode.Pointer;
    [Tooltip("Optional transform to use as the ray origin and forward for CameraForward mode (e.g., player head). If null, uses mainCamera.")]
    public Transform aimOrigin;
    
    void Start()
    {
        mainCamera = Camera.main;
        Debug.Log("mainCamera: " + mainCamera);
        
        if (digCursorPrefab != null)
        {
            digCursor = Instantiate(digCursorPrefab);
            digCursor.transform.localScale = Vector3.one * digRadius * 2f;
        }
    }
    
    void OnEnable()
    {
        if (pointAction != null && pointAction.action != null) pointAction.action.Enable();
        if (digAction != null && digAction.action != null) digAction.action.Enable();
    }

    void OnDisable()
    {
        if (pointAction != null && pointAction.action != null) pointAction.action.Disable();
        if (digAction != null && digAction.action != null) digAction.action.Disable();
    }
    
    void Update()
    {
        if (mainCamera == null)
            return;

        // Determine input and build ray according to selected aim mode
        Vector3 mousePos = Vector3.zero;
        bool leftPressed = false;

        if (digAction != null && digAction.action != null)
        {
            leftPressed = digAction.action.ReadValue<float>() > 0.5f;
        }

        Ray ray;
        if (aimMode == AimMode.Pointer)
        {
            if (pointAction == null || pointAction.action == null)
                return; // pointer mode requires a point action

            Vector2 mp = pointAction.action.ReadValue<Vector2>();
            mousePos = new Vector3(mp.x, mp.y, 0f);
            ray = mainCamera.ScreenPointToRay(mousePos);
        }
        else if (aimMode == AimMode.CameraCenter)
        {
            // Ray from the center of the screen
            Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            ray = mainCamera.ScreenPointToRay(center);
        }
        else // CameraForward
        {
            Transform origin = aimOrigin != null ? aimOrigin : mainCamera.transform;
            ray = new Ray(origin.position, origin.forward);
        }
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, digDistance))
        {
            // Show dig cursor
            if (digCursor != null)
            {
                digCursor.SetActive(true);
                digCursor.transform.position = hit.point;
            }
            
            // Dig on left click
            if (leftPressed)
            {
                TerrainChunk chunk = hit.collider.GetComponent<TerrainChunk>();
                if (chunk != null)
                {
                    chunk.DigAtPosition(hit.point, digRadius, digStrength * Time.deltaTime);
                }
            }
        }
        else
        {
            if (digCursor != null)
                digCursor.SetActive(false);
        }
    }
}