using UnityEngine;
using System.Collections.Generic;

public enum VoxelType
{
    Air,
    Dirt,
    Stone,
    Obsidian,
    IronOre,
    CopperOre,
    GoldOre,
    // Add more ore types as needed
}

[System.Serializable]
public class OreGenerationSettings
{
    public VoxelType oreType;
    
    [Header("Ore Abundance")]
    [Range(0f, 1f)]
    [Tooltip("LOWER = More Ore | HIGHER = Less Ore\nRare: 0.7-0.8 | Common: 0.5-0.6 | Abundant: 0.3-0.5")]
    public float threshold = 0.65f;
    
    [Tooltip("Vein Size: SMALLER value = BIGGER veins | Common range: 0.1-0.3")]
    [Range(0.05f, 0.5f)]
    public float scale = 0.2f;
    [Tooltip("Minimum Y level for this ore")]
    public int minY = 0;
    [Tooltip("Maximum Y level for this ore")]
    public int maxY = 40;
    [Tooltip("Seed offset for this ore type (for variety)")]
    public float seedOffset = 0f;
    
    public OreGenerationSettings(VoxelType type, float thresh, float scl, int minHeight, int maxHeight, float offset = 0f)
    {
        oreType = type;
        threshold = thresh;
        scale = scl;
        minY = minHeight;
        maxY = maxHeight;
        seedOffset = offset;
    }
}

public class VoxelData
{
    // TERRAIN SIZE CONFIGURATION
    // ChunkSize controls the horizontal dimensions (X and Z axes)
    // ChunkHeight controls the vertical dimension (Y axis)
    // The actual voxel array is (ChunkSize+1, ChunkHeight+1, ChunkSize+1) to include edges
    
    public const int ChunkSize = 23; // 23x23 horizontal area (creates 24x24 voxels with edges)
    public const int ChunkHeight = 40; // Max height for terrain (creates 41 voxels with edges)
    public const float VoxelSize = 1f; // Size of each voxel in world units
    
    // NOTE: terrainHeight should be set high enough to fill the vertical space!
    // World size will be: ChunkSize x ChunkHeight x ChunkSize units (23x40x23 = 23m wide, 40m tall)
    
    public float[,,] densityMap;
    public VoxelType[,,] voxelTypes;  // NEW: Store what type each voxel is
    public int[,,] layerIndices;  // NEW: Store which subsurface layer each voxel belongs to
    public float[,,] surfaceHeights; // NEW: Store the surface height for each x,z column
    
    // Terrain Generation Parameters
    public float terrainHeight = 30f; // Surface height - should be high enough to allow digging down!
    public float terrainScale = 0.15f;
    public float terrainAmplitude = 4f;
    public int terrainSeed = 0;
    
    // Ore Generation Parameters - now using a list for easy expansion
    public List<OreGenerationSettings> oreSettings = new List<OreGenerationSettings>();
    
    public VoxelData()
    {
        densityMap = new float[ChunkSize + 1, ChunkHeight + 1, ChunkSize + 1];
        voxelTypes = new VoxelType[ChunkSize + 1, ChunkHeight + 1, ChunkSize + 1];
        layerIndices = new int[ChunkSize + 1, ChunkHeight + 1, ChunkSize + 1];
        surfaceHeights = new float[ChunkSize + 1, ChunkSize + 1, 1];
    }
    
