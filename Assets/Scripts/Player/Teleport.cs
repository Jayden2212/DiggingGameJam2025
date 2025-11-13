using UnityEngine;

public class Teleport : MonoBehaviour
{
    public float posX, posY, posZ;
    public void OnTeleport()
    {
        transform.position = new Vector3(posX, posY, posZ);
    }
}
