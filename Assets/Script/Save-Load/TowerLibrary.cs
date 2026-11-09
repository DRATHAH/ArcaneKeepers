using System.Collections.Generic;
using UnityEngine;

public class TowerLibrary : MonoBehaviour
{
    #region Singleton

    public static TowerLibrary instance;

    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogWarning("More than one instance of library found!");
            return;
        }
        instance = this;
    }
    #endregion

    public List<Tower> towers = new List<Tower>();
}
