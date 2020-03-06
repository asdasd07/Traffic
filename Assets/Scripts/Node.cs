using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Single Node. From which we can create Paths ( or connections )
[System.Serializable]
public class Node {
    public Node(Vector3 pos) { position = pos; }

    public void SetPosition(Vector3 pos) { position = pos; }
    public Vector3 Position { get { return position; } }

    [SerializeField] private Vector3 position;
    [SerializeField] public int ID = -1;
    [HideInInspector] public Node previousNode;
    [HideInInspector] public float heuristicDistance;
    [HideInInspector] public float pathDistance;
    [HideInInspector] public float combinedHeuristic { get { return pathDistance + heuristicDistance; } }
}
