using UnityEngine;
using System.Collections.Generic;

public class OreNode : MonoBehaviour
{
    [Header("Ore Settings")]
    public VoxelType oreType;
    [Tooltip("Amount of ore resource this node gives when collected")]
    public int resourceAmount = 1;
    
    [Header("Connection Settings")]
    [Tooltip("How often to check if ore is still connected to terrain (seconds)")]
    public float checkInterval = 0.5f;
    
    [Tooltip("How many voxels around the ore to check. Higher = needs more digging to release")]
    public float checkRadius = 0.5f;
    
    [Tooltip("Minimum number of check points that must be empty before ore pops off (out of 7 total)")]
    [Range(1, 7)]
    public int pointsRequiredEmpty = 3;
    
    [HideInInspector]
    public float effectiveVoxelSize = 1.0f; // No longer used but kept for compatibility
    
    private PlayerInventory playerInventory;
    private TerrainChunk parentChunk;
    private Vector3 localVoxelPosition;
    private bool isGrounded = true;
    private float lastCheckTime;
    
    void Start()
    {
        // Find player inventory
        playerInventory = FindFirstObjectByType<PlayerInventory>();
        
        // Find parent chunk
        parentChunk = GetComponentInParent<TerrainChunk>();
        
        if (parentChunk != null)
        {
            // Store local voxel position
            localVoxelPosition = parentChunk.transform.InverseTransformPoint(transform.position);
        }
    }
    
    void OnDestroy()
    {
        // Add ore to player inventory when destroyed
        if (playerInventory != null)
        {
            playerInventory.AddResource(oreType, resourceAmount);
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
        
        // Check a few key points around the ore
        // Ore pops off when enough of these points are empty
        Vector3[] checkPoints = new Vector3[]
        {
            transform.position,  // Center (most important)
            transform.position + Vector3.up * checkRadius,
            transform.position + Vector3.down * checkRadius,
            transform.position + Vector3.left * checkRadius,
            transform.position + Vector3.right * checkRadius,
            transform.position + Vector3.forward * checkRadius,
            transform.position + Vector3.back * checkRadius
        };
        
        // Count how many points are empty
        int emptyPoints = 0;
        foreach (Vector3 point in checkPoints)
        {
            if (!parentChunk.IsPositionInTerrain(point))
            {
                emptyPoints++;
            }
        }
        
        // If enough points are empty, ore pops off
        if (emptyPoints >= pointsRequiredEmpty && isGrounded)
        {
            StartFloatingBehavior();
        }
        else if (emptyPoints < pointsRequiredEmpty)
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
