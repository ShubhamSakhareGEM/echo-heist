using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [SerializeField] private TMP_Text objectiveText;

    public bool HasArtifact { get; private set; }
    public bool GameEnded { get; private set; }

    private void Start()
    {
        objectiveText.text = "Objective: Steal the Artifact";
    }

    private void Update()
    {
        if (GameEnded && Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
        }
    }

    public void CollectArtifact()
    {
        if (GameEnded)
            return;

        HasArtifact = true;

        objectiveText.text = "Objective: Escape";

        Debug.Log("Artifact collected! Return to the exit.");
    }

    public void TryEscape()
    {
        if (GameEnded)
            return;

        if (HasArtifact)
        {
            GameEnded = true;

            objectiveText.text =
                "MISSION SUCCESS\nPress R to Restart";

            Debug.Log("MISSION SUCCESS");
        }
        else
        {
            objectiveText.text =
                "Objective: Steal the Artifact First";

            Debug.Log("You need the artifact first.");
        }
    }

    public void PlayerCaught()
    {
        if (GameEnded)
            return;

        GameEnded = true;

        objectiveText.text =
            "MISSION FAILED\nPress R to Restart";

        Debug.Log("MISSION FAILED");
    }

    private void RestartGame()
    {
        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }
}