using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Swing : MonoBehaviour
{
    Animator anim;
    [SerializeField] private InputActionReference clickAction;
    [SerializeField] private DigTool digTool; // Reference to DigTool to sync attack speed
    
    private bool isHoldingButton = false;
    private float lastSwingTime = 0f;

    void Start()
    {
        anim = GetComponent<Animator>();
        
        // Try to find DigTool if not assigned
        if (digTool == null)
        {
            digTool = GetComponentInParent<DigTool>();
            if (digTool == null)
            {
                digTool = FindFirstObjectByType<DigTool>();
            }
        }
    }
    
    void Update()
    {
        // Sync animation speed with attack speed from DigTool
        if (digTool != null && anim != null)
        {
            anim.speed = digTool.attackSpeed;
        }
        
        // Continuously swing while holding button
        if (isHoldingButton && digTool != null)
        {
            float swingCooldown = 1f / digTool.attackSpeed;
            if (Time.time - lastSwingTime >= swingCooldown)
            {
                lastSwingTime = Time.time;
                anim.SetTrigger("Mining");
            }
        }
    }

    private void OnEnable()
    {
        if (clickAction != null && clickAction.action != null)
        {
            clickAction.action.Enable();
            clickAction.action.performed += OnClickPerformed;
            clickAction.action.canceled += OnClickCanceled;
        }
    }

    private void OnDisable()
    {
        if (clickAction != null && clickAction.action != null)
        {
            clickAction.action.performed -= OnClickPerformed;
            clickAction.action.canceled -= OnClickCanceled;
            clickAction.action.Disable();
        }
    }

    private void OnClickPerformed(InputAction.CallbackContext ctx)
    {
        isHoldingButton = true;
        // Trigger first swing immediately
        anim.SetTrigger("Mining");
        lastSwingTime = Time.time;
    }
    
    private void OnClickCanceled(InputAction.CallbackContext ctx)
    {
        isHoldingButton = false;
    }
    
    // This method is called by an Animation Event halfway through the Mining animation
    // Add this as an Animation Event in your Mining animation at 50% completion
    public void OnSwingHit()
    {
        if (digTool != null)
        {
            digTool.TriggerHitFromAnimation();
        }
    }
}