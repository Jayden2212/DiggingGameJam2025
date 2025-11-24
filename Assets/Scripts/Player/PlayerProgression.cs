using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Manages player XP and skill points for tool upgrades.
/// Gain XP by mining ores, level up to earn skill points.
/// </summary>
public class PlayerProgression : MonoBehaviour
{
    [Header("XP System")]
    [Tooltip("Current XP amount")]
    public int currentXP = 0;
    
    [Tooltip("Current player level")]
    public int level = 1;
    
    [Tooltip("XP required for first level up")]
    public int baseXPRequired = 100;
    
    [Tooltip("How much XP requirement increases per level (1.5 = 50% increase)")]
    public float xpScaling = 1.5f;
    
    [Header("Skill Points")]
    [Tooltip("Current unspent skill points")]
    public int skillPoints = 0;
    
    [Tooltip("Skill points earned per level")]
    public int skillPointsPerLevel = 1;
    
    [Header("Events")]
    public UnityEvent<int> onXPGained; // Fires when XP is gained (amount)
    public UnityEvent<int> onLevelUp; // Fires when player levels up (new level)
    public UnityEvent<int> onSkillPointEarned; // Fires when skill point is earned
    public UnityEvent<int> onSkillPointSpent; // Fires when skill point is spent
    
    private PlayerInventory inventory;
    
    void Start()
    {
        // Find inventory and subscribe to resource added events
        inventory = GetComponent<PlayerInventory>();
        if (inventory == null)
        {
            inventory = FindFirstObjectByType<PlayerInventory>();
        }
        
        if (inventory != null)
        {
            inventory.onResourceAdded.AddListener(OnResourceCollected);
        }
    }
    
    void OnDestroy()
    {
        if (inventory != null)
        {
            inventory.onResourceAdded.RemoveListener(OnResourceCollected);
        }
    }
    
    /// <summary>
    /// Called when resources are added to inventory. Grants XP for ores.
    /// </summary>
    void OnResourceCollected(VoxelType type, int amount)
    {
        var resourceData = inventory.GetResourceData(type);
        if (resourceData != null && resourceData.xpValue > 0)
        {
            int xpGained = resourceData.xpValue * amount;
            AddXP(xpGained);
        }
    }
    
    /// <summary>
    /// Add XP and check for level ups.
    /// </summary>
    public void AddXP(int amount)
    {
        if (amount <= 0) return;
        
        currentXP += amount;
        onXPGained?.Invoke(amount);
        
        // Check for level up
        while (currentXP >= GetXPRequiredForNextLevel())
        {
            LevelUp();
        }
    }
    
    /// <summary>
    /// Calculate XP required for next level based on current level.
    /// </summary>
    public int GetXPRequiredForNextLevel()
    {
        return Mathf.RoundToInt(baseXPRequired * Mathf.Pow(xpScaling, level - 1));
    }
    
    /// <summary>
    /// Get XP progress as percentage (0-1) for UI bars.
    /// </summary>
    public float GetXPProgress()
    {
        int xpRequired = GetXPRequiredForNextLevel();
        return xpRequired > 0 ? (float)currentXP / xpRequired : 1f;
    }
    
    /// <summary>
    /// Level up and grant skill points.
    /// </summary>
    void LevelUp()
    {
        level++;
        currentXP = 0; // Reset XP for next level (or subtract required if you want overflow)
        
        // Grant skill points
        skillPoints += skillPointsPerLevel;
        
        onLevelUp?.Invoke(level);
        onSkillPointEarned?.Invoke(skillPointsPerLevel);
        
        Debug.Log($"Level Up! Now level {level}. Gained {skillPointsPerLevel} skill point(s). Total: {skillPoints}");
    }
    
    /// <summary>
    /// Spend a skill point (for tool upgrades). Returns true if successful.
    /// </summary>
    public bool SpendSkillPoint()
    {
        if (skillPoints > 0)
        {
            skillPoints--;
            onSkillPointSpent?.Invoke(1);
            return true;
        }
        
        Debug.LogWarning("Not enough skill points!");
        return false;
    }
    
    /// <summary>
    /// Check if player has enough skill points.
    /// </summary>
    public bool HasSkillPoints(int amount = 1)
    {
        return skillPoints >= amount;
    }
    
    /// <summary>
    /// Add skill points directly (for testing/cheats).
    /// </summary>
    public void AddSkillPoints(int amount)
    {
        if (amount > 0)
        {
            skillPoints += amount;
            onSkillPointEarned?.Invoke(amount);
        }
    }
    
    /// <summary>
    /// Reset progression (for new game).
    /// </summary>
    public void ResetProgression()
    {
        currentXP = 0;
        level = 1;
        skillPoints = 0;
    }
}
