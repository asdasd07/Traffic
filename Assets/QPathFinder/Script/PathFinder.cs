using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum Execution {
    Synchronous,
    Asynchronously
}

///
///    PathFinder instance uses GraphData to find the shorted path between Nodes
///    
public class PathFinder : MonoBehaviour {
    private static PathFinder _instance;
    public static PathFinder instance { get { return _instance; } }
    List<Transform> cars = new List<Transform>();
    Transform carsBox;
    public int amount = 10;
    public int maxCars = 100;
    public float CarsFreq = 0.1f;
    public float WorkDelay = 5f, ShopingDelay = 1f;
    public bool spawn = true;
    public bool randomSpawn = false;
    public bool save = true;
    public bool calculateTimers = true;
    public bool drawPaths = true;
    public bool showNodeId = true;
    public bool showPathId = true;
    public bool showCosts = false;
    public bool showSpawns = true;
    [HideInInspector] public GraphData graphData = new GraphData();

    public void Awake() {
        _instance = this;
        foreach (Junction j in graphData.AllJunctions) {
            j.globalTimersCalc = calculateTimers;
            foreach (Path p in j.paths) {
                p.entireQueue = 0;
            }
        }
        StartCoroutine(Spawn());
        StartCoroutine(RemoveCars());
    }
    public void OnDestroy() {
        _instance = null;
    }
    List<int>[] MakeSpawnList() {
        List<int>[] Spaw = new List<int>[5];//przyjazd/dom/sklep/praca/wyjazd
        for (int j = 0; j < 5; j++) {
            Spaw[j] = new List<int>();
        }
        foreach (Street s in graphData.AllStreets) {
            for (int j = 0; j < 5; j++) {
                for (int i = 0; i < s.Spawns[j]; i++) {
                    if (s.center.ID != -1) {
                        Spaw[j].Add(s.center.ID);
                    }
                }
            }
        }
        return Spaw;
    }
    public void SpawnPredictably(List<int>[] Spaw) {
        if (Spaw[0].Count == 0 && Spaw[1].Count == 0 || Spaw[2].Count == 0 && Spaw[3].Count == 0 && Spaw[4].Count == 0) {
            return;
        }
        List<Node> nod = null, retnod = null;
        int sp = -1, tar = -1, rsp = -1, rtar = -1;
        while (nod == null || nod.Count == 0) {
            sp = Random.Range(0, 2);
            sp = Spaw[sp].Count == 0 ? 1 - sp : sp;
            if (sp == 0) {//incoming
                tar = Random.Range(0, 2);
                tar = tar == 0 ? 1 : 4;
                tar = Spaw[tar].Count == 0 ? tar == 1 ? 5 : 1 : tar;
                if (Spaw[tar].Count == 0) break;
            } else {//house
                tar = Random.Range(0, 3);
                if (Spaw[tar + 2].Count == 0) {
                    tar = (tar + 1) % 3;
                    if (Spaw[tar + 2].Count == 0) {
                        tar = (tar + 1) % 3;
                    }
                }
                tar += 2;
            }
            rsp = Random.Range(0, Spaw[sp].Count);//spawn of type sp
            int a = Spaw[sp][rsp];
            int b = a;
            if (!Spaw[tar].Exists(item => item != a)) break;
            while (b == a) {
                rtar = Random.Range(0, Spaw[tar].Count);//target of type tar
                b = Spaw[tar][rtar];
            }
            nod = FindShortedPathSynchronousInternal(a, b);
            if (sp == 1 && (tar == 2 || tar == 3)) {//house to work or shop come back
                retnod = FindShortedPathSynchronousInternal(b, a);
            }
        }
        Spaw[sp].RemoveAt(rsp);
        Spaw[tar].RemoveAt(rtar);
        List<Path> pat = NodesToPath(nod);
        PathFollower fol = SpawnCar();
        if (retnod != null && retnod.Count != 0) {
            List<Path> retpat = NodesToPath(retnod);
            fol.ReturningPath = retpat;
            fol.ReturningType = tar;
            if (tar == 2) {
                fol.ReturningDelay = ShopingDelay;
            }
            if (tar == 3) {
                fol.ReturningDelay = WorkDelay;
            }
        }
        if (pat[0].street) {
            pat[0].street.Spawns[sp]--;
        }
        fol.Follow(pat);
    }
    public void SpawnRandom() {
        PathFollower fol = SpawnCar();
        List<Path> paths = RandomPath();
        fol.Follow(paths);
    }

