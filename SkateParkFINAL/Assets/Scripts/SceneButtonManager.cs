using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class SceneButtonManager : MonoBehaviour
{
    private float totalPointz;
    public TextMeshProUGUI totalPointsText;
    // Start is called before the first frame update
    void Start()
    {
        Cursor.visible = true;
        totalPointz = TimerUI.totalPoints;
        UpdateFinalScoreText();
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void QuitGame()
        {
        Application.Quit();
    }

    public void LoadGame()
    {
        SceneManager.LoadScene("SkateparkFinal");
    }

    public void LoadRetry()
    {
        SceneManager.LoadScene("EndMenu");
    }

    public void LoadMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void UpdateFinalScoreText(){
        if(totalPointsText!=null){
            totalPointsText.text = "Your total score:\n"+totalPointz;
        }
    }
}