using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// QPathFinder
/// Single Node. From which we can create Paths ( or connections )
/// <summary>
/// Wierzchołek grafu
/// </summary>
/// Element składowy grafu
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
    /// Przechowuje odległość przewidywaną przez heurystykę
    /// </summary>
    [HideInInspector] public float heuristicDistance;
    /// QPathFinder
    /// <summary>
    /// Przechowuje odległość od punktu początkowego
    /// </summary>
    [HideInInspector] public float pathDistance;
    /// QPathFinder
    /// <summary>
    /// Przechowuje poprzedni wierzchołek
    /// </summary>
    [HideInInspector] public Node previousNode;
    /// QPathFinder
    /// <summary>
    /// Zwraca sumę odległości
    /// </summary>
    [HideInInspector]
    public float CombinedHeuristic {
        get { return pathDistance + heuristicDistance; }
    }
}
