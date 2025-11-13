using UnityEngine;
using System.Collections.Generic;

public class OreNode : MonoBehaviour
{
    [Header("Ore Settings")]
    public OreType oreType;
    public int resourceAmount = 50;
    
    [Header("Connection Settings")]
    public float checkRadius = 1.5f;
    public float checkInterval = 0.5f;
    
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
        
        // Sample multiple points around the ore
        Vector3[] checkPoints = new Vector3[]
        {
            transform.position + Vector3.up * checkRadius,
            transform.position + Vector3.down * checkRadius,
            transform.position + Vector3.left * checkRadius,
            transform.position + Vector3.right * checkRadius,
            transform.position + Vector3.forward * checkRadius,
            transform.position + Vector3.back * checkRadius
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

public enum OreType
{
    Iron,
    Copper,
    Gold
}