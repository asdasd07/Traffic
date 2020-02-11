using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Joint {
    public List<Node> input = new List<Node>();
    public List<Node> output = new List<Node>();
    public Vector3 pos;
    public Joint(List<Node> inp, List<Node> outp) {
        input = inp;
        output = outp;
        if (inp.Count == 0) {
            pos = outp[0].Position;
        } else {
            pos = inp[0].Position;
        }
    }
}

[System.Serializable]
public class Street : MonoBehaviour {
    [HideInInspector]
    public Junction f, t;
    public Vector3 from, to;
    public Node spawn, target;
    public Joint[] joints = new Joint[2];
    public List<Path> paths = new List<Path>();
    public List<Node> nodes = new List<Node>();
    public int ifrom = 2, ito = 2;
    float lastOfset0, lastOfset1;

    public void Destroy() {
        Clear();
    }
    void Clear() {
        foreach (Path p in paths) {
            if (p.tr != null) {
                DestroyImmediate(p.tr.gameObject);
            }
        }
        paths.Clear();
    }

    public void Init(Junction from, Junction to, int fNum = 1, int tNum = 1) {
        ifrom = fNum; ito = tNum;
        f = from;
        t = to;
        Vector3 norm = t.transform.position - f.transform.position;
        norm.Normalize();
        lastOfset0 = f.max;
        lastOfset1 = t.max;
        this.from = f.transform.position + norm * lastOfset0;
        this.to = t.transform.position - norm * lastOfset1;
        Vector3 center = (this.to + this.from) / 2;
        spawn = new Node(center);
        target = new Node(center);
        nodes.Add(spawn);
        nodes.Add(target);
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
            joints[0].input[i].SetPosition(f.transform.position + norm * fmax + jPerpedic * (0.6f + i));
        }
        for (int i = 0; i < joints[0].output.Count; i++) {
            joints[0].output[i].SetPosition(f.transform.position + norm * fmax - jPerpedic * (0.6f + i));
        }
        for (int i = 0; i < joints[1].input.Count; i++) {
            joints[1].input[i].SetPosition(t.transform.position - norm * tmax - jPerpedic2 * (0.6f + i));
        }
        for (int i = 0; i < joints[1].output.Count; i++) {
            joints[1].output[i].SetPosition(t.transform.position - norm * tmax + jPerpedic2 * (0.6f + i));
        }
        foreach (Path p in paths) {
            p.Visualize();
        }
    }
    Vector3 Perpedic(Junction j) {
        Vector3 perpedic = new Vector3(-(to.z - from.z), 0, to.x - from.x);
        perpedic.Normalize();
        //Vector2 perpedic = Vector2.Perpendicular(to-from).normalized;
        int index = j.street.IndexOf(this);
        if (index == -1) {
            return perpedic;
        }
        int prev = index > 0 ? index - 1 : j.street.Count - 1;
        prev = prev < 0 ? 0 : prev;
        int next = index < j.street.Count - 1 ? index + 1 : 0;
        next = next == prev ? index : next;
        if (prev != next) {
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
            //perpedic = new Vector3(Mathf.Abs(v.x) < Mathf.Abs(perpedic.x) ? perpedic.x : v.x, 0, Mathf.Abs(v.z) < Mathf.Abs(perpedic.z) ? perpedic.z : v.z);
            //perpedic = v * (Mathf.Abs( Vector3.Cross(v, perpedic).y) + 1);
            //perpedic = Vector3.Project(perpedic, v);
            //perpedic = new Vector3(v.x / v.z * perpedic.z, 0, v.z / v.x * perpedic.x);
            perpedic = v;
        }
        if (index == next && index != 0) {
            perpedic = -perpedic;
        }
        return perpedic;
    }
    public void Recalculate() {
        Calculate();
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
            if (p.tr != null) {
                DestroyImmediate(p.tr.gameObject);
            }
        }
        paths.Clear();

        for (int i = 0; i < ifrom; i++) {
            Node n1 = new Node(from - perpedic * 0.6f - perpedic * i);
            Node n3 = new Node(to - perpedic * 0.6f - perpedic * i);
            Node n2 = new Node((n1.Position + n3.Position) / 2);
            nod1.Add(n1);
            nod2.Add(n3);
            nodes.Add(n1);
            nodes.Add(n2);
            nodes.Add(n3);
            Path[] p = new Path[4];
            p[0] = new Path(n1, n2, transform);
            p[1] = new Path(n2, n3, transform);
            p[2] = new Path(spawn, n2, transform, true);
            p[3] = new Path(n2, target, transform, true);
            paths.AddRange(p);
        }
        for (int i = 0; i < ito; i++) {
            Node n1 = new Node(to + perpedic * 0.6f + perpedic * i);
            Node n3 = new Node(from + perpedic * 0.6f + perpedic * i);
            Node n2 = new Node((n1.Position + n3.Position) / 2);
            nod3.Add(n1);
            nod4.Add(n3);
            nodes.Add(n1);
            nodes.Add(n2);
            nodes.Add(n3);
            Path[] p = new Path[4];
            p[0] = new Path(n1, n2, transform);
            p[1] = new Path(n2, n3, transform);
            p[2] = new Path(spawn, n2, transform, true);
            p[3] = new Path(n2, target, transform, true);
            paths.AddRange(p);
        }
        joints[0] = new Joint(nod4, nod1);
        joints[1] = new Joint(nod2, nod3);
    }
    public void Select(bool light = true) {
        List<MeshRenderer> mats = new List<MeshRenderer>();
        Material mat = new Material(source: paths.Find(p => p.tr != null).tr.GetComponent<MeshRenderer>().sharedMaterial);
        foreach (Path p in paths) {
            if (p.tr) {
                mats.Add(p.tr.GetComponent<MeshRenderer>());
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
