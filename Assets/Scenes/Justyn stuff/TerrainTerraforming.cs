using UnityEngine;

public class TerrainTerraforming : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private LayerMask terrainLayer;
    [SerializeField] private Camera cam;
    [SerializeField] private Transform player;
    [SerializeField] private string swingTrigger = "Swing";
    [SerializeField] private float clickCooldown = 1f;
    // Optional: name of the animator state to wait for. If empty, the script will try to read the current clip length.
    [SerializeField] private string swingStateName = "";
    // fallback duration if clip info is unavailable
    [SerializeField] private float animationDuration = 1f;
    private bool isAnimating = false;

    [SerializeField] private float playerReach;
    [SerializeField] private float miningEffectivity; // height
    [SerializeField] private float miningRange; // size

    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    private CubeMarching cubeMarching;

    private void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        cubeMarching = GetComponent<CubeMarching>();
    }

    private void Update()
    {
       
    }

    public void OnClick()
    {
        // ignore clicks while animation is playing
        if (isAnimating) return;

        // Ray from camera forward (suitable for input-action based clicks)
        Ray r = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hitInfo;

        if (!Physics.Raycast(r, out hitInfo, playerReach, terrainLayer)) return;

        // Trigger animation (if present) and mark animating state
        if (animator != null)
        {
            animator.SetTrigger(swingTrigger);
            animator.SetBool("isMining", true);
        }

        isAnimating = true;

        // apply terraform immediately
        if (cubeMarching != null)
        {
            cubeMarching.ModifyTerrainAtWorldPos(hitInfo.point, -miningEffectivity, miningRange);
        }
        else
        {
            TerraformTerrain(hitInfo.point, -miningEffectivity, miningRange);
        }

        // start coroutine to wait until animation finishes before allowing next click
        StartCoroutine(WaitForAnimationAndReset());
    }

    private System.Collections.IEnumerator WaitForAnimationAndReset()
    {
        float waitTime = clickCooldown;

        if (animator != null)
        {
            // Try to wait until the target state is active (if provided)
            if (!string.IsNullOrEmpty(swingStateName))
            {
                // wait a short frame for transition
                float timeout = 0.5f;
                while (!animator.GetCurrentAnimatorStateInfo(0).IsName(swingStateName) && timeout > 0f)
                {
                    timeout -= Time.deltaTime;
                    yield return null;
                }
                var clips = animator.GetCurrentAnimatorClipInfo(0);
                if (clips != null && clips.Length > 0)
                {
                    waitTime = clips[0].clip.length;
                }
                else
                {
                    waitTime = animationDuration;
                }
            }
            else
            {
                // No state name supplied; try to read current clip on next frame
                yield return null;
                var clips = animator.GetCurrentAnimatorClipInfo(0);
                if (clips != null && clips.Length > 0)
                {
                    waitTime = clips[0].clip.length;
                }
                else
                {
                    waitTime = animationDuration;
                }
            }
        }

        yield return new WaitForSeconds(waitTime);

        if (animator != null)
        {
            animator.SetBool("isMining", false);
        }

        isAnimating = false;
    }

    // legacy fallback (kept private). Prefer using CubeMarching.ModifyTerrainAtWorldPos instead.
    private Mesh mesh;
    private Vector3[] vertices;
    private void TerraformTerrain(Vector3 position, float height, float range)
    {
        if (meshFilter == null) return;

        mesh = meshFilter.sharedMesh;
        vertices = mesh.vertices;
        position -= meshFilter.transform.position;

        int i = 0;
        foreach (Vector3 vert in vertices)
        {
            if (Vector2.Distance(new Vector2(vert.x, vert.z), new Vector2(position.x, position.z)) <= range)
            {
                vertices[i] = vert + new Vector3(0, height, 0);
            }
            i++;
        }

        mesh.vertices = vertices;
        meshFilter.mesh = mesh;
        if (meshCollider != null) meshCollider.sharedMesh = mesh;
    }

}
