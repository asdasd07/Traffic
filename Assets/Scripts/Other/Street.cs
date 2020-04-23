using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Przyłącze. Część ulicy połączona ze skrzyżowaniem
/// </summary>
[System.Serializable]
public class Joint {
    /// <summary>
    /// Przechowuje referencje do ulicy, której jest częścią
    /// </summary>
    public Street street;
    /// <summary>
    /// Przechowuje listę wierzchołków ścieżek wjeżdżających do skrzyżowania
    /// </summary>
    public List<Node> input = new List<Node>();
    /// <summary>
    /// Przechowuje listę wierzchołków ścieżek wyjeżdżających ze skrzyżowania
    /// </summary>
    public List<Node> output = new List<Node>();
    /// <summary>
    /// Przechowuje znormalizowany wektor od środka do przyłącza ścieżki
    /// </summary>
    public Vector3 outsideVec;
    /// <summary>
    /// Zwraca przybliżoną pozycje przyłącza 
    /// </summary>
    public Vector3 Position {
        get {
            if (input.Count != 0 && output.Count != 0) {
                return (input[0].position + output[0].position) / 2;
            }
            if (input.Count != 0) {
                return input[0].position;
            }
            if (output.Count != 0) {
                return output[0].position;
            } else {
                return Vector3.zero;
            }
        }
    }
    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="Street">Ulica do której należy</param>
    /// <param name="Type">Typ przyłącza. Prawda jeżeli przyłącze "do", fałsz jeżeli przyłącze "od"</param>
    public Joint(Street Street, bool Type) {
        street = Street;
        outsideVec = (Street.to.transform.position - Street.from.transform.position).normalized;
        outsideVec = Type == false ? -outsideVec : outsideVec;
    }
}

/// <summary>
/// Ulica
/// </summary>
[System.Serializable]
public class Street : MonoBehaviour {
    /// <summary>
    /// Przechowuje referencje do skrzyżowania "od"
    /// </summary>
    [HideInInspector] public Junction from;
    /// <summary>
    /// Przechowuje referencje do skrzyżowania "do"
    /// </summary>
    [HideInInspector] public Junction to;
    /// <summary>
    /// Przechowuje pozycje 
    /// </summary>
    Vector3 fromBorder;
    Vector3 toBorder;
    /// <summary>
    /// Przechowuje nazwę ulicy
    /// </summary>
    public string Name = "";
    /// <summary>
    /// Przechowuje listę ścieżek
    /// </summary>
    public List<Path> paths = new List<Path>();
    /// <summary>
    /// Przechowuje listę wierzchołków
    /// </summary>
    public List<Node> nodes = new List<Node>();
    /// <summary>
    /// Przechowuje środkowy wierzchołek
    /// </summary>
    public Node center;
    /// <summary>
    /// Przechowuje ilość ścieżek prowadzących "od-do"
    /// </summary>
    [HideInInspector] public int iFrom = 2;
    /// <summary>
    /// Przechowuje ilość ścieżek prowadzących "do-od"
    /// </summary>
    [HideInInspector] public int iTo = 2;
    /// <summary>
    /// Przechowuje ile samochodów danego rodzaju przyjeżdża lub wyjeżdża z tej ulicy
    /// Jest to tablica przechowująca 5 rodzajów: przyjezdnych, mieszkańców, miejsc handlowych, miejsc pracy, wyjeżdżających
    /// </summary>
    [HideInInspector] public int[] spawns = new int[5] { 0, 0, 0, 0, 0 };//przyjazd/dom/sklep/praca/wyjazd

