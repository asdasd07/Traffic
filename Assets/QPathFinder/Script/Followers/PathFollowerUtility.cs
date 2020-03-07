using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SearchMode {
    Simple = 0,
    Intermediate,
    Complex
}

public static class PathFinderExtensions {
    /// Finds shortest path between Nodes.
    /// Once the path is found, it will return the path as List of Positions (not Nodes, but vector3. If you need Nodes, use FindShortestPathOfNodes). 
    /// <returns> Returns list of **Positions**</returns>
    /// <param name="startNodeID">Find the path from this node</param>
    /// <param name="endNodeID">Find the path to this node</param>
    /// <param name="pathType">Path type. It can be a straight line or curved path</param>
    /// <param name="executionType">Synchronous is immediate and locks the control till path is found and returns the path. 
    /// Asynchronous type runs in coroutines without locking the control. If you have more than 50 Nodes, Asynchronous is recommended</param>
    /// <param name="OnPathFound">Callback once the path is found</param>

    public static void FindShortestPathOfPoints(this PathFinder manager, int startNodeID, int endNodeID, PathLineType pathType, Execution executionType, System.Action<List<Vector3>> OnPathFound) {
        PathFollowerUtility.FindShortestPathOfPoints_Internal(manager, startNodeID, endNodeID, pathType, executionType, OnPathFound);
    }


    /// Finds shortest path between Nodes.
    /// Once the path is found, it will return the path as List of Positions ( not Nodes, but vector3. If you need Nodes, use FindShortestPathOfNodes). 
    /// <returns> Returns list of **Positions**</returns>
    /// <param name="startNodeID">Find the path from this node</param>
    /// <param name="endNodeID">Find the path to this node</param>
    /// <param name="pathType">Path type. It can be a straight line or curved path</param>
    /// <param name="executionType">Synchronous is immediate and locks the control till path is found and returns the path. 
    /// Asynchronous type runs in coroutines with out locking the control. If you have more than 50 Nodes, Asynchronous is recommended</param>
    /// <param name="searchMode"> This is still WIP. For now, Intermediate and Complex does a tad bit more calculations to make the path even shorter</param>
    /// <param name="OnPathFound">Callback once the path is found</param>


    public static void FindShortestPathOfPoints(this PathFinder manager, Vector3 startPoint, Vector3 endPoint, PathLineType pathType, Execution executionType, SearchMode searchMode, System.Action<List<Vector3>> OnPathFound) {
        PathFollowerUtility.FindShortestPathOfPoints_Internal(manager, startPoint, endPoint, pathType, executionType, searchMode, OnPathFound);
    }
}

public static class PathFollowerUtility {
    /// <summary>
    /// Stops the gameobject while moving along the path. 
    /// </summary>
    /// <param name="transform">The gameobject which needs to stop moving</param>
    public static void StopFollowing(Transform transform) {
        Stop(transform);
    }

    internal static void FindShortestPathOfPoints_Internal(PathFinder manager, int startNodeID, int endNodeID, PathLineType pathType, Execution execution, System.Action<List<Vector3>> OnPathFound) {
        int nearestPointFromStart = startNodeID;
        int nearestPointFromEnd = endNodeID;


        if (nearestPointFromEnd == -1 || nearestPointFromStart == -1) {
            OnPathFound(null);
            return;
        }

        float startTime = Time.realtimeSinceStartup;

        void onPathOfNodesFound(List<Node> nodes) {
            if (nodes == null || nodes.Count == 0)
                OnPathFound(null);

            List<System.Object> allNodes = new List<System.Object>();
            List<Vector3> path = null;

            if (nodes != null) {
                foreach (var a in nodes) {
                    allNodes.Add(a.position);
                }
            }
            path = (pathType == PathLineType.Straight ? GetStraightPathPoints(allNodes) : GetCatmullRomCurvePathPoints(allNodes));

            OnPathFound(path);
        }

        manager.FindShortestPathOfNodes(nearestPointFromStart, nearestPointFromEnd, execution, onPathOfNodesFound);
    }

