using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using UnityEditor;
using System;

/// QPathFinder modified
/// <summary>
/// A collection of Nodes and Paths ( Connections ).
/// </summary>
[System.Serializable]
public class GraphData {
    /// <summary>
    /// Atrybut typu List<Street>. Lista przechowująca wszystkie ulice
    /// </summary>
    [HideInInspector] public List<Street> AllStreets = new List<Street>();
    /// <summary>
    /// Atrybut typu List<Junction>. Lista przechowująca wszystkie skrzyżowania
    /// </summary>
    [HideInInspector] public List<Junction> AllJunctions = new List<Junction>();

    /// <summary>
    /// Atrybut typu List<Node>. Lista przechowująca wszystkie wierzchołki
    /// </summary>
    [HideInInspector] public List<Node> nodes = new List<Node>();
    /// <summary>
    /// Atrybut typu List<Node>. Lista przechowująca wierzchołki będące środkiem ulicy
    /// </summary>
    [HideInInspector] public List<Node> centers = new List<Node>();
    /// <summary>
    /// Atrybut typu List<Path>. Lista przechowująca wszystkie ścieżki
    /// </summary>
    [HideInInspector] public List<Path> paths = new List<Path>();
    /// <summary>
    /// Atrybut typu Dictionary<Vector2Int, int>. Słownik przechowujący parę identyfikatorów wierzchołków i identyfikator ścieżki, którą tworzą
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
    /// Metoda wczytuje strukturę SavingStructure z pliku i odpowiednio wypełnia nimi objekty
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

        if (AllJunctions.Count != Timers.Length) {
            return;
        }
        for (int i = 0; i < AllJunctions.Count; i++) {
            AllJunctions[i].timers = Timers[i];
            if (AllJunctions[i].paths.Count == eqCounter[i].Length) {
                for (int j = 0; j < AllJunctions[i].paths.Count; j++) {
                    AllJunctions[i].paths[j].entireQueue = eqCounter[i][j];
                }
            }
        }
        file.Close();
        Debug.Log("File loaded");
    }

    /// <summary>
    /// Metoda gromadzi zmienne do struktury SavingStructure i zapisuje ją do pliku
    /// </summary>
    public void SaveTimers() {
        string destination = Application.persistentDataPath + "/save.dat";
        FileStream file;

        if (File.Exists(destination)) {
            File.Delete(destination);
        }
        BinaryFormatter bf = new BinaryFormatter();
        float[][] Timers = new float[AllJunctions.Count][];
        int[][] eqCounter = new int[AllJunctions.Count][];
        for (int i = 0; i < AllJunctions.Count; i++) {
            Timers[i] = AllJunctions[i].timers;
            eqCounter[i] = new int[AllJunctions[i].paths.Count];
            for (int j = 0; j < AllJunctions[i].paths.Count; j++) {
                eqCounter[i][j] = AllJunctions[i].paths[j].entireQueue;
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
    /// Metoda zwraca obiekt Path, powiązany z parą obiektów Node
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <returns></returns>
    public Path GetPathBetween(int from, int to) {
        return paths[pathsByNodes[new Vector2Int(from, to)]];
    }

    /// <summary>
    /// Metoda zwraca obiekt Path, powiązany z parą obiektów Node
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <returns></returns>
    public Path GetPathBetween(Node from, Node to) {
        return GetPathBetween(from.ID, to.ID);
    }

    /// <summary>
    /// Metoda usówa wszystkie obiekty Junction i Street oraz czyści listy zmiennych z nimi powiązanych
    /// </summary>
    public void Clear() {
        foreach (Junction s in AllJunctions) {
            s.Destroy();
        }
        AllJunctions.Clear();
        AllStreets.Clear();
        paths.Clear();
        nodes.Clear();
    }

    /// QPathFinder modified
    /// <summary>
    /// Metoda odpowiada za przypożądkowanie identyfikatorów obiektom Node i Path 
    /// oraz stworzenie słownika przypożądkowującemu parzę identyfikatorów Node, identyfikator Path 
    /// </summary>
    public void ReGenerateIDs() {
        nodes.Clear();
        centers.Clear();
        paths.Clear();
        foreach (Street s in AllStreets) {
            nodes.AddRange(s.nodes);
            centers.Add(s.center);
            paths.AddRange(s.paths);
        }
        foreach (Junction j in AllJunctions) { paths.AddRange(j.paths); }

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
