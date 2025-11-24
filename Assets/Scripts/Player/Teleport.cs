using UnityEngine;

public class Teleport : MonoBehaviour
{
    [Header("Manual Teleport Values (Fallback)")]
    public float posX, posY, posZ;
    public float rotX, rotY, rotZ;
    
    private PlayerCam playerCam;
    private Rigidbody rb;
    
    void Start()
    {
        playerCam = FindFirstObjectByType<PlayerCam>();
        rb = GetComponent<Rigidbody>();
    }
    
    public void OnTeleport()
    {
        GameObject spawnPoint = GameObject.Find("SpawnPoint");
        if (spawnPoint != null)
        {
            // Reset physics velocity before teleporting
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                
                // Use Rigidbody methods for teleporting to avoid physics conflicts
                rb.position = spawnPoint.transform.position;
                rb.rotation = spawnPoint.transform.rotation;
            }
            else
            {
                // Fallback to transform if no Rigidbody
                transform.position = spawnPoint.transform.position;
                transform.rotation = spawnPoint.transform.rotation;
            }
            
            // Reset camera rotation via PlayerCam
            if (playerCam != null)
            {
                playerCam.ResetToSpawnPoint();
            }
            
            // Update public fields for inspector visibility
            Vector3 spawnPos = spawnPoint.transform.position;
            Vector3 spawnRot = spawnPoint.transform.rotation.eulerAngles;
            posX = spawnPos.x;
            posY = spawnPos.y;
            posZ = spawnPos.z;
            rotX = spawnRot.x;
            rotY = spawnRot.y;
            rotZ = spawnRot.z;
        }
        else
        {
            // Fallback to manual values if SpawnPoint not found
            transform.position = new Vector3(posX, posY, posZ);
            transform.rotation = Quaternion.Euler(rotX, rotY, rotZ);
        }
    }
}
