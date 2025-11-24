using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

// Pickaxe tool for mining terrain with discrete hits.
// 
// PICKAXE SYSTEM:
// - Mines in discrete hits with cooldown between swings
// - Hold button to keep swinging automatically
// - Each hit deals digStrength damage to terrain
// 
// UPGRADE SYSTEM:
// - Strength: Damage per hit
// - Radius: Size of mining area
// - Distance: Reach distance
// - Attack Speed: Time between swings (cooldown)
// - Every X upgrades (default 5) automatically increases tool tier
// - Tool tier determines which terrain layers can be dug
public class DigTool : MonoBehaviour
{
    [Header("Pickaxe Settings")]
    public float digRadius = 2f;
    public float digStrength = 10f; // Damage per hit
    public float digDistance = 10f;
    public float attackSpeed = 1f; // Attacks per second (higher = faster)
    
    [Header("Tool Upgrade")]
    [Tooltip("Current tool tier (0 = basic, 1 = upgraded, etc.)")]
    public int toolTier = 0;
    
    // Pickaxe timing
    private float lastHitTime = 0f;
    private float hitCooldown => 1f / attackSpeed; // Time between hits
    private bool isHoldingButton = false;
    
    [Header("Upgrade System")]
    [Tooltip("Number of total upgrades needed to increase tool tier")]
    public int upgradesPerTier = 5;
    
    [Tooltip("How much each strength upgrade increases dig power")]
    public float strengthUpgradeAmount = 0.5f;
    
    [Tooltip("How much each radius upgrade increases dig radius")]
    public float radiusUpgradeAmount = 0.2f;
    
    [Tooltip("How much each distance upgrade increases dig distance")]
    public float distanceUpgradeAmount = 1f;
    
    [Tooltip("How much each attack speed upgrade increases attacks per second")]
    public float attackSpeedUpgradeAmount = 0.1f;
    
    [Header("Current Upgrade Levels (Read Only)")]
    [Tooltip("Number of times strength has been upgraded")]
    public int strengthUpgradeLevel = 0;
    
    [Tooltip("Number of times radius has been upgraded")]
    public int radiusUpgradeLevel = 0;
    
    [Tooltip("Number of times distance has been upgraded")]
    public int distanceUpgradeLevel = 0;
    
    [Tooltip("Number of times attack speed has been upgraded")]
    public int attackSpeedUpgradeLevel = 0;
    
    [Tooltip("Total number of upgrades purchased (used to calculate tier)")]
    public int totalUpgrades = 0;
    
    // Base values (set on start)
    private float baseStrength;
    private float baseRadius;
    private float baseDistance;
    private float baseAttackSpeed;
    
    [Header("Visual Feedback")]
    public GameObject digCursorPrefab; // Optional: sphere to show dig location
    private GameObject digCursor;
    
    [Header("Inventory")]
    [Tooltip("Player inventory to add mined resources to. If null, will search for it.")]
    public PlayerInventory playerInventory;
    
    [Header("UI")]
    [Tooltip("Notification system to show inventory full message. If null, will search for it.")]
    public NotificationSystem notificationSystem;
    
    private bool inventoryFullShown = false; // Track if we've shown the notification this session
    private float lastInventoryFullNotificationTime = -999f; // Track when we last showed the inventory full notification
    private float inventoryFullNotificationCooldown = 3f; // Show notification at most once every 3 seconds
    private float lastToolTierWarningTime = -999f; // Track when we last showed the tool tier warning
    private float toolTierWarningCooldown = 3f; // Show warning at most once every 3 seconds
    
    private Camera mainCamera;
    
    [Header("Input")]
    // Assign these to actions from your InputSystem_Actions.inputactions asset in the Inspector
    public InputActionReference pointAction; // Vector2 (pointer position)
    public InputActionReference digAction;   // Button
    
    public enum AimMode { Pointer, CameraCenter, CameraForward }
    [Header("Aim")]
    public AimMode aimMode = AimMode.Pointer;
    [Tooltip("Optional transform to use as the ray origin and forward for CameraForward mode (e.g., player head). If null, uses mainCamera.")]
    public Transform aimOrigin;
    
