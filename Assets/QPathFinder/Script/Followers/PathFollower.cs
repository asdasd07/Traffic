using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathFollower : MonoBehaviour {
    protected List<Vector3> pointsToFollow;

    public float moveSpeed = 10f;
    public float rotateSpeed = 10f;

    protected int _currentIndex;
    Coroutine rou;

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
        int index = 0;
        int end = path.Count;
        float accelerate = 0, dist, velocity = 0, tar = 0, tmp = 0;
        //Rigidbody rb = GetComponent<Rigidbody>();
        transform.position = path[0].a.Position;
        Vector3 dir = path[0].a.Position - path[0].b.Position;
        dir.Normalize();
        while (path[index + 1].CanEnter(2) == false) {
            yield return null;
        }
        index++;
        Vector3 target = path[index].a.Position;
        Vector3? target2 = null;
        transform.position = path[index].a.Position;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(path[index].b.Position - transform.position), 400f);
        QueuePos = path[index].EnterQueue();
        dir = path[index].a.Position - path[index].b.Position;
        dir.Normalize();
        while (index < end - 1) {
            dist = Vector3.Distance(transform.position, target);
            if (target2 != null) {
                accelerate = 1;
            } else {
                accelerate = Mathf.Clamp(dist - 0.1f, 0, 1) * 2;
            }
            //velocity += accelerate * Time.deltaTime;
            //velocity = Mathf.Clamp(velocity, 0f, 1f);
            //velocity = Mathf.Clamp(dist, 0f, 2f) * Time.deltaTime*50;
            if (dist < 0.2f) {
                tar = 0.2f;
            } else if (dist < 1f) {
                tar = 1f;
            } else if (dist < 1.5f) {
                tar = 1f;
            } else {
                tar = 3;
            }
            velocity = Mathf.SmoothDamp(velocity, tar, ref tmp, 0.4f);
            if (dist < 0.1f) {
                if (target2 != null) {
                    target = (Vector3)target2;
                    target2 = null;
                } else {
                    Vector3 back = path[index].maxInQueue > 1 ? dir * (QueuePos - path[index].sQueue + 0.6f) : Vector3.zero;
                    target = path[index].b.Position + back;
                    if (path[index].sQueue == QueuePos && path[index + 1].CanEnter(path[index].priori)) {
                        target = path[index].b.Position;
                        path[index].LeaveQueue();
                        index++;
                        QueuePos = path[index].EnterQueue();
                        dir = path[index].a.Position - path[index].b.Position;
                        dir.Normalize();
                        back = path[index].maxInQueue > 1 ? dir * (QueuePos - path[index].sQueue + 0.6f) : Vector3.zero;
                        target2 = path[index].b.Position + back;
                    }
                }
            } else {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(target - transform.position), 300f * Time.deltaTime);
                transform.position = transform.position + transform.forward * velocity * Time.deltaTime * 1;
                //velocity += Time.deltaTime;
                //transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.smoothDeltaTime);
            }
            yield return null;
        }
        path[index].LeaveQueue();
        Destroy(gameObject);
    }

    public void StopFollowing() { StopAllCoroutines(); }

    IEnumerator FollowPath() {
        yield return null;
        if (Logger.CanLogInfo) Logger.LogInfo(string.Format("[{0}] Follow(), Speed:{1}", name, moveSpeed));

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

        if (Logger.CanLogInfo) Logger.LogInfo("PathFollower completed!");
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
        return pointIndex == EndIndex();
    }

    int StartIndex() {
        return 0;
    }

    public virtual Vector3 ConvertPointIfNeeded(Vector3 point) {
        return point;
    }

    int EndIndex() {
        return pointsToFollow.Count - 1;
    }

    int GetNextIndex(int currentIndex) {
        int nextIndex = -1;
        if (currentIndex < EndIndex())
            nextIndex = currentIndex + 1;

        return nextIndex;
    }

}
