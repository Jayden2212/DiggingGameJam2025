using UnityEngine;
using System.Collections;

public class CaveChest : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject player;
    [Header("Teleport Settings")]
    public Transform teleportTarget;
    public GameObject bottom;
    
    [Header("Timer")]
    public Timer timer;
    
    [Header("Terrain")]
    public TerrainChunk terrainChunk;
    
    [Header("Progression")]
    public PlayerProgression playerProgression;
    
    [Header("Interaction Prompt")]
    public NotificationSystem notificationSystem;
    public Color promptColor = Color.yellow;
    
    private bool hasBeenActivated = false;
    
    public void Interact()
    {
        // Hide prompt
        HidePrompt();
        
        // Start the victory sequence as a coroutine
        StartCoroutine(VictorySequence());
    }
    
    private IEnumerator VictorySequence()
    {
        // Stop timer and get final time
        string finalTime = "";
        if (timer != null)
        {
            finalTime = timer.GetFormattedTime();
            timer.StopTimer();
        }
        
        // Teleport player to top using Teleport component
        if (player != null)
        {
            Teleport teleportScript = player.GetComponent<Teleport>();
            if (teleportScript != null)
            {
                teleportScript.OnTeleport();
            }
        }
        
        // Reset terrain (heavy operation)
        if (terrainChunk != null)
        {
            terrainChunk.InitializeTerrain();
        }
        
        // Wait a frame for terrain reset to complete
        yield return new WaitForSeconds(1f);
        
        // Activate bottom GameObject
        if (bottom != null)
        {
            bottom.SetActive(true);
        }
        
        // Give player 999 skill points
        if (playerProgression != null)
        {
            playerProgression.skillPoints = 999;
        }

        // Mark as activated so prompt doesn't show again
        hasBeenActivated = true;
        
        // Show victory notification AFTER everything has settled
        if (notificationSystem != null)
        {
            string message = string.IsNullOrEmpty(finalTime) 
                ? "Congrats! You have won! You now have infinite upgrades" 
                : $"Congrats! You won in {finalTime}! You now have infinite upgrades";
            notificationSystem.ShowNotification(message, Color.green, persistent: false, duration: 6f);
        }
    }
    
    public void ShowPrompt()
    {
        if (!hasBeenActivated && notificationSystem != null)
        {
            notificationSystem.ShowNotification("Press E to reap your benefits", promptColor, persistent: true);
        }
    }
    
    public void HidePrompt()
    {
        if (notificationSystem != null)
        {
            notificationSystem.HideNotification();
        }
    }
}
