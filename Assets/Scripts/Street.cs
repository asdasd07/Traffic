using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Joint {
    public List<Node> input = new List<Node>();
    public List<Node> output = new List<Node>();
    public Vector3 Pos {
        get {
            if (input.Count != 0) {
                return input[0].Position;
            } else if (output.Count != 0) {
                return output[0].Position;
            } else {
                return Vector3.zero;
            }
        }
    }
    public Joint(List<Node> inp, List<Node> outp) {
        input = inp;
        output = outp;
    }
}

[System.Serializable]
public class Street : MonoBehaviour {
    [HideInInspector]
    public Junction f, t;
    public Vector3 from, to;
    public Node center;
    public string Name = "";
    public Joint[] joints = new Joint[2];
    public List<Path> paths = new List<Path>();
    public List<Node> nodes = new List<Node>();
    public int ifrom = 2, ito = 2;
    [HideInInspector]
    public int[] Spawns = new int[5] { 0, 0, 0, 0, 0 };//przyjazd/dom/sklep/praca/wyjazd

    public void Destroy(Junction spare = null) {
        if (f != spare) {
            f.RemoveStreet(this);
        }
        if (t != spare) {
            t.RemoveStreet(this);
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

    public void Init(Junction from, Junction to, int fNum = 1, int tNum = 1) {
        ifrom = fNum; ito = tNum;
        f = from;
        t = to;
        Vector3 norm = t.transform.position - f.transform.position;
        norm.Normalize();
        this.from = f.transform.position + norm * f.max;
        this.to = t.transform.position - norm * t.max;
        Vector3 center = (this.to + this.from) / 2;
        this.center = new Node(center);
        nodes.Add(this.center);
        Calculate();
    }
    public void Resize() {
        float fmax = f.max;
        float tmax = t.max;
        Vector3 norm = t.transform.position - f.transform.position;
        norm.Normalize();
        Vector2 p2 = Vector2.Perpendicular(new Vector2(norm.x, norm.z)).normalized;
        Vector3 perpedic = new Vector3(p2.x, 0, p2.y).normalized;
        Vector3 jPerpedic = Vector3.Project(Perpedic(f), norm) + perpedic;
        Vector3 jPerpedic2 = Vector3.Project(Perpedic(t), -norm) + perpedic;
        for (int i = 0; i < joints[0].input.Count; i++) {
            joints[0].input[i].SetPosition(f.transform.position + norm * fmax + jPerpedic * (0.4f + i * 0.6f));
        }
        for (int i = 0; i < joints[0].output.Count; i++) {
            joints[0].output[i].SetPosition(f.transform.position + norm * fmax - jPerpedic * (0.4f + i * 0.6f));
        }
        for (int i = 0; i < joints[1].input.Count; i++) {
            joints[1].input[i].SetPosition(t.transform.position - norm * tmax - jPerpedic2 * (0.4f + i * 0.6f));
        }
        for (int i = 0; i < joints[1].output.Count; i++) {
            joints[1].output[i].SetPosition(t.transform.position - norm * tmax + jPerpedic2 * (0.4f + i * 0.6f));
        }
        foreach (Path p in paths) {
            p.Visualize();
        }
    }
    Vector3 Perpedic(Junction j) {
        Vector3 perpedic = new Vector3(-(to.z - from.z), 0, to.x - from.x);
        perpedic.Normalize();
        int index = j.street.IndexOf(this);
        if (index == -1) {
            return perpedic;
        }
        int prev = index > 0 ? index - 1 : j.street.Count - 1;
        prev = prev < 0 ? 0 : prev;
        int next = index < j.street.Count - 1 ? index + 1 : 0;
        next = next == prev ? index : next;
        if (j.street.Count > 1) {
            Vector3 a = j.street[prev].to - j.street[prev].from;
            a.Normalize();
            Vector3 b = j.street[next].to - j.street[next].from;
            b.Normalize();
            if (Vector3.Distance(to, j.transform.position) < Vector3.Distance(from, j.transform.position)) {
                a = -a;
                b = -b;
            }
            if (Vector3.Distance(j.street[prev].to, j.transform.position) > Vector3.Distance(j.street[prev].from, j.transform.position)) {
                a = -a;
            }
            if (Vector3.Distance(j.street[next].to, j.transform.position) > Vector3.Distance(j.street[next].from, j.transform.position)) {
                b = -b;
            }
            Vector3 v = a - b;
            v.Normalize();
            float scale = 1f;
            if (j.street.Count == 2) {
                scale = (1f - Vector3.Angle(a, b) / 180f) * 3f;
            }
            perpedic = scale * v;
        }
        if (index == next && index != 0) {
            perpedic = -perpedic;
        }
        return perpedic;
    }
    public void Recalculate() {
        f.Calculate();
        t.Calculate();
    }

    public void Calculate() {
        Vector3 perpedic = new Vector3(-(to.z - from.z), 0, to.x - from.x);
        perpedic.Normalize();

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

        Vector3 norm = t.transform.position - f.transform.position;
        norm.Normalize();
        center.SetPosition((to + from) / 2 + norm * (f.max - t.max));

        Node prev = center;
        for (int i = 0; i < ifrom; i++) {
            Node n1 = new Node(from - perpedic * (0.4f + i * 0.6f));
            Node n3 = new Node(to - perpedic * (0.4f + i * 0.6f));
            Node n2 = new Node(center.Position - perpedic * (0.4f + i * 0.6f));
            nod1.Add(n1);
            nod2.Add(n3);
            nodes.Add(n1);
            nodes.Add(n2);
            nodes.Add(n3);
            Path[] p = new Path[4];
            p[0] = new Path(n1, n2, transform, 0, 2, this);
            p[1] = new Path(n2, n3, transform, 0, 2, this);
            p[2] = new Path(prev, n2, transform, 2, 2, this);
            p[3] = new Path(n2, prev, transform, 2, 2, this);
            paths.AddRange(p);
            prev = n2;
        }
        prev = center;
        for (int i = 0; i < ito; i++) {
            Node n1 = new Node(to + perpedic * (0.4f + i * 0.6f));
            Node n3 = new Node(from + perpedic * (0.4f + i * 0.6f));
            Node n2 = new Node(center.Position + perpedic * (0.4f + i * 0.6f));
            nod3.Add(n1);
            nod4.Add(n3);
            nodes.Add(n1);
            nodes.Add(n2);
            nodes.Add(n3);
            Path[] p = new Path[4];
            p[0] = new Path(n1, n2, transform, 0, 2, this);
            p[1] = new Path(n2, n3, transform, 0, 2, this);
            p[2] = new Path(prev, n2, transform, 2, 2, this);
            p[3] = new Path(n2, prev, transform, 2, 2, this);
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
