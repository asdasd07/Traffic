using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using UnityEditor;
using System;

/// QPathFinder modified
/// A collection of Nodes and Paths ( Connections ).
/// <summary>
/// Dane grafu. Przechowuje dane o wierzchołkach, ścieżkach i obiektach z nimi związanych
/// </summary>
[System.Serializable]
public class GraphData {
    /// <summary>
    /// Przechowuje listę wszystkich ulic
    /// </summary>
    [HideInInspector] public List<Street> allStreets = new List<Street>();
    /// <summary>
    /// Przechowuje listę wszystkich skrzyżowań
    /// </summary>
    [HideInInspector] public List<Junction> allJunctions = new List<Junction>();

    /// <summary>
    /// Przechowuje listę wszystkich wierzchołków
    /// </summary>
    [HideInInspector] public List<Node> nodes = new List<Node>();
    /// <summary>
    /// Przechowuje listę wierzchołków będących środkami ulic
    /// </summary>
    [HideInInspector] public List<Node> centers = new List<Node>();
    /// <summary>
    /// Przechowuje listę wszystkich ścieżek
    /// </summary>
    [HideInInspector] public List<Path> paths = new List<Path>();
    /// <summary>
    /// Przechowuje słownik określający identyfikator ścieżki, po parze identyfikatorów wierzchołków
    /// </summary>
    [HideInInspector] public Dictionary<Vector2Int, int> pathsByNodes = new Dictionary<Vector2Int, int>();

    /// <summary>
    /// Struktura używana do zapisu danych w trybie symulacji i wczytywania ich w trybie edycji
    /// </summary>
    [Serializable] struct SavingStructure {
        public float[][] Timers;
        public int[][] eqCounter;
    }


    /// <summary>
    /// Metoda gromadzi zmienne do struktury i zapisuje ją do pliku
    /// </summary>
    public void SaveTimers() {
        string destination = Application.persistentDataPath + "/save.dat";
        FileStream file;

        if (File.Exists(destination)) {
            File.Delete(destination);
        }
        BinaryFormatter bf = new BinaryFormatter();
        float[][] Timers = new float[allJunctions.Count][];
        int[][] eqCounter = new int[allJunctions.Count][];
        for (int i = 0; i < allJunctions.Count; i++) {
            Timers[i] = allJunctions[i].timers;
            eqCounter[i] = new int[allJunctions[i].paths.Count];
            for (int j = 0; j < allJunctions[i].paths.Count; j++) {
                eqCounter[i][j] = allJunctions[i].paths[j].entireQueue;
            }
        }
        SavingStructure sav = new SavingStructure {
            Timers = Timers,
            eqCounter = eqCounter
        };
        file = File.Create(destination);
        bf.Serialize(file, sav);
        file.Close();
        Debug.Log("File saved");
    }
    /// <summary>
    /// Metoda wczytuje strukturę z pliku i odpowiednio wypełnia nimi obiekty
    /// </summary>
    public void LoadTimers() {
        string destination = Application.persistentDataPath + "/save.dat";
        FileStream file;

        if (File.Exists(destination)) {
            file = File.OpenRead(destination);
        } else {
            Debug.Log("File not found");
            return;
        }
        BinaryFormatter bf = new BinaryFormatter();
        SavingStructure sav = (SavingStructure)bf.Deserialize(file);
        float[][] Timers = sav.Timers;
        int[][] eqCounter = sav.eqCounter;

        if (allJunctions.Count != Timers.Length) {
            return;
        }
        for (int i = 0; i < allJunctions.Count; i++) {
            allJunctions[i].timers = Timers[i];
            if (allJunctions[i].paths.Count == eqCounter[i].Length) {
                for (int j = 0; j < allJunctions[i].paths.Count; j++) {
                    allJunctions[i].paths[j].entireQueue = eqCounter[i][j];
                }
            }
        }
        file.Close();
        Debug.Log("File loaded");
    }

    public Path GetPathBetween(int from, int to) {
        return paths[pathsByNodes[new Vector2Int(from, to)]];
    }

    /// <summary>
    /// Metoda zwraca ścieżkę łączącą wierzchołki "od" i "do"
    /// </summary>
    /// <param name="from">Wierzchołek "od"</param>
    /// <param name="to">Wierzchołek "do"</param>
    /// <returns>Ścieżka łącząca dwa wierzchołki</returns>
    public Path GetPathBetween(Node from, Node to) {
        return GetPathBetween(from.ID, to.ID);
    }

    /// <summary>
    /// Metoda usuwa wszystkie skrzyżowania i ulice
    /// </summary>
    public void Clear() {
        foreach (Junction s in allJunctions) {
            s.Destroy();
        }
        allJunctions.Clear();
        allStreets.Clear();
        paths.Clear();
        nodes.Clear();
    }

    /// QPathFinder modified
    /// <summary>
    /// Metoda odpowiada za przyporządkowanie identyfikatorów wierzchołkom i ścieżkom 
    /// oraz za stworzenie słownika przyporządkowującemu parzę identyfikatorów wierzchołków, identyfikator ścieżki 
    /// </summary>
    public void ReGenerateIDs() {
        nodes.Clear();
        centers.Clear();
        paths.Clear();
        foreach (Street s in allStreets) {
            nodes.AddRange(s.nodes);
            centers.Add(s.center);
            paths.AddRange(s.paths);
        }
        foreach (Junction j in allJunctions) { paths.AddRange(j.paths); }

        for (int i = 0; i < nodes.Count; i++) { nodes[i].ID = i; }
        for (int i = 0; i < paths.Count; i++) {
            if (paths[i].IDOfA == -1 || paths[i].IDOfB == -1) {
                paths.RemoveAt(i);
                i--;
            } else {
                paths[i].autoGeneratedID = i;
            }
        }

        pathsByNodes = new Dictionary<Vector2Int, int>();
        for (int i = 0; i < paths.Count; i++) {
            Vector2Int v = new Vector2Int(paths[i].IDOfA, paths[i].IDOfB);
            if (pathsByNodes.ContainsKey(v)) {
                Debug.Log("Warrning duble key " + v);
                if (paths[pathsByNodes[v]] == paths[i]) {
                    Debug.Log("Error duble path");
                }
            } else {
                pathsByNodes.Add(v, i);
            }
        }
    }

}
