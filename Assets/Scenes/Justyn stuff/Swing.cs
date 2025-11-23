using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class Swing : MonoBehaviour
{
    Animator anim;
    [SerializeField] private InputActionReference clickAction;

    void Start()
    {
        anim = GetComponent<Animator>();
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
        anim.SetTrigger("Mining");
    }
    private void OnClickCanceled(InputAction.CallbackContext ctx)
    {
        // Nothing
    }
}