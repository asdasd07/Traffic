using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.SceneManagement;

namespace QPathFinder {
    [CustomEditor(typeof(PathFinder))]
    public class PathFinderEditor : Editor {
        enum SceneMode {
            AddJunction,
            AddStreet,
            SelectJunction,
            SelectStreet,
            None,
            DeleteJunction,
            DeleteStreet
        }
        Junction last, SelectedJunction;
        Street SelectedStreet;
        int ifro = 2, ito = 2;
        private SceneMode sceneMode;
        private PathFinder script;


        const string nodeGUITextColor = "#ff00ffff";
        const string pathGUITextColor = "#00ffffff";
        const string costGUITextColor = "#00a2ffff";
        const string junctionGUITextColor = "#00ff00ff";
        const string groundColliderLayerName = "Default";

        [MenuItem("GameObject/Create a 2D PathFinder in scene with a collider")]
        public static void Create2DPathFinderObjectInScene() {
            if (GameObject.FindObjectOfType<PathFinder>() == null) {
                var colliderGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                colliderGo.name = "Ground";
                colliderGo.GetComponent<Renderer>().sharedMaterial.SetColor("_Color", Color.black);

                colliderGo.transform.localScale = new Vector3(100f, 100f, 1f); ;
                var boxCollider = colliderGo.AddComponent<BoxCollider>();
                boxCollider.isTrigger = true;
            }
        }

        [MenuItem("GameObject/Create a 3D PathFinder in scene with a collider")]
        public static void Create3DPathFinderObjectInScene() {
            if (GameObject.FindObjectOfType<PathFinder>() == null) {
                var colliderGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                colliderGo.name = "Ground";
                colliderGo.GetComponent<Renderer>().sharedMaterial.SetColor("_Color", Color.green);

                colliderGo.transform.localScale = new Vector3(100f, 1f, 100f); ;
                colliderGo.transform.position = Vector3.down * 20;

                var boxCollider = colliderGo.AddComponent<BoxCollider>();
                boxCollider.isTrigger = true;
            }
        }

        private void OnSceneGUI() {
            int controlID = GUIUtility.GetControlID(FocusType.Passive);
            HandleUtility.AddDefaultControl(controlID);
            DrawGUIWindowOnScene();
            UpdateMouseInput();
            DrawPathLine();
            CheckGUIChanged();
        }

