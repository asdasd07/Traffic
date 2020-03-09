﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// QPathFinder modified
/// <summary>
/// 
/// </summary>
public class PathFollower : MonoBehaviour {
    public float velocity = 0;
    List<Path> returningPath;
    float returningDelay;
    int returningType = -1;
    float waitingTime = 0;
    MeshRenderer mesh;
    Material material;

    protected int _currentIndex;
    Coroutine routine;
    private void Awake() {
        mesh = GetComponent<MeshRenderer>();
        mesh.enabled = false;
        material = new Material(mesh.sharedMaterial) {
            color = new Color(1f, 0f, 0f)
        };
        mesh.material = material;
    }

    public void Follow(List<Path> path, int ReturningType, float WaitingTime, List<Path> ReturningPath) {
        returningType = ReturningType;
        returningDelay = WaitingTime;
        returningPath = ReturningPath;
        Follow(path);
    }
    public void Follow(List<Path> path) {
        if (routine != null) {
            StopCoroutine(routine);
        }
        routine = StartCoroutine(FollowRoutine(path));
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
        transform.position = path[0].a.position;
        Vector3 dir = path[0].a.position - path[0].b.position;
        dir.Normalize();
        while (path[index + 1].CanEnter(BlockType.Open) == false) {
            yield return null;
        }
        mesh.enabled = true;
        index++;
        Vector3 target;
        float back;
        Vector3? target2 = null;
        transform.position = path[index].a.position;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(path[index].b.position - transform.position), 400f);
        QueuePos = path[index].EnterQueue();
        dir = path[index].a.position - path[index].b.position;
        dir.Normalize();
        float padding = 0.4f;
        back = path[index].maxInQueue > 1 ? (QueuePos - path[index].leftQueue + 1f) : padding;
        target = path[index].b.position + dir * back;
        while (index < end) {
            dist = Vector3.Distance(transform.position, target);
            dist1 = Vector3.Distance(transform.position, path[index].b.position);
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
                dir = path[index].a.position - path[index].b.position;
                dir.Normalize();
                if (index + 1 != end) {
                    target2 = path[index + 1].b.position;
                }
                back = path[index].maxInQueue > 1 ? (QueuePos - path[index].leftQueue + 1f) : padding;
            }

            //midpoint reached, recalculate next midpoint
            if (dist < 0.5f) {
                float prop = path[index].maxInQueue > 1 ? (QueuePos - path[index].leftQueue + 1f) : padding;
                back = back > prop ? prop : back;
                waitingTime += Time.deltaTime;
                //canEnter, go to centerpoint, endpoint true
                if (!endpoint && dist1 < 1.3f && path[index].leftQueue == QueuePos && path[index + 1].CanEnter(path[index].priori)) {
                    endpoint = true;
                    path[index].LeaveQueue();
                    QueuePos = path[index + 1].EnterQueue();
                    waitingTime = 0;
                    //back = path[index].maxInQueue > 1 ? (QueuePos - path[index].sQueue + 1f) : padding;
                    dist = Vector3.Distance(transform.position, target);
                    back = 0;
                }
            }
            target = path[index].b.position + dir * back;
            if (dist >= 0.1f) {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(target - transform.position), 100f * Time.deltaTime);
                transform.position = transform.position + transform.forward * velocity * Time.deltaTime * 1;
            }
            float waitingScale = waitingTime / 10;
            int integer = Mathf.Clamp(Mathf.FloorToInt(waitingScale), 0, 2);
            material.color = Color.Lerp(gradient[integer], gradient[integer + 1], waitingScale - integer);
            mesh.material = material;
            yield return null;
        }
        path[index].LeaveQueue();
        if (returningType == -1) {
            Destroy(gameObject);
        } else {
            mesh.enabled = false;
            yield return new WaitForSeconds(returningDelay);
            mesh.enabled = true;
            if (returningPath[0].street) {
                returningPath[0].street.spawns[returningType]--;
            }
            returningType = -1;
            routine = StartCoroutine(FollowRoutine(returningPath));
        }
    }

    public void StopFollowing() { StopAllCoroutines(); }


}
