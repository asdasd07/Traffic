using System;
using System.Collections.Generic;
using UnityEngine;

public class MouseClickDemo : MonoBehaviour {
    public new Camera camera;   // Needed for mouse click to world position convertion. 
    public float playerSpeed = 20.0f;
    public GameObject playerObj;


    // For PathFollowerWithGroundSnap - This will snap the player to the ground while it follows the path. 
    public float playerFloatOffset;     // This is how high the player floats above the ground. 
    public float raycastOriginOffset;   // This is how high above the player u want to raycast to ground. 
    public int raycastDistanceFromOrigin = 40;   // This is how high above the player u want to raycast to ground. 
    public bool thoroughPathFinding = false;    // uses few extra steps in pathfinding to find accurate result. 


    void Update() {
        if (Input.GetMouseButtonUp(0)) {
            MovePlayerToMousePosition();
        }
    }

    void MovePlayerToMousePosition() {
        //Debug.LogError(PathFinder.instance.graphData.groundColliderLayerName + " " + LayerMask.NameToLayer( PathFinder.instance.graphData.groundColliderLayerName ));
        LayerMask backgroundLayerMask = 1 << LayerMask.NameToLayer(PathFinder.instance.graphData.groundColliderLayerName);

        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        Vector3 hitPos = Vector3.zero;
        if (Physics.Raycast(ray, out RaycastHit hit, 10000f, backgroundLayerMask)) {
            hitPos = hit.point;
        } else {
            Debug.LogError("ERROR!");
            return;
        }


        PathFinder.instance.FindShortestPathOfPoints(playerObj.transform.position, hitPos, PathFinder.instance.graphData.lineType,
            Execution.Asynchronously,
            thoroughPathFinding ? SearchMode.Complex : SearchMode.Simple,
            delegate (List<Vector3> points) {
                PathFollowerUtility.StopFollowing(playerObj.transform);
                FollowThePathNormally(points);
            }
         );

    }

    void FollowThePathNormally(List<Vector3> nodes) {
        PathFollowerUtility.FollowPath(playerObj.transform, nodes, playerSpeed);
    }
}
