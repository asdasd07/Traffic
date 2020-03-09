using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Joint {
    public List<Node> input = new List<Node>();
    public List<Node> output = new List<Node>();
    public Vector3 Position {
        get {
            if (input.Count != 0) {
                return input[0].position;
            } else if (output.Count != 0) {
                return output[0].position;
            } else {
                return Vector3.zero;
            }
        }
    }
    public Joint(List<Node> Input, List<Node> Output) {
        input = Input;
        output = Output;
    }
}

[System.Serializable]
public class Street : MonoBehaviour {
    //[HideInInspector]
    public Junction from, to;
    Vector3 fromMargin, toMargin;
    public string Name = "";
    public Joint[] joints = new Joint[2];
    public List<Path> paths = new List<Path>();
    public List<Node> nodes = new List<Node>();
    public Node center;
    [HideInInspector] public int iFrom = 2, iTo = 2;
    [HideInInspector] public int[] spawns = new int[5] { 0, 0, 0, 0, 0 };//przyjazd/dom/sklep/praca/wyjazd

    public void Init(Junction from, Junction to, int fromCount = 1, int toCount = 1) {
        iFrom = fromCount; iTo = toCount;
        this.from = from;
        this.to = to;
        Vector3 centerPos = (this.toMargin + this.fromMargin) / 2;
        this.center = new Node(centerPos);
        nodes.Add(this.center);
        Calculate();
    }
    public void Destroy(Junction spare = null) {
        if (from != spare) {
            from.RemoveStreet(this);
        }
        if (to != spare) {
            to.RemoveStreet(this);
        }
        Clear();
        DestroyImmediate(gameObject);
    }
    void Clear() {
        foreach (Path p in paths) {
            if (p.transform != null) {
                DestroyImmediate(p.transform.gameObject);
            }
        }
        paths.Clear();
        nodes.Clear();
        nodes.Add(center);
    }
    public void Resize() {
        float fBorder = from.border;
        float tBorder = to.border;
        Vector3 norm = (to.transform.position - from.transform.position).normalized;
        Vector2 p2 = Vector2.Perpendicular(new Vector2(norm.x, norm.z)).normalized;
        Vector3 perpendic = new Vector3(p2.x, 0, p2.y).normalized;
        Vector3 jPerpendic = Vector3.Project(Perpendic(from), norm) + perpendic;
        for (int i = 0; i < joints[0].input.Count; i++) {
            joints[0].input[i].position = (from.transform.position + norm * fBorder + jPerpendic * (0.4f + i * 0.6f));
        }
        for (int i = 0; i < joints[0].output.Count; i++) {
            joints[0].output[i].position = (from.transform.position + norm * fBorder - jPerpendic * (0.4f + i * 0.6f));
        }
        Vector3 jPerpendic2 = Vector3.Project(Perpendic(to), -norm) + perpendic;
        for (int i = 0; i < joints[1].input.Count; i++) {
            joints[1].input[i].position = (to.transform.position - norm * tBorder - jPerpendic2 * (0.4f + i * 0.6f));
        }
        for (int i = 0; i < joints[1].output.Count; i++) {
            joints[1].output[i].position = (to.transform.position - norm * tBorder + jPerpendic2 * (0.4f + i * 0.6f));
        }
        foreach (Path p in paths) {
            p.Visualize();
        }
    }
    Vector3 Perpendic(Junction j) {
        Vector3 perpendic = new Vector3(-(toMargin.z - fromMargin.z), 0, toMargin.x - fromMargin.x).normalized;
        int index = j.street.IndexOf(this);
        if (index == -1) {
            return perpendic;
        }
        int prev = index > 0 ? index - 1 : j.street.Count - 1;
        prev = prev < 0 ? 0 : prev;
        int next = index < j.street.Count - 1 ? index + 1 : 0;
        next = next == prev ? index : next;
        if (j.street.Count > 1) {
            Vector3 a = (j.street[prev].toMargin - j.street[prev].fromMargin).normalized;
            Vector3 b = (j.street[next].toMargin - j.street[next].fromMargin).normalized;
            //if (Vector3.Distance(toMargin, j.transform.position) < Vector3.Distance(fromMargin, j.transform.position)) {
            if (j == from) {
                a = -a;
                b = -b;
            }
            //if (Vector3.Distance(j.street[prev].toMargin, j.transform.position) > Vector3.Distance(j.street[prev].fromMargin, j.transform.position)) {
            if (j.street[prev].from == j) {
                a = -a;
            }
            //if (Vector3.Distance(j.street[next].toMargin, j.transform.position) > Vector3.Distance(j.street[next].fromMargin, j.transform.position)) {
            if (j.street[next].from == j) {
                b = -b;
            }
            Vector3 v = (a - b).normalized;
            float scale = 1f;
            if (j.street.Count == 2) {
                scale = (1f - Vector3.Angle(a, b) / 180f) * 3f;
            }
            perpendic = scale * v;
        }
        if (index == next && index != 0) {
            perpendic = -perpendic;
        }
        return perpendic;
    }
    public void Recalculate() {
        from.Calculate();
        to.Calculate();
    }

