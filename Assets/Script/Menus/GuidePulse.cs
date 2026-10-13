using UnityEngine;

// Gently pulses a UI element to show the player what to tap next
public class GuidePulse : MonoBehaviour
{
    public enum Condition { Always, WhenNoTeam, WhenTeamReady }

    public Condition condition = Condition.Always;
    public float amount = 0.06f;
    public float speed = 3f;

    Vector3 restScale;

    private void Awake()
    {
        restScale = transform.localScale;
    }

    private void Update()
    {
        bool active = condition == Condition.Always
            || (condition == Condition.WhenNoTeam && !TeamInfo.HasTeam)
            || (condition == Condition.WhenTeamReady && TeamInfo.HasTeam);

        float scale = active ? 1 + Mathf.Sin(Time.unscaledTime * speed) * amount : 1;
        transform.localScale = restScale * scale;
    }

    private void OnDisable()
    {
        transform.localScale = restScale;
    }
}