    void Start()
    {
        mainCamera = Camera.main;
        Debug.Log("mainCamera: " + mainCamera);
        
        // Find PlayerInventory if not assigned
        if (playerInventory == null)
        {
            playerInventory = FindFirstObjectByType<PlayerInventory>();
            if (playerInventory == null)
            {
                Debug.LogWarning("PlayerInventory not found! Resources won't be collected.");
            }
        }
        
        // Find notification system if not assigned
        if (notificationSystem == null)
        {
            notificationSystem = FindFirstObjectByType<NotificationSystem>();
            if (notificationSystem == null)
            {
                Debug.LogWarning("NotificationSystem not found! Inventory full messages won't display.");
            }
            else
            {
                Debug.Log("NotificationSystem found successfully!");
            }
        }
        
        // Subscribe to inventory events
        if (playerInventory != null)
        {
            playerInventory.onResourceRemoved.AddListener(OnResourceRemoved);
        }
        
        // Store base values for upgrade calculations
        baseStrength = digStrength;
        baseRadius = digRadius;
        baseDistance = digDistance;
        baseAttackSpeed = attackSpeed;
        
        if (digCursorPrefab != null)
        {
            digCursor = Instantiate(digCursorPrefab);
            digCursor.transform.localScale = Vector3.one * digRadius * 2f;
        }
    }
    
    void OnEnable()
    {
        if (pointAction != null && pointAction.action != null) pointAction.action.Enable();
        if (digAction != null && digAction.action != null) digAction.action.Enable();
    }

    void OnDisable()
    {
        if (pointAction != null && pointAction.action != null) pointAction.action.Disable();
        if (digAction != null && digAction.action != null) digAction.action.Disable();
    }
    
    // Call this method to upgrade the tool (e.g., from UI or inventory system)
    public void UpgradeTool()
    {
        toolTier++;
        Debug.Log($"Tool upgraded to tier {toolTier}");
    }
    
    // Call this method to set tool tier directly
    public void SetToolTier(int tier)
    {
        toolTier = Mathf.Max(0, tier);
        Debug.Log($"Tool tier set to {toolTier}");
    }
    
    // ===== UPGRADE SYSTEM METHODS =====
    
    // Upgrade the dig strength. Costs 1 skill point.
    // Returns true if upgrade was successful, false if not.
    public bool UpgradeStrength()
    {
        strengthUpgradeLevel++;
        digStrength = baseStrength + (strengthUpgradeLevel * strengthUpgradeAmount);
        
        IncrementTotalUpgrades();
        
        Debug.Log($"Strength upgraded to level {strengthUpgradeLevel}! New strength: {digStrength:F1}");
        return true;
    }

    // Upgrade the dig radius. Costs 1 skill point.
    // Returns true if upgrade was successful, false if not.
    public bool UpgradeRadius()
    {
        radiusUpgradeLevel++;
        digRadius = baseRadius + (radiusUpgradeLevel * radiusUpgradeAmount);
        
        // Update cursor size if it exists
        if (digCursor != null)
        {
            digCursor.transform.localScale = Vector3.one * digRadius * 2f;
        }
        
        IncrementTotalUpgrades();
        
        Debug.Log($"Radius upgraded to level {radiusUpgradeLevel}! New radius: {digRadius:F1}");
        return true;
    }
    
    // Upgrade the dig distance. Costs 1 skill point.
    // Returns true if upgrade was successful, false if not.
    public bool UpgradeDistance()
    {
        distanceUpgradeLevel++;
        digDistance = baseDistance + (distanceUpgradeLevel * distanceUpgradeAmount);
        
        IncrementTotalUpgrades();
        
        Debug.Log($"Distance upgraded to level {distanceUpgradeLevel}! New distance: {digDistance:F1}");
        return true;
    }
    
