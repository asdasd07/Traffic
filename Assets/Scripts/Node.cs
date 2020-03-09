using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// QPathFinder
/// Single Node. From which we can create Paths ( or connections )
/// <summary>
/// 
/// </summary>
[System.Serializable]
public class Node {
    public Node(Vector3 pos) { position = pos; }

    [SerializeField] public int ID = -1;
    [HideInInspector] public Node previousNode;
    [HideInInspector] public float heuristicDistance, pathDistance;
    [HideInInspector] public float CombinedHeuristic => pathDistance + heuristicDistance;

    [SerializeField] public Vector3 position;
}
