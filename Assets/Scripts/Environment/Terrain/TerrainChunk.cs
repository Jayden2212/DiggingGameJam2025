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
    [Tooltip("Map each ore type to its prefab")]
    public List<OrePrefabMapping> orePrefabs = new List<OrePrefabMapping>()
    {
        new OrePrefabMapping(VoxelType.IronOre, null),
        new OrePrefabMapping(VoxelType.CopperOre, null),
        // Add more: new OrePrefabMapping(VoxelType.GoldOre, null),
    };

    [Header("━━━━━━━━━━ TERRAIN GENERATION ━━━━━━━━━━")]
    [Header("Basic Terrain Settings")]
    [Tooltip("Surface height of terrain - increase this to make terrain deeper/taller (max ChunkHeight = 40)")]
    [Range(0f, 40f)]
    public float terrainHeight = 40f;
    
    [Tooltip("Scale of the terrain noise (smaller = smoother hills, larger = more variation)")]
    [Range(0.01f, 0.5f)]
    public float terrainScale = 0.15f;
    
    [Tooltip("How much the terrain height varies (creates hills and valleys)")]
    [Range(0f, 15f)]
    public float terrainAmplitude = 4f;
    
    [Tooltip("Random seed for terrain generation (change for different terrain)")]
    public int terrainSeed = 0;
    
    [Header("Ore Generation (Edit in List Below)")]
    [Tooltip("Configure all ore types here. Order matters - first matching ore wins!\n\nTo spawn MORE ore: LOWER the threshold (try 0.5)\nTo spawn LESS ore: RAISE the threshold (try 0.75)")]
    public List<OreGenerationSettings> oreSettings = new List<OreGenerationSettings>()
    {
        // Iron - Common (low threshold = more spawns)
        new OreGenerationSettings(VoxelType.IronOre, 0.55f, 0.2f, 5, 30, 0f),
        // Copper - Uncommon (medium threshold)
        new OreGenerationSettings(VoxelType.CopperOre, 0.65f, 0.25f, 8, 35, 50f),
        // Gold - Rare (high threshold = less spawns, smaller veins, deeper)
        // new OreGenerationSettings(VoxelType.GoldOre, 0.75f, 0.18f, 2, 15, 100f),
    };

    [Header("━━━━━━━━━━ LAYER SYSTEM ━━━━━━━━━━")]
    [Header("Subsurface Layers (Depth-Based)")]
    [Tooltip("Layers ordered by depth from surface. First layer = surface, last = deepest. Affects color and digging difficulty.")]
    public List<SubsurfaceLayer> subsurfaceLayers = new List<SubsurfaceLayer>()
    {
        new SubsurfaceLayer() 
        { 
            name = "Topsoil", 
            depthStart = 0f, 
            depthEnd = 3f, 
            color = new Color(0.62f, 0.45f, 0.27f), 
            blendRange = 0.5f,
            requiredToolTier = 0,
            hardness = 1f
        },
        new SubsurfaceLayer() 
        { 
            name = "Stone", 
            depthStart = 3f, 
            depthEnd = 8f, 
            color = new Color(0.5f, 0.5f, 0.5f), 
            blendRange = 1f,
            requiredToolTier = 1,
            hardness = 2f
        },
        new SubsurfaceLayer() 
        { 
            name = "Hard Rock", 
            depthStart = 8f, 
            depthEnd = 15f, 
            color = new Color(0.3f, 0.3f, 0.35f), 
            blendRange = 0.8f,
            requiredToolTier = 2,
            hardness = 4f
        },
        new SubsurfaceLayer() 
        { 
            name = "Obsidian", 
            depthStart = 15f, 
            depthEnd = 100f, 
            color = new Color(0.05f, 0.05f, 0.08f), 
            blendRange = 0.5f,
            requiredToolTier = 3,
            hardness = 8f
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
        voxelData = new VoxelData();
        
        // Apply terrain generation settings from inspector
        voxelData.terrainHeight = terrainHeight;
        voxelData.terrainScale = terrainScale;
        voxelData.terrainAmplitude = terrainAmplitude;
        voxelData.terrainSeed = terrainSeed;
        
        // Apply ore generation settings from inspector
        voxelData.oreSettings = new List<OreGenerationSettings>(oreSettings);
        
        voxelData.GenerateSimpleTerrain();
        
        // Apply subsurface layers after terrain generation
        voxelData.ApplySubsurfaceLayers(subsurfaceLayers);
        
        meshGenerator = new VoxelMeshGenerator(voxelData);
        // Pass subsurface layers to generator
        meshGenerator.SubsurfaceLayers = subsurfaceLayers;
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
        }

        // Use the actual array sizes to avoid off-by-one / ChunkSize mismatch
        int sizeX = voxelData.voxelTypes.GetLength(0);
        int sizeY = voxelData.voxelTypes.GetLength(1);
        int sizeZ = voxelData.voxelTypes.GetLength(2);

        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    // Skip empty voxels (density <= 0)
                    if (voxelData.densityMap[x, y, z] <= 0)
                        continue;

                    VoxelType vt = voxelData.voxelTypes[x, y, z];
                    
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
    
    public void DigAtPosition(Vector3 worldPosition, float radius, float strength)
    {
        Vector3 localPos = transform.InverseTransformPoint(worldPosition);
        localPos /= VoxelData.VoxelSize;
        if (voxelData == null)
        {
            Debug.LogWarning("DigAtPosition called but voxelData is null.");
            return;
        }

        voxelData.ModifyDensity(localPos, radius, strength);
        RegenerateMesh();
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