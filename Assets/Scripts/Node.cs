using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// QPathFinder
/// Single Node. From which we can create Paths ( or connections )
/// <summary>
/// Wierzchołek grafu
/// </summary>
[System.Serializable]
public class Node {
    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="Position">Pozycja wierzchołka w przestrzeni trójwymiarowej</param>
    public Node(Vector3 Position) { position = Position; }
    /// <summary>
    /// Przechowuje pozycje wierzchołka w przestrzeni trójwymiarowej
    /// </summary>
    [SerializeField] public Vector3 position;
    /// <summary>
    /// Przechowuje identyfikator wierzchołka
    /// </summary>
    [SerializeField] public int ID = -1;
    /// QPathFinder
    /// <summary>
    /// Przechowuje poprzedni wierzchołek. Wykorzystywane podczas wytyczania ścieżki.
    /// </summary>
    [HideInInspector] public Node previousNode;
    /// QPathFinder ////
    /// <summary>
    /// 
    /// </summary>
    [HideInInspector] public float heuristicDistance, pathDistance;
    [HideInInspector] public float CombinedHeuristic => pathDistance + heuristicDistance;

}
