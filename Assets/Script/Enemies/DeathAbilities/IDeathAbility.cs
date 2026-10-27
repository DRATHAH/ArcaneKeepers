using UnityEngine;

public interface IDeathAbility
{
    //Function for conducting the death ability.
    public void OnDeathAbility();

    //Function for setting an enemy's current tile
    public void SetTile(int newTile); 
}