        private void CheckGUIChanged() {
            if (GUI.changed) {
                SceneView.RepaintAll();
            }
        }
        private void DrawGUIWindowOnScene() {
            GUILayout.Window(1, new Rect(0f, 25f, 70f, 80f),
                delegate (int windowID) {
                    EditorGUILayout.BeginHorizontal();
                    sceneMode = (SceneMode)GUILayout.SelectionGrid((int)sceneMode, new string[] { "Add Junction", "Add Street", "Select Junction", "Select Street", "None", "Delete Junction", "Delete Street" }, 1);

                    if (sceneMode != SceneMode.SelectStreet && SelectedStreet) {
                        SelectedStreet.Select(false);
                        SelectedStreet = null;
                    }
                    if (sceneMode != SceneMode.SelectJunction && SelectedJunction) {
                        SelectedJunction.Select(false);
                        SelectedJunction = null;
                    }

                    GUI.color = Color.white;
                    EditorGUILayout.EndHorizontal();
                }, "Mode");

            GUILayout.Window(2, new Rect(0, 195f, 70f, 80f),
                delegate (int windowID) {
                    EditorGUILayout.BeginVertical();
                    if (GUILayout.Button("Clear All")) {
                        ClearAll();
                    }
                    if (GUILayout.Button("Refresh Data")) {
                        Debug.Log(script == null);
                        Debug.Log(script.graphData == null);
                        Debug.Log(script.graphData.AllStreets == null);
                        foreach (Street s in script.graphData.AllStreets) {
                            s.Recalculate();
                        }
                        script.graphData.ReGenerateIDs();
                        foreach (Junction j in script.graphData.AllJunctions) {
                            j.Calculate();
                        }
                        script.graphData.ReGenerateIDs();
                    }
                    GUI.color = Color.white;
                    EditorGUILayout.EndVertical();
                }, "");

            if (sceneMode == SceneMode.AddStreet) {
                GUI.Window(4, new Rect(100f, 25f, 140f, 60f), delegate (int windowID) {
                    EditorGUILayout.BeginHorizontal();
                    GUI.color = Color.white;
                    GUI.Label(new Rect(0, 20, 70, 15), "Lines IN");
                    GUI.Label(new Rect(0, 40, 70, 15), "Lines OUT");
                    GUI.Label(new Rect(90, 20, 20, 15), ifro.ToString());
                    GUI.Label(new Rect(90, 40, 20, 15), ito.ToString());
                    if (GUI.Button(new Rect(105, 20, 15, 15), "<")) {
                        ifro = Mathf.Clamp((ifro - 1), 0, 10);
                    }
                    if (GUI.Button(new Rect(120, 20, 15, 15), ">")) {
                        ifro = Mathf.Clamp((ifro + 1), 0, 10);
                    }
                    if (GUI.Button(new Rect(105, 40, 15, 15), "<")) {
                        ito = Mathf.Clamp((ito - 1), 0, 10);
                    }
                    if (GUI.Button(new Rect(120, 40, 15, 15), ">")) {
                        ito = Mathf.Clamp((ito + 1), 0, 10);
                    }
                    EditorGUILayout.EndHorizontal();
                }, "Add Street");
            }
            if (sceneMode == SceneMode.SelectJunction && SelectedJunction != null) {
                GUI.Window(5, new Rect(100f, 25f, 140f, 160f),
                delegate (int windowID) {
                    GUI.color = Color.white;
                    EditorGUILayout.BeginHorizontal();
                    GUI.Label(new Rect(0, 20, 100, 25), "Roundabout");
                    GUI.Label(new Rect(0, 40, 100, 25), "Calculate Timers");
                    SelectedJunction.Rondo = GUI.Toggle(new Rect(110, 20, 20, 15), SelectedJunction.Rondo, "");
                    SelectedJunction.timersCalc = GUI.Toggle(new Rect(110, 40, 20, 15), SelectedJunction.timersCalc, "");
                    if (SelectedJunction.Rondo == false) {
                        Color[] col = new Color[4] { Color.red, Color.green, Color.blue, Color.yellow };
                        GUI.Label(new Rect(0, 60, 120, 15), "Timers");
                        for (int i = 0; i < SelectedJunction.timers.Length; i++) {
                            GUI.color = col[i];
                            string tmp = GUI.TextField(new Rect(110, 60 + i * 20, 25, 15), SelectedJunction.timers[i].ToString(), 25);
                            tmp = tmp.Length == 0 ? "0" : tmp;
                            try {
                                SelectedJunction.timers[i] = float.Parse(tmp);
                            }
                            catch (FormatException) { }
                        }
                    }
                    GUI.color = Color.white;
                    EditorGUILayout.EndHorizontal();
                }, "Selected Junction");
            }
            if (sceneMode == SceneMode.SelectStreet && SelectedStreet) {
                GUI.Window(3, new Rect(100f, 25f, 140f, 190f),
                delegate (int windowID) {
                    GUI.color = Color.white;
                    EditorGUILayout.BeginHorizontal();
                    GUI.Label(new Rect(0, 20, 70, 15), "Name");
                    GUI.Label(new Rect(0, 40, 70, 15), "Lines IN");
                    GUI.Label(new Rect(0, 60, 70, 15), "Lines OUT");
                    SelectedStreet.Name = GUI.TextField(new Rect(45, 20, 90, 15), SelectedStreet.Name, 25);
                    GUI.Label(new Rect(90, 40, 20, 15), SelectedStreet.ifrom.ToString());
                    GUI.Label(new Rect(90, 60, 20, 15), SelectedStreet.ito.ToString());
                    if (GUI.Button(new Rect(105, 40, 15, 15), "<")) {
                        SelectedStreet.ifrom = Mathf.Clamp((SelectedStreet.ifrom - 1), 0, 10);
                        SelectedStreet.Calculate();
                        script.graphData.ReGenerateIDs();
                        SelectedStreet.Recalculate();
                        script.graphData.ReGenerateIDs();
                    }
                    if (GUI.Button(new Rect(120, 40, 15, 15), ">")) {
                        SelectedStreet.ifrom = Mathf.Clamp((SelectedStreet.ifrom + 1), 0, 10);
                        SelectedStreet.Calculate();
                        script.graphData.ReGenerateIDs();
                        SelectedStreet.Recalculate();
                        script.graphData.ReGenerateIDs();
                    }
                    if (GUI.Button(new Rect(105, 60, 15, 15), "<")) {
                        SelectedStreet.ito = Mathf.Clamp((SelectedStreet.ito - 1), 0, 10);
                        SelectedStreet.Calculate();
                        script.graphData.ReGenerateIDs();
                        SelectedStreet.Recalculate();
                        script.graphData.ReGenerateIDs();
                    }
                    if (GUI.Button(new Rect(120, 60, 15, 15), ">")) {
                        SelectedStreet.ito = Mathf.Clamp((SelectedStreet.ito + 1), 0, 10);
                        SelectedStreet.Calculate();
                        script.graphData.ReGenerateIDs();
                        SelectedStreet.Recalculate();
                        script.graphData.ReGenerateIDs();
                    }
                    //przyjezdni przybysz delegat / dom mieszkañcy / sklep miejsca handlowe / praca miejsca pracy / wyje¿d¿aj¹cy emigranci 
                    string[] Describe = new string[5] { "Incoming", "Inhabitants", "Shopping places", "Work places", "Leaving" };
                    Color[] col = new Color[5] { new Color(0.9f, 0.9f, 0.9f), new Color(0, 1, 0), new Color(0, 0.75f, 1), new Color(1, 1, 0), new Color(1, 0.58f, 0.62f) };
                    for (int i = 0; i < 5; i++) {
                        GUI.color = col[i];
                        GUI.Label(new Rect(0, 80 + i * 20, 120, 15), Describe[i]);
                        string tmp = GUI.TextField(new Rect(110, 80 + i * 20, 25, 15), SelectedStreet.Spawns[i].ToString(), 25);
                        tmp = tmp.Length == 0 ? "0" : tmp;
                        try {
                            SelectedStreet.Spawns[i] = int.Parse(tmp);
                        }
                        catch (FormatException) { }
                    }
                    GUI.color = Color.white;
                    EditorGUILayout.EndHorizontal();
                }, "Selected Street");
            }
        }

