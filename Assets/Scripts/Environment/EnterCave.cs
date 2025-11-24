using UnityEngine;

public class EnterCave : MonoBehaviour
{
    public GameObject topOfCave;
    
    public void OnTriggerEnter(Collider other) 
    {
        if (other.CompareTag("Player"))
        {
            topOfCave.SetActive(true);
        }
    }
}
