using UnityEngine;

public enum VoxelType
{
    Air,
    Dirt,
    Stone,
    IronOre,
    CopperOre,
    GoldOre,
    // Add more ore types as needed
}

public class VoxelData
{
    public const int ChunkSize = 16;
    public const float VoxelSize = 1f;
    
    public float[,,] densityMap;
    public VoxelType[,,] voxelTypes;  // NEW: Store what type each voxel is
    
    public VoxelData()
    {
        densityMap = new float[ChunkSize + 1, ChunkSize + 1, ChunkSize + 1];
        voxelTypes = new VoxelType[ChunkSize + 1, ChunkSize + 1, ChunkSize + 1];
    }
    
    public void GenerateSimpleTerrain()
    {
        for (int x = 0; x <= ChunkSize; x++)
        {
            for (int y = 0; y <= ChunkSize; y++)
            {
                for (int z = 0; z <= ChunkSize; z++)
                {
                    densityMap[x, y, z] = 8 - y;
                    
                    // Default to dirt/stone
                    if (densityMap[x, y, z] > 0)
                    {
                        voxelTypes[x, y, z] = VoxelType.Dirt;
                        
                        // Randomly place ore veins
                        float noise = Mathf.PerlinNoise(x * 0.1f, z * 0.1f);
                        if (noise > 0.7f && y < 6)
                        {
                            voxelTypes[x, y, z] = VoxelType.IronOre;
                        }
                    }
                    else
                    {
                        voxelTypes[x, y, z] = VoxelType.Air;
                    }
                }
            }
        }
    }
    
    public void ModifyDensity(Vector3 localPosition, float radius, float strength)
    {
        int minX = Mathf.Max(0, Mathf.FloorToInt(localPosition.x - radius));
        int maxX = Mathf.Min(ChunkSize, Mathf.CeilToInt(localPosition.x + radius));
        
        int minY = Mathf.Max(0, Mathf.FloorToInt(localPosition.y - radius));
        int maxY = Mathf.Min(ChunkSize, Mathf.CeilToInt(localPosition.y + radius));
        
        int minZ = Mathf.Max(0, Mathf.FloorToInt(localPosition.z - radius));
        int maxZ = Mathf.Min(ChunkSize, Mathf.CeilToInt(localPosition.z + radius));
        
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    Vector3 gridPos = new Vector3(x, y, z);
                    float distance = Vector3.Distance(localPosition, gridPos);
                    
                    if (distance <= radius)
                    {
                        float falloff = 1f - (distance / radius);
                        float oldDensity = densityMap[x, y, z];
                        densityMap[x, y, z] -= strength * falloff;
                        
                        // If we dug through solid terrain
                        if (oldDensity > 0 && densityMap[x, y, z] <= 0)
                        {
                            voxelTypes[x, y, z] = VoxelType.Air;
                        }
                    }
                }
            }
        }
    }
}