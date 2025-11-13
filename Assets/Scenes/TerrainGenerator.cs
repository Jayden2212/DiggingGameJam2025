using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[ExecuteInEditMode]
public class TerrainGenerator : MonoBehaviour
{
    [SerializeField] int xSize = 10;
    [SerializeField] int zSize = 10;

    [SerializeField] int xOffset;
    [SerializeField] int zOffset;

    [SerializeField] float noiseScale = 0.03f;
    [SerializeField] float heightMultiplier = 7;

    private Mesh mesh;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        mesh = GetComponent<MeshFilter>().sharedMesh;
    }

    public void GenerateTerrain()
    {
        CreateMesh();
    }

    private void CreateMesh()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        // VERTICES
        // create array with # by # square with vertices
        // ex. 3x3 has 4x4 vertices
        Vector3[] vertices = new Vector3[(xSize + 1) * (zSize + 1)];

        int i = 0;
        for (int z = 0; z <= zSize; z++)
        {
            for (int x = 0; x <= xSize; x++)
            {
                float yPos = Mathf.PerlinNoise((x + xOffset) * noiseScale, (z + zOffset) * noiseScale) * heightMultiplier;
                vertices[i] = new Vector3(x, yPos, z);
                i++;
            }
        }

        // TRIANGLES
        // 2 triangles per square
        // 3 vertices per triangle
        // #x# square times 2 * 3 = 6 vertices
        int[] triangles = new int[xSize * zSize * 6];

        int vertex = 0;
        int triangleIndex = 0;

        for (int z = 0; z < zSize; z++)
        {
            for (int x = 0; x < xSize; x++)
            {
                // bottom left triangle
                // start bottem left vertex = 0 
                // then move up a row = xSize + 1 
                // then next to the starting vertex (bottom right) = vertex + 1
                triangles[triangleIndex + 0] = vertex + 0;
                triangles[triangleIndex + 1] = vertex + xSize + 1;
                triangles[triangleIndex + 2] = vertex + 1;

                // upper right triangle
                // start at same position where first one ended
                // then move up a row
                // then move up a row and to the right
                triangles[triangleIndex + 3] = vertex + 1;
                triangles[triangleIndex + 4] = vertex + xSize + 1;
                triangles[triangleIndex + 5] = vertex + xSize + 2;

                vertex++;
                triangleIndex += 6;
            }

            // go up a row
            vertex++;
        }

        // clears terrain to allow size to be changed
        mesh.Clear();

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }

    /*
    // Draws a visual aid of spheres of the vertices
    private void OnDrawGizmos()
    {
        foreach (Vector3 pos in vertices)
        {
            Gizmos.DrawSphere(pos, 0.2f);
        }
    }
    */
}