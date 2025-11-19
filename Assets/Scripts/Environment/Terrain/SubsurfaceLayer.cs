using UnityEngine;

[System.Serializable]
public class SubsurfaceLayer
{
    public string name = "Layer";
    
    [Tooltip("Depth from surface where this layer starts (0 = surface)")]
    public float depthStart = 0f;
    
    [Tooltip("Depth from surface where this layer ends")]
    public float depthEnd = 5f;
    
    [Tooltip("Display color for this layer")]
    public Color color = Color.white;
    
    [Tooltip("Blend range near layer edges for smooth color transitions")]
    public float blendRange = 1f;
    
    [Header("Digging Properties")]
    [Tooltip("Minimum tool tier required to dig this layer (0 = basic, 1 = upgraded, etc.)")]
    public int requiredToolTier = 0;
    
    [Tooltip("How resistant this layer is to digging (multiplier on dig time)")]
    [Range(0.1f, 10f)]
    public float hardness = 1f;
    
    [Tooltip("Visual effect to play when digging this layer (optional)")]
    public GameObject digEffectPrefab;
}
