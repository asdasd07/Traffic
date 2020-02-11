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
    List<Transform> Cars = new List<Transform>();
    float timer1 = 0, timer2 = 0;
    Transform CarsBox;
    public int amount = 10;
    public int maxCars = 100;
    public float CarsFreq = 0.1f;
    [HideInInspector]
    public GraphData graphData = new GraphData();

    public void Awake() {
        _instance = this;
        StartCoroutine(Spawn());
    }
    public void OnDestroy() {
        _instance = null;
    }
    public void SpawnRandom() {
        if (!CarsBox) {
            CarsBox = (new GameObject("Cars")).transform;
        }
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.transform.localScale = new Vector3(0.4f, 0.4f, 0.8f);
        go.transform.parent = CarsBox;
        Cars.Add(go.transform);
        var fol = go.AddComponent<PathFollower>();
        List<Path> paths = RandomPath();
        fol.Follow(paths);
    }
    public List<Path> RandomPath() {
        List<Node> nod = null;
        while (nod == null || nod.Count == 0) {
            int a = Random.Range(0, graphData.Spawn.Count);
            int b = a;
            while (b == a) {
                b = Random.Range(0, graphData.Target.Count);
            }
            Node spa = graphData.Spawn[a];
            Node tar = graphData.Target[b];
            nod = FindShortedPathSynchronousInternal(spa.ID, tar.ID);
        }
        List<Path> pat = NodesToPath(nod);
        return pat;
    }
    protected IEnumerator Spawn() {

        while (true) {
            if (graphData.spawn) {
                if (timer2 <= 0) {
                    timer2 = 2f;
                    Cars.RemoveAll(item => item == null);
                    amount = Cars.Count;
                }
                if (timer1 <= 0 && maxCars > amount) {
                    timer1 = CarsFreq;
                    SpawnRandom();
                    amount++;
                }
                timer1 -= Time.deltaTime;
                timer2 -= Time.deltaTime;
            }
            yield return null;
        }
    }
    void Update() {
    }
    List<Path> NodesToPath(List<Node> nodes) {
        List<Path> paths = new List<Path>();
        for (int i = 0; i < nodes.Count - 1; i++) {
            Path p = graphData.FindPath(nodes[i].ID, nodes[i + 1].ID);
            if (p == null) { return null; }
            //Path p = graphData.pathsSorted. .AllStreets.Paths.Find (item => item.a.ID == nodes[i].ID && item.b.ID == nodes[i + 1].ID);
            if (p == null) {
                Debug.Log("null" + nodes[i].ID + " " + nodes[i + 1].ID);
            }
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
            if (Logger.CanLogInfo) Logger.LogInfo(" FindShortestPathAsynchronous triggered from " + fromNodeID + " to " + toNodeID, true);
            StartCoroutine(FindShortestPathAsynchonousInternal(fromNodeID, toNodeID, callback));
        } else {

            if (Logger.CanLogInfo) Logger.LogInfo(" FindShortestPathSynchronous triggered from " + fromNodeID + " to " + toNodeID, true);
            callback(FindShortedPathSynchronousInternal(fromNodeID, toNodeID));
        }
    }

    public int FindNearestNode(Vector3 point) {
        float minDistance = float.MaxValue;
        Node nearestNode = null;

        foreach (var node in graphData.nodes) {
            if (Vector3.Distance(node.Position, point) < minDistance) {
                minDistance = Vector3.Distance(node.Position, point);
                nearestNode = node;
            }
        }

        return nearestNode != null ? nearestNode.ID : -1;
    }

    /*** Protected & Private ***/
    #region PRIVATE

    protected IEnumerator FindShortestPathAsynchonousInternal(int fromNodeID, int toNodeID, System.Action<List<Node>> callback) {
        if (callback == null)
            yield break;

        int startPointID = fromNodeID;
        int endPointID = toNodeID;
        bool found = false;

        graphData.ReGenerateIDs();

        Node startPoint = graphData.nodesSorted[startPointID];
        Node endPoint = graphData.nodesSorted[endPointID];

        foreach (var point in graphData.nodes) {
            point.heuristicDistance = -1;
            point.previousNode = null;
        }

        List<Node> completedPoints = new List<Node>();
        List<Node> nextPoints = new List<Node>();
        List<Node> finalPath = new List<Node>();

        startPoint.pathDistance = 0;
        startPoint.heuristicDistance = Vector3.Distance(startPoint.Position, endPoint.Position);
        nextPoints.Add(startPoint);

        while (true) {
            Node leastCostPoint = null;

            float minCost = 99999;
            foreach (var point in nextPoints) {
                if (point.heuristicDistance <= 0)
                    point.heuristicDistance = Vector3.Distance(point.Position, endPoint.Position) * 2;

                if (minCost > point.combinedHeuristic) {
                    leastCostPoint = point;
                    minCost = point.combinedHeuristic;
                }
            }

            if (leastCostPoint == null)
                break;

            if (leastCostPoint == endPoint) {
                found = true;
                Node prevPoint = leastCostPoint;
                while (prevPoint != null) {
                    finalPath.Insert(0, prevPoint);
                    prevPoint = prevPoint.previousNode;
                }

                if (Logger.CanLogInfo) {
                    if (finalPath != null) {
                        string str = "";
                        foreach (var a in finalPath) {
                            str += "=>" + a.ID.ToString();
                        }
                        Logger.LogInfo("Path found between " + fromNodeID + " and " + toNodeID + ":" + str, true);
                    }
                }
                callback(finalPath);
                yield break;
            }

            //for (Path path = graphData.getnext(); path != null; path = graphData.getnext()) {
            foreach (var path in graphData.Paths) {
                if (path.IDOfA == leastCostPoint.ID
                || path.IDOfB == leastCostPoint.ID) {
                    if (path.isOneWay) {
                        if (leastCostPoint.ID == path.IDOfB)
                            continue;
                    }

                    Node otherPoint = path.IDOfA == leastCostPoint.ID ?
                                            graphData.nodesSorted[path.IDOfB] : graphData.nodesSorted[path.IDOfA];

                    if (otherPoint.heuristicDistance <= 0)
                        otherPoint.heuristicDistance = Vector3.Distance(otherPoint.Position, endPoint.Position) + Vector3.Distance(otherPoint.Position, startPoint.Position);

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

        if (!found) {
            if (Logger.CanLogWarning) Logger.LogWarning("Path not found between " + fromNodeID + " and " + toNodeID, true);
            callback(null);
            yield break;
        }

        if (Logger.CanLogError) Logger.LogError("Unknown error while finding the path!", true);

        callback(null);
        yield break;
    }

    protected IEnumerator FindPaths(Node startPoint, Node endPoint, List<Node> callback) {
        if (callback == null)
            yield break;

        graphData.ReGenerateIDs();

        if (startPoint == null || endPoint == null) {
            yield break;
        }
        foreach (var point in graphData.nodes) {
            point.heuristicDistance = -1;
            point.previousNode = null;
        }

        List<Node> completedPoints = new List<Node>();
        List<Node> nextPoints = new List<Node>();
        List<Node> finalPath = new List<Node>();

        startPoint.pathDistance = 0;
        startPoint.heuristicDistance = Vector3.Distance(startPoint.Position, endPoint.Position);
        nextPoints.Add(startPoint);

        while (true) {
            Node leastCostPoint = null;

            float minCost = 99999;
            foreach (var point in nextPoints) {
                if (point.heuristicDistance <= 0)
                    point.heuristicDistance = Vector3.Distance(point.Position, endPoint.Position) * 2;

                if (minCost > point.combinedHeuristic) {
                    leastCostPoint = point;
                    minCost = point.combinedHeuristic;
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
                callback = finalPath;
                yield break;
            }

            //for (Path path = graphData.getnext(); path != null; path = graphData.getnext()) {
            foreach (var path in graphData.Paths) {
                if (path.a == leastCostPoint
                || path.b == leastCostPoint) {
                    if (path.isOneWay) {
                        if (leastCostPoint == path.b)
                            continue;
                    }
                    Node otherPoint = path.a == leastCostPoint ? path.b : path.a;

                    if (otherPoint.heuristicDistance <= 0)
                        otherPoint.heuristicDistance = Vector3.Distance(otherPoint.Position, endPoint.Position) + Vector3.Distance(otherPoint.Position, startPoint.Position);

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
        callback = null;
        yield break;
    }

    private List<Node> FindPathSynchronous(Node startPoint, Node endPoint) {
        foreach (var point in graphData.nodes) {
            point.heuristicDistance = -1;
            point.previousNode = null;
        }

        List<Node> completedPoints = new List<Node>();
        List<Node> nextPoints = new List<Node>();
        List<Node> finalPath = new List<Node>();

        startPoint.pathDistance = 0;
        startPoint.heuristicDistance = Vector3.Distance(startPoint.Position, endPoint.Position);
        nextPoints.Add(startPoint);

        while (true) {
            Node leastCostPoint = null;

            float minCost = 99999;
            foreach (var point in nextPoints) {
                if (point.heuristicDistance <= 0)
                    point.heuristicDistance = Vector3.Distance(point.Position, endPoint.Position) + Vector3.Distance(point.Position, startPoint.Position);

                if (minCost > point.combinedHeuristic) {
                    leastCostPoint = point;
                    minCost = point.combinedHeuristic;
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

            //for (Path path = graphData.getnext(); path != null; path = graphData.getnext()) {
            foreach (var path in graphData.Paths) {
                if (path.a == leastCostPoint
                || path.b == leastCostPoint) {

                    if (path.isOneWay) {
                        if (leastCostPoint == path.b)
                            continue;
                    }

                    Node otherPoint = path.a == leastCostPoint ?
                                            path.b : path.a;

                    if (otherPoint.heuristicDistance <= 0)
                        otherPoint.heuristicDistance = Vector3.Distance(otherPoint.Position, endPoint.Position) + Vector3.Distance(otherPoint.Position, startPoint.Position);

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


    private List<Node> FindShortedPathSynchronousInternal(int fromNodeID, int toNodeID) {
        int startPointID = fromNodeID;
        int endPointID = toNodeID;
        bool found = false;

        graphData.ReGenerateIDs();

        Node startPoint = graphData.nodesSorted[startPointID];
        Node endPoint = graphData.nodesSorted[endPointID];

        foreach (var point in graphData.nodes) {
            point.heuristicDistance = -1;
            point.previousNode = null;
        }

        List<Node> completedPoints = new List<Node>();
        List<Node> nextPoints = new List<Node>();
        List<Node> finalPath = new List<Node>();

        startPoint.pathDistance = 0;
        startPoint.heuristicDistance = Vector3.Distance(startPoint.Position, endPoint.Position);
        nextPoints.Add(startPoint);

        while (true) {
            Node leastCostPoint = null;

            float minCost = 99999;
            foreach (var point in nextPoints) {
                if (point.heuristicDistance <= 0)
                    point.heuristicDistance = Vector3.Distance(point.Position, endPoint.Position) + Vector3.Distance(point.Position, startPoint.Position);

                if (minCost > point.combinedHeuristic) {
                    leastCostPoint = point;
                    minCost = point.combinedHeuristic;
                }
            }

            if (leastCostPoint == null)
                break;

            if (leastCostPoint == endPoint) {
                found = true;
                Node prevPoint = leastCostPoint;
                while (prevPoint != null) {
                    finalPath.Insert(0, prevPoint);
                    prevPoint = prevPoint.previousNode;
                }

                if (Logger.CanLogInfo) {
                    if (finalPath != null) {
                        string str = "";
                        foreach (var a in finalPath) {
                            str += "=>" + a.ID.ToString();
                        }
                        Logger.LogInfo("Path found between " + fromNodeID + " and " + toNodeID + ":" + str, true);
                    }
                }

                return finalPath;
            }

            //for (Path path = graphData.getnext(); path != null; path = graphData.getnext()) {
            foreach (var path in graphData.Paths) {
                if (path.IDOfA == leastCostPoint.ID
                || path.IDOfB == leastCostPoint.ID) {

                    if (path.isOneWay) {
                        if (leastCostPoint.ID == path.IDOfB)
                            continue;
                    }

                    Node otherPoint = path.IDOfA == leastCostPoint.ID ?
                                            graphData.nodesSorted[path.IDOfB] : graphData.nodesSorted[path.IDOfA];

                    if (otherPoint.heuristicDistance <= 0)
                        otherPoint.heuristicDistance = Vector3.Distance(otherPoint.Position, endPoint.Position) + Vector3.Distance(otherPoint.Position, startPoint.Position);

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

        if (!found) {
            if (Logger.CanLogWarning) Logger.LogWarning("Path not found between " + fromNodeID + " and " + toNodeID, true);
            return null;
        }

        if (Logger.CanLogError) Logger.LogError("Unknown error while finding the path!", true);
        return null;
    }
    #endregion // PROTECTED
}