    PathFollower SpawnCar() {
        if (!carsBox) {
            carsBox = (new GameObject("Cars")).transform;
        }
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.transform.localScale = new Vector3(0.4f, 0.4f, 0.8f);
        go.transform.parent = carsBox;
        cars.Add(go.transform);
        var fol = go.AddComponent<PathFollower>();
        return fol;
    }
    public List<Path> RandomPath() {
        List<Node> nod = null;
        while (nod == null || nod.Count == 0) {
            int a = Random.Range(0, graphData.centers.Count);
            int b = a;
            while (b == a) {
                b = Random.Range(0, graphData.centers.Count);
            }
            Node spa = graphData.centers[a];
            Node tar = graphData.centers[b];
            nod = FindShortedPathSynchronousInternal(spa.ID, tar.ID);
        }
        List<Path> pat = NodesToPath(nod);
        return pat;
    }
    protected IEnumerator Spawn() {
        List<int>[] Spn = MakeSpawnList();
        while (true) {
            if (spawn) {
                if (maxCars > amount) {
                    if (randomSpawn) {
                        SpawnRandom();
                    } else {
                        SpawnPredictably(Spn);
                    }
                    amount++;
                }
            }
            yield return new WaitForSeconds(CarsFreq);
        }
    }
    IEnumerator RemoveCars() {
        while (true) {
            cars.RemoveAll(item => item == null);
            amount = cars.Count;
            yield return new WaitForSeconds(2);
        }
    }
    List<Path> NodesToPath(List<Node> nodes) {
        List<Path> paths = new List<Path>();
        for (int i = 0; i < nodes.Count - 1; i++) {
            Path p = graphData.GetPathBetween(nodes[i].ID, nodes[i + 1].ID);
            if (p == null) { return null; }
            paths.Add(p);
        }
        return paths;
    }


    /// Finds shortest path between Nodes.
    /// Once the path if found, it will return the path as List of nodes (not positions, but nodes. If you need positions, use FindShortestPathOfPoints). 
    /// <returns> Returns list of **Nodes**</returns>
    /// <param name="fromNodeID">Find the path from this node</param>
    /// <param name="toNodeID">Find the path to this node</param>
    /// <param name="executionType">Synchronous is immediate and locks the control till path is found and returns the path. 
    /// Asynchronous type runs in coroutines without locking the control. If you have more than 50 Nodes, Asynchronous is recommended</param>
    /// <param name="callback">Callback once the path is found</param>


    public void FindShortestPathOfNodes(int fromNodeID, int toNodeID, Execution executionType, System.Action<List<Node>> callback) {
        if (executionType == Execution.Asynchronously) {
            StartCoroutine(FindShortestPathAsynchonousInternal(fromNodeID, toNodeID, callback));
        } else {
            callback(FindShortedPathSynchronousInternal(fromNodeID, toNodeID));
        }
    }

    public int FindNearestNode(Vector3 point) {
        float minDistance = float.MaxValue;
        Node nearestNode = null;

        foreach (var node in graphData.nodes) {
            if (Vector3.Distance(node.position, point) < minDistance) {
                minDistance = Vector3.Distance(node.position, point);
                nearestNode = node;
            }
        }

        return nearestNode != null ? nearestNode.ID : -1;
    }

