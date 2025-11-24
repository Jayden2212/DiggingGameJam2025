using UnityEngine;

public class SellingStation : MonoBehaviour, IInteractable
{
    public PopUpSystem pop;
    public void Interact()
    {
        pop.PopUp("SELLING STATION");
    }
}
