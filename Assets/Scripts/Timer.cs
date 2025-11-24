using TMPro;
using UnityEngine;

public class Timer : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI timerText;
    float elapsedTime;
    bool isRunning = true;

    // Update is called once per frame
    void Update()
    {
        if (isRunning)
        {
            elapsedTime += Time.deltaTime;
            int minutes = Mathf.FloorToInt(elapsedTime / 60);
            int seconds = Mathf.FloorToInt(elapsedTime % 60);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }
    
    public void StopTimer()
    {
        isRunning = false;
    }
    
    public string GetFormattedTime()
    {
        int minutes = Mathf.FloorToInt(elapsedTime / 60);
        int seconds = Mathf.FloorToInt(elapsedTime % 60);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }
    
    public void ResetTimer()
    {
        elapsedTime = 0f;
        isRunning = true;
    }
}
