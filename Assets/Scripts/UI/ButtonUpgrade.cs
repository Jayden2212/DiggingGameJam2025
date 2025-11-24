using UnityEngine;

public class ButtonUpgrade : MonoBehaviour
{
    public void Upgrade()
    {
        DigTool digTool = FindFirstObjectByType<DigTool>();
        PlayerProgression progression = FindFirstObjectByType<PlayerProgression>();
        PlayerInventory inventory = FindFirstObjectByType<PlayerInventory>();
        PopUpSystem popUpSystem = FindFirstObjectByType<PopUpSystem>();
        
        if (digTool == null)
        {
            Debug.LogWarning("No DigTool found in scene!");
            return;
        }
        
        if (progression == null || !progression.HasSkillPoints())
        {
            if (popUpSystem != null)
            {
                popUpSystem.popUpText.text = "NOT ENOUGH SKILL POINTS";
            }
            return;
        }
        
        bool upgraded = false;
        
        if (gameObject.name.Equals("RangeButton"))
        {
            upgraded = digTool.UpgradeDistance();
        }
        else if (gameObject.name.Equals("SpeedButton"))
        {
            upgraded = digTool.UpgradeAttackSpeed();
        }
        else if (gameObject.name.Equals("StrengthButton"))
        {
            upgraded = digTool.UpgradeStrength();
        }
        else if (gameObject.name.Equals("RadiusButton"))
        {
            upgraded = digTool.UpgradeRadius();
        }
        else if (gameObject.name.Equals("StorageButton"))
        {
            if (inventory != null)
            {
                inventory.maxInventoryCapacity += inventory.inventoryCapacityIncrement;
                upgraded = true;
                Debug.Log($"Storage upgraded! New capacity: {inventory.maxInventoryCapacity}");
            }
        }
        
        // Spend the skill point if upgrade was successful
        if (upgraded)
        {
            progression.SpendSkillPoint();
        }
    }

    public void Sell()
    {
        SellingStation sellingStation = FindFirstObjectByType<SellingStation>();
        if (sellingStation != null)
        {
            sellingStation.SellAllResources();
        }
        else
        {
            Debug.LogWarning("No SellingStation found in scene!");
        }
    }
}
