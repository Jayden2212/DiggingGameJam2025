using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Swing : MonoBehaviour
{

    public GameObject tool;

    [Tooltip("Animator on the tool (optional). If not set, will try to get one from the 'tool' GameObject.")]
    public Animator animator;

    [Tooltip("Name of the swing animation state/clip to play.")]
    public string swingStateName = "Swing";

    [Tooltip("Optional state to return to after swing (can be empty).")]
    public string idleStateName = "New State";

    [Tooltip("Fallback duration (seconds) to wait if clip length can't be determined.")]
    public float fallbackDuration = 1f;

    bool isAnimating = false;

    void Start()
    {
        if (animator == null && tool != null)
            animator = tool.GetComponent<Animator>();
    }

    // Call this method when a click/input is registered
    public void OnClick()
    {
        if (isAnimating) return;
        StartCoroutine(ToolSwing());
    }

    System.Collections.IEnumerator ToolSwing()
    {
        if (animator == null)
            yield break;

        isAnimating = true;
        animator.SetBool("isMining", true);
        
        // Play the swing state if available
        animator.Play(swingStateName);

        // Try to determine clip length for the state
        float waitTime = fallbackDuration;
        var controller = animator.runtimeAnimatorController;
        if (controller != null)
        {
            foreach (var clip in controller.animationClips)
            {
                if (clip != null && clip.name == swingStateName)
                {
                    waitTime = clip.length;
                    break;
                }
            }
        }

        yield return new WaitForSeconds(waitTime);

        if (!string.IsNullOrEmpty(idleStateName))
        {
            animator.Play(idleStateName);
        }
    
        isAnimating = false;
        animator.SetBool("isMining", false);
    }
}