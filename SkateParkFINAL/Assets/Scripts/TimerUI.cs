using UnityEngine;
using TMPro;

public class TimerUI : MonoBehaviour
{
    public TextMeshProUGUI timerText; // Reference to a TextMeshPro UI element
    private float startTime = 180f; // Start from 3 minutes (180 seconds)
    private float remainingTime;
    private bool isRunning = true;

    void Start()
    {
        remainingTime = startTime;
        UpdateTimerText();
    }

    void Update()
    {
        if (isRunning && remainingTime > 0)
        {
            remainingTime -= Time.deltaTime;
            remainingTime = Mathf.Max(remainingTime, 0); // Ensure it never goes below zero
            UpdateTimerText();
        }
    }

    void UpdateTimerText()
    {
        int minutes = Mathf.FloorToInt(remainingTime / 60);
        int seconds = Mathf.FloorToInt(remainingTime % 60);
        int milliseconds = Mathf.FloorToInt((remainingTime * 100) % 100);
        timerText.text = string.Format("{0:00}:{1:00}.{2:00}", minutes, seconds, milliseconds);
    }

    public void StartTimer()
    {
        isRunning = true;
    }

    public void StopTimer()
    {
        isRunning = false;
    }

    public void ResetTimer()
    {
        remainingTime = startTime;
        UpdateTimerText();
    }
}
