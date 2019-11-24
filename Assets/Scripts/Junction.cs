using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Junction : MonoBehaviour {
    public List<Joint> joints = new List<Joint>();
    public List<Street> street = new List<Street>();
    public List<Path> paths = new List<Path>();
    public float max = 1;
    public int phase = 0;
    public bool rondo = false;
    public List<Path>[] phases;
    public float tim = 0;
    void Start() {
        Calculate();
        if (rondo) {
            foreach (Path p in paths) {
                p.Block = 0;
            }
        }
    }
    void Update() {
        if (!rondo && phases != null) {
            tim -= Time.deltaTime;
            if (tim <= 0) {
                if (isFree()) {
                    tim = 5;
                    phase = (phase + 1) % phases.Length;
                    foreach (Path p in phases[phase]) {
                        p.Block = 2;
                    }
                } else {
                    foreach (Path p in paths) {
                        p.Block = 0;
                    }
                }
            }
        } else {
            for (int i = 0; i < paths.Count; i++) {
                int k = (i + 1) % paths.Count;
                if (paths[i].CurrentQueue == 0) {
                    paths[k].Block = 2;
                } else {
                    paths[k].Block = 1;
                }
            }
        }
    }
    //public void Upd() {
    //    if (rondo) {
    //        foreach (Path p in paths) {
    //            p.Block = 0;
    //        }
    //    }
    //}
    private void OnDestroy() {
        Clear();
    }
    public void AddStreet(Street s, int start = 0) {
        //max -= street.Count*0.5f;
        street.Add(s);
        joints.Add(s.joints[start]);
        //float counter = s.ifrom < 1 ? 0 : 0.6f + (s.ifrom - 1) * 0.5f;
        //counter += s.ito < 1 ? 0 : 0.6f + (s.ito - 1) * 0.5f;
        //if (max < counter) {
        //    max = counter;
        //}
        //max += street.Count*0.5f;
        max = street.Count < 3 ? 0 : street.Count* street.Count *0.1f+ street.Count * 0.2f; ;

        Vector3 forw = joints[0].pos - transform.position;

        joints.Sort(delegate (Joint a, Joint b) {
            float angleA = Vector3.SignedAngle(forw, a.pos - transform.position, Vector3.up);
            float angleB = Vector3.SignedAngle(forw, b.pos - transform.position, Vector3.up);
            if (angleA == angleB) return 0;
            else if (angleA > angleB) return -1;
            else return 1;
        });
        List<Street> str = new List<Street>();
        foreach(Joint j in joints) {
            str.Add(street.Find(p=>p.joints[0] == j || p.joints[1]==j));
        }
        street = str;
        foreach (Street ss in street) {
            ss.Resize();
        }
        Calculate();
    }

    public void Calculate() {
        Clear();
        if (joints.Count < 3) {
            return;
        }
        List<Path>[][] tab = new List<Path>[joints.Count][];
        phases = new List<Path>[joints.Count];
        for (int i = 0; i < joints.Count; i++) {
            phases[i] = new List<Path>();
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
            phases[0].AddRange(tab[0][2]);
            phases[0].AddRange(tab[0][1]);
            phases[0].AddRange(tab[2][1]);
            phases[1].AddRange(tab[1][2]);
            phases[1].AddRange(tab[2][0]);
            phases[1].AddRange(tab[2][1]);
            phases[2].AddRange(tab[1][2]);
            phases[2].AddRange(tab[1][0]);
            phases[2].AddRange(tab[0][2]);
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
            if (mode % 2 == 0) {//faza I1
                phases[0].AddRange(tab[3][2]);
                for (int i = 0; i < 3; i++) { phases[0].AddRange(tab[0][i]); }
                phases[2].AddRange(tab[1][2]);
                for (int i = 0; i < 3; i++) { phases[2].AddRange(tab[2][i]); }
            } else {//faza II1
                phases[0].AddRange(tab[3][2]);
                phases[0].AddRange(tab[1][2]);
                phases[0].AddRange(tab[0][0]);
                phases[0].AddRange(tab[2][0]);
                phases[2].AddRange(tab[2][1]);
                phases[2].AddRange(tab[2][2]);
                phases[2].AddRange(tab[0][1]);
                phases[2].AddRange(tab[0][2]);
            }
            if (mode < 2) {//faza I2
                phases[1].AddRange(tab[0][2]);
                for (int i = 0; i < 3; i++) { phases[1].AddRange(tab[1][i]); }
                phases[3].AddRange(tab[2][2]);
                for (int i = 0; i < 3; i++) { phases[3].AddRange(tab[3][i]); }
            } else {//faza II2
                phases[1].AddRange(tab[0][2]);
                phases[1].AddRange(tab[2][2]);
                phases[1].AddRange(tab[1][0]);
                phases[1].AddRange(tab[3][0]);
                phases[3].AddRange(tab[1][1]);
                phases[3].AddRange(tab[1][2]);
                phases[3].AddRange(tab[3][1]);
                phases[3].AddRange(tab[3][2]);
            }
        }
        for (int i = 0; i < joints.Count; i++) {
            for (int j = 0; j < 3; j++) {
                paths.AddRange(tab[i][j]);
            }
        }
    }

    void Clear() {
        foreach (Path p in paths) {
            if (p.tr != null) {
                DestroyImmediate(p.tr.gameObject);
            }
        }
        paths.Clear();
    }
    bool isFree() {
        foreach(Path p in paths) {
            if (p.CurrentQueue != 0) {
                return false;
            }
        }
        return true;
    }
    List<Path>[] WayPaths(int curent, bool single = false) {
        int[] tab = new int[3] { 0, 0, 0 };
        //P S L
        List<Path>[] tabs = new List<Path>[3];
        for (int i = 0; i < 3; i++) {
            tabs[i] = new List<Path>(); 
        }
        //L S P
        int sum = joints[0].input.Count;
        int p = (1 + curent) % 4, s = (2 + curent) % 4, l = (3 + curent) % 4;
        int max = joints[curent].input.Count, maxp, maxs, maxl;
        int sstart = 0;
        if (joints.Count == 3) {
            maxp = p == 3 ? 0 : joints[p].output.Count;
            maxs = s == 3 ? 0 : joints[s].output.Count;
            maxl = l == 3 ? 0 : joints[l].output.Count;
            if (s != 3 && p != 3 && joints[p].output.Count != 0 && joints[s].output.Count != 0) {
                sum++;
            }
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
                    tab[i % 3]++;
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
            //Debug.Log(curent + " " + tab[1] + " " + sstart + " " + i + " " + +max + " " + maxs);
            Path pat = new Path(joints[curent].input[sstart - i], joints[s].output[maxs - i - 1], transform);
            tabs[1].Add(pat);
        }
        for (int i = 0; i < tab[2]; i++) {
            Path pat = new Path(joints[curent].input[i], joints[l].output[i], transform);
            tabs[0].Add(pat);
        }

        return tabs;
    }
}