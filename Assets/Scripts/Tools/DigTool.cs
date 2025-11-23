using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Pickaxe tool for mining terrain with discrete hits.
/// 
/// PICKAXE SYSTEM:
/// - Mines in discrete hits with cooldown between swings
/// - Hold button to keep swinging automatically
/// - Each hit deals digStrength damage to terrain
/// 
/// UPGRADE SYSTEM:
/// - Strength: Damage per hit
/// - Radius: Size of mining area
/// - Distance: Reach distance
/// - Attack Speed: Time between swings (cooldown)
/// - Every X upgrades (default 5) automatically increases tool tier
/// - Tool tier determines which terrain layers can be dug
/// </summary>
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
    
    /// <summary>
    /// Upgrade the dig strength. Costs 1 skill point.
    /// Returns true if upgrade was successful, false if not.
    /// </summary>
    public bool UpgradeStrength()
    {
        strengthUpgradeLevel++;
        digStrength = baseStrength + (strengthUpgradeLevel * strengthUpgradeAmount);
        
        IncrementTotalUpgrades();
        
        Debug.Log($"Strength upgraded to level {strengthUpgradeLevel}! New strength: {digStrength:F1}");
        return true;
    }
    
    /// <summary>
    /// Upgrade the dig radius. Costs 1 skill point.
    /// Returns true if upgrade was successful, false if not.
    /// </summary>
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
    
    /// <summary>
    /// Upgrade the dig distance. Costs 1 skill point.
    /// Returns true if upgrade was successful, false if not.
    /// </summary>
    public bool UpgradeDistance()
    {
        distanceUpgradeLevel++;
        digDistance = baseDistance + (distanceUpgradeLevel * distanceUpgradeAmount);
        
        IncrementTotalUpgrades();
        
        Debug.Log($"Distance upgraded to level {distanceUpgradeLevel}! New distance: {digDistance:F1}");
        return true;
    }
    
    /// <summary>
    /// Upgrade the attack speed. Costs 1 skill point.
    /// Returns true if upgrade was successful, false if not.
    /// </summary>
    public bool UpgradeAttackSpeed()
    {
        attackSpeedUpgradeLevel++;
        attackSpeed = baseAttackSpeed + (attackSpeedUpgradeLevel * attackSpeedUpgradeAmount);
        
        IncrementTotalUpgrades();
        
        Debug.Log($"Attack Speed upgraded to level {attackSpeedUpgradeLevel}! New speed: {attackSpeed:F2} hits/sec (cooldown: {hitCooldown:F2}s)");
        return true;
    }
    
    /// <summary>
    /// Increments total upgrades and checks if tool tier should increase
    /// </summary>
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
    
    /// <summary>
    /// Called when the tool tier increases. Override or add listeners for special effects.
    /// </summary>
    private void OnToolTierUp(int newTier)
    {
        Debug.Log($"★★★ TOOL TIER INCREASED TO {newTier}! ★★★");
        // Add visual/audio feedback here (particle effect, sound, UI popup, etc.)
        // Example: Play tier up animation, unlock new dig areas, etc.
    }
    
    /// <summary>
    /// Get the current upgrade progress toward the next tier
    /// </summary>
    public int GetUpgradesUntilNextTier()
    {
        return upgradesPerTier - (totalUpgrades % upgradesPerTier);
    }
    
    /// <summary>
    /// Get the progress percentage toward the next tier (0-1)
    /// </summary>
    public float GetTierProgressPercentage()
    {
        return (float)(totalUpgrades % upgradesPerTier) / upgradesPerTier;
    }
    
    /// <summary>
    /// Reset all upgrades (useful for testing or new game)
    /// </summary>
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
    
    /// <summary>
    /// Add skill points and upgrade stats (for testing)
    /// </summary>
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
    
    /// <summary>
    /// Performs a single pickaxe hit at the target location
    /// </summary>
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
                    // TODO: Play "can't mine" sound/effect
                    return;
                }
                
                // Apply hardness multiplier to dig strength (single hit damage)
                float effectiveStrength = digStrength / layer.hardness;
                chunk.DigAtPosition(hit.point, digRadius, effectiveStrength);
            }
            else
            {
                // No layer info, use default strength
                chunk.DigAtPosition(hit.point, digRadius, digStrength);
            }
            
            // TODO: Play pickaxe swing sound/animation
            // TODO: Spawn hit particles at hit.point
        }
    }
}