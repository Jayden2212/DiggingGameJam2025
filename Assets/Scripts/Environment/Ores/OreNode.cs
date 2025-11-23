using UnityEngine;
using System.Collections.Generic;

public class OreNode : MonoBehaviour
{
    [Header("Ore Settings")]
    public VoxelType oreType;
    public int resourceAmount = 20;
    
    [Header("Connection Settings")]
    [Tooltip("Base check radius multiplied by ore scale - represents the effective voxel size")]
    public float checkRadius = 0.1f;
    
    [Tooltip("How often to check if ore is still connected to terrain (seconds)")]
    public float checkInterval = 0.5f;
    
    [HideInInspector]
    public float effectiveVoxelSize = 1.0f; // Set by TerrainChunk based on oreScale
    
    private TerrainChunk parentChunk;
    private Vector3 localVoxelPosition;
    private bool isGrounded = true;
    private float lastCheckTime;
    
    void Start()
    {
        // Find parent chunk
        parentChunk = GetComponentInParent<TerrainChunk>();
        
        if (parentChunk != null)
        {
            // Store local voxel position
            localVoxelPosition = parentChunk.transform.InverseTransformPoint(transform.position);
        }
    }
    
    void Update()
    {
        // Periodically check if still connected to terrain
        if (Time.time - lastCheckTime > checkInterval)
        {
            lastCheckTime = Time.time;
            CheckTerrainConnection();
        }
    }
    
    void CheckTerrainConnection()
    {
        if (parentChunk == null) return;
        
        // Check if there's solid terrain nearby
        bool hasNearbyTerrain = false;
        
        // Calculate actual check distance based on scaled voxel size and base radius
        float actualCheckRadius = checkRadius * effectiveVoxelSize;
        
        // Sample multiple points around the ore
        Vector3[] checkPoints = new Vector3[]
        {
            transform.position + Vector3.up * actualCheckRadius,
            transform.position + Vector3.down * actualCheckRadius,
            transform.position + Vector3.left * actualCheckRadius,
            transform.position + Vector3.right * actualCheckRadius,
            transform.position + Vector3.forward * actualCheckRadius,
            transform.position + Vector3.back * actualCheckRadius
        };
        
        foreach (Vector3 point in checkPoints)
        {
            if (parentChunk.IsPositionInTerrain(point))
            {
                hasNearbyTerrain = true;
                break;
            }
        }
        
        // If no terrain nearby, ore is floating - destroy it
        if (!hasNearbyTerrain && isGrounded)
        {
            StartFloatingBehavior();
        }
        else if (hasNearbyTerrain)
        {
            isGrounded = true;
        }
    }
    
    void StartFloatingBehavior()
    {
        isGrounded = false;
        
        // Option 1: Just destroy
        Destroy(gameObject);
        
        // Option 2: Make it fall and become collectible
        // AddPhysics();
        
        // Option 3: Crumble into particles
        // PlayCrumbleEffect();
        // Destroy(gameObject, 2f);
    }
    
    void AddPhysics()
    {
        // Add rigidbody to make it fall
        Rigidbody rb = gameObject.AddComponent<Rigidbody>();
        rb.mass = resourceAmount * 0.1f;
        
        // Add collider if not present
        if (GetComponent<Collider>() == null)
        {
            gameObject.AddComponent<SphereCollider>();
        }
        
        // Destroy after falling for a while
        Destroy(gameObject, 10f);
    }
}