    // Upgrade the attack speed. Costs 1 skill point.
    // Returns true if upgrade was successful, false if not.
    public bool UpgradeAttackSpeed()
    {
        attackSpeedUpgradeLevel++;
        attackSpeed = baseAttackSpeed + (attackSpeedUpgradeLevel * attackSpeedUpgradeAmount);
        
        IncrementTotalUpgrades();
        
        Debug.Log($"Attack Speed upgraded to level {attackSpeedUpgradeLevel}! New speed: {attackSpeed:F2} hits/sec (cooldown: {hitCooldown:F2}s)");
        return true;
    }
    
    // Increments total upgrades and checks if tool tier should increase
    private void IncrementTotalUpgrades()
    {
        totalUpgrades++;
        
        // Calculate what tier we should be at based on total upgrades
        int newTier = totalUpgrades / upgradesPerTier;
        
        // Check if we've reached a new tier
        if (newTier > toolTier)
        {
            toolTier = newTier;
            OnToolTierUp(toolTier);
        }
    }
    
    // Called when the tool tier increases. Override or add listeners for special effects.
    private void OnToolTierUp(int newTier)
    {
        Debug.Log($"★★★ TOOL TIER INCREASED TO {newTier}! ★★★");
        // Add visual/audio feedback here (particle effect, sound, UI popup, etc.)
        // Example: Play tier up animation, unlock new dig areas, etc.
    }
    
    // Get the current upgrade progress toward the next tier
    public int GetUpgradesUntilNextTier()
    {
        return upgradesPerTier - (totalUpgrades % upgradesPerTier);
    }
    
    // Get the progress percentage toward the next tier (0-1)
    public float GetTierProgressPercentage()
    {
        return (float)(totalUpgrades % upgradesPerTier) / upgradesPerTier;
    }
    
    // Reset all upgrades (useful for testing or new game)
    public void ResetUpgrades()
    {
        strengthUpgradeLevel = 0;
        radiusUpgradeLevel = 0;
        distanceUpgradeLevel = 0;
        attackSpeedUpgradeLevel = 0;
        totalUpgrades = 0;
        toolTier = 0;
        
        digStrength = baseStrength;
        digRadius = baseRadius;
        digDistance = baseDistance;
        attackSpeed = baseAttackSpeed;
        
        if (digCursor != null)
        {
            digCursor.transform.localScale = Vector3.one * digRadius * 2f;
        }
        
        Debug.Log("All upgrades reset to base values");
    }
    
    // ===== DEBUG/TESTING METHODS =====
    
    // Add skill points and upgrade stats (for testing)
    public void TestUpgradeSequence(int pointsToSpend)
    {
        Debug.Log($"=== Testing {pointsToSpend} upgrades ===");
        
        for (int i = 0; i < pointsToSpend; i++)
        {
            // Rotate through upgrades for testing
            int upgradeType = i % 3;
            switch (upgradeType)
            {
                case 0:
                    UpgradeStrength();
                    break;
                case 1:
                    UpgradeRadius();
                    break;
                case 2:
                    UpgradeDistance();
                    break;
            }
        }
        
        Debug.Log($"=== Test complete. Tier: {toolTier}, Total Upgrades: {totalUpgrades} ===");
    }
    
    // Unity Editor Context Menu Commands (Right-click component in Inspector)
    [ContextMenu("Upgrade Strength")]
    private void DebugUpgradeStrength()
    {
        UpgradeStrength();
    }
    
    [ContextMenu("Upgrade Radius")]
    private void DebugUpgradeRadius()
    {
        UpgradeRadius();
    }
    
    [ContextMenu("Upgrade Distance")]
    private void DebugUpgradeDistance()
    {
        UpgradeDistance();
    }
    
    [ContextMenu("Test 10 Upgrades")]
    private void DebugTest10Upgrades()
    {
        TestUpgradeSequence(10);
    }
    
    [ContextMenu("Reset All Upgrades")]
    private void DebugResetUpgrades()
    {
        ResetUpgrades();
    }
    
