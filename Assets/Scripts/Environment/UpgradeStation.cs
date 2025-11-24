using UnityEngine;
using TMPro;

public class UpgradeStation : MonoBehaviour, IInteractable
{
    public PopUpSystem pop;
    
    [Header("Interaction Prompt")]
    public NotificationSystem notificationSystem;
    public Color promptColor = Color.white;
    
    [Header("UI Text Elements")]
    public TMP_Text skillPointsText;
    public TMP_Text strengthLevelText;
    public TMP_Text radiusLevelText;
    public TMP_Text distanceLevelText;
    public TMP_Text speedLevelText;
    public TMP_Text storageLevelText;
    public TMP_Text toolTierText;
    public TMP_Text tierProgressText;

    void Update()
    {
        // Continuously update UI if the popup is active
        if (pop != null && pop.popUpBox != null && pop.popUpBox.activeSelf)
        {
            UpdateUpgradeUI();
        }
    }

    public void Interact()
    {
        // Toggle the menu if it's already open
        if (pop != null && pop.popUpBox != null && pop.popUpBox.activeSelf)
        {
            pop.ClosePopUp();
        }
        else
        {
            // Hide the interaction prompt when opening the menu
            HidePrompt();
            
            UpdateUpgradeUI();
            pop.PopUp("MINE AND SELL MATERIALS FOR POINTS");
        }
    }
    
    public void ShowPrompt()
    {
        if (notificationSystem != null)
        {
            notificationSystem.ShowNotification("Press E to Upgrade Your Tool", promptColor, persistent: true);
        }
    }
    
    public void HidePrompt()
    {
        if (notificationSystem != null)
        {
            notificationSystem.HideNotification();
        }
    }
    
    void UpdateUpgradeUI()
    {
        PlayerProgression progression = FindFirstObjectByType<PlayerProgression>();
        DigTool digTool = FindFirstObjectByType<DigTool>();
        PlayerInventory inventory = FindFirstObjectByType<PlayerInventory>();
        
        // Update skill points
        if (progression != null && skillPointsText != null)
        {
            skillPointsText.text = $"{progression.skillPoints}";
        }
        
        if (digTool != null)
        {
            // Update individual upgrade values (actual stats, not upgrade count)
            if (strengthLevelText != null)
                strengthLevelText.text = $"{digTool.digStrength:F1}";
            
            if (radiusLevelText != null)
                radiusLevelText.text = $"{digTool.digRadius:F1}";
            
            if (distanceLevelText != null)
                distanceLevelText.text = $"{digTool.digDistance:F1}";
            
            if (speedLevelText != null)
                speedLevelText.text = $"{digTool.attackSpeed:F1}";
            
            // Update tool tier
            if (toolTierText != null)
                toolTierText.text = $"{digTool.toolTier}";
            
            // Update progress to next tier
            if (tierProgressText != null)
            {
                int currentUpgrades = digTool.totalUpgrades % digTool.upgradesPerTier;
                int needed = digTool.upgradesPerTier;
                tierProgressText.text = $"{currentUpgrades}/{needed}";
            }
        }
        
        // Update storage capacity
        if (inventory != null && storageLevelText != null)
        {
            storageLevelText.text = $"{inventory.maxInventoryCapacity:F0}";
        }
    }
}
