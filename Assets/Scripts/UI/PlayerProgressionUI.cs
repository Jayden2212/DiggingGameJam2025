using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Updates UI to display player level, skill points, and XP progress as a circular fill.
public class PlayerProgressionUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The PlayerProgression script to monitor")]
    public PlayerProgression playerProgression;
    
    [Header("UI Elements")]
    [Tooltip("Image component with Fill type (circular or radial 360)")]
    public Image xpFillImage;
    
    [Tooltip("Text showing current level and skill points")]
    public TMP_Text levelText;
    
    [Tooltip("Optional: Text showing current/required XP numbers")]
    public TMP_Text xpText;
    
    [Header("Display Format")]
    [Tooltip("Show just the level number")]
    public bool showLevelOnly = true;
    
    void Start()
    {
        // Find PlayerProgression if not assigned
        if (playerProgression == null)
        {
            playerProgression = FindFirstObjectByType<PlayerProgression>();
            if (playerProgression == null)
            {
                Debug.LogError("PlayerProgressionUI: Could not find PlayerProgression!");
                return;
            }
        }
        
        // Validate fill image
        if (xpFillImage != null && xpFillImage.type != Image.Type.Filled)
        {
            Debug.LogWarning("PlayerProgressionUI: xpFillImage should be set to 'Filled' type for fill amount to work!");
        }
        
        // Subscribe to events for real-time updates
        if (playerProgression.onXPGained != null)
        {
            playerProgression.onXPGained.AddListener(OnXPChanged);
        }
        if (playerProgression.onLevelUp != null)
        {
            playerProgression.onLevelUp.AddListener(OnLevelChanged);
        }
        if (playerProgression.onSkillPointSpent != null)
        {
            playerProgression.onSkillPointSpent.AddListener(OnSkillPointsChanged);
        }
        
        // Initial update
        UpdateUI();
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (playerProgression != null)
        {
            if (playerProgression.onXPGained != null)
                playerProgression.onXPGained.RemoveListener(OnXPChanged);
            if (playerProgression.onLevelUp != null)
                playerProgression.onLevelUp.RemoveListener(OnLevelChanged);
            if (playerProgression.onSkillPointSpent != null)
                playerProgression.onSkillPointSpent.RemoveListener(OnSkillPointsChanged);
        }
    }
    
    void Update()
    {
        // Update every frame to catch any changes
        // For better performance, you could update only on events
        UpdateUI();
    }
    
    void OnXPChanged(int amount)
    {
        UpdateUI();
    }
    
    void OnLevelChanged(int newLevel)
    {
        UpdateUI();
    }
    
    void OnSkillPointsChanged(int amount)
    {
        UpdateUI();
    }
    
    void UpdateUI()
    {
        if (playerProgression == null) return;
        
        // Update fill amount (0 to 1)
        if (xpFillImage != null)
        {
            xpFillImage.fillAmount = playerProgression.GetXPProgress();
        }
        
        // Update level text - just show the number
        if (levelText != null)
        {
            if (showLevelOnly)
            {
                levelText.text = playerProgression.level.ToString();
            }
            else
            {
                levelText.text = $"Level {playerProgression.level}\n{playerProgression.skillPoints} Points";
            }
        }
        
        // Update XP numbers text (optional)
        if (xpText != null)
        {
            int currentXP = playerProgression.currentXP;
            int requiredXP = playerProgression.GetXPRequiredForNextLevel();
            xpText.text = $"{currentXP} / {requiredXP}";
        }
    }
}