        private void DrawPathLine() {
            if (script == null|| script.graphData == null || script.graphData.nodes == null)
                return;

            ColoredTexture Colors = new ColoredTexture();
            Vector3 currNode;
            Vector2 guiPosition;

            foreach (Path pd in script.graphData.paths) {
                switch (pd.block) {
                    case BlockType.Blocked:
                        Handles.color = Color.red;
                        break;
                    case BlockType.Priority:
                        Handles.color = Color.green;
                        break;
                    default:
                        Handles.color = Color.yellow;
                        break;
                }
                if (script.drawPaths && sceneMode != SceneMode.SelectJunction)
                    Handles.DrawLine(pd.a.position, pd.b.position);

                Handles.BeginGUI();
                currNode = (pd.a.position + pd.b.position) / 2;
                guiPosition = HandleUtility.WorldToGUIPoint(currNode);
                string str = "";
                if (script.showPathId)
                    str += "<Color=" + pathGUITextColor + ">" + pd.autoGeneratedID.ToString() + "</Color>";
                if (script.showCosts && pd.hide == 0) {
                    if (!string.IsNullOrEmpty(str))
                        str += "<Color=" + costGUITextColor + ">" + "  Cost: " + "</Color>";
                    str += "<Color=" + costGUITextColor + ">" + pd.Cost.ToString("0.0") + "</Color>";
                }

                if (!string.IsNullOrEmpty(str))
                    GUI.Label(new Rect(guiPosition.x - 10, guiPosition.y - 30, 40, 20), str, CustomGUI.GetStyleWithRichText());

                Handles.EndGUI();
            }
            if (script.showSpawns) {
                Handles.BeginGUI();
                foreach (Street s in script.graphData.AllStreets) {
                    guiPosition = HandleUtility.WorldToGUIPoint(s.center.position);
                    int i = 0;
                    string str = "<Color=#ffffffff>" + s.Name + "</Color>";
                    GUI.Label(new Rect(guiPosition.x, guiPosition.y - 25, 100, 20), str, CustomGUI.GetStyleWithRichText());
                    for (int j = 0; j < 5; j++) {
                        if (s.Spawns[j] > 0) {
                            GUI.Label(new Rect(guiPosition.x + i * 15, guiPosition.y, 20, 20), Colors.colo[j]);//przyjazd/dom/sklep/praca/wyjazd
                            GUI.Box(new Rect(guiPosition.x + i * 15, guiPosition.y - 2, 20, 20), s.Spawns[j].ToString(), CustomGUI.GetStyleWithRichText());
                            i++;
                        }
                    }
                }
                Handles.EndGUI();
            }
            if (script.showSpawns) {
                Handles.BeginGUI();
                foreach (Junction j in script.graphData.AllJunctions) {
                    if (j.phases != null && j.phases.Length > 0 && j.Rondo == false) {
                        guiPosition = HandleUtility.WorldToGUIPoint(j.transform.position);
                        GUI.Label(new Rect(guiPosition.x - 20, guiPosition.y - 20, 40, 20), "<Color=" + junctionGUITextColor + ">" + j.timeToPhase.ToString("0.0") + "</Color>", CustomGUI.GetStyleWithRichText());
                    }
                    if (script.showSpawns) {
                        foreach (Path p in j.paths) {
                            if (p.transform) {
                                guiPosition = HandleUtility.WorldToGUIPoint(p.transform.position);
                                GUI.Label(new Rect(guiPosition.x - 20, guiPosition.y - 20, 40, 20), "<Color=" + costGUITextColor + ">" + p.entireQueue.ToString() + "</Color>", CustomGUI.GetStyleWithRichText());
                            }
                        }
                    }
                }
                Handles.EndGUI();
            }
            if (script.showNodeId) {
                Handles.BeginGUI();
                foreach (Node n in script.graphData.nodes) {
                    guiPosition = HandleUtility.WorldToGUIPoint(n.position);
                    GUI.Label(new Rect(guiPosition.x - 10, guiPosition.y - 30, 20, 20), "<Color=" + nodeGUITextColor + ">" + n.ID.ToString() + "</Color>", CustomGUI.GetStyleWithRichText());
                }
                Handles.EndGUI();
            }

            if (sceneMode == SceneMode.SelectJunction && SelectedJunction != null && SelectedJunction.Rondo == false) {
                Color[] colors = new Color[4] { Color.red, Color.green, Color.blue, Color.yellow };
                for (int i = 0; i < SelectedJunction.phases.Length; i++) {
                    Handles.color = colors[i];
                    Vector3 v = new Vector3(0, i * 0.1f, 0);
                    foreach (int j in SelectedJunction.phases[i].routes) {
                        Handles.DrawLine(SelectedJunction.paths[j].a.position + v, SelectedJunction.paths[j].b.position + v);
                    }
                }
            }
            Handles.color = Color.white;
        }

