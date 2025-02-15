using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    private Queue<string> trickHistory = new Queue<string>(5); // Stores the last 5 tricks
    private string currentTrick = "";
    private float flatt = 0f;
    private float multt = 1f; // Start at 1 to prevent multiplication by zero
    private float totalComboScore;
    private float totalScore;
    private float trickTimer = 0f; // Timer to track trick inactivity
    private float trickTimeout = 5f; // 5-second timeout
    
    public TextMeshProUGUI flatText;
    public TextMeshProUGUI multText;
    public TextMeshProUGUI scoreText;

    void Start()
    {
        UpdateFlatText();
        UpdateMultText();
        UpdateScoreText();
    }

    void Update()
    {
        // Only track time if a trick has been performed
        if (flatt > 0) 
        {
            trickTimer += Time.deltaTime;
            
            if (trickTimer >= trickTimeout)
            {
                Debug.Log("Trick timeout reached! Calculating score...");
                CalculateScore();
            }
        }
    }

    public void ScoreTrick(string name, float flat)
    {
        Debug.Log($"Trick Scored | Current Trick: {currentTrick} | New Trick: {name}");

        // Determine how many previous tricks to check based on multt value
        int checkCount = 1; // Default to checking only the last trick
        if (multt >= 100) checkCount = 4;
        else if (multt >= 60) checkCount = 3;
        else if (multt >= 25) checkCount = 2;

        // Convert queue to list for indexed access
        List<string> trickList = new List<string>(trickHistory);

        // Check the last 'checkCount' tricks
        bool repeatFound = false;
        for (int i = 1; i <= checkCount; i++)
        {
            if (trickList.Count >= i && trickList[trickList.Count - i] == name)
            {
                repeatFound = true;
                break;
            }
        }

        if (repeatFound)
        {
            Debug.Log("Repeated trick detected! Resetting timer.");
            trickTimer = 0f;
            CalculateScore();
        }

        // Store trick history (keep only the last 5 tricks)
        if (trickHistory.Count >= 5)
        {
            trickHistory.Dequeue();
        }
        trickHistory.Enqueue(name);

        // Update trick values
        currentTrick = name;
        flatt += flat;
        UpdateFlatText();

        trickTimer = 0f;
    }


    public void ScoreMult(float mult)
    {
        multt += mult;
        UpdateMultText();
    }

    public void ScoreGrind()
    {
        Debug.Log("Grind Scored");

        flatt += 10;
        UpdateFlatText();

        trickTimer = 0f; // Reset the timer when grinding
    }

    public void ScoreAir()
    {
        Debug.Log("Air Scored");

        flatt += 5;
        UpdateFlatText();
        
        // No timer reset here, meaning it doesn't affect trick combos
    }

    public void CalculateScore()
    {
        if (flatt > 0) // Prevents unnecessary resets when no tricks were done
        {
            totalComboScore = flatt * multt;
            totalScore += totalComboScore;
            UpdateScoreText();

            // Reset combo
            flatt = 0;
            multt = 1;
            trickTimer = 0f; // Reset timer since we calculated the score
            UpdateFlatText();
            UpdateMultText();
        }
    }

    private void UpdateFlatText()
    {
        flatText.text = flatt.ToString();
    }

    private void UpdateMultText()
    {
        multText.text = multt.ToString();
    }

    private void UpdateScoreText()
    {
        scoreText.text = totalScore.ToString();
    }
}
