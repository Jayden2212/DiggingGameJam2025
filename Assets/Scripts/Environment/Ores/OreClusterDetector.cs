using System.Collections.Generic;
using UnityEngine;

public class OreClusterDetector
{
    private VoxelData voxelData;
    private bool[,,] visited;
    
    public OreClusterDetector(VoxelData data)
    {
        voxelData = data;
    }
    
    public List<List<Vector3Int>> FindDisconnectedOreClusters()
    {
        visited = new bool[VoxelData.ChunkSize + 1, VoxelData.ChunkSize + 1, VoxelData.ChunkSize + 1];
        List<List<Vector3Int>> clusters = new List<List<Vector3Int>>();
        
        // Find all ore voxels
        for (int x = 0; x <= VoxelData.ChunkSize; x++)
        {
            for (int y = 0; y <= VoxelData.ChunkSize; y++)
            {
                for (int z = 0; z <= VoxelData.ChunkSize; z++)
                {
                    if (!visited[x, y, z] && IsOre(x, y, z) && voxelData.densityMap[x, y, z] > 0)
                    {
                        // Found unvisited ore, start flood fill
                        List<Vector3Int> cluster = new List<Vector3Int>();
                        bool connectedToGround = FloodFill(x, y, z, cluster);
                        
                        // If cluster is not connected to ground (y=0), it's floating
                        if (!connectedToGround)
                        {
                            clusters.Add(cluster);
                        }
                    }
                }
            }
        }
        
        return clusters;
    }
    
    private bool FloodFill(int x, int y, int z, List<Vector3Int> cluster)
    {
        Stack<Vector3Int> stack = new Stack<Vector3Int>();
        stack.Push(new Vector3Int(x, y, z));
        bool touchesGround = false;
        
        while (stack.Count > 0)
        {
            Vector3Int pos = stack.Pop();
            
            if (pos.x < 0 || pos.x > VoxelData.ChunkSize ||
                pos.y < 0 || pos.y > VoxelData.ChunkSize ||
                pos.z < 0 || pos.z > VoxelData.ChunkSize)
                continue;
            
            if (visited[pos.x, pos.y, pos.z])
                continue;
            
            if (voxelData.densityMap[pos.x, pos.y, pos.z] <= 0)
                continue;
            
            visited[pos.x, pos.y, pos.z] = true;
            
            // Check if touching ground
            if (pos.y == 0)
                touchesGround = true;
            
            // If this is ore, add to cluster
            if (IsOre(pos.x, pos.y, pos.z))
            {
                cluster.Add(pos);
            }
            
            // Check all 6 neighbors (not diagonals for simplicity)
            stack.Push(new Vector3Int(pos.x + 1, pos.y, pos.z));
            stack.Push(new Vector3Int(pos.x - 1, pos.y, pos.z));
            stack.Push(new Vector3Int(pos.x, pos.y + 1, pos.z));
            stack.Push(new Vector3Int(pos.x, pos.y - 1, pos.z));
            stack.Push(new Vector3Int(pos.x, pos.y, pos.z + 1));
            stack.Push(new Vector3Int(pos.x, pos.y, pos.z - 1));
        }
        
        return touchesGround;
    }
    
    private bool IsOre(int x, int y, int z)
    {
        VoxelType type = voxelData.voxelTypes[x, y, z];
        return type == VoxelType.IronOre || type == VoxelType.CopperOre;
    }
}