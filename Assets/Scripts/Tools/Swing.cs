using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Swing : MonoBehaviour
{
    Animator anim;
    [SerializeField] private InputActionReference clickAction;
    [SerializeField] private DigTool digTool; // Reference to DigTool to sync attack speed
    private PlayerInventory playerInventory;
    
    private bool isHoldingButton = false;
    private float lastSwingTime = 0f;
    private float lastNotificationTime = -999f; // Track when we last showed notification

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
        
        // Find PlayerInventory
        playerInventory = FindFirstObjectByType<PlayerInventory>();
    }
    
    void Update()
    {
        // Sync animation speed with attack speed from DigTool
        if (digTool != null && anim != null)
        {
            anim.speed = digTool.attackSpeed;
        }
        
        // Don't swing if inventory is completely full (but don't show notification here - only on click)
        if (playerInventory != null && playerInventory.IsFull())
        {
            return;
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
        // Don't swing if inventory is completely full
        if (playerInventory != null && playerInventory.IsFull())
        {
            // Show notification when trying to click with full inventory
            if (digTool != null)
            {
                Debug.Log($"Swing.OnClickPerformed: Calling TriggerInventoryFullMessage");
                digTool.TriggerInventoryFullMessage();
            }
            return;
        }
        
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
        // Don't trigger hit if inventory is full
        if (playerInventory != null && playerInventory.IsFull())
        {
            Debug.Log("OnSwingHit: Inventory is full, blocking hit!");
            // Trigger the inventory full notification from DigTool
            if (digTool != null)
            {
                digTool.TriggerInventoryFullMessage();
            }
            return;
        }
        
        Debug.Log("OnSwingHit: Triggering hit");
        if (digTool != null)
        {
            digTool.TriggerHitFromAnimation();
        }
    }
}