using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class TerrainChunk : MonoBehaviour
{
    private VoxelData voxelData;
    private VoxelMeshGenerator meshGenerator;
    
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    
    [Header("Ore Settings")]
    public GameObject ironOrePrefab;
    public GameObject copperOrePrefab;

    [Header("Subsurface Layers")]
    public List<SubsurfaceLayer> subsurfaceLayers = new List<SubsurfaceLayer>()
    {
        new SubsurfaceLayer() { name = "Topsoil", minY = 8, maxY = 16, color = new Color(0.62f, 0.45f, 0.27f), blend = 1f },
        new SubsurfaceLayer() { name = "Stone", minY = 2, maxY = 7, color = new Color(0.5f,0.5f,0.5f), blend = 1f },
        new SubsurfaceLayer() { name = "Obsidian", minY = 0, maxY = 1, color = new Color(0.05f,0.05f,0.08f), blend = 0.5f }
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
    
    public void InitializeTerrain()
    {
        voxelData = new VoxelData();
        voxelData.GenerateSimpleTerrain();
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
                    if (vt == VoxelType.IronOre && ironOrePrefab != null)
                    {
                        Vector3 worldPos = transform.TransformPoint(new Vector3(x, y, z) * VoxelData.VoxelSize);
                        GameObject ore = Instantiate(ironOrePrefab, worldPos, Quaternion.identity, transform);
                        OreNode oreNode = ore.GetComponent<OreNode>();
                        if (oreNode != null) oreNodes.Add(oreNode);
                    }
                    else if (vt == VoxelType.CopperOre && copperOrePrefab != null)
                    {
                        Vector3 worldPos = transform.TransformPoint(new Vector3(x, y, z) * VoxelData.VoxelSize);
                        GameObject ore = Instantiate(copperOrePrefab, worldPos, Quaternion.identity, transform);
                        OreNode oreNode = ore.GetComponent<OreNode>();
                        if (oreNode != null) oreNodes.Add(oreNode);
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
}