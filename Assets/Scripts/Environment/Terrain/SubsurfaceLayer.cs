using UnityEngine;

[System.Serializable]
public class SubsurfaceLayer
{
    public string name = "Layer";
    [Tooltip("Inclusive minimum voxel Y (0 = bottom of chunk)")]
    public int minY = 0;
    [Tooltip("Inclusive maximum voxel Y")]
    public int maxY = 0;
    [Tooltip("Display color for this layer")]
    public Color color = Color.white;
    [Tooltip("Blend range in voxels near the layer edges for smooth transitions")]
    public float blend = 0f;
}
