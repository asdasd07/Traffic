using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Faza skrzyżowania
/// </summary>
/// Decyduje które ścieżki są przejezdne w danym czasie.
[System.Serializable]
public class Phase {
    /// <summary>
    /// Przechowuje listę ścieżek, należące do fazy
    /// </summary>
    public List<int> routes = new List<int>();
    /// <summary>
    /// Przechowuje listę par, określających ulice i ścieżkę, z których nadjeżdżają pojazdy
    /// </summary>
    public List<Vector2Int> streetsPaths = new List<Vector2Int>();
    /// <summary>
    /// Przechowuje czas, jaki czekały pojazdy do czasu otwarcia ścieżek
    /// </summary>
    public float queueTime = 5f;

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="dictionary">Słownik zawierający numery ścieżek i kody skrętu</param>
    /// <param name="mode">Modyfikator skrzyżowania</param>
    /// <param name="i">Numer fazy, która ma być stworzona</param>
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

/// <summary>
/// Skrzyżowanie
/// </summary>
/// Obiekt łączący ulice skrzyżowaniem świetlnym lub skrzyżowaniem o ruchu okrężnym
[System.Serializable]
public class Junction : MonoBehaviour {
    /// <summary>
    /// Przechowuje listę przyłącz
    /// </summary>
    public List<Joint> joints = new List<Joint>();
    /// <summary>
    /// Przechowuje listę ścieżek stworzonych przez skrzyżowanie
    /// </summary>
    public List<Path> paths = new List<Path>();
    /// <summary>
    /// Przechowuje tablice faz skrzyżowania
    /// </summary>
    public Phase[] phases;
    /// <summary>
    /// Przechowuje czas do zmiany fazy skrzyżowania
    /// </summary>
    public float timeToPhase = 0f;
    /// <summary>
    /// Przechowuje wartość odstępu, jaki powinny zachować przyłącza
    /// </summary>
    public float margin = 0;
    /// <summary>
    /// Przechowuje czas, jaki powinien zająć pełny cykl faz skrzyżowania
    /// </summary>
    public float cycleTime = 20f;
    /// <summary>
    /// Przechowuje tablice czasów trwania każdej fazy
    /// </summary>
    [SerializeField] public float[] timers;
    [SerializeField] bool rondo = false;
    /// <summary>
    /// Określa czy skrzyżowanie ma obliczać czas trwania faz
    /// </summary>
    public bool timersCalc = true;
    [HideInInspector] public bool globalTimersCalc = true;
    /// <summary>
    /// Przechowuje numer aktualnie wykonywanej fazy skrzyżowania
    /// </summary>
    int phase = 0;

