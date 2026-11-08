using System.Collections;
using UnityEngine;

// The reward that appears once the last enemy is dead. It shows for a moment so the win reads,
// then ends the level by itself: the victory screen bursts out of it.
public class LevelEndObject : MonoBehaviour
{
    [SerializeField] private Collider hitBox;
    [Tooltip("Seconds the reward is on screen before the victory screen starts")]
    [SerializeField] private float showTime = 0.8f;

    // Scaled time, so pausing during the beat holds the win back too
    IEnumerator Start()
    {
        yield return new WaitForSeconds(showTime);

        WinLossManager winLoss = WinLossManager.instance;
        if (winLoss == null || winLoss.winner || winLoss.loser) yield break;

        winLoss.SetFocus(hitBox.bounds.center);
        winLoss.winner = true;
        GameManager.instance.GetMatchReward();
    }
}
