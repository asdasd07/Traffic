﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BlockType {
    Blocked = 0,
    Priority = 1,
    Open = 2
};
public enum HidePath {
    Shown = 0,
    Internal = 1,
    Hiden = 2
};
// Path is a connection between 2 Nodes. It will have zero cost by default unless specified in inspector. 
// A path can be a oneway too. 
[System.Serializable]
public class Path {
    [SerializeField] [HideInInspector] public Node a, b;
    List<float> queueTimes = new List<float>();
    [SerializeField] float cost;
    public int leftQueue = 0, entireQueue = 0, maxInQueue, autoGeneratedID;
    public BlockType priori, block = BlockType.Open;
    public HidePath hide;
    public Street street;
    [HideInInspector] public Transform transform;

    public int IDOfA => a.ID;
    public int IDOfB => b.ID;
    public int CurrentQueue => entireQueue - leftQueue;
    public float Cost => CurrentQueue + cost;
    public float SumaryWaitingTime {
        get {
            float sum = 0;
            if (queueTimes == null) {
                queueTimes = new List<float>();
            }
            foreach (float f in queueTimes) {
                sum += Time.time - f;
            }
            return sum;
        }
    }

    public Path(Node aa, Node bb, Transform parent, HidePath hid = HidePath.Shown, BlockType prioritet = BlockType.Open, Street str = null) {
        queueTimes = new List<float>();
        hide = hid;
        street = str;
        priori = prioritet;
        a = aa; b = bb;
        if (hide < HidePath.Hiden) {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); transform = go.transform;
            transform.parent = parent;
            go.GetComponent<Renderer>().material = Resources.Load<Material>("street") as Material;
        }
        Visualize();
    }

    public void Visualize() {
        cost = hide == 0 ? Vector3.Distance(a.position, b.position) : 0;
        maxInQueue = (int)Mathf.Floor(cost - 0.5f);
        maxInQueue = maxInQueue < 1 ? 1 : maxInQueue;
        if (transform != null) {
            transform.position = (a.position + b.position) / 2f;
            transform.LookAt(b.position);
            transform.localScale = new Vector3(0.6f, 0.1f, Vector3.Distance(transform.position, b.position) * 2);
        }
    }
    public int EnterQueue() {
        entireQueue++;
        if (queueTimes == null) {
            queueTimes = new List<float>();
        }
        queueTimes.Add(Time.time);
        return entireQueue - 1;
    }
    public void LeaveQueue() {
        queueTimes.RemoveAt(0);
        ++leftQueue;
    }
    public bool CanEnter(BlockType prio) {
        if (block >= prio && maxInQueue > entireQueue - leftQueue) {
            return true;
        }
        return false;
    }
}