    internal static void FindShortestPathOfPoints_Internal(PathFinder manager, Vector3 startPoint, Vector3 endPoint, PathLineType pathType, Execution execution, SearchMode searchMode, System.Action<List<Vector3>> OnPathFound) {
        bool makeItMoreAccurate = searchMode == SearchMode.Intermediate || searchMode == SearchMode.Complex;
        int nearestPointFromStart = manager.FindNearestNode(startPoint);
        int nearestPointFromEnd = -1;
        if (nearestPointFromStart != -1)
            nearestPointFromEnd = manager.FindNearestNode(endPoint);

        if (nearestPointFromEnd == -1 || nearestPointFromStart == -1) {
            OnPathFound(null);
            return;
        }
        float startTime = Time.realtimeSinceStartup;
        void onPathOfNodesFound(List<Node> nodes) {
            if (nodes == null || nodes.Count == 0) {
                OnPathFound(null);
                return;
            }

            List<System.Object> allNodes = new List<System.Object>();
            if (nodes != null) {
                foreach (var a in nodes) {
                    allNodes.Add(a);
                }
            }

            if (makeItMoreAccurate) {
                if (allNodes.Count > 1) {
                    Vector3 shortestPointOnPath;
                    int nearestNode = -1;
                    Path currentPath = null;
                    int shortestPathID = -1;

                    nearestNode = ((Node)allNodes[0]).ID;
                    shortestPathID = -1;
                    currentPath = manager.graphData.GetPathBetween(GetNodeFromNodeOrVector(allNodes, 0), GetNodeFromNodeOrVector(allNodes, 1));

                    if (currentPath != null) {
                        shortestPointOnPath = GetClosestPointOnAnyPath(nearestNode, manager, startPoint, out shortestPathID);
                        if (shortestPathID == currentPath.autoGeneratedID) {
                            allNodes[0] = shortestPointOnPath;
                        } else {
                            allNodes.Insert(0, shortestPointOnPath);
                        }
                    }

                    shortestPathID = -1;
                    nearestNode = ((Node)allNodes[allNodes.Count - 1]).ID;
                    currentPath = manager.graphData.GetPathBetween(GetNodeFromNodeOrVector(allNodes, allNodes.Count - 2), GetNodeFromNodeOrVector(allNodes, allNodes.Count - 1));

                    if (currentPath != null) {
                        shortestPointOnPath = GetClosestPointOnAnyPath(nearestNode, manager, endPoint, out shortestPathID);
                        if (shortestPathID == currentPath.autoGeneratedID) {
                            allNodes[allNodes.Count - 1] = shortestPointOnPath;
                        } else {
                            allNodes.Add(shortestPointOnPath);
                        }
                    }
                }
            }
            List<Vector3> path = null;
            allNodes.Insert(0, startPoint);
            path = (pathType == PathLineType.Straight ? GetStraightPathPoints(allNodes) : GetCatmullRomCurvePathPoints(allNodes));

            OnPathFound(path);
        }
        manager.FindShortestPathOfNodes(nearestPointFromStart, nearestPointFromEnd, execution, onPathOfNodesFound);
    }

    internal static List<Vector3> GetStraightPathPoints(List<System.Object> nodePoints) {
        if (nodePoints == null)
            return null;

        List<Vector3> path = new List<Vector3>();
        if (nodePoints.Count < 2) {
            return null;
        }
        for (int i = 0; i < nodePoints.Count; i++) {
            path.Add(GetPositionFromNodeOrVector(nodePoints, i));
        }
        return path;
    }

