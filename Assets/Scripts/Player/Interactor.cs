using UnityEngine;
using UnityEngine.InputSystem;

interface IInteractable
{
    public void Interact();
}
public class Interactor : MonoBehaviour
{
    // the transform from which the interacting ray will be casted
    public Transform interactorSource;

    // length of interacting raycast
    public float interactRange;

    // layer of the target object
    public LayerMask mask;

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