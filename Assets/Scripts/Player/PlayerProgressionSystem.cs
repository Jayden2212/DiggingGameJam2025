using UnityEngine;

// work in progress template
public class PlayerProgressionSystem : MonoBehaviour
{
    [Header("References")]
    public DigTool digTool;
    
    [Header("Progression")]
    [Tooltip("Available skill points to spend on upgrades")]
    public int skillPoints = 0;
    
    [Tooltip("Current player level")]
    public int playerLevel = 1;
    
    [Tooltip("Current experience points")]
    public int currentExp = 0;
    
    [Tooltip("Experience needed for next level")]
    public int expToNextLevel = 100;
    
    [Header("Settings")]
    [Tooltip("Skill points gained per level up")]
    public int skillPointsPerLevel = 1;
    
    [Tooltip("Experience multiplier per level")]
    public float expScalingFactor = 1.5f;
    
    void Start()
    {
        if (digTool == null)
        {
            digTool = FindFirstObjectByType<DigTool>();
            if (digTool == null)
            {
                Debug.LogError("PlayerProgressionSystem: No DigTool found!");
            }
        }
    }
    
    // ===== EXPERIENCE & LEVELING =====
    public void AddExperience(int amount)
    {
        currentExp += amount;
        Debug.Log($"Gained {amount} EXP! ({currentExp}/{expToNextLevel})");
        
        // Check for level up
        while (currentExp >= expToNextLevel)
        {
            LevelUp();
        }
    }
    
    private void LevelUp()
    {
        currentExp -= expToNextLevel;
        playerLevel++;
        skillPoints += skillPointsPerLevel;
        
        // Calculate next level requirement
        expToNextLevel = Mathf.RoundToInt(expToNextLevel * expScalingFactor);
        
        Debug.Log($"★★★ LEVEL UP! Now level {playerLevel}! ★★★");
        Debug.Log($"Gained {skillPointsPerLevel} skill point(s). Total: {skillPoints}");
        
        // Add visual/audio feedback here
        OnLevelUp();
    }
    
    private void OnLevelUp()
    {
        // Play level up sound, show UI popup, particle effects, etc.
    }
    
    // ===== SKILL POINT SPENDING =====
    
    public bool TryUpgradeStrength()
    {
        if (skillPoints <= 0)
        {
            Debug.LogWarning("Not enough skill points!");
            return false;
        }
        
        if (digTool == null)
        {
            Debug.LogError("No DigTool reference!");
            return false;
        }
        
        skillPoints--;
        digTool.UpgradeStrength();
        Debug.Log($"Spent 1 skill point. Remaining: {skillPoints}");
        return true;
    }
    
    public bool TryUpgradeRadius()
    {
        if (skillPoints <= 0)
        {
            Debug.LogWarning("Not enough skill points!");
            return false;
        }
        
        if (digTool == null)
        {
            Debug.LogError("No DigTool reference!");
            return false;
        }
        
        skillPoints--;
        digTool.UpgradeRadius();
        Debug.Log($"Spent 1 skill point. Remaining: {skillPoints}");
        return true;
    }
    
    public bool TryUpgradeDistance()
    {
        if (skillPoints <= 0)
        {
            Debug.LogWarning("Not enough skill points!");
            return false;
        }
        
        if (digTool == null)
        {
            Debug.LogError("No DigTool reference!");
            return false;
        }
        
        skillPoints--;
        digTool.UpgradeDistance();
        Debug.Log($"Spent 1 skill point. Remaining: {skillPoints}");
        return true;
    }
    
    // ===== PUBLIC GETTERS =====
    
    public int GetSkillPoints() => skillPoints;
    public int GetPlayerLevel() => playerLevel;
    public float GetExpPercentage() => (float)currentExp / expToNextLevel;
    
    // ===== EXAMPLE INTEGRATION METHODS =====
    public void OnTerrainDug(float amount)
    {
        // Grant experience based on amount dug
        int expGained = Mathf.RoundToInt(amount * 2f);
        AddExperience(expGained);
    }
    
    public void OnMaterialsSold(int materialValue)
    {
        // Grant experience based on sale value
        int expGained = materialValue * 5;
        AddExperience(expGained);
    }
    
    public void OnOreCollected(int oreValue)
    {
        int expGained = oreValue * 10;
        AddExperience(expGained);
    }
    
    // ===== DEBUG/TESTING METHODS =====
    
    [ContextMenu("Add 100 EXP")]
    private void DebugAdd100Exp()
    {
        AddExperience(100);
    }
    
    [ContextMenu("Add 5 Skill Points")]
    private void DebugAdd5SkillPoints()
    {
        skillPoints += 5;
        Debug.Log($"Added 5 skill points. Total: {skillPoints}");
    }
    
    [ContextMenu("Level Up Now")]
    private void DebugLevelUp()
    {
        LevelUp();
    }
    
    [ContextMenu("Show Progression Info")]
    private void DebugShowInfo()
    {
        Debug.Log($"=== PLAYER PROGRESSION INFO ===");
        Debug.Log($"Level: {playerLevel}");
        Debug.Log($"Skill Points: {skillPoints}");
        Debug.Log($"EXP: {currentExp}/{expToNextLevel} ({GetExpPercentage() * 100:F0}%)");
        if (digTool != null)
        {
            Debug.Log($"Tool Tier: {digTool.toolTier}");
            Debug.Log($"Total Tool Upgrades: {digTool.totalUpgrades}");
        }
    }
}
