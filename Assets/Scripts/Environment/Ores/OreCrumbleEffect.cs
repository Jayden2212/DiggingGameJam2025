using UnityEngine;

public class OreCrumbleEffect : MonoBehaviour
{
    public ParticleSystem crumbleParticles;
    
    public void Crumble()
    {
        // Play particle effect
        if (crumbleParticles != null)
        {
            Instantiate(crumbleParticles, transform.position, Quaternion.identity);
        }
        
        // Optional: Play sound
        // AudioSource.PlayClipAtPoint(crumbleSound, transform.position);
        
        Destroy(gameObject);
    }
}