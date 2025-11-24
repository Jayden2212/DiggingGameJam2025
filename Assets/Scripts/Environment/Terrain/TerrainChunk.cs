using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class OrePrefabMapping
{
    public VoxelType oreType;
    public GameObject prefab;
    
    public OrePrefabMapping(VoxelType type, GameObject prefabObj)
    {
        oreType = type;
        prefab = prefabObj;
    }
}

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class TerrainChunk : MonoBehaviour
{
    private VoxelData voxelData;
    private VoxelMeshGenerator meshGenerator;
    
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    
    [Header("Ore Prefabs")]
    [Tooltip("Map each ore type to its prefab - assign prefabs in Inspector")]
    public List<OrePrefabMapping> orePrefabs = new List<OrePrefabMapping>()
    {
        new OrePrefabMapping(VoxelType.CopperOre, null),
        new OrePrefabMapping(VoxelType.IronOre, null),
        new OrePrefabMapping(VoxelType.GoldOre, null),
        new OrePrefabMapping(VoxelType.AmethystOre, null),
        new OrePrefabMapping(VoxelType.DiamondOre, null),
    };
    
    [Tooltip("Prevent ores from spawning this many voxels from the edge (prevents clipping)")]
    [Range(0, 5)]
    public int oreSpawnBorderMargin = 2;

    [Header("━━━━━━━━━━ TERRAIN GENERATION ━━━━━━━━━━")]
    [Header("Basic Terrain Settings")]
    [Tooltip("Surface height of terrain - increase this to make terrain deeper/taller (max ChunkHeight = 40)")]
    [Range(0f, 40f)]
    public float terrainHeight = 39f;
    
    [Tooltip("Scale of the terrain noise (smaller = smoother hills, larger = more variation)")]
    [Range(0.01f, 0.5f)]
    public float terrainScale = 0.12f;
    
    [Tooltip("How much the terrain height varies (creates hills and valleys)")]
    [Range(0f, 15f)]
    public float terrainAmplitude = 1f;
    
    [Tooltip("Random seed for terrain generation (change for different terrain, -1 = random seed)")]
    public int terrainSeed = -1;
    
    [Header("Ore Generation (Edit in List Below)")]
    [Tooltip("Configure all ore types here. Order matters - first matching ore wins!\n\nTo spawn MORE ore: LOWER the threshold (try 0.3)\nTo spawn LESS ore: RAISE the threshold (try 0.5)\n\nFor 3D volumetric veins: Keep threshold between 0.3-0.45 and scale between 0.15-0.25\n\nNOTE: minY/maxY are Y-coordinates (0=bottom, 40=top), NOT depth from surface!")]
    public List<OreGenerationSettings> oreSettings = new List<OreGenerationSettings>()
    {
        // Copper - Common (large 3D veins, shallow depth 3-15m = Y: 25-37)
        new OreGenerationSettings(VoxelType.CopperOre, 0.2f, 0.465f, 25, 37, 0f),
        // Iron - Common (moderate 3D veins, mid-depth 10-25m = Y: 15-30)
        new OreGenerationSettings(VoxelType.IronOre, 0.2f, 0.465f, 15, 30, 50f),
        // Gold - Uncommon (smaller veins, deeper 15-30m = Y: 10-25)
        new OreGenerationSettings(VoxelType.GoldOre, 0.2f, 0.465f, 10, 25, 100f),
        // Amethyst - Rare (small veins, deep 20-35m = Y: 5-20)
        new OreGenerationSettings(VoxelType.AmethystOre, 0.2f, 0.465f, 5, 20, 150f),
        // Diamond - Very Rare (tiny veins, very deep 28-38m = Y: 2-12)
        new OreGenerationSettings(VoxelType.DiamondOre, 0.2f, 0.465f, 2, 12, 200f),
    };

    [Header("━━━━━━━━━━ LAYER SYSTEM ━━━━━━━━━━")]
    [Header("Subsurface Layers (Depth-Based)")]
    [Tooltip("Layers ordered by depth from surface. First layer = surface, last = deepest. Affects color and digging difficulty.")]
    public List<SubsurfaceLayer> subsurfaceLayers = new List<SubsurfaceLayer>()
    {
        new SubsurfaceLayer() 
        { 
            name = "Grass", 
            depthStart = 0f, 
            depthEnd = 2f, 
            color = new Color(108 / 255f, 158 / 255f, 47 / 255f, 1f), // Brown soil
            blendRange = 0.5f,
            requiredToolTier = 0,
            hardness = 1f
        },
        new SubsurfaceLayer() 
        { 
            name = "Soil", 
            depthStart = 2f, 
            depthEnd = 4f, 
            color = new Color(0.55f, 0.40f, 0.25f, 1f), // Brown soil
            blendRange = 0.5f,
            requiredToolTier = 0,
            hardness = 1f
        },
        new SubsurfaceLayer() 
        { 
            name = "Limestone", 
            depthStart = 4f, 
            depthEnd = 10f, 
            color = new Color(0.75f, 0.72f, 0.60f, 1f), // Yellow-grey limestone
            blendRange = 1f,
            requiredToolTier = 0,
            hardness = 1.5f
        },
        new SubsurfaceLayer() 
        { 
            name = "Granite", 
            depthStart = 10f, 
            depthEnd = 25f, 
            color = new Color(0.35f, 0.35f, 0.38f, 1f), // Dark grey granite
            blendRange = 1f,
            requiredToolTier = 1,
            hardness = 3f
        },
        new SubsurfaceLayer() 
        { 
            name = "Deep Granite", 
            depthStart = 25f, 
            depthEnd = 35f, 
            color = new Color(0.25f, 0.25f, 0.28f, 1f), // Darker granite
            blendRange = 0.8f,
            requiredToolTier = 2,
            hardness = 5f
        },
        new SubsurfaceLayer() 
        { 
            name = "Bedrock", 
            depthStart = 35f, 
            depthEnd = 100f, 
            color = new Color(0.15f, 0.15f, 0.15f, 1f), // Black bedrock
            blendRange = 0.5f,
            requiredToolTier = 3,
            hardness = 10f
        }
    };
    
    private List<OreNode> oreNodes = new List<OreNode>();
    
    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
    }
    
    void Start()
    {
        InitializeTerrain();
        SpawnOreNodes();
        RegenerateMesh();
    }
    
    // ===== CONTEXT MENU COMMANDS (Right-click component in Inspector) =====
    
    [ContextMenu("Regenerate Terrain")]
    private void DebugRegenerateTerrain()
    {
        // Clear old ore nodes
        foreach (var ore in oreNodes)
        {
            if (ore != null) Destroy(ore.gameObject);
        }
        oreNodes.Clear();
        
        // Regenerate everything
        InitializeTerrain();
        SpawnOreNodes();
        RegenerateMesh();
        
        Debug.Log("Terrain regenerated with current settings!");
    }
    
    [ContextMenu("Show Terrain Info")]
    private void DebugShowTerrainInfo()
    {
        Debug.Log("=== TERRAIN INFO ===");
        Debug.Log($"Terrain Size: {VoxelData.ChunkSize}x{VoxelData.ChunkHeight}x{VoxelData.ChunkSize} (WxHxD)");
        Debug.Log($"Height: {terrainHeight} ± {terrainAmplitude} (Range: {terrainHeight - terrainAmplitude} to {terrainHeight + terrainAmplitude})");
        Debug.Log($"Scale: {terrainScale}, Seed: {terrainSeed}");
        Debug.Log($"Ore Types: {oreSettings.Count}");
        Debug.Log($"Subsurface Layers: {subsurfaceLayers.Count}");
        Debug.Log($"Spawned Ore Nodes: {oreNodes.Count}");
    }
    
    public void InitializeTerrain()
    {
        // Clear old ore nodes before regenerating
        foreach (var node in oreNodes)
        {
            if (node != null)
            {
                Destroy(node);
            }
        }
        oreNodes.Clear();
        
        voxelData = new VoxelData();
        
        // Apply terrain generation settings from inspector
        voxelData.terrainHeight = terrainHeight;
        voxelData.terrainScale = terrainScale;
        voxelData.terrainAmplitude = terrainAmplitude;
        if (terrainSeed == -1)
        {
            voxelData.terrainSeed = System.DateTime.Now.Millisecond;
        }
        else
        {
            voxelData.terrainSeed = terrainSeed;        
        }
        
        // Apply ore generation settings from inspector
        voxelData.oreSettings = new List<OreGenerationSettings>(oreSettings);
        
        // Generate terrain with subsurface layer data
        voxelData.GenerateSimpleTerrain(subsurfaceLayers);
        
        // Apply subsurface layers after terrain generation (for layer indices)
        voxelData.ApplySubsurfaceLayers(subsurfaceLayers);
        
        meshGenerator = new VoxelMeshGenerator(voxelData);
        // Pass subsurface layers to generator
        meshGenerator.SubsurfaceLayers = subsurfaceLayers;
        
        // Regenerate the mesh to visually update the terrain
        RegenerateMesh();
        
        // Spawn new ore nodes
        SpawnOreNodes();
    }
    
    void SpawnOreNodes()
    {
        // Spawn ore node GameObjects from voxelData.
        if (voxelData == null)
        {
            Debug.LogWarning("SpawnOreNodes called but voxelData is null.");
            return;
        }

        // Build a quick lookup dictionary for ore prefabs
        Dictionary<VoxelType, GameObject> orePrefabDict = new Dictionary<VoxelType, GameObject>();
        foreach (var mapping in orePrefabs)
        {
            if (mapping.prefab != null)
            {
                orePrefabDict[mapping.oreType] = mapping.prefab;
            }
            else
            {
                Debug.LogWarning($"Ore prefab for {mapping.oreType} is null!");
            }
        }
        
        // Count ores found in voxel data (before spawning)
        Dictionary<VoxelType, int> oreCountsInData = new Dictionary<VoxelType, int>();

        // Use the actual array sizes to avoid off-by-one / ChunkSize mismatch
        int sizeX = voxelData.voxelTypes.GetLength(0);
        int sizeY = voxelData.voxelTypes.GetLength(1);
        int sizeZ = voxelData.voxelTypes.GetLength(2);

        // Calculate ore spawn boundaries (avoid edges)
        int minX = oreSpawnBorderMargin;
        int maxX = sizeX - oreSpawnBorderMargin - 1;
        int minZ = oreSpawnBorderMargin;
        int maxZ = sizeZ - oreSpawnBorderMargin - 1;

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    // Skip empty voxels (density <= 0)
                    if (voxelData.densityMap[x, y, z] <= 0)
                        continue;

                    VoxelType vt = voxelData.voxelTypes[x, y, z];
                    
                    // Count all ores in voxel data
                    if (vt == VoxelType.CopperOre || vt == VoxelType.IronOre || vt == VoxelType.GoldOre || 
                        vt == VoxelType.AmethystOre || vt == VoxelType.DiamondOre)
                    {
                        if (!oreCountsInData.ContainsKey(vt))
                            oreCountsInData[vt] = 0;
                        oreCountsInData[vt]++;
                    }
                    
                    // Check if this voxel type is an ore with a prefab
                    if (orePrefabDict.ContainsKey(vt))
                    {
                        Vector3 worldPos = transform.TransformPoint(new Vector3(x, y, z) * VoxelData.VoxelSize);
                        
                        // Rotate -90 on X axis to fix orientation, random Y rotation for variety
                        Quaternion rotation = Quaternion.Euler(-90f, Random.Range(0f, 360f), 0f);
                        GameObject ore = Instantiate(orePrefabDict[vt], worldPos, rotation, transform);
                        
                        // Set the ore type on the OreNode component
                        OreNode oreNode = ore.GetComponent<OreNode>();
                        if (oreNode != null)
                        {
                            oreNode.oreType = vt; // Assign the voxel type to the ore node
                            oreNodes.Add(oreNode);
                        }
                    }
                }
            }
        }
        
        // Log ore counts
        Debug.Log("=== ORE GENERATION REPORT ===");
        foreach (var kvp in oreCountsInData)
        {
            Debug.Log($"{kvp.Key}: {kvp.Value} voxels found in data");
        }
        Debug.Log($"Total ore nodes spawned: {oreNodes.Count}");
    }
    
    public void RegenerateMesh()
    {
        if (meshGenerator == null)
        {
            Debug.LogWarning("RegenerateMesh called but meshGenerator is null.");
            return;
        }

        Mesh mesh = meshGenerator.GenerateMesh();
        if (mesh == null)
        {
            Debug.LogWarning("meshGenerator.GenerateMesh() returned null.");
            return;
        }

        meshFilter.mesh = mesh;
        mesh.RecalculateBounds();
        meshCollider.sharedMesh = mesh;
        
        // Notify ore nodes that terrain has changed
        foreach (OreNode ore in oreNodes)
        {
            if (ore != null)
            {
                ore.SendMessage("CheckTerrainConnection", SendMessageOptions.DontRequireReceiver);
            }
        }
    }
    
    public Dictionary<VoxelType, int> DigAtPosition(Vector3 worldPosition, float radius, float strength)
    {
        Vector3 localPos = transform.InverseTransformPoint(worldPosition);
        localPos /= VoxelData.VoxelSize;
        if (voxelData == null)
        {
            Debug.LogWarning("DigAtPosition called but voxelData is null.");
            return null;
        }

        Dictionary<VoxelType, int> minedVoxels = voxelData.ModifyDensity(localPos, radius, strength);
        RegenerateMesh();
        return minedVoxels;
    }
    
    // Check if a world position is inside solid terrain
    public bool IsPositionInTerrain(Vector3 worldPosition)
    {
        Vector3 localPos = transform.InverseTransformPoint(worldPosition);
        localPos /= VoxelData.VoxelSize;
        if (voxelData == null)
        {
            Debug.LogWarning("IsPositionInTerrain called but voxelData is null.");
            return false;
        }

        // Prefer floor to map world-to-voxel consistently
        int x = Mathf.FloorToInt(localPos.x);
        int y = Mathf.FloorToInt(localPos.y);
        int z = Mathf.FloorToInt(localPos.z);

        int sizeX = voxelData.densityMap.GetLength(0);
        int sizeY = voxelData.densityMap.GetLength(1);
        int sizeZ = voxelData.densityMap.GetLength(2);

        // Check bounds (exclusive upper bound)
        if (x < 0 || x >= sizeX || y < 0 || y >= sizeY || z < 0 || z >= sizeZ)
        {
            return false;
        }

        // Return true if density is positive (solid)
        return voxelData.densityMap[x, y, z] > 0;
    }
    
    // Get the subsurface layer at a world position
    public SubsurfaceLayer GetLayerAtPosition(Vector3 worldPosition)
    {
        if (voxelData == null || voxelData.layerIndices == null)
            return null;
        
        Vector3 localPos = transform.InverseTransformPoint(worldPosition);
        localPos /= VoxelData.VoxelSize;
        
        int x = Mathf.FloorToInt(localPos.x);
        int y = Mathf.FloorToInt(localPos.y);
        int z = Mathf.FloorToInt(localPos.z);
        
        int sizeX = voxelData.layerIndices.GetLength(0);
        int sizeY = voxelData.layerIndices.GetLength(1);
        int sizeZ = voxelData.layerIndices.GetLength(2);
        
        // Check bounds
        if (x < 0 || x >= sizeX || y < 0 || y >= sizeY || z < 0 || z >= sizeZ)
            return null;
        
        int layerIdx = voxelData.layerIndices[x, y, z];
        
        if (layerIdx >= 0 && layerIdx < subsurfaceLayers.Count)
            return subsurfaceLayers[layerIdx];
        
        return null;
    }
}