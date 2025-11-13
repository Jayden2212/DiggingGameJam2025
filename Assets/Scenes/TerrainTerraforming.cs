using UnityEngine;

public class TerrainTerraforming : MonoBehaviour
{

    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private Mesh mesh;
    private Vector3[] vertices;
    private void TerraformTerrain(Vector3 position, float height, float range)
    {
        mesh = meshFilter.sharedMesh;
        vertices = mesh.vertices;
        position -= meshFilter.transform.position;

        // which vertex in loop
        // running through all vertices in mesh
        int i = 0;
        foreach (Vector3 vert in vertices)
        {
            // if the distance between vertex and the position we want to edit on terrain is <= range
            // don't need y positon, each vertice holds one y position, Vector2 used
            if (Vector2.Distance(new Vector2(vert.x, vert.z), new Vector2(position.x, position.z)) <= range)
            {
                vertices[i] = vert + new Vector3(0, height, 0);
            }
            i++;
        }

        mesh.vertices = vertices;
        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
    }

}
