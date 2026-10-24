using TMPro;
using UnityEngine;

public class GuardDebugUI : MonoBehaviour
{
    [SerializeField] private TMP_Text debugText;
    [SerializeField] private GuardAI guardAI;
    [SerializeField] private GuardVision guardVision;
    [SerializeField] private GameObject lastKnownMarker;

    private bool debugVisible = true;

    private void Start()
    {
        UpdateVisibility();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            debugVisible = !debugVisible;
            UpdateVisibility();
        }

        if (!debugVisible)
            return;

        UpdateDebugText();
        UpdateLastKnownMarker();
    }

    private void UpdateDebugText()
    {
        string visible =
            guardVision.CanSeePlayer ? "YES" : "NO";

        string lastSeen =
            guardAI.HasSeenPlayer
                ? guardAI.LastKnownPlayerPosition.ToString("F1")
                : "None";

        string pathStatus =
            guardAI.HasActivePath ? "ACTIVE" : "DIRECT";

        debugText.text =
            "AI DEBUG\n" +
            "State: " + guardAI.CurrentStateName + "\n" +
            "Player Visible: " + visible + "\n" +
            "Last Seen: " + lastSeen + "\n" +
            "Navigation: " + pathStatus + "\n" +
            "Path Nodes Left: " + guardAI.RemainingPathNodes;
    }

    private void UpdateLastKnownMarker()
    {
        if (lastKnownMarker == null)
            return;

        if (!guardAI.HasSeenPlayer)
        {
            lastKnownMarker.SetActive(false);
            return;
        }

        lastKnownMarker.SetActive(true);

        lastKnownMarker.transform.position =
            guardAI.LastKnownPlayerPosition;
    }

    private void UpdateVisibility()
    {
        debugText.gameObject.SetActive(debugVisible);

        if (!debugVisible &&
            lastKnownMarker != null)
        {
            lastKnownMarker.SetActive(false);
        }
    }
}