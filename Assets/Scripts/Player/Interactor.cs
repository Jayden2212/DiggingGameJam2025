using UnityEngine;
using UnityEngine.InputSystem;

interface IInteractable
{
    public void Interact();
    public void ShowPrompt();
    public void HidePrompt();
}
public class Interactor : MonoBehaviour
{
    // the transform from which the interacting ray will be casted
    public Transform interactorSource;

    // length of interacting raycast
    public float interactRange;

    // layer of the target object
    public LayerMask mask;
    
    private IInteractable currentInteractable;

    void Update()
    {
        // Continuously raycast to detect interactable objects
        Ray r = new Ray(interactorSource.position, interactorSource.forward);
        if (Physics.Raycast(r, out RaycastHit hitInfo, interactRange, mask))
        {
            if (hitInfo.collider.gameObject.TryGetComponent(out IInteractable interactObj))
            {
                // If we're looking at a new interactable
                if (currentInteractable != interactObj)
                {
                    // Hide prompt from previous object
                    if (currentInteractable != null)
                    {
                        currentInteractable.HidePrompt();
                    }
                    
                    // Show prompt for new object
                    currentInteractable = interactObj;
                    currentInteractable.ShowPrompt();
                }
                return;
            }
        }
        
        // Not looking at anything interactable
        if (currentInteractable != null)
        {
            currentInteractable.HidePrompt();
            currentInteractable = null;
        }
    }

    public void OnPressButton()
    {
        Ray r = new Ray(interactorSource.position, interactorSource.forward);
        if (Physics.Raycast(r, out RaycastHit hitInfo, interactRange, mask))
        {
            if (hitInfo.collider.gameObject.TryGetComponent(out IInteractable interactObj))
            {
                interactObj.Interact();
            }
        }
    }
}