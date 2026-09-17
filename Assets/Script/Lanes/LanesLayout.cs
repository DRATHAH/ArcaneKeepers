using UnityEngine;
using System;
using System.Collections.Generic;

public class LanesLayout : MonoBehaviour
{
    [SerializeField] public int GridX = 10;
    [SerializeField] private static int GridY = 4;

    [Tooltip("Each set of elements represent items in a row.")]
    [SerializeField] private List<GameObject> tilesGrid = new List<GameObject>();


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnDrawGizmos()
    {
        for (int i = 0; i < GridY; i++)
        {
            for (int j = 0; j < GridX; j++)
            {
                if(j%2 != 0)
                {
                    Gizmos.color = Color.red;
                }
                else
                {
                    Gizmos.color = Color.green;
                }

                Gizmos.DrawCube(new Vector3(0 + j, 0, 0 + i), Vector3.one);
            }
        }
    }
}
