using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour {

    // Use this for initialization
    void Start() {
        List<Node> ino = new List<Node>();
        StartCoroutine(elo(ino));
        Debug.Log(ino.Count);

    }

    protected IEnumerator elo(List<Node> iii) {
        yield return null;
    }
}
