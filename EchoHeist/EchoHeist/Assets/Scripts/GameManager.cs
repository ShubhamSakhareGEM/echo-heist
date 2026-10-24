using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private TMP_Text objectiveText;

    public bool HasArtifact { get; private set; }

    private void Start()
    {
        objectiveText.text = "Objective: Steal the Artifact";
    }

    public void CollectArtifact()
    {
        HasArtifact = true;

        objectiveText.text = "Objective: Escape";

        Debug.Log("Artifact collected! Return to the exit.");
    }

    public void TryEscape()
    {
        if (HasArtifact)
        {
            objectiveText.text = "MISSION SUCCESS";

            Debug.Log("MISSION SUCCESS");
        }
        else
        {
            objectiveText.text = "Objective: Steal the Artifact First";

            Debug.Log("You need the artifact first.");
        }
    }
}