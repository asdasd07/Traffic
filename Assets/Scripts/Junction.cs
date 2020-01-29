using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class Phase {
    public List<int> Routes = new List<int>();
    public Phase() { }
    public Phase(Dictionary<int, string> dic, int mode, int i) {
        if (mode == 5) {
            switch (i) {
                case 0:
                    Routes = (from x in dic where x.Value == "02" || x.Value == "01" || x.Value == "21" select x.Key).ToList();
                    break;
                case 1:
                    Routes = (from x in dic where x.Value == "12" || x.Value == "20" || x.Value == "21" select x.Key).ToList();
                    break;
                case 2:
                    Routes = (from x in dic where x.Value == "12" || x.Value == "10" || x.Value == "02" select x.Key).ToList();
                    break;
            }
        } else {
            #region coment
            //if (mode % 2 == 0) {//faza I1
            //    //0
            //    Routes = (from x in dic where x.Value == "32" || x.Value == "00" || x.Value == "01" || x.Value == "02" select x.Key).ToList();
            //    //2
            //    Routes = (from x in dic where x.Value == "12" || x.Value == "20" || x.Value == "21" || x.Value == "22" select x.Key).ToList();
            //} else {//faza II1
            //    Routes = (from x in dic where x.Value == "32" || x.Value == "12" || x.Value == "00" || x.Value == "20" select x.Key).ToList();
            //    //0
            //    Routes = (from x in dic where x.Value == "21" || x.Value == "22" || x.Value == "01" || x.Value == "02" select x.Key).ToList();
            //    //2
            //}
            //if (mode < 2) {//faza I2
            //    //1
            //    Routes = (from x in dic where x.Value == "02" || x.Value == "10" || x.Value == "11" || x.Value == "12" select x.Key).ToList();
            //    //3
            //    Routes = (from x in dic where x.Value == "22" || x.Value == "30" || x.Value == "31" || x.Value == "32" select x.Key).ToList();
            //} else {//faza II2
            //    Routes = (from x in dic where x.Value == "02" || x.Value == "22" || x.Value == "10" || x.Value == "30" select x.Key).ToList();
            //    //1
            //    Routes = (from x in dic where x.Value == "11" || x.Value == "12" || x.Value == "31" || x.Value == "32" select x.Key).ToList();
            //    //3
            //} 
            #endregion
            switch (i) {
                case 0:
                    if (mode % 2 == 0) {
                        Routes = (from x in dic where x.Value == "32" || x.Value == "00" || x.Value == "01" || x.Value == "02" select x.Key).ToList();
                    } else { Routes = (from x in dic where x.Value == "32" || x.Value == "12" || x.Value == "00" || x.Value == "20" select x.Key).ToList(); }
                    break;
                case 1:
                    if (mode < 2) {
                        Routes = (from x in dic where x.Value == "02" || x.Value == "10" || x.Value == "11" || x.Value == "12" select x.Key).ToList();
                    } else { Routes = (from x in dic where x.Value == "02" || x.Value == "22" || x.Value == "10" || x.Value == "30" select x.Key).ToList(); }
                    break;
                case 2:
                    if (mode % 2 == 0) {
                        Routes = (from x in dic where x.Value == "12" || x.Value == "20" || x.Value == "21" || x.Value == "22" select x.Key).ToList();
                    } else { Routes = (from x in dic where x.Value == "21" || x.Value == "22" || x.Value == "01" || x.Value == "02" select x.Key).ToList(); }
                    break;
                case 3:
                    if (mode < 2) {
                        Routes = (from x in dic where x.Value == "22" || x.Value == "30" || x.Value == "31" || x.Value == "32" select x.Key).ToList();
                    } else { Routes = (from x in dic where x.Value == "11" || x.Value == "12" || x.Value == "31" || x.Value == "32" select x.Key).ToList(); }
                    break;
            }
        }
    }
}
[System.Serializable]
public class Pair {
    public int a, b;
    public Pair(int a, int b) {
        this.a = a;
        this.b = b;
    }
}


