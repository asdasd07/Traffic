using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class Phase {
    public List<int> routes = new List<int>();
    public List<Vector2Int> streetsPaths = new List<Vector2Int>();
    public float queueTime = 5f;

    public Phase(Dictionary<int, string> dictionary, int mode, int i) {
        if (mode == 5) {
            switch (i) {
                case 0:
                    routes = (from x in dictionary where x.Value == "02" || x.Value == "01" || x.Value == "21" select x.Key).ToList();
                    break;
                case 1:
                    routes = (from x in dictionary where x.Value == "12" || x.Value == "20" || x.Value == "21" select x.Key).ToList();
                    break;
                case 2:
                    routes = (from x in dictionary where x.Value == "12" || x.Value == "10" || x.Value == "02" select x.Key).ToList();
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
                        routes = (from x in dictionary where x.Value == "00" || x.Value == "01" || x.Value == "02" select x.Key).ToList();
                        if (routes.Count != 0) {
                            routes.AddRange(from x in dictionary where x.Value == "32" select x.Key);
                        }
                    } else { routes = (from x in dictionary where x.Value == "32" || x.Value == "12" || x.Value == "00" || x.Value == "20" select x.Key).ToList(); }
                    break;
                case 1:
                    if (mode < 2) {
                        routes = (from x in dictionary where x.Value == "10" || x.Value == "11" || x.Value == "12" select x.Key).ToList();
                        if (routes.Count != 0) {
                            routes.AddRange(from x in dictionary where x.Value == "02" select x.Key);
                        }
                    } else { routes = (from x in dictionary where x.Value == "02" || x.Value == "22" || x.Value == "10" || x.Value == "30" select x.Key).ToList(); }
                    break;
                case 2:
                    if (mode % 2 == 0) {
                        routes = (from x in dictionary where x.Value == "20" || x.Value == "21" || x.Value == "22" select x.Key).ToList();
                        if (routes.Count != 0) {
                            routes.AddRange(from x in dictionary where x.Value == "12" select x.Key);
                        }
                    } else { routes = (from x in dictionary where x.Value == "21" || x.Value == "22" || x.Value == "01" || x.Value == "02" select x.Key).ToList(); }
                    break;
                case 3:
                    if (mode < 2) {
                        routes = (from x in dictionary where x.Value == "30" || x.Value == "31" || x.Value == "32" select x.Key).ToList();
                        if (routes.Count != 0) {
                            routes.AddRange(from x in dictionary where x.Value == "22" select x.Key);
                        }
                    } else { routes = (from x in dictionary where x.Value == "11" || x.Value == "12" || x.Value == "31" || x.Value == "32" select x.Key).ToList(); }
                    break;
            }
        }
    }
}

[System.Serializable]
public class Junction : MonoBehaviour {
    public List<Joint> joints = new List<Joint>();//private
    public List<Path> paths = new List<Path>();
    public List<Street> street = new List<Street>();
    public List<Vector2Int> streetEnd = new List<Vector2Int>();
    public Phase[] phases;
    public float timeToPhase = 0f;
    public float border = 0;
    public float cycleTime = 20f;
    [SerializeField] public float[] timers;
    [SerializeField] bool rondo = false;
    public bool timersCalc = true;
    [HideInInspector] public bool globalTimersCalc = true;
    int phase = 0;