    /// <summary>
    /// Metoda odpowiada za zainicjowanie ulicy
    /// </summary>
    /// <param name="From">Skrzyżowanie "od"</param>
    /// <param name="To">Skrzyżowanie "do"</param>
    /// <param name="fromCount">Ilość ścieżek "od-do"</param>
    /// <param name="toCount">Ilość ścieżek "do-od"</param>
    public void Init(Junction From, Junction To, int fromCount = 1, int toCount = 1) {
        iFrom = fromCount; iTo = toCount;
        this.from = From; this.to = To;
        From.AddJoint(new Joint(this, false));
        To.AddJoint(new Joint(this, true));
        Vector3 centerPos = (To.transform.position + From.transform.position) / 2;
        this.center = new Node(centerPos);
        nodes.Add(this.center);
        Calculate();
    }
    /// <summary>
    /// Metoda odpowiada za zniszczenie obiektu
    /// </summary>
    /// <param name="spare">Skrzyżowanie, które ma zachować referencje do ulicy</param>
    public void Destroy(Junction spare = null) {
        if (from != spare) {
            from.RemoveJoint(this);
        }
        if (to != spare) {
            to.RemoveJoint(this);
        }
        Clear();
        DestroyImmediate(gameObject);
    }
    /// <summary>
    /// Metoda odpowiada za usunięcie posiadanych ścieżek i wierzchołków
    /// </summary>
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
    /// <summary>
    /// Metoda odpowiada za zmianę długości ścieżek
    /// </summary>
    public void Resize() {
        Vector3 norm = (to.transform.position - from.transform.position).normalized;
        Vector2 p2 = Vector2.Perpendicular(new Vector2(norm.x, norm.z)).normalized;
        Vector3 perpendic = new Vector3(p2.x, 0, p2.y).normalized;
        Vector3 jPerpendic = Vector3.Project(Perpendic(from), norm) + perpendic;
        Joint jointFrom = from.joints.Find(item => item.street == this);
        Joint jointTo = to.joints.Find(item => item.street == this);
        for (int i = 0; i < jointFrom.input.Count; i++) {
            jointFrom.input[i].position = (from.transform.position + norm * from.margin + jPerpendic * (0.4f + i * 0.6f));
        }
        for (int i = 0; i < jointFrom.output.Count; i++) {
            jointFrom.output[i].position = (from.transform.position + norm * from.margin - jPerpendic * (0.4f + i * 0.6f));
        }
        Vector3 jPerpendic2 = Vector3.Project(Perpendic(to), -norm) + perpendic;
        for (int i = 0; i < jointTo.input.Count; i++) {
            jointTo.input[i].position = (to.transform.position - norm * to.margin - jPerpendic2 * (0.4f + i * 0.6f));
        }
        for (int i = 0; i < jointTo.output.Count; i++) {
            jointTo.output[i].position = (to.transform.position - norm * to.margin + jPerpendic2 * (0.4f + i * 0.6f));
        }
        foreach (Path p in paths) {
            p.Visualize();
        }
    }
    /// <summary>
    /// Metoda wylicza wektor, według którego będą kończyły się ścieżki doprowadzone do skrzyżowania
    /// </summary>
    /// <param name="j">Skrzyżowanie, którego wektor będzie dotyczył</param>
    /// <returns>Przeskalowany wektor prostopadły do innych ulic skrzyżowania</returns>
    Vector3 Perpendic(Junction j) {
        Vector3 perpendic = new Vector3(-(toBorder.z - fromBorder.z), 0, toBorder.x - fromBorder.x).normalized;
        int index = j.joints.FindIndex(item => item.street == this);
        if (index == -1) {
            return perpendic;
        }
        int prev = index > 0 ? index - 1 : j.joints.Count - 1;
        prev = prev < 0 ? 0 : prev;
        int next = index < j.joints.Count - 1 ? index + 1 : 0;
        next = next == prev ? index : next;
        if (j.joints.Count > 1) {
            Vector3 a = j.joints[prev].outsideVec;
            Vector3 b = j.joints[next].outsideVec;
            if (j == to) {
                a = -a;
                b = -b;
            }
            Vector3 v = (a - b).normalized;
            float scale = 1f;
            if (j.joints.Count == 2) {
                scale = (1f - Vector3.Angle(a, b) / 180f) * 3f;
            }
            perpendic = scale * v;
        }
        if (index == next && index != 0) {
            perpendic = -perpendic;
        }
        return perpendic;
    }

    /// <summary>
    /// Metoda odpowiada za przeliczenie przyłączonych skrzyżowań
    /// </summary>
    public void RecalcJunction() {
        from.Calculate();
        to.Calculate();
    }
    /// <summary>
    /// Metoda odpowiada za stworzenie wcześniej ustalonej ilości ścieżek
    /// </summary>
    public void Calculate() {
        Vector3 norm = (to.transform.position - from.transform.position).normalized;
        fromBorder = from.transform.position + norm * from.margin;
        toBorder = to.transform.position - norm * to.margin;

        Vector3 perpedic = new Vector3(-(toBorder.z - fromBorder.z), 0, toBorder.x - fromBorder.x).normalized;

        List<Node> nod1 = new List<Node>();
        List<Node> nod2 = new List<Node>();
        List<Node> nod3 = new List<Node>();
        List<Node> nod4 = new List<Node>();
        Clear();

        center.position = ((toBorder + fromBorder) / 2 + norm * (from.margin - to.margin));

        Node prev = center;
        for (int i = 0; i < iFrom; i++) {
            Node n1 = new Node(fromBorder - perpedic * (0.4f + i * 0.6f));
            Node n3 = new Node(toBorder - perpedic * (0.4f + i * 0.6f));
            Node n2 = new Node(center.position - perpedic * (0.4f + i * 0.6f));
            nod1.Add(n1);
            nod2.Add(n3);
            nodes.Add(n1);
            nodes.Add(n2);
            nodes.Add(n3);
            Path[] p = new Path[4];
            p[0] = new Path(n1, n2, transform, HidePath.Shown, BlockType.Open);
            p[1] = new Path(n2, n3, transform, HidePath.Shown, BlockType.Open);
            p[2] = new Path(prev, n2, transform, HidePath.Hiden, BlockType.Open);
            p[3] = new Path(n2, prev, transform, HidePath.Hiden, BlockType.Open);
            paths.AddRange(p);
            prev = n2;
        }
        prev = center;
        for (int i = 0; i < iTo; i++) {
            Node n1 = new Node(toBorder + perpedic * (0.4f + i * 0.6f));
            Node n3 = new Node(fromBorder + perpedic * (0.4f + i * 0.6f));
            Node n2 = new Node(center.position + perpedic * (0.4f + i * 0.6f));
            nod3.Add(n1);
            nod4.Add(n3);
            nodes.Add(n1);
            nodes.Add(n2);
            nodes.Add(n3);
            Path[] p = new Path[4];
            p[0] = new Path(n1, n2, transform, HidePath.Shown, BlockType.Open);
            p[1] = new Path(n2, n3, transform, HidePath.Shown, BlockType.Open);
            p[2] = new Path(prev, n2, transform, HidePath.Hiden, BlockType.Open);
            p[3] = new Path(n2, prev, transform, HidePath.Hiden, BlockType.Open);
            paths.AddRange(p);
            prev = n2;
        }
        Joint jointFrom = from.joints.Find(item => item.street == this);
        Joint jointTo = to.joints.Find(item => item.street == this);
        jointFrom.input = nod4;
        jointFrom.output = nod1;
        jointTo.input = nod2;
        jointTo.output = nod3;
    }
    /// <summary>
    /// Metoda odpowiada za podświetlenie ulicy jako zaznaczonej
    /// </summary>
    /// <param name="light">Prawda jeżeli zaznaczone</param>
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
