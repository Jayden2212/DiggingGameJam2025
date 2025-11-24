using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Displays temporary notification messages at the bottom of the screen without interrupting gameplay.
/// </summary>
public class NotificationSystem : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The panel that contains the notification")]
    public GameObject notificationPanel;
    
    [Tooltip("The text component that displays the message")]
    public TMP_Text notificationText;
    
    [Header("Settings")]
    [Tooltip("How long the notification stays on screen")]
    public float displayDuration = 3f;
    
    [Tooltip("Fade in duration")]
    public float fadeInDuration = 0.3f;
    
    [Tooltip("Fade out duration")]
    public float fadeOutDuration = 0.5f;
    
    private CanvasGroup canvasGroup;
    private Coroutine currentNotification;
    private bool isPersistent = false;
    
    void Awake()
    {
        // Get or add CanvasGroup for fading
        canvasGroup = notificationPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = notificationPanel.AddComponent<CanvasGroup>();
        }
        
        // Start hidden
        canvasGroup.alpha = 0f;
        notificationPanel.SetActive(false);
    }
    
    // Shows a notification message on screen.
    /// <param name="message">The message to display</param>
    /// <param name="color">Text color (optional, uses current color if not specified)</param>
    /// <param name="persistent">If true, notification stays visible until HideNotification is called</param>
    public void ShowNotification(string message, Color? color = null, bool persistent = false)
    {
        // If there's already a notification showing, stop it
        if (currentNotification != null)
        {
            StopCoroutine(currentNotification);
        }
        
        isPersistent = persistent;
        
        // Set text color if specified
        if (color.HasValue)
        {
            notificationText.color = color.Value;
        }
        
        currentNotification = StartCoroutine(DisplayNotification(message));
    }
    
    // Hides the current notification (useful for persistent notifications)
    public void HideNotification()
    {
        if (currentNotification != null)
        {
            StopCoroutine(currentNotification);
        }
        
        isPersistent = false;
        currentNotification = StartCoroutine(FadeOutNotification());
    }
    
    private IEnumerator DisplayNotification(string message)
    {
        // Set the message
        notificationText.text = message;
        notificationPanel.SetActive(true);
        
        // Fade in
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
        
        // If persistent, don't fade out - just stay visible
        if (isPersistent)
        {
            currentNotification = null;
            yield break;
        }
        
        // Wait for display duration
        yield return new WaitForSeconds(displayDuration);
        
        // Fade out
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        
        notificationPanel.SetActive(false);
        currentNotification = null;
    }
    
    private IEnumerator FadeOutNotification()
    {
        // Fade out
        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        
        notificationPanel.SetActive(false);
        currentNotification = null;
    }
}
