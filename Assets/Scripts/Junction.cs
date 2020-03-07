using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class Phase {
    public List<int> routes = new List<int>();
    public List<Vector2Int> streetsPaths = new List<Vector2Int>();
    public Phase(Dictionary<int, string> dic, int mode, int i) {
        if (mode == 5) {
            switch (i) {
                case 0:
                    routes = (from x in dic where x.Value == "02" || x.Value == "01" || x.Value == "21" select x.Key).ToList();
                    break;
                case 1:
                    routes = (from x in dic where x.Value == "12" || x.Value == "20" || x.Value == "21" select x.Key).ToList();
                    break;
                case 2:
                    routes = (from x in dic where x.Value == "12" || x.Value == "10" || x.Value == "02" select x.Key).ToList();
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
                        routes = (from x in dic where x.Value == "00" || x.Value == "01" || x.Value == "02" select x.Key).ToList();
                        if (routes.Count != 0) {
                            routes.AddRange(from x in dic where x.Value == "32" select x.Key);
                        }
                    } else { routes = (from x in dic where x.Value == "32" || x.Value == "12" || x.Value == "00" || x.Value == "20" select x.Key).ToList(); }
                    break;
                case 1:
                    if (mode < 2) {
                        routes = (from x in dic where x.Value == "10" || x.Value == "11" || x.Value == "12" select x.Key).ToList();
                        if (routes.Count != 0) {
                            routes.AddRange(from x in dic where x.Value == "02" select x.Key);
                        }
                    } else { routes = (from x in dic where x.Value == "02" || x.Value == "22" || x.Value == "10" || x.Value == "30" select x.Key).ToList(); }
                    break;
                case 2:
                    if (mode % 2 == 0) {
                        routes = (from x in dic where x.Value == "20" || x.Value == "21" || x.Value == "22" select x.Key).ToList();
                        if (routes.Count != 0) {
                            routes.AddRange(from x in dic where x.Value == "12" select x.Key);
                        }
                    } else { routes = (from x in dic where x.Value == "21" || x.Value == "22" || x.Value == "01" || x.Value == "02" select x.Key).ToList(); }
                    break;
                case 3:
                    if (mode < 2) {
                        routes = (from x in dic where x.Value == "30" || x.Value == "31" || x.Value == "32" select x.Key).ToList();
                        if (routes.Count != 0) {
                            routes.AddRange(from x in dic where x.Value == "22" select x.Key);
                        }
                    } else { routes = (from x in dic where x.Value == "11" || x.Value == "12" || x.Value == "31" || x.Value == "32" select x.Key).ToList(); }
                    break;
            }
        }
    }
    public float queueTime = 5f;
}

[System.Serializable]
public class Junction : MonoBehaviour {
    public List<Joint> joints = new List<Joint>();
    public List<Street> street = new List<Street>();
    public List<Path> paths = new List<Path>();
    public Phase[] phases;
    public float timeToPhase = 0f;
    public List<Vector2Int> streetEnd = new List<Vector2Int>();
    public float max = 1;
    public float cycleTime = 20f;
    public int phase = 0;
    [SerializeField] bool rondo = false;
    public bool timersCalc = true;
    [HideInInspector] public bool globalTimersCalc = true;
    [SerializeField] public float[] timers;

    public bool Rondo {
        get => rondo; set {
            if (value == false && joints.Count > 4) {
                rondo = true;
            } else {
                if (rondo != value) {
                    rondo = value;
                    Calculate();
                }
            }
        }
    }

