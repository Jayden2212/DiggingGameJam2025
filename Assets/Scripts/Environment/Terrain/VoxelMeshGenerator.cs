using UnityEngine;
using System.Collections.Generic;

public class VoxelMeshGenerator
{
    private VoxelData voxelData;
    // Optional: layers defined per chunk (minY/maxY in voxel coordinates)
    public System.Collections.Generic.List<SubsurfaceLayer> SubsurfaceLayers;
    
    public VoxelMeshGenerator(VoxelData data)
    {
        voxelData = data;
    }
    
    public Mesh GenerateMesh()
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Color> colors = new List<Color>();
        
        // Iterate through each cube in the grid
        for (int x = 0; x < VoxelData.ChunkSize; x++)
        {
            for (int y = 0; y < VoxelData.ChunkHeight; y++)
            {
                for (int z = 0; z < VoxelData.ChunkSize; z++)
                {
                    MarchCube(x, y, z, vertices, triangles, colors);
                }
            }
        }
        
        Mesh mesh = new Mesh();
        mesh.indexFormat = vertices.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        if (colors.Count == vertices.Count)
            mesh.colors = colors.ToArray();
        mesh.RecalculateNormals();
        
        // For low-poly look, don't smooth normals
        // Optionally use flat shading
        
        return mesh;
    }
    
    private void MarchCube(int x, int y, int z, List<Vector3> vertices, List<int> triangles, List<Color> colors)
    {
        // Get density values at 8 corners of cube
        float[] cube = new float[8];
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = MarchingCubesTable.CornerOffsets[i];
            cube[i] = voxelData.densityMap[
                x + (int)corner.x,
                y + (int)corner.y,
                z + (int)corner.z
            ];
        }
        
        // Calculate configuration index (0-255)
        int configIndex = 0;
        for (int i = 0; i < 8; i++)
        {
            if (cube[i] > 0) // Inside terrain
            {
                configIndex |= (1 << i);
            }
        }
        
        // No triangles needed for this cube
        if (configIndex == 0 || configIndex == 255)
            return;
        
    // Get edge vertices for this configuration
    int[] edges = MarchingCubesTable.TriangleTable[configIndex];
        
        Vector3 cubePosition = new Vector3(x, y, z) * VoxelData.VoxelSize;
        
        // Create triangles from edge list
        for (int i = 0; edges[i] != -1; i += 3)
        {
            // Get vertices for this triangle
            Color c1, c2, c3;
            Vector3 v1 = GetEdgeVertex(cubePosition, edges[i], cube, out c1);
            Vector3 v2 = GetEdgeVertex(cubePosition, edges[i + 1], cube, out c2);
            Vector3 v3 = GetEdgeVertex(cubePosition, edges[i + 2], cube, out c3);
            
            // Add vertices to mesh
            int vertIndex = vertices.Count;
            vertices.Add(v1);
            colors.Add(c1);
            vertices.Add(v2);
            colors.Add(c2);
            vertices.Add(v3);
            colors.Add(c3);
            
            triangles.Add(vertIndex);
            triangles.Add(vertIndex + 1);
            triangles.Add(vertIndex + 2);
        }
    }
    
    private Vector3 GetEdgeVertex(Vector3 cubePos, int edgeIndex, float[] cube, out Color outColor)
    {
        // Get the two corners this edge connects
        int corner1 = MarchingCubesTable.EdgeConnections[edgeIndex, 0];
        int corner2 = MarchingCubesTable.EdgeConnections[edgeIndex, 1];
        
        Vector3 pos1 = cubePos + MarchingCubesTable.CornerOffsets[corner1] * VoxelData.VoxelSize;
        Vector3 pos2 = cubePos + MarchingCubesTable.CornerOffsets[corner2] * VoxelData.VoxelSize;
        
        // Linear interpolation based on density values
        float density1 = cube[corner1];
        float density2 = cube[corner2];
        
        // ASTRONEER style: snap to grid more for chunkier look
        float t = Mathf.InverseLerp(density1, density2, 0);
        
        // Quantize for low-poly effect (optional)
        // t = Mathf.Round(t * 4f) / 4f; // Uncomment for even chunkier vertices
        Vector3 pos = Vector3.Lerp(pos1, pos2, t);

        // Check if this vertex is on an ore voxel - if so, use ore color instead of layer color
        int vx1 = (int)MarchingCubesTable.CornerOffsets[corner1].x + Mathf.FloorToInt(cubePos.x / VoxelData.VoxelSize);
        int vy1 = (int)MarchingCubesTable.CornerOffsets[corner1].y + Mathf.FloorToInt(cubePos.y / VoxelData.VoxelSize);
        int vz1 = (int)MarchingCubesTable.CornerOffsets[corner1].z + Mathf.FloorToInt(cubePos.z / VoxelData.VoxelSize);
        
        // Bounds check for ore detection
        if (vx1 >= 0 && vx1 < voxelData.voxelTypes.GetLength(0) &&
            vy1 >= 0 && vy1 < voxelData.voxelTypes.GetLength(1) &&
            vz1 >= 0 && vz1 < voxelData.voxelTypes.GetLength(2))
        {
            VoxelType vt = voxelData.voxelTypes[vx1, vy1, vz1];
            
            // If this is ore, use the hardcoded ore color instead of layer color
            if (vt == VoxelType.CopperOre || vt == VoxelType.IronOre || vt == VoxelType.GoldOre || 
                vt == VoxelType.AmethystOre || vt == VoxelType.DiamondOre)
            {
                outColor = VoxelTypeToColor(vt);
                return pos;
            }
        }
        
        // Determine color based on subsurface layers using layer indices
        if (SubsurfaceLayers != null && SubsurfaceLayers.Count > 0 && voxelData.layerIndices != null)
        {
            // Get voxel coordinates
            int vx = (int)MarchingCubesTable.CornerOffsets[corner1].x + Mathf.FloorToInt(cubePos.x / VoxelData.VoxelSize);
            int vy = (int)MarchingCubesTable.CornerOffsets[corner1].y + Mathf.FloorToInt(cubePos.y / VoxelData.VoxelSize);
            int vz = (int)MarchingCubesTable.CornerOffsets[corner1].z + Mathf.FloorToInt(cubePos.z / VoxelData.VoxelSize);
            
            // Bounds check
            if (vx >= 0 && vx < voxelData.layerIndices.GetLength(0) &&
                vy >= 0 && vy < voxelData.layerIndices.GetLength(1) &&
                vz >= 0 && vz < voxelData.layerIndices.GetLength(2))
            {
                int layerIdx = voxelData.layerIndices[vx, vy, vz];
                
                if (layerIdx >= 0 && layerIdx < SubsurfaceLayers.Count)
                {
                    SubsurfaceLayer layer = SubsurfaceLayers[layerIdx];
                    Color baseColor = layer.color;
                    
                    // Calculate depth from surface for blending
                    float surfaceHeight = voxelData.surfaceHeights[vx, vz, 0];
                    float depthFromSurface = surfaceHeight - pos.y / VoxelData.VoxelSize;
                    
                    // Blend with adjacent layers if within blend range
                    if (layer.blendRange > 0f)
                    {
                        float distToStart = Mathf.Abs(depthFromSurface - layer.depthStart);
                        float distToEnd = Mathf.Abs(depthFromSurface - layer.depthEnd);
                        
                        // Blend at layer start
                        if (distToStart < layer.blendRange && layerIdx > 0)
                        {
                            float blendT = distToStart / layer.blendRange;
                            Color prevColor = SubsurfaceLayers[layerIdx - 1].color;
                            outColor = Color.Lerp(prevColor, baseColor, blendT);
                            return pos;
                        }
                        // Blend at layer end
                        else if (distToEnd < layer.blendRange && layerIdx < SubsurfaceLayers.Count - 1)
                        {
                            float blendT = distToEnd / layer.blendRange;
                            Color nextColor = SubsurfaceLayers[layerIdx + 1].color;
                            outColor = Color.Lerp(nextColor, baseColor, blendT);
                            return pos;
                        }
                    }
                    
                    outColor = baseColor;
                    return pos;
                }
            }
        }

        // Fallback: Determine color by blending the voxel types at the two corners
        VoxelType vt1 = voxelData.voxelTypes[
            (int)MarchingCubesTable.CornerOffsets[corner1].x + Mathf.RoundToInt(cubePos.x / VoxelData.VoxelSize),
            (int)MarchingCubesTable.CornerOffsets[corner1].y + Mathf.RoundToInt(cubePos.y / VoxelData.VoxelSize),
            (int)MarchingCubesTable.CornerOffsets[corner1].z + Mathf.RoundToInt(cubePos.z / VoxelData.VoxelSize)
        ];

        VoxelType vt2 = voxelData.voxelTypes[
            (int)MarchingCubesTable.CornerOffsets[corner2].x + Mathf.RoundToInt(cubePos.x / VoxelData.VoxelSize),
            (int)MarchingCubesTable.CornerOffsets[corner2].y + Mathf.RoundToInt(cubePos.y / VoxelData.VoxelSize),
            (int)MarchingCubesTable.CornerOffsets[corner2].z + Mathf.RoundToInt(cubePos.z / VoxelData.VoxelSize)
        ];

        Color col1 = VoxelTypeToColor(vt1);
        Color col2 = VoxelTypeToColor(vt2);

        outColor = Color.Lerp(col1, col2, t);

        return pos;
    }

    private Color VoxelTypeToColor(VoxelType vt)
    {
        switch (vt)
        {
            // Terrain Types
            case VoxelType.Grass: return new Color(0.4f, 0.6f, 0.3f, 1f); // green grass
            case VoxelType.Dirt: return new Color(0.55f, 0.40f, 0.25f, 1f); // brown soil
            case VoxelType.LimeStone: return new Color(0.75f, 0.72f, 0.60f, 1f); // yellow-grey limestone
            case VoxelType.Granite: return new Color(0.35f, 0.35f, 0.38f, 1f); // dark grey granite
            case VoxelType.Bedrock: return new Color(0.15f, 0.15f, 0.15f, 1f); // black bedrock
            case VoxelType.Molten: return new Color(0.9f, 0.3f, 0.1f, 1f); // orange-red molten
            
            // Ore Types
            case VoxelType.CopperOre: return new Color(0.72f, 0.45f, 0.20f, 1f); // shiny copper
            case VoxelType.IronOre: return new Color(0.65f, 0.65f, 0.68f, 1f); // shiny grey iron
            case VoxelType.GoldOre: return new Color(1.0f, 0.84f, 0.0f, 1f); // gold/yellow
            case VoxelType.AmethystOre: return new Color(0.68f, 0.40f, 0.72f, 1f); // purple-pink amethyst
            case VoxelType.DiamondOre: return new Color(0.68f, 0.85f, 0.90f, 1f); // light blue-ish diamond
            
            case VoxelType.Air: return Color.clear;
            default: return Color.magenta;
        }
    }
}