        void UpdateMouseInput() {
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0)
                OnMouseClick(e.mousePosition);
        }

        void OnMouseClick(Vector2 mousePos) {
            LayerMask backgroundLayerMask = 1 << LayerMask.NameToLayer(groundColliderLayerName);
            Ray ray = HandleUtility.GUIPointToWorldRay(mousePos);
            if (Physics.Raycast(ray, out RaycastHit hit, 100000f, backgroundLayerMask)) {
                if (sceneMode == SceneMode.AddJunction) {
                    Vector3 hitPos = hit.point;
                    hitPos += (-ray.direction.normalized) * script.graphData.heightFromTheGround;
                    CreateJunction(hitPos);
                }
                if (sceneMode == SceneMode.AddStreet) {
                    if (last == null) {
                        last = hit.transform.gameObject.GetComponent<Junction>();
                    } else {
                        Junction b = hit.transform.gameObject.GetComponent<Junction>();
                        if (b != null && last != b) {
                            CreateStreet(last, b);
                            last = null;
                        }
                    }
                }
                if (sceneMode == SceneMode.SelectJunction) {
                    if (hit.transform.name == "Junction") {
                        if (SelectedJunction) {
                            SelectedJunction.Select(false);
                            SelectedJunction = null;
                        }
                        SelectedJunction = hit.transform.GetComponent<Junction>();
                        SelectedJunction.Select();
                    }
                } else {
                    if (SelectedJunction) {
                        SelectedJunction.Select(false);
                        SelectedJunction = null;
                    }
                }
                if (sceneMode == SceneMode.SelectStreet) {
                    if (hit.transform.parent.name == "Street") {
                        if (SelectedStreet) {
                            SelectedStreet.Select(false);
                            SelectedStreet = null;
                        }
                        SelectedStreet = hit.transform.parent.GetComponent<Street>();
                        SelectedStreet.Select();
                    }
                } else {
                    if (SelectedStreet) {
                        SelectedStreet.Select(false);
                        SelectedStreet = null;
                    }
                }
                if (sceneMode == SceneMode.DeleteJunction) {
                    if (hit.transform.name == "Junction") {
                        DeleteJunction(hit.transform.GetComponent<Junction>());
                    }
                }
                if (sceneMode == SceneMode.DeleteStreet) {
                    if (hit.transform.parent.name == "Street") {
                        DeleteStreet(hit.transform.parent.GetComponent<Street>());
                    }
                }
            }
        }