[System.Serializable]
public class Junction : MonoBehaviour {
    public List<Joint> joints = new List<Joint>();
    public List<Street> street = new List<Street>();
    public List<Path> paths = new List<Path>();
    public List<Pair> streetEnd = new List<Pair>();
    public float max = 1;
    public int phase = 0;
    public bool rondo = false;
    public Phase[] phases;
    void Start() {
        if (phases.Length != 0) {
            StartCoroutine(PhaseChanger());
        }
        if (rondo) {
            StartCoroutine(Rondo());
        }
    }
    void Update() {
    }
    IEnumerator PhaseChanger() {
        while (true) {
            yield return new WaitUntil(() => isFree());
            yield return new WaitForSeconds(1);
            phase = (phase + 1) % phases.Length;
            foreach (int i in phases[phase].Routes) {
                paths[i].Block = 2;
            }
            yield return new WaitForSeconds(5);
            foreach (Path p in paths) {
                p.Block = 0;
            }
        }
    }
    IEnumerator Rondo() {
        while (true) {
            for (int i = 0; i < paths.Count; i++) {
                int k = (i + 1) % paths.Count;
                if (paths[i].CurrentQueue == 0) {
                    paths[k].Block = 2;
                } else {
                    paths[k].Block = 1;
                }
            }
            yield return new WaitForSeconds(0.1f);
        }
    }
    private bool isFree() {
        foreach (Path p in paths) {
            if (p.CurrentQueue != 0) {
                return false;
            }
        }
        return true;
    }
    public void AddStreet(Street s, int start = 0) {
        Debug.Log(street.Count);
        Debug.Log(streetEnd.Count);
        street.Add(s);
        Pair a = new Pair(street.IndexOf(s), start);
        streetEnd.Add(a);
        Calculate();
    }
    public void Calculate() {
        max = street.Count < 2 ? 0 : street.Count * street.Count * 0.2f + street.Count * 0f; ;
        SortStreets();
        joints = new List<Joint>();
        for (int i = 0; i < streetEnd.Count; i++) {
            joints.Add(GetJoint(i));
        }

        foreach (Street ss in street) {
            ss.Resize();
        }
        Clear();
        if (joints.Count < 2) {
            return;
        }
        //L S P
        Vector3 forw = joints[0].pos - transform.position;

        if (joints.Count > 4) {
            rondo = true;
        }
        if (rondo) {
            List<Node> circle = new List<Node>();
            for (int i = 0; i < joints.Count; i++) {
                for (int j = joints[i].output.Count - 1; j >= 0; j--) {
                    circle.Add(joints[i].output[j]);
                }
                for (int j = 0; j < joints[i].input.Count; j++) {
                    circle.Add(joints[i].input[j]);
                }
            }
            for (int i = 0; i < circle.Count - 1; i++) {
                Path p = new Path(circle[i], circle[i + 1], transform, false, 1);
                paths.Add(p);
            }
            Path cir = new Path(circle[circle.Count - 1], circle[0], transform, false, 1);
            paths.Add(cir);
            return;
        }
        if (joints.Count == 2) {
            for (int k = 0; k < 2; k++) {
                int jmax = Mathf.Max(joints[0 + k].input.Count, joints[1 - k].output.Count);
                int j = 0;
                if (joints[0 + k].input.Count < joints[1 - k].output.Count) {
                    for (int i = 0; i < jmax; i++) {
                        Path cir = new Path(joints[0 + k].input[j], joints[1 - k].output[i], transform);
                        paths.Add(cir);
                        j = j + 1 < joints[0 + k].input.Count ? j + 1 : j;
                    }
                } else {
                    for (int i = 0; i < jmax; i++) {
                        Path cir = new Path(joints[0 + k].input[i], joints[1 - k].output[j], transform);
                        paths.Add(cir);
                        j = j + 1 < joints[1 - k].output.Count ? j + 1 : j;
                    }
                }
            }
            return;
        }
        List<Path>[][] tab = new List<Path>[joints.Count][];
        phases = new Phase[joints.Count];
        if (joints.Count == 3) {
            float angleA = Vector3.SignedAngle(forw, joints[1].pos - joints[0].pos, Vector3.up);
            float angleB = Vector3.SignedAngle(forw, joints[2].pos - joints[0].pos, Vector3.up);
            if (angleA > 120) {
                Joint temp = joints[0];
                joints[0] = joints[1];
                joints[1] = joints[2];
                joints[2] = temp;
            }
            if (angleB < -120) {
                Joint temp = joints[0];
                joints[0] = joints[2];
                joints[2] = joints[1];
                joints[1] = temp;
            }
            tab[0] = WayPaths(0);
            tab[1] = WayPaths(1);
            tab[2] = WayPaths(2);
            Dictionary<int, string> dic = new Dictionary<int, string>();
            int c = 0;
            for (int i = 0; i < joints.Count; i++) {
                for (int j = 0; j < 3; j++) {
                    foreach (Path p in tab[i][j]) {
                        paths.Insert(c, p);
                        dic.Add(c, i.ToString() + j.ToString());
                        c++;
                    }
                }
            }
            phases[0] = new Phase(dic, 5, 0);
            phases[1] = new Phase(dic, 5, 1);
            phases[2] = new Phase(dic, 5, 2);
        }
        if (joints.Count == 4) {
            int mode = 0;
            if (joints[0].input.Count > 1 && joints[2].input.Count > 1 && joints[1].output.Count > 0 && joints[3].output.Count > 0) {
                mode = 1;//faza II1
                tab[0] = WayPaths(0);
                tab[2] = WayPaths(2);
            } else {//faza I1
                tab[0] = WayPaths(0, true);
                tab[2] = WayPaths(2, true);
            }
            if (joints[1].input.Count > 1 && joints[3].input.Count > 1 && joints[0].output.Count > 0 && joints[2].output.Count > 0) {
                mode += 2;//faza II2
                tab[1] = WayPaths(1);
                tab[3] = WayPaths(3);
            } else {//faza I2
                tab[1] = WayPaths(1, true);
                tab[3] = WayPaths(3, true);
            }
            Dictionary<int, string> dic = new Dictionary<int, string>();
            int c = 0;
            for (int i = 0; i < joints.Count; i++) {//corss 1234
                for (int j = 0; j < 3; j++) {//LSP
                    foreach (Path p in tab[i][j]) {
                        paths.Add(p);
                        dic.Add(c, i.ToString() + j.ToString());
                        Debug.Log(c + " " + i + " " + j);
                        c++;
                    }
                }
            }
            phases[0] = new Phase(dic, mode, 0);
            phases[1] = new Phase(dic, mode, 1);
            phases[2] = new Phase(dic, mode, 2);
            phases[3] = new Phase(dic, mode, 3);
        }

        foreach (Path p in paths) {
            p.Block = 0;
        }
    }

