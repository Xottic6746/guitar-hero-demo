using UnityEngine;
using UnityEngine.UI;

public class ScoreController : MonoBehaviour
{
    public RhythmGameManager gameManager;
    public Text scoreText;
    public Text comboText;
    public Text healthText;

    private void Update()
    {
        if (gameManager == null) return;

        if (scoreText != null)
        {
            scoreText.text = "Score: " + gameManager.Score;
        }

        if (comboText != null)
        {
            comboText.text = "Combo: " + gameManager.Combo;
        }

        if (healthText != null)
        {
            healthText.text = "Health: " + gameManager.Health;
        }
    }
}
