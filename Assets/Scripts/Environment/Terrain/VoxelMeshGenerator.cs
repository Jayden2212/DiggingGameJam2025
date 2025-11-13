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
            for (int y = 0; y < VoxelData.ChunkSize; y++)
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

        // If subsurface layers are defined, use them to determine vertex color by Y position
        float voxelY = pos.y / VoxelData.VoxelSize;
        if (SubsurfaceLayers != null && SubsurfaceLayers.Count > 0)
        {
            // Find the layer that contains voxelY
            SubsurfaceLayer found = null;
            foreach (var layer in SubsurfaceLayers)
            {
                if (voxelY >= layer.minY && voxelY <= layer.maxY)
                {
                    found = layer;
                    break;
                }
            }

            if (found != null)
            {
                Color baseColor = found.color;

                // Blend near edges if blend > 0
                if (found.blend > 0f)
                {
                    float distToMin = voxelY - found.minY;
                    float distToMax = found.maxY - voxelY;
                    float nearest = Mathf.Min(distToMin, distToMax);

                    if (nearest < found.blend)
                    {
                        // Find adjacent neighbor color (lower or upper)
                        Color neighborColor = baseColor;
                        if (distToMin < distToMax)
                        {
                            // near lower edge: find layer whose maxY < found.minY and closest
                            int best = int.MinValue;
                            SubsurfaceLayer lower = null;
                            foreach (var l in SubsurfaceLayers)
                            {
                                if (l.maxY < found.minY && l.maxY > best)
                                {
                                    best = l.maxY;
                                    lower = l;
                                }
                            }
                            if (lower != null) neighborColor = lower.color;
                        }
                        else
                        {
                            // near upper edge: find layer whose minY > found.maxY and closest
                            int best = int.MaxValue;
                            SubsurfaceLayer upper = null;
                            foreach (var l in SubsurfaceLayers)
                            {
                                if (l.minY > found.maxY && l.minY < best)
                                {
                                    best = l.minY;
                                    upper = l;
                                }
                            }
                            if (upper != null) neighborColor = upper.color;
                        }

                        float blendT = Mathf.Clamp01(nearest / found.blend);
                        outColor = Color.Lerp(neighborColor, baseColor, blendT);
                        return pos;
                    }
                }

                outColor = baseColor;
                return pos;
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
            case VoxelType.Dirt: return new Color(0.62f, 0.45f, 0.27f, 1f); // brown
            case VoxelType.Stone: return new Color(0.5f, 0.5f, 0.5f, 1f); // gray
            case VoxelType.IronOre: return new Color(0.4f, 0.1f, 0.1f, 1f); // dark red
            case VoxelType.CopperOre: return new Color(0.8f, 0.45f, 0.2f, 1f); // orange
            case VoxelType.GoldOre: return new Color(0.9f, 0.8f, 0.2f, 1f); // yellow
            case VoxelType.Air: return Color.clear;
            default: return Color.magenta;
        }
    }
}