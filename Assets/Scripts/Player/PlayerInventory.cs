using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlayerInventory : MonoBehaviour
{
    [Header("Inventory Settings")]
    [Tooltip("How much the max capacity increases per storage upgrade")]
    public float inventoryCapacityIncrement = 25f;
    
    [System.Serializable]
    public class ResourceData
    {
        public VoxelType resourceType;
        public int amount;
        public int maxCapacity;
        public Color displayColor;
        [Tooltip("How much inventory space each unit takes (ores = 1.0, rubble = 0.1)")]
        public float inventoryWeight = 1f;
        [Tooltip("Sell value per unit")]
        public int sellValue = 1;
        [Tooltip("XP gained when collecting (ores only)")]
        public int xpValue = 0;
        
        public ResourceData(VoxelType type, int capacity, Color color, float weight = 1f, int value = 1, int xp = 0)
        {
            resourceType = type;
            amount = 0;
            maxCapacity = capacity;
            displayColor = color;
            inventoryWeight = weight;
            sellValue = value;
            xpValue = xp;
        }
        
        public float FillPercentage => maxCapacity > 0 ? (float)amount / maxCapacity : 0f;
        public bool IsFull => amount >= maxCapacity;
    }
    
    [Header("Inventory Settings")]
    [Tooltip("Total inventory capacity (weight-based)")]
    public float maxInventoryCapacity = 200f;
    
    [Header("Resource Storage")]
    public List<ResourceData> resources = new List<ResourceData>();
    
    [Header("Events")]
    public UnityEvent<VoxelType, int> onResourceAdded; // Fires when resource is added (type, amount)
    public UnityEvent<VoxelType, int> onResourceRemoved; // Fires when resource is removed
    public UnityEvent<VoxelType> onResourceFull; // Fires when a resource reaches max capacity
    public UnityEvent onInventoryFull; // Fires when total inventory is full
    
    // Quick lookup dictionary
    private Dictionary<VoxelType, ResourceData> resourceLookup = new Dictionary<VoxelType, ResourceData>();
    
    void Awake()
    {
        InitializeInventory();
    }
    
    void InitializeInventory()
    {
        // If resources list is empty, auto-populate with all types
        if (resources.Count == 0)
        {
            // Ores - high value, high XP, fills inventory faster (weight = 1.0)
            resources.Add(new ResourceData(VoxelType.CopperOre, 9999, new Color(0.72f, 0.45f, 0.20f), 1f, 5, 10));
            resources.Add(new ResourceData(VoxelType.IronOre, 9999, new Color(0.75f, 0.75f, 0.75f), 1f, 10, 15));
            resources.Add(new ResourceData(VoxelType.GoldOre, 9999, new Color(1f, 0.84f, 0f), 1f, 20, 25));
            resources.Add(new ResourceData(VoxelType.AmethystOre, 9999, new Color(0.58f, 0.44f, 0.86f), 1f, 40, 40));
            resources.Add(new ResourceData(VoxelType.DiamondOre, 9999, new Color(0.68f, 0.85f, 0.90f), 1f, 100, 50));
            
            // Rubble/Terrain - low value, small XP, fills inventory slower (weight = 0.1)
            resources.Add(new ResourceData(VoxelType.Dirt, 9999, new Color(0.55f, 0.4f, 0.3f), 0.1f, 1, 1));
            resources.Add(new ResourceData(VoxelType.LimeStone, 9999, new Color(0.8f, 0.8f, 0.7f), 0.1f, 2, 2));
            resources.Add(new ResourceData(VoxelType.Granite, 9999, new Color(0.4f, 0.4f, 0.4f), 0.1f, 3, 3));
            resources.Add(new ResourceData(VoxelType.Bedrock, 9999, new Color(0.1f, 0.1f, 0.1f), 0.1f, 5, 4));
            resources.Add(new ResourceData(VoxelType.Molten, 9999, new Color(1f, 0.3f, 0f), 0.1f, 8, 5));
        }
        
        // Build lookup dictionary
        resourceLookup.Clear();
        foreach (var resource in resources)
        {
            if (!resourceLookup.ContainsKey(resource.resourceType))
            {
                resourceLookup.Add(resource.resourceType, resource);
            }
        }
    }
    
    // Add resources to inventory. Returns actual amount added (may be less if capacity is reached).
    public int AddResource(VoxelType type, int amount)
    {
        if (!resourceLookup.ContainsKey(type))
        {
            Debug.LogWarning($"Tried to add {type} but it's not in the inventory system!");
            return 0;
        }
        
        ResourceData resource = resourceLookup[type];
        
        // Calculate how much weight this would add
        float weightPerUnit = resource.inventoryWeight;
        float currentTotalWeight = GetCurrentWeight();
        float availableWeight = maxInventoryCapacity - currentTotalWeight;
        
        // Calculate how many units we can add based on available weight
        int maxCanAdd = Mathf.FloorToInt(availableWeight / weightPerUnit);
        int amountToAdd = Mathf.Min(amount, maxCanAdd);
        
        if (amountToAdd > 0)
        {
            resource.amount += amountToAdd;
            onResourceAdded?.Invoke(type, amountToAdd);
            
            // Check if inventory is now full
            if (GetCurrentWeight() >= maxInventoryCapacity)
            {
                onInventoryFull?.Invoke();
            }
        }
        
        return amountToAdd;
    }
    
    // Remove resources from inventory. Returns actual amount removed (may be less if not enough in inventory).
    public int RemoveResource(VoxelType type, int amount)
    {
        if (!resourceLookup.ContainsKey(type))
        {
            Debug.LogWarning($"Tried to remove {type} but it's not in the inventory system!");
            return 0;
        }
        
        ResourceData resource = resourceLookup[type];
        int amountToRemove = Mathf.Min(amount, resource.amount);
        
        if (amountToRemove > 0)
        {
            resource.amount -= amountToRemove;
            onResourceRemoved?.Invoke(type, amountToRemove);
        }
        
        return amountToRemove;
    }
    
    // Get current amount of a specific resource type.
    public int GetResourceAmount(VoxelType type)
    {
        if (resourceLookup.ContainsKey(type))
        {
            return resourceLookup[type].amount;
        }
        return 0;
    }
    
    // Get the ResourceData for a specific type (for UI display).
    public ResourceData GetResourceData(VoxelType type)
    {
        if (resourceLookup.ContainsKey(type))
        {
            return resourceLookup[type];
        }
        return null;
    }
    
    // Check if inventory has space for more of a resource type.
    public bool HasSpace(VoxelType type, int amount = 1)
    {
        if (resourceLookup.ContainsKey(type))
        {
            ResourceData resource = resourceLookup[type];
            float weightNeeded = resource.inventoryWeight * amount;
            float currentWeight = GetCurrentWeight();
            return currentWeight + weightNeeded <= maxInventoryCapacity;
        }
        return false;
    }
    
    // Check if inventory has any space at all (even a tiny bit)
    public bool HasAnySpace()
    {
        return GetCurrentWeight() < maxInventoryCapacity;
    }
    
    // Check if inventory is completely full (can't fit the smallest item)
    public bool IsFull()
    {
        float currentWeight = GetCurrentWeight();
        float minimumItemWeight = 0.1f; // Smallest possible item (rubble)
        float availableSpace = maxInventoryCapacity - currentWeight;
        // Be conservative - block when we have less than 2x the minimum item weight
        // This prevents the race condition where multiple hits queue up
        bool isFull = availableSpace < (minimumItemWeight * 2f);
        
        if (isFull)
        {
            Debug.Log($"IsFull() = TRUE. Current: {currentWeight}, Max: {maxInventoryCapacity}, Available: {availableSpace}, Threshold: {minimumItemWeight * 2f}");
        }
        
        return isFull;
    }
    
    // Get current total weight in inventory
    public float GetCurrentWeight()
    {
        float totalWeight = 0f;
        foreach (var resource in resources)
        {
            totalWeight += resource.amount * resource.inventoryWeight;
        }
        return totalWeight;
    }
    
    // Get total fill percentage across all resources (for overall storage bar).
    public float GetTotalFillPercentage()
    {
        return maxInventoryCapacity > 0 ? GetCurrentWeight() / maxInventoryCapacity : 0f;
    }
    
    // Clear all resources (for new game, death, etc).
    public void ClearInventory()
    {
        foreach (var resource in resources)
        {
            resource.amount = 0;
        }
    }
    
    // Set max capacity for a specific resource type (for upgrades).
    public void SetMaxCapacity(VoxelType type, int newCapacity)
    {
        if (resourceLookup.ContainsKey(type))
        {
            resourceLookup[type].maxCapacity = newCapacity;
        }
    }
    
    // Increase max capacity for a specific resource type (for upgrades).
    public void IncreaseMaxCapacity(VoxelType type, int additionalCapacity)
    {
        if (resourceLookup.ContainsKey(type))
        {
            resourceLookup[type].maxCapacity += additionalCapacity;
        }
    }
}
