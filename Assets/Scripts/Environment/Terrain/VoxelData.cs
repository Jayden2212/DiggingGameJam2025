using UnityEngine;
using System.Collections.Generic;

public enum VoxelType
{
    Air,
    Grass,
    Dirt,
    LimeStone,
    Granite,
    Bedrock,
    Molten,
    CopperOre,
    IronOre,
    GoldOre,
    AmethystOre,
    DiamondOre
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
    
    public void GenerateSimpleTerrain(List<SubsurfaceLayer> subsurfaceLayers = null)
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
                        // Determine base terrain type from depth using subsurface layer data
                        VoxelType terrainType = VoxelType.Granite; // Default fallback
                        
                        if (subsurfaceLayers != null && subsurfaceLayers.Count > 0)
                        {
                            // Find which layer this depth belongs to and assign corresponding terrain type
                            for (int i = 0; i < subsurfaceLayers.Count; i++)
                            {
                                SubsurfaceLayer layer = subsurfaceLayers[i];
                                if (depthFromSurface >= layer.depthStart && depthFromSurface < layer.depthEnd)
                                {
                                    // Map layer names to voxel types
                                    terrainType = GetTerrainTypeFromLayerName(layer.name);
                                    break;
                                }
                            }
                            
                            // If depth exceeds all layers, use the deepest layer
                            if (depthFromSurface >= subsurfaceLayers[subsurfaceLayers.Count - 1].depthStart)
                            {
                                terrainType = GetTerrainTypeFromLayerName(subsurfaceLayers[subsurfaceLayers.Count - 1].name);
                            }
                        }
                        else
                        {
                            // Fallback if no layers provided: simple depth-based assignment
                            if (depthFromSurface < 3f)
                                terrainType = VoxelType.Dirt;
                            else if (depthFromSurface < 10f)
                                terrainType = VoxelType.LimeStone;
                            else if (depthFromSurface < 35f)
                                terrainType = VoxelType.Granite;
                            else
                                terrainType = VoxelType.Bedrock;
                        }
                        
                        voxelTypes[x, y, z] = terrainType;
                        
                        // Default layer index (will be set by TerrainChunk if layers are provided)
                        layerIndices[x, y, z] = 0;
                        
                        // Check for all ore types generically (ores override terrain type)
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
        // Use true 3D noise by combining three 2D noise samples across different axes
        // This creates more natural, volumetric vein patterns instead of flat horizontal veins
        
        float noise1 = Mathf.PerlinNoise(
            (x + seedX + offset) * scale,
            (y + offset) * scale  // Full Y weight for vertical variation
        );
        
        float noise2 = Mathf.PerlinNoise(
            (z + seedZ + offset) * scale,
            (y + offset * 1.3f) * scale  // Full Y weight, different offset for variety
        );
        
        float noise3 = Mathf.PerlinNoise(
            (x + offset * 0.7f) * scale,
            (z + offset * 1.7f) * scale  // X-Z plane for additional 3D structure
        );
        
        // Multiply all three for truly volumetric veins
        return noise1 * noise2 * noise3;
    }
    
    // Helper method to map subsurface layer names to voxel types
    private VoxelType GetTerrainTypeFromLayerName(string layerName)
    {
        // Map common layer names to voxel types
        string nameLower = layerName.ToLower();
        
        if (nameLower.Contains("grass"))
            return VoxelType.Grass;
        else if (nameLower.Contains("soil") || nameLower.Contains("dirt"))
            return VoxelType.Dirt;
        else if (nameLower.Contains("limestone") || nameLower.Contains("lime"))
            return VoxelType.LimeStone;
        else if (nameLower.Contains("granite"))
            return VoxelType.Granite;
        else if (nameLower.Contains("molten") || nameLower.Contains("lava"))
            return VoxelType.Molten;
        else if (nameLower.Contains("bedrock"))
            return VoxelType.Bedrock;
        
        // Default to granite if no match
        return VoxelType.Granite;
    }
    
    public Dictionary<VoxelType, int> ModifyDensity(Vector3 localPosition, float radius, float strength)
    {
        Dictionary<VoxelType, int> minedVoxels = new Dictionary<VoxelType, int>();
        
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
                            VoxelType minedType = voxelTypes[x, y, z];
                            
                            // Track what was mined
                            if (minedVoxels.ContainsKey(minedType))
                            {
                                minedVoxels[minedType]++;
                            }
                            else
                            {
                                minedVoxels[minedType] = 1;
                            }
                            
                            voxelTypes[x, y, z] = VoxelType.Air;
                        }
                    }
                }
            }
        }
        
        return minedVoxels;
    }
}