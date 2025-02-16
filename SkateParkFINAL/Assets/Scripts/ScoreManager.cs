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
    public TextMeshProUGUI trickNameText; // Add this line
    public float trickDisplayDuration = 2f; // Duration for trick name display
    public float popScale = 1.5f; // Scale for the popping effect

    private float currentDisplayedScore; // Add this line
    public float tickDuration = 0.5f; // Duration for the score ticking effect
    private Coroutine scoreTickCoroutine; // Add this line


    private Coroutine trickDisplayCoroutine;

    void Start()
    {
        UpdateFlatText();
        UpdateMultText();
        UpdateScoreText();
        trickNameText.gameObject.SetActive(false); // Hide text initially
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
                ResetTrickName(); // Reset trick name when the trick timer expires
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

        // Display the trick name
        if (trickDisplayCoroutine != null)
        {
            StopCoroutine(trickDisplayCoroutine);
        }
        trickDisplayCoroutine = StartCoroutine(DisplayTrickName(name));
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

            // Start the score ticking effect
            if (scoreTickCoroutine != null)
            {
                StopCoroutine(scoreTickCoroutine);
            }
            scoreTickCoroutine = StartCoroutine(AnimateScore(totalScore));

            // Reset combo
            flatt = 0;
            multt = 1;
            trickTimer = 0f; // Reset timer since we calculated the score
            UpdateFlatText();
            UpdateMultText();
        }
    }

    private IEnumerator AnimateScore(float targetScore)
    {
        float startScore = currentDisplayedScore;
        float elapsedTime = 0f;

        while (elapsedTime < tickDuration)
        {
            currentDisplayedScore = Mathf.Lerp(startScore, targetScore, elapsedTime / tickDuration);
            scoreText.text = Mathf.RoundToInt(currentDisplayedScore).ToString();
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        currentDisplayedScore = targetScore;
        scoreText.text = Mathf.RoundToInt(currentDisplayedScore).ToString();
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

    private IEnumerator DisplayTrickName(string name)
    {
        if (trickNameText != null)
        {
            trickNameText.gameObject.SetActive(true);
            trickNameText.text = name;

            // Popping effect
            Vector3 originalScale = trickNameText.transform.localScale;
            Vector3 popScale = originalScale * this.popScale;
            float popDuration = 0.2f;

            // Enlarge
            for (float t = 0; t < 1f; t += Time.deltaTime / popDuration)
            {
                trickNameText.transform.localScale = Vector3.Lerp(originalScale, popScale, t);
                yield return null;
            }

            // Shrink back
            for (float t = 0; t < 1f; t += Time.deltaTime / popDuration)
            {
                trickNameText.transform.localScale = Vector3.Lerp(popScale, originalScale, t);
                yield return null;
            }

            // Display for the given duration
            yield return new WaitForSeconds(trickDisplayDuration);
            trickNameText.gameObject.SetActive(false); // Hide after duration
        }
    }

    private void ResetTrickName()
    {
        if (trickNameText != null)
        {
            trickNameText.gameObject.SetActive(false);
        }
    }
}
