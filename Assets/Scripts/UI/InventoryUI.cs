using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    [System.Serializable]
    public class ResourceBar
    {
        public VoxelType resourceType;
        public Image fillImage; // The bar that fills up
        public TextMeshProUGUI amountText; // Optional: text showing "50/100"
    }
    
    [Header("References")]
    public PlayerInventory playerInventory;
    
    [Header("Resource Bars")]
    public List<ResourceBar> resourceBars = new List<ResourceBar>();
    
    [Header("Total Storage (Optional)")]
    public Image totalStorageBar; // Combined bar showing all resources
    public TextMeshProUGUI totalStorageText;
    
    void Start()
    {
        // Find PlayerInventory if not assigned
        if (playerInventory == null)
        {
            playerInventory = FindFirstObjectByType<PlayerInventory>();
        }
        
        // Initialize bar colors from inventory
        if (playerInventory != null)
        {
            foreach (var bar in resourceBars)
            {
                var resourceData = playerInventory.GetResourceData(bar.resourceType);
                if (resourceData != null && bar.fillImage != null)
                {
                    bar.fillImage.color = resourceData.displayColor;
                }
            }
        }
    }
    
    void Update()
    {
        if (playerInventory == null) return;
        
        // Update individual resource bars
        foreach (var bar in resourceBars)
        {
            var resourceData = playerInventory.GetResourceData(bar.resourceType);
            if (resourceData != null)
            {
                // Update fill amount
                if (bar.fillImage != null)
                {
                    bar.fillImage.fillAmount = resourceData.FillPercentage;
                }
                
                // Update text
                if (bar.amountText != null)
                {
                    bar.amountText.text = $"{resourceData.amount}/{resourceData.maxCapacity}";
                }
            }
        }
        
        // Update total storage bar (shows combined fill across all resources)
        if (totalStorageBar != null)
        {
            totalStorageBar.fillAmount = playerInventory.GetTotalFillPercentage();
        }
        
        if (totalStorageText != null)
        {
            int totalAmount = 0;
            int totalCapacity = 0;
            foreach (var resource in playerInventory.resources)
            {
                totalAmount += resource.amount;
                totalCapacity += resource.maxCapacity;
            }
            totalStorageText.text = $"{totalAmount}/{totalCapacity}";
        }
    }
    
    // Optional: Create a multi-colored bar that shows proportions of each ore
    // Call this if you want a single bar with multiple colors representing different ores
    public void UpdateMultiColorBar(Image barImage, List<Image> colorSegments)
    {
        if (playerInventory == null) return;
        
        float totalFill = playerInventory.GetTotalFillPercentage();
        barImage.fillAmount = totalFill;
        
        // You can extend this to show each ore as a different colored segment
        // This requires more complex UI setup with multiple overlapping fill images
    }
}
