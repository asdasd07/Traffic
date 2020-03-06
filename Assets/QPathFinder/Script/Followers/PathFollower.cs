using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathFollower : MonoBehaviour {
    protected List<Vector3> pointsToFollow;

    public float moveSpeed = 10f;
    public float rotateSpeed = 10f;
    public float velocity = 0;
    public List<Path> ReturningPath;
    public float ReturningDelay;
    public int ReturningType = -1;
    float waitingTime = 0;
    MeshRenderer mesh;
    Material mat;

    protected int _currentIndex;
    Coroutine rou;
    private void Awake() {
        mesh = GetComponent<MeshRenderer>();
        mesh.enabled = false;
        mat = new Material(mesh.sharedMaterial) {
            color = new Color(1f, 0f, 0f)
        };
        mesh.material = mat;
    }

    public void Follow(List<Vector3> pointsToFollow, float moveSpeed) {
        this.pointsToFollow = pointsToFollow;
        this.moveSpeed = moveSpeed;

        StopFollowing();
        _currentIndex = 0;
        StartCoroutine(FollowPath());
    }

    public void Follow(List<Path> path) {
        if (rou != null) {
            StopCoroutine(rou);
        }
        rou = StartCoroutine(FollowRoutine(path));
    }

    IEnumerator FollowRoutine(List<Path> path) {
        if (path == null || path.Count < 1) {
            Debug.Log("path empty");
            yield break;
        }
        int QueuePos;
        Color[] gradient = { Color.white, Color.green, Color.blue, Color.red };
        int index = 0;
        int end = path.Count;
        float dist, dist1, colo = 0;
        bool endpoint = false;
        transform.position = path[0].a.Position;
        Vector3 dir = path[0].a.Position - path[0].b.Position;
        dir.Normalize();
        while (path[index + 1].CanEnter(2) == false) {
            yield return null;
        }
        mesh.enabled = true;
        index++;
        Vector3 target;
        float back;
        Vector3? target2 = null;
        transform.position = path[index].a.Position;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(path[index].b.Position - transform.position), 400f);
        QueuePos = path[index].EnterQueue();
        dir = path[index].a.Position - path[index].b.Position;
        dir.Normalize();
        float padding = 0.4f;
        back = path[index].maxInQueue > 1 ? (QueuePos - path[index].sQueue + 1f) : padding;
        target = path[index].b.Position + dir * back;
        while (index < end) {
            dist = Vector3.Distance(transform.position, target);
            dist1 = Vector3.Distance(transform.position, path[index].b.Position);
            float angle = dist1 > 0.2f ? Quaternion.Angle(transform.rotation, Quaternion.LookRotation(target - transform.position)) : Quaternion.Angle(transform.rotation, Quaternion.LookRotation(target2 ?? target - transform.position));
            float tar;

            if (dist < 0.5f && !endpoint) {
                tar = 0.1f;
            } else {
                //y = 0.992881 + 0.18181*x - 0.4129524*x^2 + 0.4111319*x^3
                tar = 1f + 0.2f * dist - 0.4f * Mathf.Pow(dist, 2) + 0.4f * Mathf.Pow(dist, 3);
                tar = tar > 3 ? 3 : tar;
            }
            if (dist >= 0.4f) {
                tar = angle < 3 ? tar : tar / (angle / 3);
            }
            velocity = Mathf.SmoothDamp(velocity, tar, ref colo, 0.3f);

            //centerpoint reached, index++, endpoint false
            if (endpoint && dist1 < padding) {
                endpoint = false;
                index++;
                if (index + 1 == end) {
                    break;
                }
                dir = path[index].a.Position - path[index].b.Position;
                dir.Normalize();
                if (index + 1 != end) {
                    target2 = path[index + 1].b.Position;
                }
                back = path[index].maxInQueue > 1 ? (QueuePos - path[index].sQueue + 1f) : padding;
            }

            //midpoint reached, recalculate next midpoint
            if (dist < 0.5f) {
                float prop = path[index].maxInQueue > 1 ? (QueuePos - path[index].sQueue + 1f) : padding;
                back = back > prop ? prop : back;
                waitingTime += Time.deltaTime;
                //canEnter, go to centerpoint, endpoint true
                if (!endpoint && dist1 < 1.3f && path[index].sQueue == QueuePos && path[index + 1].CanEnter(path[index].priori)) {
                    endpoint = true;
                    path[index].LeaveQueue();
                    QueuePos = path[index + 1].EnterQueue();
                    waitingTime = 0;
                    //back = path[index].maxInQueue > 1 ? (QueuePos - path[index].sQueue + 1f) : padding;
                    dist = Vector3.Distance(transform.position, target);
                    back = 0;
                }
            }
            target = path[index].b.Position + dir * back;
            if (dist >= 0.1f) {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(target - transform.position), 100f * Time.deltaTime);
                transform.position = transform.position + transform.forward * velocity * Time.deltaTime * 1;
            }
            float waitingScale = waitingTime / 10;
            int integer = Mathf.Clamp(Mathf.FloorToInt(waitingScale), 0, 2);
            mat.color = Color.Lerp(gradient[integer], gradient[integer + 1], waitingScale - integer);
            mesh.material = mat;
            yield return null;
        }
        path[index].LeaveQueue();
        if (ReturningType == -1) {
            Destroy(gameObject);
        } else {
            mesh.enabled = false;
            yield return new WaitForSeconds(ReturningDelay);
            mesh.enabled = true;
            if (ReturningPath[0].street) {
                ReturningPath[0].street.Spawns[ReturningType]--;
            }
            ReturningType = -1;
            rou = StartCoroutine(FollowRoutine(ReturningPath));
        }
    }

    public void StopFollowing() { StopAllCoroutines(); }

    IEnumerator FollowPath() {
        yield return null;

        while (true) {
            _currentIndex = Mathf.Clamp(_currentIndex, 0, pointsToFollow.Count - 1);

            if (IsOnPoint(_currentIndex)) {
                if (IsEndPoint(_currentIndex)) break;
                else _currentIndex = GetNextIndex(_currentIndex);
            } else {
                MoveTo(_currentIndex);
            }
            yield return null;
        }

    }

    public virtual void MoveTo(int pointIndex) {
        var targetPos = pointsToFollow[pointIndex];

        var deltaPos = targetPos - transform.position;
        //deltaPos.z = 0f;
        transform.up = Vector3.up;
        transform.forward = deltaPos.normalized;

        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.smoothDeltaTime);
    }

    protected virtual bool IsOnPoint(int pointIndex) { return (transform.position - pointsToFollow[pointIndex]).sqrMagnitude < 0.1f; }

    bool IsEndPoint(int pointIndex) {
        return pointIndex == pointsToFollow.Count - 1;
    }

    int GetNextIndex(int currentIndex) {
        int nextIndex = -1;
        if (currentIndex < pointsToFollow.Count - 1)
            nextIndex = currentIndex + 1;

        return nextIndex;
    }

}