    public void GenerateSimpleTerrain()
    {
        // Use seed for consistent but varied terrain
        float seedOffsetX = terrainSeed * 100f;
        float seedOffsetZ = terrainSeed * 100.5f;
        
        // First pass: Calculate surface heights for each column
        for (int x = 0; x <= ChunkSize; x++)
        {
            for (int z = 0; z <= ChunkSize; z++)
            {
                // Generate height using Perlin noise for varied terrain
                float noiseValue = Mathf.PerlinNoise(
                    (x + seedOffsetX) * terrainScale,
                    (z + seedOffsetZ) * terrainScale
                );
                
                // Create hills and valleys
                float surfaceHeight = terrainHeight + (noiseValue - 0.5f) * terrainAmplitude;
                surfaceHeights[x, z, 0] = surfaceHeight;
            }
        }
        
        // Second pass: Generate voxels based on depth from surface
        for (int x = 0; x <= ChunkSize; x++)
        {
            for (int z = 0; z <= ChunkSize; z++)
            {
                float surfaceHeight = surfaceHeights[x, z, 0];
                
                for (int y = 0; y <= ChunkHeight; y++)
                {
                    // Density based on distance from surface
                    float distanceFromSurface = surfaceHeight - y;
                    densityMap[x, y, z] = distanceFromSurface;
                    
                    // Calculate depth from surface (for layer determination)
                    float depthFromSurface = surfaceHeight - y;
                    
                    // Determine voxel type based on density and position
                    if (densityMap[x, y, z] > 0)
                    {
                        // Default to stone
                        voxelTypes[x, y, z] = VoxelType.Stone;
                        
                        // Default layer index (will be set by TerrainChunk if layers are provided)
                        layerIndices[x, y, z] = 0;
                        
                        // Check for all ore types generically
                        foreach (var ore in oreSettings)
                        {
                            if (y >= ore.minY && y <= ore.maxY)
                            {
                                float oreNoise = Calculate3DOreNoise(
                                    x, y, z, 
                                    seedOffsetX, seedOffsetZ, 
                                    ore.scale, ore.seedOffset
                                );
                                
                                if (oreNoise > ore.threshold)
                                {
                                    voxelTypes[x, y, z] = ore.oreType;
                                    break; // First matching ore wins (allows ore priority)
                                }
                            }
                        }
                        
                        // Top layer is dirt (if still stone)
                        if (depthFromSurface >= -0.5f && depthFromSurface <= 2f)
                        {
                            if (voxelTypes[x, y, z] == VoxelType.Stone)
                            {
                                voxelTypes[x, y, z] = VoxelType.Dirt;
                            }
                        }
                    }
                    else
                    {
                        voxelTypes[x, y, z] = VoxelType.Air;
                        layerIndices[x, y, z] = -1; // No layer for air
                    }
                }
            }
        }
    }
    
    // Apply subsurface layers based on depth from surface
    public void ApplySubsurfaceLayers(List<SubsurfaceLayer> layers)
    {
        if (layers == null || layers.Count == 0) return;
        
        for (int x = 0; x <= ChunkSize; x++)
        {
            for (int z = 0; z <= ChunkSize; z++)
            {
                float surfaceHeight = surfaceHeights[x, z, 0];
                
                for (int y = 0; y <= ChunkHeight; y++)
                {
                    // Skip air voxels
                    if (densityMap[x, y, z] <= 0)
                        continue;
                    
                    // Calculate depth from surface
                    float depthFromSurface = surfaceHeight - y;
                    
                    // Find which layer this voxel belongs to
                    for (int i = 0; i < layers.Count; i++)
                    {
                        if (depthFromSurface >= layers[i].depthStart && depthFromSurface < layers[i].depthEnd)
                        {
                            layerIndices[x, y, z] = i;
                            break;
                        }
                    }
                    
                    // If no layer found, use the deepest layer
                    if (depthFromSurface >= layers[layers.Count - 1].depthStart)
                    {
                        layerIndices[x, y, z] = layers.Count - 1;
                    }
                }
            }
        }
    }
    
    // Helper method to calculate 3D ore noise
    private float Calculate3DOreNoise(int x, int y, int z, float seedX, float seedZ, float scale, float offset)
    {
        // Use 3D Perlin noise by multiplying two 2D noise samples
        // This creates more natural vein-like patterns
        float noise1 = Mathf.PerlinNoise(
            (x + seedX + offset) * scale,
            (y + seedX * 0.5f + offset) * scale
        );
        
        float noise2 = Mathf.PerlinNoise(
            (z + seedZ + offset) * scale,
            (y + seedZ * 0.7f + offset) * scale
        );
        
        return noise1 * noise2;
    }
    
    public void ModifyDensity(Vector3 localPosition, float radius, float strength)
    {
        // Arrays are sized (ChunkSize+1, ChunkHeight+1, ChunkSize+1), so max indices are inclusive
        int minX = Mathf.Max(0, Mathf.FloorToInt(localPosition.x - radius));
        int maxX = Mathf.Min(ChunkSize, Mathf.CeilToInt(localPosition.x + radius));
        
        int minY = Mathf.Max(0, Mathf.FloorToInt(localPosition.y - radius));
        int maxY = Mathf.Min(ChunkHeight, Mathf.CeilToInt(localPosition.y + radius));
        
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