    public bool Rondo {
        get => rondo;
        set {
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
        int maxInputOutput = 0;
        if (street.Count > 1) {
            foreach (Street s in street) {
                maxInputOutput = Mathf.Max(s.iFrom, s.iTo, maxInputOutput);
            }
            maxInputOutput = Mathf.Max(street.Count, maxInputOutput);
        }
        border = maxInputOutput < 2 ? 0 : maxInputOutput * maxInputOutput * 0.08f + maxInputOutput * 0.4f;
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
        List<Path>[][] streetWays = new List<Path>[joints.Count][];
        phases = new Phase[joints.Count];
        if (joints.Count == 3) {
            streetWays[0] = StreetWays(0);
            streetWays[1] = StreetWays(1);
            streetWays[2] = StreetWays(2);
            Dictionary<int, string> dict = new Dictionary<int, string>();
            int c = 0;
            for (int i = 0; i < joints.Count; i++) {
                for (int j = 0; j < 3; j++) {
                    foreach (Path p in streetWays[i][j]) {
                        paths.Insert(c, p);
                        dict.Add(c, i.ToString() + j.ToString());
                        c++;
                    }
                }
            }
            phases[0] = new Phase(dict, 5, 0);
            phases[1] = new Phase(dict, 5, 1);
            phases[2] = new Phase(dict, 5, 2);
        }
        if (joints.Count == 4) {
            int mode = 0;
            if (GetJoint(0).input.Count > 1 && GetJoint(2).input.Count > 1 && GetJoint(1).output.Count > 0 && GetJoint(3).output.Count > 0) {
                mode = 1;//faza II1
                streetWays[0] = StreetWays(0);
                streetWays[2] = StreetWays(2);
            } else {//faza I1
                streetWays[0] = StreetWays(0, true);
                streetWays[2] = StreetWays(2, true);
            }
            if (GetJoint(1).input.Count > 1 && GetJoint(3).input.Count > 1 && GetJoint(0).output.Count > 0 && GetJoint(2).output.Count > 0) {
                mode += 2;//faza II2
                streetWays[1] = StreetWays(1);
                streetWays[3] = StreetWays(3);
            } else {//faza I2
                streetWays[1] = StreetWays(1, true);
                streetWays[3] = StreetWays(3, true);
            }
            Dictionary<int, string> dict = new Dictionary<int, string>();
            int c = 0;
            for (int i = 0; i < joints.Count; i++) {//corss 1234
                for (int j = 0; j < 3; j++) {//LSP
                    foreach (Path p in streetWays[i][j]) {
                        paths.Add(p);
                        dict.Add(c, i.ToString() + j.ToString());
                        c++;
                    }
                }
            }
            phases[0] = new Phase(dict, mode, 0);
            phases[1] = new Phase(dict, mode, 1);
            phases[2] = new Phase(dict, mode, 2);
            phases[3] = new Phase(dict, mode, 3);
        }
        //add used street and path index to phase
        foreach (Phase p in phases) {
            foreach (int i in p.routes) {
                int a = paths[i].IDOfA;
                int streetIndex = street.FindIndex(item => item.nodes.Exists(nid => nid.ID == a));
                if (streetIndex != -1) {
                    int pathIndex = street[streetIndex].paths.FindIndex(item => item.IDOfB == a);
                    p.streetsPaths.Add(new Vector2Int(streetIndex, pathIndex));
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

    List<Path>[] StreetWays(int curentIndex, bool individual = false) {
        int[] wayCounter = new int[3] { 0, 0, 0 };//R S L
        List<Path>[] streetWays = new List<Path>[3];//L S R
        for (int i = 0; i < 3; i++) {
            streetWays[i] = new List<Path>();
        }
        int sumWays = GetJoint(curentIndex).input.Count;
        if (sumWays == 0) {
            return streetWays;
        }
        int rightIndex = (1 + curentIndex) % 4, straightIndex = (2 + curentIndex) % 4, leftIndex = (3 + curentIndex) % 4;
        int maxCurrent = sumWays, maxRight, maxStraight, maxLeft;
        int straightStart = 0;
        if (joints.Count == 3) {
            //4. wjazd ma 0 dróg
            maxRight = rightIndex == 3 ? 0 : GetJoint(rightIndex).output.Count;
            maxStraight = straightIndex == 3 ? 0 : GetJoint(straightIndex).output.Count;
            maxLeft = leftIndex == 3 ? 0 : GetJoint(leftIndex).output.Count;
            //right and straight at once
            if (straightIndex != 3 && rightIndex != 3 && GetJoint(rightIndex).output.Count != 0 && GetJoint(straightIndex).output.Count != 0) {
                sumWays++;
            }
            //left and straight at once
            if (straightIndex != 3 && leftIndex != 3 && GetJoint(leftIndex).output.Count != 0 && GetJoint(straightIndex).output.Count != 0) {
                sumWays++;
                straightStart--;
            }
            //left and right at once
            if (straightIndex == 3 && GetJoint(rightIndex).output.Count != 0 && GetJoint(leftIndex).output.Count != 0) {
                sumWays++;
            }
        } else {//4
            maxRight = GetJoint(rightIndex).output.Count;
            maxStraight = GetJoint(straightIndex).output.Count;
            maxLeft = GetJoint(leftIndex).output.Count;
            //right and straight at once    
            if (GetJoint(rightIndex).output.Count != 0 && GetJoint(straightIndex).output.Count != 0) {
                sumWays++;
            }
            //left and straight at once
            if (GetJoint(leftIndex).output.Count != 0 && individual && GetJoint(straightIndex).output.Count != 0) {
                sumWays++;
            }
            //left and right at once
            if (GetJoint(straightIndex).output.Count == 0 && GetJoint(rightIndex).output.Count != 0 && GetJoint(leftIndex).output.Count != 0) {
                sumWays++;
            }
        }
        sumWays = Mathf.Min(sumWays, maxStraight + maxRight + maxLeft);

        for (int i = 1; i < sumWays + 1; i++) {
            int j = ((i % 3) + 1 + curentIndex) % 4;
            if (joints.Count == 3 && j != 3 || joints.Count == 4) {
                if (wayCounter[i % 3] < GetJoint(j).output.Count) {
                    wayCounter[i % 3]++; //tab{1,1,1}psl
                    continue;
                }
            }
            sumWays++;
        }
        straightStart += maxCurrent - wayCounter[0];
        straightStart = Mathf.Clamp(straightStart, 0, maxCurrent - 1);
        //Debug.Log(sstart + " " + sum + " " + maxs + " " + max + " " + maxp + " " + maxl + " " + tab[0] + " " + tab[1] + " " + tab[2]);
        for (int i = 0; i < wayCounter[0]; i++) {//right
            Path pat = new Path(GetJoint(curentIndex).input[maxCurrent - i - 1], GetJoint(rightIndex).output[maxRight - i - 1], transform, HidePath.Internal);
            streetWays[2].Add(pat);
        }
        for (int i = 0; i < wayCounter[1]; i++) {//straight
            Path pat = new Path(GetJoint(curentIndex).input[straightStart - i], GetJoint(straightIndex).output[maxStraight - i - 1], transform, HidePath.Internal);
            streetWays[1].Add(pat);
        }
        for (int i = 0; i < wayCounter[2]; i++) {//left
            Path pat = new Path(GetJoint(curentIndex).input[i], GetJoint(leftIndex).output[i], transform, HidePath.Internal);
            streetWays[0].Add(pat);
        }
        return streetWays;//L S R
    }
    private void SortStreets() {
        if (streetEnd.Count < 1) {
            return;
        }
        Vector3 forw = GetJoint(0).Position - transform.position;
        streetEnd.Sort(delegate (Vector2Int a, Vector2Int b) {
            float angleA = Vector3.SignedAngle(forw, street[a.x].joints[a.y].Position - transform.position, Vector3.up);
            float angleB = Vector3.SignedAngle(forw, street[b.x].joints[b.y].Position - transform.position, Vector3.up);
            if (angleA == angleB) return 0;
            else if (angleA > angleB) return -1;
            return 1;
        });
        if (street.Count == 3) {
            float angleA = Vector3.SignedAngle(forw, GetJoint(1).Position - GetJoint(0).Position, Vector3.up);
            float angleB = Vector3.SignedAngle(forw, GetJoint(2).Position - GetJoint(0).Position, Vector3.up);
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