    void Start() {
        if (Rondo) {
            StartCoroutine(RondoRutine());
        } else {
            if (phases.Length != 0) {
                StartCoroutine(PhaseChanger());
            }
        }
    }
    IEnumerator PhaseChanger() {
        while (true) {
            float tim = 0;
            while (tim < 9f && !IsFree()) {
                tim += 0.1f;
                yield return new WaitForSeconds(0.1f);
            }
            if (phases[phase].routes.Count != 0) {
                yield return new WaitForSeconds(1);
                phases[phase].queueTime = 0;
                foreach (Vector2Int v in phases[phase].streetsPaths) {
                    //Debug.Log(street[v.x].paths[v.y].autoGeneratedID + street[v.x].paths[v.y].CurrentQueue);
                    phases[phase].queueTime += street[v.x].paths[v.y].SumaryWaitingTime;
                }
                foreach (int i in phases[phase].routes) {
                    paths[i].block = BlockType.Open;
                }
                if (globalTimersCalc && timersCalc && phase == phases.Length - 1) {
                    float allTime = 0;
                    float oneTen = 0.1f * cycleTime;
                    foreach (Phase p in phases) {
                        p.queueTime = p.queueTime < oneTen ? oneTen : p.queueTime;
                        allTime += p.queueTime;
                    }
                    for (int i = 0; i < timers.Length; i++) {
                        float scale = phases[i].queueTime / allTime;
                        timers[i] = (float)Math.Round((double)(timers[i] + scale * cycleTime) / 2f, 1);
                    }
                }
                timeToPhase = timers[phase];
                while (timeToPhase > 0) {
                    yield return new WaitForSeconds(0.1f);
                    timeToPhase -= 0.1f;
                }
                timeToPhase = 0;
                foreach (Path p in paths) {
                    p.block = BlockType.Blocked;//blocked
                }
            }
            phase = (phase + 1) % phases.Length;
        }
    }
    IEnumerator RondoRutine() {
        while (true) {
            for (int i = 0; i < paths.Count; i++) {
                int k = (i + 1) % paths.Count;
                if (paths[i].CurrentQueue == 0) {
                    paths[k].block = BlockType.Open;
                } else {
                    paths[k].block = BlockType.Priority;
                }
            }
            yield return new WaitForSeconds(0.1f);
        }
    }
    bool IsFree() {
        foreach (Path p in paths) {
            if (p.CurrentQueue != 0) {
                return false;
            }
        }
        return true;
    }
    public void AddStreet(Street s, int start = 0) {
        street.Add(s);
        Vector2Int a = new Vector2Int(street.IndexOf(s), start);
        streetEnd.Add(a);
        Calculate();
    }
    public void RemoveStreet(Street s) {
        int index = street.IndexOf(s);
        if (index != -1) {
            joints.Remove(street[index].joints[0]);
            joints.Remove(street[index].joints[1]);
            streetEnd.RemoveAt(index);
            street.RemoveAt(index);
            for (int i = index; i < streetEnd.Count; i++) {
                streetEnd[i] = streetEnd[i] - new Vector2Int(1, 0);
            }
        }
        Calculate();
    }
    public void Calculate() {
        int maxinou = 0;
        if (street.Count > 1) {
            foreach (Street s in street) {
                maxinou = Mathf.Max(s.ifrom, s.ito, maxinou);
            }
            maxinou = Mathf.Max(street.Count, maxinou);
        }
        max = maxinou < 2 ? 0 : maxinou * maxinou * 0.08f + maxinou * 0.4f;
        SortStreets();
        joints = new List<Joint>();
        phases = null;
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
        if (joints.Count > 4) {
            rondo = true;
        }
        if (Rondo) {
            List<Node> circle = new List<Node>();
            for (int i = 0; i < joints.Count; i++) {
                for (int j = GetJoint(i).output.Count - 1; j >= 0; j--) {
                    circle.Add(GetJoint(i).output[j]);
                }
                for (int j = 0; j < GetJoint(i).input.Count; j++) {
                    circle.Add(GetJoint(i).input[j]);
                }
            }
            for (int i = 0; i < circle.Count - 1; i++) {
                Path p = new Path(circle[i], circle[i + 1], transform, HidePath.Internal, BlockType.Priority);
                paths.Add(p);
            }
            Path cir = new Path(circle[circle.Count - 1], circle[0], transform, HidePath.Internal, BlockType.Priority);
            paths.Add(cir);
            return;
        }
        if (joints.Count == 2) {
            for (int k = 0; k < 2; k++) {
                if (GetJoint(k).input.Count == 0 || GetJoint(1 - k).output.Count == 0) continue;
                int jmax = Mathf.Max(GetJoint(k).input.Count, GetJoint(1 - k).output.Count);
                int j = 0;
                if (GetJoint(k).input.Count < GetJoint(1 - k).output.Count) {
                    for (int i = 0; i < jmax; i++) {
                        Path cir = new Path(GetJoint(k).input[j], GetJoint(1 - k).output[i], transform, HidePath.Internal);
                        paths.Add(cir);
                        j = j + 1 < GetJoint(k).input.Count ? j + 1 : j;
                    }
                } else {
                    for (int i = 0; i < jmax; i++) {
                        Path cir = new Path(GetJoint(k).input[i], GetJoint(1 - k).output[j], transform, HidePath.Internal);
                        paths.Add(cir);
                        j = j + 1 < GetJoint(1 - k).output.Count ? j + 1 : j;
                    }
                }
            }
            return;
        }
        List<Path>[][] tab = new List<Path>[joints.Count][];
        phases = new Phase[joints.Count];
        if (joints.Count == 3) {
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
            if (GetJoint(0).input.Count > 1 && GetJoint(2).input.Count > 1 && GetJoint(1).output.Count > 0 && GetJoint(3).output.Count > 0) {
                mode = 1;//faza II1
                tab[0] = WayPaths(0);
                tab[2] = WayPaths(2);
            } else {//faza I1
                tab[0] = WayPaths(0, true);
                tab[2] = WayPaths(2, true);
            }
            if (GetJoint(1).input.Count > 1 && GetJoint(3).input.Count > 1 && GetJoint(0).output.Count > 0 && GetJoint(2).output.Count > 0) {
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
                        c++;
                    }
                }
            }
            phases[0] = new Phase(dic, mode, 0);
            phases[1] = new Phase(dic, mode, 1);
            phases[2] = new Phase(dic, mode, 2);
            phases[3] = new Phase(dic, mode, 3);
        }
        //
        foreach (Phase p in phases) {
            foreach (int i in p.routes) {
                int a = paths[i].IDOfA;
                int strid = street.FindIndex(item => item.nodes.Exists(nid => nid.ID == a));
                if (strid != -1) {
                    int patid = street[strid].paths.FindIndex(item => item.IDOfB == a);
                    p.streetsPaths.Add(new Vector2Int(strid, patid));
                }
            }
        }


        timers = new float[phases.Length];
        for (int j = 0; j < phases.Length; j++) {
            timers[j] = 5f;
        }

        foreach (Path p in paths) {
            p.block = BlockType.Blocked;
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
        int sum = GetJoint(curent).input.Count;
        //Debug.Log(GetJoint(curent).input.Count + " " + GetJoint(curent).output.Count);
        if (sum == 0) {
            return tabs;
        }
        int p = (1 + curent) % 4, s = (2 + curent) % 4, l = (3 + curent) % 4;
        int max = sum, maxp, maxs, maxl;
        int sstart = 0;
        if (joints.Count == 3) {
            //4 wjazd ma 0 dróg
            maxp = p == 3 ? 0 : GetJoint(p).output.Count;
            maxs = s == 3 ? 0 : GetJoint(s).output.Count;
            maxl = l == 3 ? 0 : GetJoint(l).output.Count;
            //rozjazd na prawo i prosto
            if (s != 3 && p != 3 && GetJoint(p).output.Count != 0 && GetJoint(s).output.Count != 0) {
                sum++;
            }
            //rozjazd na lewo i prosto
            if (s != 3 && l != 3 && GetJoint(s).output.Count != 0 && GetJoint(l).output.Count != 0) {
                sum++;
                sstart--;
            }
            if (s == 3 && GetJoint(p).output.Count != 0 && GetJoint(l).output.Count != 0) {
                sum++;
            }
        } else {//4
            maxp = GetJoint(p).output.Count;
            maxs = GetJoint(s).output.Count;
            maxl = GetJoint(l).output.Count;
            if (GetJoint(p).output.Count != 0 && GetJoint(s).output.Count != 0) {
                sum++;//1
            }
            if (single && GetJoint(s).output.Count != 0 && GetJoint(l).output.Count != 0) {
                sum++;//1
                //sstart--;
            }
            if (GetJoint(s).output.Count == 0 && GetJoint(p).output.Count != 0 && GetJoint(l).output.Count != 0) {
                sum++;//1
            }
        }
        sum = Mathf.Min(sum, maxs + maxp + maxl);

        for (int i = 1; i < sum + 1; i++) {
            int j = ((i % 3) + 1 + curent) % 4;
            if (joints.Count == 3 && j != 3 || joints.Count == 4) {
                if (tab[i % 3] < GetJoint(j).output.Count) {
                    tab[i % 3]++; //tab{1,1,1}psl
                    continue;
                }
            }
            sum++;
        }
        sstart += max - tab[0];
        sstart = Mathf.Clamp(sstart, 0, max - 1);
        //Debug.Log(sstart + " " + sum + " " + maxs + " " + max + " " + maxp + " " + maxl + " " + tab[0] + " " + tab[1] + " " + tab[2]);
        for (int i = 0; i < tab[0]; i++) {//right
            Path pat = new Path(GetJoint(curent).input[max - i - 1], GetJoint(p).output[maxp - i - 1], transform, HidePath.Internal);
            tabs[2].Add(pat);
        }
        for (int i = 0; i < tab[1]; i++) {//streit
            Path pat = new Path(GetJoint(curent).input[sstart - i], GetJoint(s).output[maxs - i - 1], transform, HidePath.Internal);
            tabs[1].Add(pat);
        }
        for (int i = 0; i < tab[2]; i++) {//left
            Path pat = new Path(GetJoint(curent).input[i], GetJoint(l).output[i], transform, HidePath.Internal);
            tabs[0].Add(pat);
        }
        return tabs;//lsp
    }
    private void SortStreets() {
        if (streetEnd.Count < 1) {
            return;
        }
        Vector3 forw = GetJoint(0).position - transform.position;
        streetEnd.Sort(delegate (Vector2Int a, Vector2Int b) {
            float angleA = Vector3.SignedAngle(forw, street[a.x].joints[a.y].position - transform.position, Vector3.up);
            float angleB = Vector3.SignedAngle(forw, street[b.x].joints[b.y].position - transform.position, Vector3.up);
            if (angleA == angleB) return 0;
            else if (angleA > angleB) return -1;
            return 1;
        });
        if (street.Count == 3) {
            float angleA = Vector3.SignedAngle(forw, GetJoint(1).position - GetJoint(0).position, Vector3.up);
            float angleB = Vector3.SignedAngle(forw, GetJoint(2).position - GetJoint(0).position, Vector3.up);
            if (angleA > 120) {
                Vector2Int temp = streetEnd[0];
                streetEnd[0] = streetEnd[1];
                streetEnd[1] = streetEnd[2];
                streetEnd[2] = temp;
            }
            if (angleB < -120) {
                Vector2Int temp = streetEnd[0];
                streetEnd[0] = streetEnd[2];
                streetEnd[2] = streetEnd[1];
                streetEnd[1] = temp;
            }
        }
        List<Street> sortedStreet = new List<Street>();
        for (int i = 0; i < streetEnd.Count; i++) {
            sortedStreet.Add(street[streetEnd[i].x]);
            streetEnd[i] = new Vector2Int(i, streetEnd[i].y);
        }
        street = sortedStreet;
    }

    public Joint GetJoint(int index) {
        if (index >= streetEnd.Count) return null;
        return street[streetEnd[index].x].joints[streetEnd[index].y];
    }

    public void Destroy() {
        foreach (Street s in street) {
            if (s != null)
                s.Destroy(this);
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
    }
    public void Select(bool light = true) {
        MeshRenderer mats = GetComponent<MeshRenderer>();
        Material mat = new Material(source: GetComponent<MeshRenderer>().sharedMaterial);

        if (light) {
            mat.color = new Color(1f, 0f, 0f);
        } else {
            mat.color = new Color(0.44f, 0.44f, 0.44f);
        }
        mats.material = mat;
    }
}