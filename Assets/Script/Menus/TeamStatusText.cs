using TMPro;
using UnityEngine;

// Shows a different message depending on whether the player has picked a team yet
[RequireComponent(typeof(TextMeshProUGUI))]
public class TeamStatusText : MonoBehaviour
{
    [TextArea] public string emptyText = "No team yet";
    [Tooltip("{0} = towers picked, {1} = max team size")]
    [TextArea] public string readyText = "Team ready: {0} of {1}";
    public bool useColors = true;
    public Color emptyColor = new Color32(200, 60, 30, 255);
    public Color readyColor = new Color32(40, 130, 40, 255);

    TextMeshProUGUI label;
    int shownCount = -1;

    private void Awake()
    {
        label = GetComponent<TextMeshProUGUI>();
    }

    private void Update()
    {
        // Polled so it stays correct while the player edits their team
        int count = TeamInfo.Count;
        if (count == shownCount)
        {
            return;
        }
        shownCount = count;

        bool ready = count > 0;
        label.text = ready ? string.Format(readyText, count, TeamInfo.MaxTeamSize) : emptyText;
        if (useColors)
        {
            label.color = ready ? readyColor : emptyColor;
        }
    }
}
