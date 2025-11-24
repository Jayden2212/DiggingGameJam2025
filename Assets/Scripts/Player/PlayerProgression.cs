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
        // Find inventory reference for potential future use
        inventory = GetComponent<PlayerInventory>();
        if (inventory == null)
        {
            inventory = FindFirstObjectByType<PlayerInventory>();
        }
    }

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
    
    public int GetXPRequiredForNextLevel()
    {
        return Mathf.RoundToInt(baseXPRequired * Mathf.Pow(xpScaling, level - 1));
    }
    
    public float GetXPProgress()
    {
        int xpRequired = GetXPRequiredForNextLevel();
        return xpRequired > 0 ? (float)currentXP / xpRequired : 1f;
    }
    
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
    
    public bool HasSkillPoints(int amount = 1)
    {
        return skillPoints >= amount;
    }
    
    public void AddSkillPoints(int amount)
    {
        if (amount > 0)
        {
            skillPoints += amount;
            onSkillPointEarned?.Invoke(amount);
        }
    }
    
    public void ResetProgression()
    {
        currentXP = 0;
        level = 1;
        skillPoints = 0;
    }
}