    public void Calculate() {
        Vector3 norm = (to.transform.position - from.transform.position).normalized;
        fromMargin = from.transform.position + norm * from.border;
        toMargin = to.transform.position - norm * to.border;

        Vector3 perpedic = new Vector3(-(toMargin.z - fromMargin.z), 0, toMargin.x - fromMargin.x).normalized;

        List<Node> nod1 = new List<Node>();
        List<Node> nod2 = new List<Node>();
        List<Node> nod3 = new List<Node>();
        List<Node> nod4 = new List<Node>();
        foreach (Path p in paths) {
            if (p.transform != null) {
                DestroyImmediate(p.transform.gameObject);
            }
        }
        paths.Clear();
        nodes.Clear();
        nodes.Add(center);

        center.position = ((toMargin + fromMargin) / 2 + norm * (from.border - to.border));

        Node prev = center;
        for (int i = 0; i < iFrom; i++) {
            Node n1 = new Node(fromMargin - perpedic * (0.4f + i * 0.6f));
            Node n3 = new Node(toMargin - perpedic * (0.4f + i * 0.6f));
            Node n2 = new Node(center.position - perpedic * (0.4f + i * 0.6f));
            nod1.Add(n1);
            nod2.Add(n3);
            nodes.Add(n1);
            nodes.Add(n2);
            nodes.Add(n3);
            Path[] p = new Path[4];
            p[0] = new Path(n1, n2, transform, HidePath.Shown, BlockType.Open, this);
            p[1] = new Path(n2, n3, transform, HidePath.Shown, BlockType.Open, this);
            p[2] = new Path(prev, n2, transform, HidePath.Hiden, BlockType.Open, this);
            p[3] = new Path(n2, prev, transform, HidePath.Hiden, BlockType.Open, this);
            paths.AddRange(p);
            prev = n2;
        }
        prev = center;
        for (int i = 0; i < iTo; i++) {
            Node n1 = new Node(toMargin + perpedic * (0.4f + i * 0.6f));
            Node n3 = new Node(fromMargin + perpedic * (0.4f + i * 0.6f));
            Node n2 = new Node(center.position + perpedic * (0.4f + i * 0.6f));
            nod3.Add(n1);
            nod4.Add(n3);
            nodes.Add(n1);
            nodes.Add(n2);
            nodes.Add(n3);
            Path[] p = new Path[4];
            p[0] = new Path(n1, n2, transform, HidePath.Shown, BlockType.Open, this);
            p[1] = new Path(n2, n3, transform, HidePath.Shown, BlockType.Open, this);
            p[2] = new Path(prev, n2, transform, HidePath.Hiden, BlockType.Open, this);
            p[3] = new Path(n2, prev, transform, HidePath.Hiden, BlockType.Open, this);
            paths.AddRange(p);
            prev = n2;
        }
        joints[0] = new Joint(nod4, nod1);
        joints[1] = new Joint(nod2, nod3);
    }
    public void Select(bool light = true) {
        List<MeshRenderer> mats = new List<MeshRenderer>();
        Material mat = new Material(source: paths.Find(p => p.transform != null).transform.GetComponent<MeshRenderer>().sharedMaterial);
        foreach (Path p in paths) {
            if (p.transform) {
                mats.Add(p.transform.GetComponent<MeshRenderer>());
            }
        }
        if (light) {
            mat.color = new Color(1f, 1f, 1f);
        } else {
            mat.color = new Color(0.5f, 0.5f, 0.5f);
        }
        foreach (MeshRenderer m in mats) {
            m.material = mat;
        }
    }
}