    [ContextMenu("Show Upgrade Info")]
    private void DebugShowInfo()
    {
        Debug.Log($"=== PICKAXE UPGRADE INFO ===");
        Debug.Log($"Tool Tier: {toolTier}");
        Debug.Log($"Total Upgrades: {totalUpgrades}");
        Debug.Log($"Strength Level: {strengthUpgradeLevel} (Damage: {digStrength:F1} per hit)");
        Debug.Log($"Radius Level: {radiusUpgradeLevel} (Radius: {digRadius:F1})");
        Debug.Log($"Distance Level: {distanceUpgradeLevel} (Distance: {digDistance:F1})");
        Debug.Log($"Attack Speed Level: {attackSpeedUpgradeLevel} (Speed: {attackSpeed:F2} hits/sec)");
        Debug.Log($"Upgrades until next tier: {GetUpgradesUntilNextTier()}");
        Debug.Log($"Progress to next tier: {GetTierProgressPercentage() * 100:F0}%");
    }
    
    void Update()
    {
        if (mainCamera == null)
            return;

        // Determine input and build ray according to selected aim mode
        Vector3 mousePos = Vector3.zero;
        bool leftPressed = false;

        if (digAction != null && digAction.action != null)
        {
            leftPressed = digAction.action.ReadValue<float>() > 0.5f;
        }

        Ray ray;
        if (aimMode == AimMode.Pointer)
        {
            if (pointAction == null || pointAction.action == null)
                return; // pointer mode requires a point action

            Vector2 mp = pointAction.action.ReadValue<Vector2>();
            mousePos = new Vector3(mp.x, mp.y, 0f);
            ray = mainCamera.ScreenPointToRay(mousePos);
        }
        else if (aimMode == AimMode.CameraCenter)
        {
            // Ray from the center of the screen
            Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            ray = mainCamera.ScreenPointToRay(center);
        }
        else // CameraForward
        {
            Transform origin = aimOrigin != null ? aimOrigin : mainCamera.transform;
            ray = new Ray(origin.position, origin.forward);
        }
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, digDistance))
        {
            // Show dig cursor
            if (digCursor != null)
            {
                digCursor.SetActive(true);
                digCursor.transform.position = hit.point;
            }
            
            // Note: Hits are now triggered by animation events via Swing.cs
            // The old direct hit logic is commented out to prevent double-hits
            /*
            // Pickaxe swing system - discrete hits with cooldown
            if (leftPressed)
            {
                isHoldingButton = true;
                
                // Check if enough time has passed since last hit
                if (Time.time - lastHitTime >= hitCooldown)
                {
                    lastHitTime = Time.time;
                    PerformPickaxeHit(hit);
                }
            }
            else
            {
                isHoldingButton = false;
            }
            */
        }
        else
        {
            if (digCursor != null)
                digCursor.SetActive(false);
        }
    }
    
    // Performs a single pickaxe hit at the target location
    // Called by animation event to trigger hit at the right moment
    public void TriggerHitFromAnimation()
    {
        // Get raycast hit for current aim position
        Ray ray;
        if (aimMode == AimMode.Pointer)
        {
            Vector2 pointerPos = pointAction.action.ReadValue<Vector2>();
            ray = mainCamera.ScreenPointToRay(pointerPos);
        }
        else if (aimMode == AimMode.CameraCenter)
        {
            ray = mainCamera.ScreenPointToRay(new Vector2(Screen.width / 2f, Screen.height / 2f));
        }
        else
        {
            Transform origin = aimOrigin != null ? aimOrigin : mainCamera.transform;
            ray = new Ray(origin.position, origin.forward);
        }
        
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, digDistance))
        {
            PerformPickaxeHit(hit);
        }
    }
    
    private void PerformPickaxeHit(RaycastHit hit)
    {
        // Don't dig if inventory is completely full
        if (playerInventory != null && playerInventory.IsFull())
        {
            Debug.Log($"PerformPickaxeHit: Inventory FULL! Weight: {playerInventory.GetCurrentWeight()}/{playerInventory.maxInventoryCapacity}");
            
            // Show notification with cooldown
            if (Time.time - lastInventoryFullNotificationTime >= inventoryFullNotificationCooldown)
            {
                lastInventoryFullNotificationTime = Time.time;
                ShowInventoryFullPopup();
            }
            return;
        }
        
        Debug.Log($"PerformPickaxeHit: Inventory has space. Weight: {playerInventory.GetCurrentWeight()}/{playerInventory.maxInventoryCapacity}");
        TerrainChunk chunk = hit.collider.GetComponent<TerrainChunk>();
        if (chunk != null)
        {
            // Check if we can dig at this position based on tool tier
            SubsurfaceLayer layer = chunk.GetLayerAtPosition(hit.point);
            if (layer != null)
            {
                if (toolTier < layer.requiredToolTier)
                {
                    // Tool too weak - show feedback
                    Debug.Log($"Tool tier {toolTier} too weak for {layer.name} (requires tier {layer.requiredToolTier})");
                    
                    // Show notification if cooldown has passed
                    if (Time.time - lastToolTierWarningTime >= toolTierWarningCooldown)
                    {
                        lastToolTierWarningTime = Time.time;
                        ShowToolTierWarning(layer.name, layer.requiredToolTier);
                    }
                    
                    return;
                }
                
                // Double-check inventory space right before digging (in case it filled up between checks)
                if (playerInventory != null && playerInventory.IsFull())
                {
                    Debug.Log($"PerformPickaxeHit: Inventory became full, aborting dig");
                    if (Time.time - lastInventoryFullNotificationTime >= inventoryFullNotificationCooldown)
                    {
                        lastInventoryFullNotificationTime = Time.time;
                        ShowInventoryFullPopup();
                    }
                    return;
                }
                
                // Apply hardness multiplier to dig strength (single hit damage)
                float effectiveStrength = digStrength / layer.hardness;
                Dictionary<VoxelType, int> minedVoxels = chunk.DigAtPosition(hit.point, digRadius, effectiveStrength);
                
                // Spawn layer-specific particle effect
                if (layer.digEffectPrefab != null)
                {
                    SpawnDigEffect(layer.digEffectPrefab, hit.point, hit.normal);
                }
                
                // Add only terrain/rubble to inventory (ores are handled by OreNode prefabs)
                // Process items one at a time until inventory is full
                if (playerInventory != null && minedVoxels != null)
                {
                    int itemsAdded = 0;
                    int itemsDiscarded = 0;
                    
                    foreach (var kvp in minedVoxels)
                    {
                        VoxelType voxelType = kvp.Key;
                        int count = kvp.Value;
                        
                        // Only add non-ore terrain types (rubble)
                        if (voxelType != VoxelType.Air && !IsOreType(voxelType))
                        {
                            // Try to add items one at a time until inventory is full
                            int added = playerInventory.AddResource(voxelType, count);
                            itemsAdded += added;
                            itemsDiscarded += (count - added);
                            
                            // If we couldn't add all items, inventory is full - stop processing
                            if (added < count)
                            {
                                break;
                            }
                        }
                    }
                    
                    // Show notification if inventory became full and we discarded items
                    if (itemsDiscarded > 0)
                    {
                        if (Time.time - lastInventoryFullNotificationTime >= inventoryFullNotificationCooldown)
                        {
                            lastInventoryFullNotificationTime = Time.time;
                            ShowInventoryFullPopup();
                        }
                    }
                }
            }
            else
            {
                // Double-check inventory space right before digging (in case it filled up between checks)
                if (playerInventory != null && playerInventory.IsFull())
                {
                    Debug.Log($"PerformPickaxeHit: Inventory became full, aborting dig (no layer path)");
                    if (Time.time - lastInventoryFullNotificationTime >= inventoryFullNotificationCooldown)
                    {
                        lastInventoryFullNotificationTime = Time.time;
                        ShowInventoryFullPopup();
                    }
                    return;
                }
                
                // No layer info, use default strength
                Dictionary<VoxelType, int> minedVoxels = chunk.DigAtPosition(hit.point, digRadius, digStrength);
                
                // Add only terrain/rubble to inventory (ores are handled by OreNode prefabs)
                // Process items one at a time until inventory is full
                if (playerInventory != null && minedVoxels != null)
                {
                    int itemsAdded = 0;
                    int itemsDiscarded = 0;
                    
                    foreach (var kvp in minedVoxels)
                    {
                        VoxelType voxelType = kvp.Key;
                        int count = kvp.Value;
                        
                        // Only add non-ore terrain types (rubble)
                        if (voxelType != VoxelType.Air && !IsOreType(voxelType))
                        {
                            // Try to add items one at a time until inventory is full
                            int added = playerInventory.AddResource(voxelType, count);
                            itemsAdded += added;
                            itemsDiscarded += (count - added);
                            
                            // If we couldn't add all items, inventory is full - stop processing
                            if (added < count)
                            {
                                break;
                            }
                        }
                    }
                    
                    // Show notification if inventory became full and we discarded items
                    if (itemsDiscarded > 0)
                    {
                        if (Time.time - lastInventoryFullNotificationTime >= inventoryFullNotificationCooldown)
                        {
                            lastInventoryFullNotificationTime = Time.time;
                            ShowInventoryFullPopup();
                        }
                    }
                }
            }
            
        }
    }
    
    void SpawnDigEffect(GameObject effectPrefab, Vector3 position, Vector3 normal)
    {
        if (effectPrefab == null) return;
        
        // Instantiate the effect facing away from the surface
        Quaternion rotation = Quaternion.LookRotation(normal);
        GameObject effect = Instantiate(effectPrefab, position, rotation);
        
        // Auto-destroy after particle system finishes (if it has one)
        ParticleSystem ps = effect.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            Destroy(effect, ps.main.duration + ps.main.startLifetime.constantMax);
        }
        else
        {
            // No particle system, destroy after 2 seconds
            Destroy(effect, 2f);
        }
    }
    
    // Helper to check if a voxel type is an ore
    private bool IsOreType(VoxelType type)
    {
        return type == VoxelType.CopperOre ||
               type == VoxelType.IronOre ||
               type == VoxelType.GoldOre ||
               type == VoxelType.AmethystOre ||
               type == VoxelType.DiamondOre;
    }
    
    // Called when resources are removed from inventory (e.g., selling).
    // Resets the inventory full popup flag.
    private void OnResourceRemoved(VoxelType type, int amount)
    {
        inventoryFullShown = false;
    }
    
    // Shows notification when tool tier is too low for a layer.
    private void ShowToolTierWarning(string layerName, int requiredTier)
    {
        if (notificationSystem != null)
        {
            notificationSystem.ShowNotification($"Tool Tier {requiredTier} required to mine {layerName}!", Color.red, false, 3);
        }
        else
        {
            Debug.LogWarning($"Tool Tier {requiredTier} required to mine {layerName}!");
        }
    }
    
    // Public method to trigger inventory full message (called from Swing when blocking a hit)
    public void TriggerInventoryFullMessage()
    {
        Debug.Log($"TriggerInventoryFullMessage called. Time since last: {Time.time - lastInventoryFullNotificationTime}");
        if (Time.time - lastInventoryFullNotificationTime >= inventoryFullNotificationCooldown)
        {
            Debug.Log("Cooldown passed, showing popup");
            lastInventoryFullNotificationTime = Time.time;
            ShowInventoryFullPopup();
        }
        else
        {
            Debug.Log($"Cooldown not passed yet. Need {inventoryFullNotificationCooldown - (Time.time - lastInventoryFullNotificationTime)} more seconds");
        }
    }
    
    // Shows notification when inventory is full.
    private void ShowInventoryFullPopup()
    {
        Debug.Log("ShowInventoryFullPopup called!");
        if (notificationSystem != null)
        {
            Debug.Log("Showing notification via NotificationSystem");
            notificationSystem.ShowNotification("Inventory Full!\nGo to the truck and sell\n(Press 'T' to teleport)", Color.red, false, 3);
        }
        else
        {
            Debug.LogWarning("NotificationSystem is null! Inventory full! Go to the truck and sell (Press 'T' to teleport)");
        }
    }
}