        void CreateJunction(Vector3 pos) {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "Junction";
            go.transform.localScale = new Vector3(1, 0.1f, 1);
            go.transform.position = pos;
            go.GetComponent<Renderer>().material = Resources.Load<Material>("junction") as Material;
            go.transform.parent = script.transform;

            Junction ju = go.AddComponent<Junction>();
            script.graphData.AllJunctions.Add(ju);
            script.graphData.ReGenerateIDs();
        }
        void CreateStreet(Junction a, Junction b) {
            GameObject go = new GameObject("Street");
            go.transform.parent = script.transform;
            Street st = go.AddComponent<Street>();
            st.Init(a, b, ifro, ito);
            script.graphData.AllStreets.Add(st);
            script.graphData.ReGenerateIDs();
            a.AddStreet(st);
            b.AddStreet(st, 1);
            script.graphData.ReGenerateIDs();
        }

        void DeleteJunction(Junction j) {
            foreach (Street s in j.street) {
                script.graphData.AllStreets.Remove(s);
            }
            script.graphData.AllJunctions.Remove(j);

            j.Destroy();
            script.graphData.ReGenerateIDs();
        }
        void DeleteStreet(Street s) {
            script.graphData.AllStreets.Remove(s);
            s.Destroy();

            script.graphData.ReGenerateIDs();
        }
        void ClearAll() {
            foreach (Street s in script.graphData.AllStreets) {
                DestroyImmediate(s.gameObject);
            }
            foreach (Junction s in script.graphData.AllJunctions) {
                DestroyImmediate(s.gameObject);
            }
            script.graphData.Clear();
        }

        private void OnEnable() {
            foreach (Path p in script.graphData.paths) {
                p.block = BlockType.Open;
            }
            sceneMode = SceneMode.None;
            script = FindObjectOfType<PathFinder>();
            if (script.save) {
                EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
                EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            }
        }
        private void OnDisable() {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }
        void OnPlayModeStateChanged(PlayModeStateChange state) {
            if (state == PlayModeStateChange.EnteredEditMode) {
                script.graphData.LoadTimers();
            }
            if (state == PlayModeStateChange.ExitingPlayMode) {
                script.graphData.SaveTimers();
            }
        }
    }
}