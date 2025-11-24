using Unity.VisualScripting;
using UnityEngine;

public class ButtonUpgrade : MonoBehaviour
{
    public void Upgrade()
    {
        if (gameObject.name.Equals("RangeButton"))
        {
            Debug.Log("Range");
        }
        else if (gameObject.name.Equals("SpeedButton"))
        {
            Debug.Log("Speed");
        }
        else if (gameObject.name.Equals("StrengthButton"))
        {
            Debug.Log("Strength");
        }
        else if (gameObject.name.Equals("RadiusButton"))
        {
            Debug.Log("Radius");
        }
        else if (gameObject.name.Equals("StorageButton"))
        {
            Debug.Log("Storage");
        }
    }
}
