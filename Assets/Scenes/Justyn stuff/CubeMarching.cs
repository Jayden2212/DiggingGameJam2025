using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class CubeMarching : MonoBehaviour
{
    [SerializeField] private int width = 30;
    [SerializeField] private int height = 10;

    [SerializeField] float resolution = 1;
    [SerializeField] float noiseScale = 1;

    [SerializeField] private float heightTresshold = 0.5f;

    [SerializeField] bool visualizeNoise;
    [SerializeField] bool use3DNoise;

    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();
    private float[,,] heights;

    private MeshFilter meshFilter;
    private MeshCollider meshCollider;

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();

        // initialize heights once. If using 3D noise and you want animated updates, enable the coroutine.
        if (use3DNoise)
        {
            StartCoroutine(TestAll());
        }
        else
        {
            SetHeights();
            MarchCubes();
            SetMesh();
        }
    }

    private IEnumerator TestAll()
    {
        while (true)
        {
            SetHeights();
            MarchCubes();
            SetMesh();
            yield return new WaitForSeconds(1f);
        }
    }

    private void SetMesh()
    {
        Mesh mesh = new Mesh();

        mesh.vertices = vertices.ToArray();

        // Ensure triangle winding is consistent (flip winding if needed).
        int[] triArr = triangles.ToArray();
        for (int i = 0; i < triArr.Length; i += 3)
        {
            if (i + 2 >= triArr.Length) break;
            int tmp = triArr[i];
            triArr[i] = triArr[i + 1];
            triArr[i + 1] = tmp;
        }

        mesh.triangles = triArr;
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
        if (meshCollider != null)
            meshCollider.sharedMesh = mesh;
    }

    private void SetHeights()
    {
        heights = new float[width + 1, height + 1, width + 1];

        for (int x = 0; x < width + 1; x++)
        {
            for (int y = 0; y < height + 1; y++)
            {
                for (int z = 0; z < width + 1; z++)
                {
                    if (use3DNoise)
                    {
                        float currentHeight = PerlinNoise3D((float)x / width * noiseScale, (float)y / height * noiseScale, (float)z / width * noiseScale);

                        heights[x, y, z] = currentHeight;
                    }
                    else
                    {
                        // produce a density value where positive means "inside" (below terrain height)
                        float currentHeight = height * Mathf.PerlinNoise(x * noiseScale, z * noiseScale);
                        heights[x, y, z] = currentHeight - y;
                    }
                } 
            }
        }
    }
    private float PerlinNoise3D (float x, float y, float z)
    {
        float xy = Mathf.PerlinNoise(x, y);
        float xz = Mathf.PerlinNoise(x, z);
        float yz = Mathf.PerlinNoise(y, z);

        float yx = Mathf.PerlinNoise(y, x);
        float zx = Mathf.PerlinNoise(z, x);
        float zy = Mathf.PerlinNoise(z, y);

        return (xy + xz + yz + yx + zx + zy) / 6;
    }

    private int GetConfigIndex (float[] cubeCorners)
    {
        int configIndex = 0;

        for (int i = 0; i < 8; i++)
        {
            if (cubeCorners[i] > heightTresshold)
            {
                configIndex |= 1 << i;
            }
        }

        return configIndex;
    }

    private void MarchCubes()
    {
        vertices.Clear();
        triangles.Clear();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < width; z++)
                {
                    float[] cubeCorners = new float[8];

                    for (int i = 0; i < 8; i++)
                    {
                        Vector3Int corner = new Vector3Int(x, y, z) + MarchingTable.Corners[i];
                        cubeCorners[i] = heights[corner.x, corner.y, corner.z];
                    }

                    MarchCube(new Vector3(x, y, z), cubeCorners);
                }
            }
        }
    }

    private void MarchCube (Vector3 position, float[] cubeCorners)
    {
        int configIndex = GetConfigIndex(cubeCorners);

        if (configIndex == 0 || configIndex == 255)
        {
            return;
        }

        int edgeIndex = 0;
        for (int t = 0; t < 5; t++)
        {
            for (int v = 0; v < 3; v++)
            {
                int triTableValue = MarchingTable.Triangles[configIndex, edgeIndex];

                if (triTableValue == -1)
                {
                    return;
                }

                // compute local-space positions scaled by resolution
                Vector3 edgeStart = (position + MarchingTable.Edges[triTableValue, 0]) * resolution;
                Vector3 edgeEnd = (position + MarchingTable.Edges[triTableValue, 1]) * resolution;

                Vector3 vertex = (edgeStart + edgeEnd) / 2f;

                vertices.Add(vertex);
                triangles.Add(vertices.Count - 1);

                edgeIndex++;
            }
        }
    }

    // Public API: modify the internal height/density grid at a world position and rebuild mesh
    public void ModifyTerrainAtWorldPos(Vector3 worldPos, float deltaHeight, float range)
    {
        if (heights == null)
        {
            SetHeights();
        }

        // convert world to local (mesh space) accounting for rotation/scale
        Vector3 localPos = transform.InverseTransformPoint(worldPos);

        // convert to grid coordinates (float)
        float gx = localPos.x / resolution;
        float gy = localPos.y / resolution;
        float gz = localPos.z / resolution;

        int rx = Mathf.CeilToInt(range / resolution);
        for (int x = 0; x < width + 1; x++)
        {
            for (int y = 0; y < height + 1; y++)
            {
                for (int z = 0; z < width + 1; z++)
                {
                    float dx = x - gx;
                    float dy = y - gy;
                    float dz = z - gz;

                    float dist = Mathf.Sqrt(dx * dx + dy * dy + dz * dz) * resolution;

                    if (dist <= range)
                    {
                        heights[x, y, z] += deltaHeight;
                    }
                }
            }
        }

        // rebuild mesh from modified heights
        MarchCubes();
        SetMesh();
    }

    // Editor-time generation: build mesh and assign sharedMesh so the mesh is visible in the Scene (edit mode)
    public void GenerateInEditor()
    {
        if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
        if (meshCollider == null) meshCollider = GetComponent<MeshCollider>();

        SetHeights();
        MarchCubes();

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        // assign sharedMesh so the mesh persists in the scene view
        meshFilter.sharedMesh = mesh;
        if (meshCollider != null)
            meshCollider.sharedMesh = mesh;

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(meshFilter);
        if (meshCollider != null) UnityEditor.EditorUtility.SetDirty(meshCollider);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
#endif
    }

    private void OnDrawGizmosSelected()
    {
        if (!visualizeNoise || !Application.isPlaying)
        {
            return;
        }

        for (int x = 0; x < width + 1; x++)
        {
            for (int y = 0; y < height + 1; y++)
            {
                for (int z = 0; z < width + 1; z++)
                {
                    Gizmos.color = new Color(heights[x, y, z], heights[x, y, z], heights[x, y, z], 1);
                    // draw gizmos in world space, respecting this object's transform (rotation/scale)
                    Vector3 worldPos = transform.TransformPoint(new Vector3(x * resolution, y * resolution, z * resolution));
                    Gizmos.DrawSphere(worldPos, 0.2f * resolution * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z));
                }
            }
        }
    }
}