    List<Path>[] WayPaths(int curent, bool single = false) {
        int[] tab = new int[3] { 0, 0, 0 };
        //P S L
        List<Path>[] tabs = new List<Path>[3];
        for (int i = 0; i < 3; i++) {
            tabs[i] = new List<Path>();
        }
        //L S P
        int sum = joints[curent].input.Count;
        if (sum == 0) {
            return tabs;
        }
        int p = (1 + curent) % 4, s = (2 + curent) % 4, l = (3 + curent) % 4;
        int max = sum, maxp, maxs, maxl;
        int sstart = 0;
        if (joints.Count == 3) {
            //4 wjazd ma 0 dróg
            maxp = p == 3 ? 0 : joints[p].output.Count;
            maxs = s == 3 ? 0 : joints[s].output.Count;
            maxl = l == 3 ? 0 : joints[l].output.Count;
            //rozjazd na prawo i prosto
            if (s != 3 && p != 3 && joints[p].output.Count != 0 && joints[s].output.Count != 0) {
                sum++;
            }
            //rozjazd na lewo i prosto
            if (s != 3 && l != 3 && joints[s].output.Count != 0 && joints[l].output.Count != 0) {
                sum++;
                sstart--;
            }
        } else {
            maxp = joints[p].output.Count;
            maxs = joints[s].output.Count;
            maxl = joints[l].output.Count;
            if (joints[p].output.Count != 0 && joints[s].output.Count != 0) {
                sum++;
            }
            if (single && joints[s].output.Count != 0 && joints[l].output.Count != 0) {
                sum++;
                sstart--;
            }
        }
        sum = Mathf.Min(sum, maxs + maxp + maxl);

        for (int i = 1; i < sum + 1; i++) {
            int j = ((i % 3) + 1 + curent) % 4;
            if (joints.Count == 3 && j != 3 || joints.Count == 4) {
                if (tab[i % 3] < joints[j].output.Count) {
                    tab[i % 3]++; //tab{0,1,0}psl
                    continue;
                }
            }
            sum++;
        }
        sstart += max - tab[0];
        for (int i = 0; i < tab[0]; i++) {
            Path pat = new Path(joints[curent].input[max - i - 1], joints[p].output[maxp - i - 1], transform);
            tabs[2].Add(pat);
        }
        for (int i = 0; i < tab[1]; i++) {
            Path pat = new Path(joints[curent].input[sstart - i], joints[s].output[maxs - i - 1], transform);
            tabs[1].Add(pat);
        }
        for (int i = 0; i < tab[2]; i++) {
            Path pat = new Path(joints[curent].input[i], joints[l].output[i], transform);
            tabs[0].Add(pat);
        }
        return tabs;//lsp
    }
    private void SortStreets() {
        Vector3 forw = GetJoint(0).pos - transform.position;
        streetEnd.Sort(delegate (Pair a, Pair b) {
            float angleA = Vector3.SignedAngle(forw, street[a.a].joints[a.b].pos - transform.position, Vector3.up);
            float angleB = Vector3.SignedAngle(forw, street[b.a].joints[b.b].pos - transform.position, Vector3.up);
            if (angleA == angleB) return 0;
            else if (angleA > angleB) return -1;
            return 1;
        });
        List<Street> sortedStreet = new List<Street>();
        for (int i = 0; i < streetEnd.Count; i++) {
            sortedStreet.Add(street[streetEnd[i].a]);
            streetEnd[i] = new Pair(i, streetEnd[i].b);
        }
        street = sortedStreet;
        //if (streetEnd.Count == 4) {
        //    Debug.Log(GetJoint(0).input[0].ID + " " + GetJoint(1).input[0].ID + " " + GetJoint(2).input[0].ID + " " + GetJoint(3).input[0].ID);
        //}
    }
    Joint GetJoint(int index) {
        return street[streetEnd[index].a].joints[streetEnd[index].b];
    }
    private void OnDestroy() {
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
}