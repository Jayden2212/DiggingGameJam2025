using UnityEngine;
using UnityEngine.InputSystem;

public class Diggin : MonoBehaviour
{
    public ParticleSystem digParticlesPrefab; 
    //[SerializeField] private InputActionReference clickAction;

    public void OnClick()
    {
        TriggerDiggingParticles();
    }

    public void TriggerDiggingParticles()
    {
        // Instantiate the particle system at the digging location (e.g., character's position)
        ParticleSystem newParticles = Instantiate(digParticlesPrefab, transform.position, Quaternion.identity);
        // The system is configured to Play on Awake and Destroy on Stop Action
    }
}