    protected IEnumerator FindShortestPathAsynchonousInternal(int fromNodeID, int toNodeID, System.Action<List<Node>> callback) {
        if (callback == null)
            yield break;

        int startPointID = fromNodeID;
        int endPointID = toNodeID;

        graphData.ReGenerateIDs();

        Node startPoint = graphData.nodes[startPointID];
        Node endPoint = graphData.nodes[endPointID];

        foreach (var point in graphData.nodes) {
            point.heuristicDistance = -1;
            point.previousNode = null;
        }

        List<Node> completedPoints = new List<Node>();
        List<Node> nextPoints = new List<Node>();
        List<Node> finalPath = new List<Node>();

        startPoint.pathDistance = 0;
        startPoint.heuristicDistance = Vector3.Distance(startPoint.position, endPoint.position);
        nextPoints.Add(startPoint);

        while (true) {
            Node leastCostPoint = null;

            float minCost = 99999;
            foreach (var point in nextPoints) {
                if (point.heuristicDistance <= 0)
                    point.heuristicDistance = Vector3.Distance(point.position, endPoint.position) * 2;

                if (minCost > point.CombinedHeuristic) {
                    leastCostPoint = point;
                    minCost = point.CombinedHeuristic;
                }
            }

            if (leastCostPoint == null)
                break;

            if (leastCostPoint == endPoint) {
                Node prevPoint = leastCostPoint;
                while (prevPoint != null) {
                    finalPath.Insert(0, prevPoint);
                    prevPoint = prevPoint.previousNode;
                }
                callback(finalPath);
                yield break;
            }

            foreach (var path in graphData.paths) {
                if (path.IDOfA == leastCostPoint.ID
                || path.IDOfB == leastCostPoint.ID) {
                    if (leastCostPoint.ID == path.IDOfB) {
                        continue;
                    }

                    Node otherPoint = path.IDOfA == leastCostPoint.ID ? graphData.nodes[path.IDOfB] : graphData.nodes[path.IDOfA];

                    if (otherPoint.heuristicDistance <= 0)
                        otherPoint.heuristicDistance = Vector3.Distance(otherPoint.position, endPoint.position) + Vector3.Distance(otherPoint.position, startPoint.position);

                    if (completedPoints.Contains(otherPoint))
                        continue;

                    if (nextPoints.Contains(otherPoint)) {
                        if (otherPoint.pathDistance >
                            (leastCostPoint.pathDistance + path.Cost)) {
                            otherPoint.pathDistance = leastCostPoint.pathDistance + path.Cost;
                            otherPoint.previousNode = leastCostPoint;
                        }
                    } else {
                        otherPoint.pathDistance = leastCostPoint.pathDistance + path.Cost;
                        otherPoint.previousNode = leastCostPoint;
                        nextPoints.Add(otherPoint);
                    }
                }
            }
            nextPoints.Remove(leastCostPoint);
            completedPoints.Add(leastCostPoint);

            yield return null;
        }

        callback(null);
        yield break;
    }

    private List<Node> FindShortedPathSynchronousInternal(int fromNodeID, int toNodeID) {
        int startPointID = fromNodeID;
        int endPointID = toNodeID;

        graphData.ReGenerateIDs();

        Node startPoint = graphData.nodes[startPointID];
        Node endPoint = graphData.nodes[endPointID];

        foreach (var point in graphData.nodes) {
            point.heuristicDistance = -1;
            point.previousNode = null;
        }

        List<Node> completedPoints = new List<Node>();
        List<Node> nextPoints = new List<Node>();
        List<Node> finalPath = new List<Node>();

        startPoint.pathDistance = 0;
        startPoint.heuristicDistance = Vector3.Distance(startPoint.position, endPoint.position);
        nextPoints.Add(startPoint);

        while (true) {
            Node leastCostPoint = null;

            float minCost = 99999;
            foreach (var point in nextPoints) {
                if (point.heuristicDistance <= 0)
                    point.heuristicDistance = Vector3.Distance(point.position, endPoint.position) + Vector3.Distance(point.position, startPoint.position);

                if (minCost > point.CombinedHeuristic) {
                    leastCostPoint = point;
                    minCost = point.CombinedHeuristic;
                }
            }

            if (leastCostPoint == null)
                break;

            if (leastCostPoint == endPoint) {
                Node prevPoint = leastCostPoint;
                while (prevPoint != null) {
                    finalPath.Insert(0, prevPoint);
                    prevPoint = prevPoint.previousNode;
                }
                return finalPath;
            }

            foreach (var path in graphData.paths) {
                if (path.IDOfA == leastCostPoint.ID || path.IDOfB == leastCostPoint.ID) {

                    if (leastCostPoint.ID == path.IDOfB) {
                        continue;
                    }

                    Node otherPoint = path.IDOfA == leastCostPoint.ID ? graphData.nodes[path.IDOfB] : graphData.nodes[path.IDOfA];

                    if (otherPoint.heuristicDistance <= 0)
                        otherPoint.heuristicDistance = Vector3.Distance(otherPoint.position, endPoint.position) + Vector3.Distance(otherPoint.position, startPoint.position);

                    if (completedPoints.Contains(otherPoint))
                        continue;

                    if (nextPoints.Contains(otherPoint)) {
                        if (otherPoint.pathDistance >
                            (leastCostPoint.pathDistance + path.Cost)) {
                            otherPoint.pathDistance = leastCostPoint.pathDistance + path.Cost;
                            otherPoint.previousNode = leastCostPoint;
                        }
                    } else {
                        otherPoint.pathDistance = leastCostPoint.pathDistance + path.Cost;
                        otherPoint.previousNode = leastCostPoint;
                        nextPoints.Add(otherPoint);
                    }
                }
            }
            nextPoints.Remove(leastCostPoint);
            completedPoints.Add(leastCostPoint);
        }
        return null;
    }
}