    /// <summary>
    /// Określa, czy skrzyżowanie jest rondem 
    /// </summary>
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
    /// <summary>
    /// Metoda uruchamiana przy rozpoczęciu symulacji. Decyduje czy skrzyżowanie będzie działać jako skrzyżowanie świetlne czy rondo.
    /// </summary>
    void Start() {
        if (Rondo) {
            StartCoroutine(RondoRutine());
        } else {
            if (phases.Length != 0) {
                StartCoroutine(PhaseChanger());
            }
        }
    }
    /// <summary>
    /// Rutyna, która odpowiada za działanie skrzyżowania świetlnego
    /// </summary>
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
                    phases[phase].queueTime += joints[v.x].street.paths[v.y].SumaryWaitingTime;
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
    /// <summary>
    /// Rutyna, która odpowiada za działanie ronda
    /// </summary>
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
    /// <summary>
    /// Metoda określa czy skrzyżowanie jest opuszczone
    /// </summary>
    /// <returns>Prawda, jeżeli ścieżki skrzyżowania są puste</returns>
    bool IsFree() {
        foreach (Path p in paths) {
            if (p.CurrentQueue != 0) {
                return false;
            }
        }
        return true;
    }
    /// <summary>
    /// Metoda odpowiada za dodanie nowego przyłącza
    /// </summary>
    /// <param name="joint">Przyłącze dodawanej ulicy</param>
    public void AddJoint(Joint joint) {
        joints.Add(joint);
    }
    /// <summary>
    /// Metoda odpowiada za usunięcie przyłącza ze skrzyżowania
    /// </summary>
    /// <param name="street">Ulica, której przyłącze jest częścią</param>
    public void RemoveJoint(Street street) {
        joints.RemoveAll(item => item.street == street);
        foreach (Joint jo in joints) {
            jo.street.Calculate();
        }
        Calculate();
    }
    /// <summary>
    /// Metoda odpowiada za stworzenie ścieżek między przyłączami i dodanie ich do faz skrzyżowania
    /// </summary>
    public void Calculate() {
        int maxInputOutput = 0;
        if (joints.Count > 1) {
            foreach (Joint jo in joints) {
                maxInputOutput = Mathf.Max(jo.input.Count, jo.output.Count, maxInputOutput);
            }
            maxInputOutput = Mathf.Max(joints.Count, maxInputOutput);
        }
        margin = maxInputOutput < 2 ? 0 : maxInputOutput * maxInputOutput * 0.08f + maxInputOutput * 0.4f;
        SortStreets();
        phases = null;

        foreach (Joint jo in joints) {
            jo.street.Resize();
        }
        Clear();
        if (joints.Count < 2) {
            return;
        }
        //L S P
        if (joints.Count > 4) {
            rondo = true;
        }
        if (joints.Count < 3) {
            rondo = false;
        }
        if (Rondo) {
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
                Path p = new Path(circle[i], circle[i + 1], transform, HidePath.Internal, BlockType.Priority);
                paths.Add(p);
            }
            Path cir = new Path(circle[circle.Count - 1], circle[0], transform, HidePath.Internal, BlockType.Priority);
            paths.Add(cir);
            return;
        }
        if (joints.Count == 2) {
            for (int k = 0; k < 2; k++) {
                if (joints[k].input.Count == 0 || joints[1 - k].output.Count == 0) continue;
                int jmax = Mathf.Max(joints[k].input.Count, joints[1 - k].output.Count);
                int j = 0;
                if (joints[k].input.Count < joints[1 - k].output.Count) {
                    for (int i = 0; i < jmax; i++) {
                        Path cir = new Path(joints[k].input[j], joints[1 - k].output[i], transform, HidePath.Internal);
                        paths.Add(cir);
                        j = j + 1 < joints[k].input.Count ? j + 1 : j;
                    }
                } else {
                    for (int i = 0; i < jmax; i++) {
                        Path cir = new Path(joints[k].input[i], joints[1 - k].output[j], transform, HidePath.Internal);
                        paths.Add(cir);
                        j = j + 1 < joints[1 - k].output.Count ? j + 1 : j;
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
            if (joints[0].input.Count > 1 && joints[2].input.Count > 1 && joints[1].output.Count > 0 && joints[3].output.Count > 0) {
                mode = 1;//faza II1
                streetWays[0] = StreetWays(0);
                streetWays[2] = StreetWays(2);
            } else {//faza I1
                streetWays[0] = StreetWays(0, true);
                streetWays[2] = StreetWays(2, true);
            }
            if (joints[1].input.Count > 1 && joints[3].input.Count > 1 && joints[0].output.Count > 0 && joints[2].output.Count > 0) {
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

                for (int j = 0; j < 3; j++) {
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
        //add used street index and path index to phase. needed to queue time
        foreach (Phase p in phases) {
            foreach (int i in p.routes) {
                int a = paths[i].IDOfA;
                int jointIndex = joints.FindIndex(item => item.street.nodes.Exists(nid => nid.ID == a));
                if (jointIndex != -1) {
                    int pathIndex = joints[jointIndex].street.paths.FindIndex(item => item.IDOfB == a);
                    p.streetsPaths.Add(new Vector2Int(jointIndex, pathIndex));
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
    /// <summary>
    /// Tworzy ścieżki z jednego przyłącza do reszty
    /// </summary>
    /// <param name="curentIndex">Indeks ulicy, z której wjeżdżają pojazdy</param>
    /// <param name="individual">Określa czy ulica będzie w fazie indywidualnej</param>
    /// <returns>Trzyelementową tablicę list ścieżek wychodzących z przyłącza. Są to listy ścieżek prowadzących na lewo, na przód i na prawo</returns>
    List<Path>[] StreetWays(int curentIndex, bool individual = false) {
        int[] wayCounter = new int[3] { 0, 0, 0 };//R S L
        List<Path>[] streetWays = new List<Path>[3];//L S R
        for (int i = 0; i < 3; i++) {
            streetWays[i] = new List<Path>();
        }
        int sumWays = joints[curentIndex].input.Count;
        if (sumWays == 0) {
            return streetWays;
        }
        int rightIndex = (1 + curentIndex) % 4, straightIndex = (2 + curentIndex) % 4, leftIndex = (3 + curentIndex) % 4;
        int maxCurrent = sumWays, maxRight, maxStraight, maxLeft;
        int straightStart = 0;
        if (joints.Count == 3) {
            //4. wjazd ma 0 dróg
            maxRight = rightIndex == 3 ? 0 : joints[rightIndex].output.Count;
            maxStraight = straightIndex == 3 ? 0 : joints[straightIndex].output.Count;
            maxLeft = leftIndex == 3 ? 0 : joints[leftIndex].output.Count;
            //right and straight at once
            if (straightIndex != 3 && rightIndex != 3 && joints[rightIndex].output.Count != 0 && joints[straightIndex].output.Count != 0) {
                sumWays++;
            }
            //left and straight at once
            if (straightIndex != 3 && leftIndex != 3 && joints[leftIndex].output.Count != 0 && joints[straightIndex].output.Count != 0) {
                sumWays++;
                straightStart--;
            }
            //left and right at once
            if (straightIndex == 3 && joints[rightIndex].output.Count != 0 && joints[leftIndex].output.Count != 0) {
                sumWays++;
            }
        } else {//4
            maxRight = joints[rightIndex].output.Count;
            maxStraight = joints[straightIndex].output.Count;
            maxLeft = joints[leftIndex].output.Count;
            //right and straight at once    
            if (joints[rightIndex].output.Count != 0 && joints[straightIndex].output.Count != 0) {
                sumWays++;
            }
            //left and straight at once
            if (joints[leftIndex].output.Count != 0 && individual && joints[straightIndex].output.Count != 0) {
                sumWays++;
            }
            //left and right at once
            if (joints[straightIndex].output.Count == 0 && joints[rightIndex].output.Count != 0 && joints[leftIndex].output.Count != 0) {
                sumWays++;
            }
        }
        sumWays = Mathf.Min(sumWays, maxStraight + maxRight + maxLeft);

        for (int i = 1; i < sumWays + 1; i++) {
            int j = ((i % 3) + 1 + curentIndex) % 4;
            if (joints.Count == 3 && j != 3 || joints.Count == 4) {
                if (wayCounter[i % 3] < joints[j].output.Count) {
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
            Path pat = new Path(joints[curentIndex].input[maxCurrent - i - 1], joints[rightIndex].output[maxRight - i - 1], transform, HidePath.Internal);
            streetWays[2].Add(pat);
        }
        for (int i = 0; i < wayCounter[1]; i++) {//straight
            Path pat = new Path(joints[curentIndex].input[straightStart - i], joints[straightIndex].output[maxStraight - i - 1], transform, HidePath.Internal);
            streetWays[1].Add(pat);
        }
        for (int i = 0; i < wayCounter[2]; i++) {//left
            Path pat = new Path(joints[curentIndex].input[i], joints[leftIndex].output[i], transform, HidePath.Internal);
            streetWays[0].Add(pat);
        }
        return streetWays;//L S R
    
    }
    /// <summary>
    /// Metoda odpowiada za sortowanie listy przyłącz, kierunku odwrotnym do wskazówek zegara względem skrzyżowania
    /// </summary>
    private void SortStreets() {
        if (joints.Count < 1) {
            return;
        }
        Vector3 forw = joints[0].Position - transform.position;
        joints.Sort(delegate (Joint a, Joint b) {
            float angleA = Vector3.SignedAngle(forw, a.Position - transform.position, Vector3.up);
            float angleB = Vector3.SignedAngle(forw, b.Position - transform.position, Vector3.up);
            angleA = angleA <= 0 ? angleA + 360 : angleA;
            angleB = angleB <= 0 ? angleB + 360 : angleB;
            if (angleA == angleB) return 0;
            else if (angleA > angleB) return -1;
            return 1;
        });
        if (joints.Count == 2) {
            float angleA = Vector3.SignedAngle(forw, joints[1].Position - joints[0].Position, Vector3.up);
            if (angleA > 0) {
                Joint temp = joints[0];
                joints[0] = joints[1];
                joints[1] = temp;
            }
        }
        if (joints.Count == 3) {
            float angleA = Vector3.SignedAngle(forw, joints[1].Position - joints[0].Position, Vector3.up);
            float angleB = Vector3.SignedAngle(forw, joints[2].Position - joints[0].Position, Vector3.up);
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
        }
    }
    /// <summary>
    /// Metoda odpowiada za zniszczenie obiektu i przyłączonych ulic
    /// </summary>
    public void Destroy() {
        foreach (Joint jo in joints) {
            if (jo.street != null)
                jo.street.Destroy(this);
        }
        Clear();
        DestroyImmediate(gameObject);
    }
    /// <summary>
    /// Metoda odpowiada za usunięcie posiadanych ścieżek
    /// </summary>
    void Clear() {
        foreach (Path p in paths) {
            if (p.transform != null) {
                DestroyImmediate(p.transform.gameObject);
            }
        }
        paths.Clear();
    }
    /// <summary>
    /// Metoda odpowiada za podświetlenie skrzyżowania jako zaznaczonego
    /// </summary>
    /// <param name="light">Prawda jeżeli zaznaczone</param>
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