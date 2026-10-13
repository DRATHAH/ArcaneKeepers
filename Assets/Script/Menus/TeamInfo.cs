// How many towers the player has picked for their team, from whichever manager exists in this scene
public static class TeamInfo
{
    public const int MaxTeamSize = 4;

    public static int Count
    {
        get
        {
            // In the character menu, the live inventory is the source of truth
            if (Inventory.instance != null)
            {
                int count = 0;
                foreach (TowerEntry tower in Inventory.instance.selectedTowers)
                {
                    if (tower != null)
                    {
                        count++;
                    }
                }
                return count;
            }
            if (PlayerManager.instance != null)
            {
                return PlayerManager.instance.equippedTowers.Count;
            }
            return 0;
        }
    }

    public static bool HasTeam
    {
        get { return Count > 0; }
    }
}
