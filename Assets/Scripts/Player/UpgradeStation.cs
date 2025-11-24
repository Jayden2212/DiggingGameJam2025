using UnityEngine;

public class UpgradeStation : MonoBehaviour, IInteractable
{
    public PopUpSystem pop;
    public void Interact()
    {
        pop.PopUp("UPGRADE STATION");
    }
}
