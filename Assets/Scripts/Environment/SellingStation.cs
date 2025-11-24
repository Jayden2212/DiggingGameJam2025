using UnityEngine;
using System.Collections.Generic;

public class SellingStation : MonoBehaviour, IInteractable
{
    [Header("References")]
    public PopUpSystem popUpSystem;
    
    private PlayerInventory inventory;
    private PlayerProgression progression;
    
    void Start()
    {
        inventory = FindFirstObjectByType<PlayerInventory>();
        progression = FindFirstObjectByType<PlayerProgression>();
        
        if (inventory == null)
        {
            Debug.LogError("SellStation: No PlayerInventory found in scene!");
        }
        if (progression == null)
        {
            Debug.LogError("SellStation: No PlayerProgression found in scene!");
        }
    }
    
    public void Interact()
    {
        if (popUpSystem != null)
        {
            popUpSystem.PopUp("SELLING STATION");
        }
    }
    
    public void SellAllResources()
    {
        if (inventory == null || progression == null) return;
        
        int totalMoney = 0;
        int totalXP = 0;
        int totalItemsSold = 0;
        
        // Get all resource types
        var resourceTypes = new List<VoxelType>
        {
            VoxelType.CopperOre, VoxelType.IronOre, VoxelType.GoldOre, 
            VoxelType.AmethystOre, VoxelType.DiamondOre,
            VoxelType.Dirt, VoxelType.LimeStone, VoxelType.Granite, VoxelType.Bedrock, VoxelType.Molten
        };
        
        // Sell each resource type
        foreach (var resourceType in resourceTypes)
        {
            int amount = inventory.GetResourceAmount(resourceType);
            if (amount > 0)
            {
                var resourceData = inventory.GetResourceData(resourceType);
                if (resourceData != null)
                {
                    int moneyGained = resourceData.sellValue * amount;
                    int xpGained = resourceData.xpValue * amount;
                    
                    totalMoney += moneyGained;
                    totalXP += xpGained;
                    totalItemsSold += amount;
                    
                    // Remove resources from inventory
                    inventory.RemoveResource(resourceType, amount);
                }
            }
        }
        
        // Grant XP
        if (totalXP > 0)
        {
            progression.AddXP(totalXP);
        }
        
        // Display results (you can enhance this with a proper UI)
        if (totalItemsSold > 0)
        {
            Debug.Log($"Sold {totalItemsSold} items for ${totalMoney} and {totalXP} XP!");
            
            if (popUpSystem != null)
            {
                popUpSystem.PopUp($"SOLD!\n{totalItemsSold} items\n${totalMoney}\n+{totalXP} XP");
            }
        }
        else
        {
            Debug.Log("No items to sell!");
            
            if (popUpSystem != null)
            {
                popUpSystem.PopUp("SELL STATION\nNo items to sell!");
            }
        }
    }
    
    /// <summary>
    /// Sells a specific resource type.
    /// </summary>
    public void SellResource(VoxelType resourceType, int amount)
    {
        if (inventory == null || progression == null) return;
        
        int currentAmount = inventory.GetResourceAmount(resourceType);
        if (currentAmount < amount)
        {
            Debug.LogWarning($"Not enough {resourceType} to sell! Have {currentAmount}, trying to sell {amount}");
            return;
        }
        
        var resourceData = inventory.GetResourceData(resourceType);
        if (resourceData != null)
        {
            int moneyGained = resourceData.sellValue * amount;
            int xpGained = resourceData.xpValue * amount;
            
            // Remove resources and grant XP
            inventory.RemoveResource(resourceType, amount);
            
            if (xpGained > 0)
            {
                progression.AddXP(xpGained);
            }
            
            Debug.Log($"Sold {amount} {resourceType} for ${moneyGained} and {xpGained} XP!");
        }
    }
}