    internal static List<Vector3> GetCatmullRomCurvePathPoints(List<System.Object> nodePoints) {
        if (nodePoints == null)
            return null;


        List<Vector3> path = new List<Vector3>();

        if (nodePoints.Count < 3) {
            for (int i = 0; i < nodePoints.Count; i++) {
                path.Add(GetPositionFromNodeOrVector(nodePoints, i));
            }
            return path;
        }

        Vector3[] catmullRomPoints = new Vector3[nodePoints.Count + 2];
        for (int i = 0; i < nodePoints.Count; i++) {
            catmullRomPoints[i + 1] = GetPositionFromNodeOrVector(nodePoints, i);
        }
        int endIndex = catmullRomPoints.Length - 1;

        catmullRomPoints[0] = catmullRomPoints[1] + (catmullRomPoints[1] - catmullRomPoints[2]) + (catmullRomPoints[3] - catmullRomPoints[2]);
        catmullRomPoints[endIndex] = catmullRomPoints[endIndex - 1] + (catmullRomPoints[endIndex - 1] - catmullRomPoints[endIndex - 2])
                                + (catmullRomPoints[endIndex - 3] - catmullRomPoints[endIndex - 2]);

        path.Add(GetPositionFromNodeOrVector(nodePoints, 0));

        for (int i = 0; i < catmullRomPoints.Length - 3; i++) {
            for (float t = 0.05f; t <= 1.0f; t += 0.05f) {
                Vector3 pt = ComputeCatmullRom(catmullRomPoints[i], catmullRomPoints[i + 1], catmullRomPoints[i + 2], catmullRomPoints[i + 3], t);
                path.Add(pt);
            }
        }

        path.Add(GetPositionFromNodeOrVector(nodePoints, nodePoints.Count - 1));
        return path;
    }
    private static void Stop(Transform transform) {
        var pathFollower = transform.GetComponent<PathFollower>();
        if (pathFollower != null) {
            pathFollower.StopFollowing(); GameObject.DestroyImmediate(pathFollower);
        }
    }

    private static Vector3 ComputeCatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t) {
        float t2 = t * t;
        float t3 = t2 * t;

        Vector3 pt = 0.5f * ((-p0 + 3f * p1 - 3f * p2 + p3) * t3
                    + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2
                    + (-p0 + p2) * t
                    + 2f * p1);

        return pt;
    }
    private static Vector3 GetClosestPointOnAnyPath(int nodeID, PathFinder manager, Vector3 pos, out int pathID) {
        Node node = manager.graphData.nodes[nodeID];
        Vector3 vClosestPoint = node.position;
        float fClosestDist = (pos - node.position).sqrMagnitude;
        pathID = -1;

        foreach (Path pd in manager.graphData.paths) {
            if (pd.IDOfA == node.ID || pd.IDOfB == node.ID) {
                Vector3 vPos = ComputeClosestPointFromPointToLine(pos, manager.graphData.nodes[pd.IDOfA].position, manager.graphData.nodes[pd.IDOfB].position, out bool isOnExtremities);
                float fDist = (vPos - pos).sqrMagnitude;

                if (fDist < fClosestDist) {
                    fClosestDist = fDist;
                    vClosestPoint = vPos;
                    pathID = pd.autoGeneratedID;
                }
            }
        }
        return vClosestPoint;
    }

    private static Vector3 ComputeClosestPointFromPointToLine(Vector3 vPt, Vector3 vLinePt0, Vector3 vLinePt1, out bool isOnExtremities) {
        float t = -Vector3.Dot(vPt - vLinePt0, vLinePt1 - vLinePt0) / Vector3.Dot(vLinePt0 - vLinePt1, vLinePt1 - vLinePt0);

        Vector3 vClosestPt;

        if (t < 0f) {
            vClosestPt = vLinePt0;
            isOnExtremities = true;
        } else if (t > 1f) {
            vClosestPt = vLinePt1;
            isOnExtremities = true;
        } else {
            vClosestPt = vLinePt0 + t * (vLinePt1 - vLinePt0);
            isOnExtremities = false;
        }
        return vClosestPt;
    }

    static System.Func<List<System.Object>, int, Vector3> GetPositionFromNodeOrVector =
        delegate (List<System.Object> list, int index) {
            return (list[index] is Vector3 ? (Vector3)list[index] : ((Node)list[index]).position);
        };
    static System.Func<List<System.Object>, int, Node> GetNodeFromNodeOrVector =
        delegate (List<System.Object> list, int index) {
            return (list[index] is Node ? (Node)list[index] : null);
        };
}
