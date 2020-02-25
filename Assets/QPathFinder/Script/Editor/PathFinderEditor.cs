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
            SelectJunction,
            AddStreet,
            SelectStreet,
            None,
            DeleteJunction,
            DeleteStreet
        }
        Junction last, SelectedJunction;
        Street SelectedStreet;
        int ifro = 2, ito = 2;

        [MenuItem("GameObject/Create a 2D PathFinder in scene with a collider")]
        public static void Create2DPathFinderObjectInScene() {
            if (GameObject.FindObjectOfType<PathFinder>() == null) {
                var managerGo = new GameObject("PathFinder");
                var colliderGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                colliderGo.name = "Ground";
                colliderGo.GetComponent<Renderer>().sharedMaterial.SetColor("_Color", Color.black);

                colliderGo.transform.localScale = new Vector3(100f, 100f, 1f); ;
                var boxCollider = colliderGo.AddComponent<BoxCollider>();
                boxCollider.isTrigger = true;

                var manager = managerGo.AddComponent<PathFinder>();
            } else {
                if (Logger.CanLogError) Logger.LogError("PathFollower Script already exists!");
            }
        }

        [MenuItem("GameObject/Create a 3D PathFinder in scene with a collider")]
        public static void Create3DPathFinderObjectInScene() {
            if (GameObject.FindObjectOfType<PathFinder>() == null) {
                var managerGo = new GameObject("PathFinder");
                var colliderGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                colliderGo.name = "Ground";
                colliderGo.GetComponent<Renderer>().sharedMaterial.SetColor("_Color", Color.green);

                colliderGo.transform.localScale = new Vector3(100f, 1f, 100f); ;
                colliderGo.transform.position = Vector3.down * 20;

                var boxCollider = colliderGo.AddComponent<BoxCollider>();
                boxCollider.isTrigger = true;

                var manager = managerGo.AddComponent<PathFinder>();
            } else {
                if (Logger.CanLogError) Logger.LogError("PathFollower Script already exists!");
            }
        }


        #region OnInspectorGUI

        //public override void OnInspectorGUI() {
        //    DrawDefaultInspector();
        //    CustomGUI.DrawSeparator(Color.gray);
        //    EditorGUILayout.Space();
        //}

        private void ShowNodesAndPathInInspector() {
            script.graphData.nodeSize = EditorGUILayout.Slider("Node gizmo Size", script.graphData.nodeSize, 0.1f, 3f);
            script.graphData.lineColor = EditorGUILayout.ColorField("Path Color", script.graphData.lineColor);
            script.graphData.lineType = (PathLineType)EditorGUILayout.EnumPopup("Path Type", script.graphData.lineType);
            script.graphData.heightFromTheGround = EditorGUILayout.FloatField("Offset from ground( Height )", script.graphData.heightFromTheGround);
            script.graphData.groundColliderLayerName = EditorGUILayout.TextField("Ground collider layer name", script.graphData.groundColliderLayerName);
            EditorGUILayout.Space();
            GUILayout.Label("<size=12><b>Nodes</b></size>", CustomGUI.GetStyleWithRichText());

            if (script.graphData.nodes.Count > 0) {

                List<Node> nodeList = script.graphData.nodes;
                for (int j = 0; j < nodeList.Count; j++) {
                    GUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("\t" + "Node <Color=" + nodeGUITextColor + ">" + nodeList[j].ID + "</Color>", CustomGUI.GetStyleWithRichText(), GUILayout.Width(120f));

                        nodeList[j].SetPosition(EditorGUILayout.Vector3Field("", nodeList[j].Position));
                        if (GUILayout.Button("+", GUILayout.Width(25f)))
                            AddNode(nodeList[j].Position + Vector3.right + Vector3.up, j + 1);
                        if (GUILayout.Button("-", GUILayout.Width(25f)))
                            DeleteNode(j);
                    }
                    GUILayout.EndHorizontal();
                }
            } else {
                EditorGUILayout.LabelField("<Color=green> Nodes are empty. Use <b>Add Node</b> in scene view to create Nodes!</Color>", CustomGUI.GetStyleWithRichText(CustomGUI.SetAlignmentForText(TextAnchor.MiddleCenter)));
            }
            EditorGUILayout.Space();
            GUILayout.Label("<size=12><b>Paths</b></size>", CustomGUI.GetStyleWithRichText());

            //for (Path pd = script.graphData.getnext(); pd != null; pd = script.graphData.getnext()) {
            List<Path> paths = script.graphData.Paths;
            foreach (Path pd in paths) {
                GUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("\t" + "Path <Color=" + pathGUITextColor + ">" + pd.autoGeneratedID + "</Color>", CustomGUI.GetStyleWithRichText(), GUILayout.Width(120f));

                    EditorGUILayout.LabelField("From", EditorStyles.miniLabel, GUILayout.Width(30f)); pd.IDOfA = EditorGUILayout.IntField(pd.IDOfA, GUILayout.Width(50f));
                    EditorGUILayout.LabelField("To", EditorStyles.miniLabel, GUILayout.Width(25f)); pd.IDOfB = EditorGUILayout.IntField(pd.IDOfB, GUILayout.Width(50f));
                    EditorGUILayout.LabelField("<Color=" + costGUITextColor + ">" + "Cost" + "</Color>", CustomGUI.GetStyleWithRichText(EditorStyles.miniLabel), GUILayout.Width(30f)); pd.Cost = EditorGUILayout.FloatField(pd.Cost, GUILayout.Width(50f));

                    EditorGUILayout.LabelField("One Way", EditorStyles.miniLabel, GUILayout.Width(50f)); pd.isOneWay = EditorGUILayout.Toggle(pd.isOneWay);

                    //if (GUILayout.Button("+", GUILayout.Width(25f)))
                    //    AddPath(j + 1);
                    //if (GUILayout.Button("-", GUILayout.Width(25f)))
                    //    DeletePath(j);
                }
                GUILayout.EndHorizontal();
            }

            if (GUI.changed)
                MarkThisDirty();
        }

        #endregion

        #region On Scene Rendering and Scene GUI



        private void OnSceneGUI() {
            int controlID = GUIUtility.GetControlID(FocusType.Passive);
            HandleUtility.AddDefaultControl(controlID);
            DrawGUIWindowOnScene();
            UpdateMouseInput();

            //if (sceneMode == SceneMode.AddJunction) {
            //    DrawNodes(Color.green);
            //} else if (sceneMode == SceneMode.SelectJunction) {
            //    DrawNodes(Color.magenta, true);
            //} else if (sceneMode == SceneMode.ConnectPath) {
            //    DrawNodes(Color.green, false, script.graphData.GetNode(selectedNodeForConnectNodesMode), Color.red);
            //} else if (sceneMode == SceneMode.AddStreet) {
            //    DrawNodes(Color.green, false, script.graphData.GetNode(selectedNodeForConnectNodesMode), Color.red);
            //} else
            //    DrawNodes(Color.gray);
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
                    sceneMode = (SceneMode)GUILayout.SelectionGrid((int)sceneMode, new string[] { "Add Node", "Select Node", "Add Street", "Select Street", "None","Delete Junction","Delete Street" }, 1);

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

            GUILayout.Window(2, new Rect(0, 155f, 70f, 80f),
                delegate (int windowID) {
                    EditorGUILayout.BeginVertical();
                    if (GUILayout.Button("Delete Node"))
                        DeleteNode();
                    //if (GUILayout.Button("Delete Path"))
                    //    DeletePath();
                    if (GUILayout.Button("Clear All")) {
                        ClearNodes();
                        ClearPaths();
                    }
                    if (GUILayout.Button("Refresh Data")) {
                        script.graphData.ReCalculate();
                    }
                    GUI.color = Color.white;
                    EditorGUILayout.EndVertical();
                }, "");

            if (sceneMode == SceneMode.SelectStreet && SelectedStreet) {
                GUI.Window(3, new Rect(100f, 25f, 140f, 160f),
                delegate (int windowID) {
                    GUI.color = Color.white;
                    EditorGUILayout.BeginHorizontal();
                    GUI.Label(new Rect(0, 20, 70, 15), "Pasy ruchu");
                    GUI.Label(new Rect(0, 40, 70, 15), "Pasy ruchu");
                    GUI.Label(new Rect(90, 20, 20, 15), SelectedStreet.ifrom.ToString());
                    GUI.Label(new Rect(90, 40, 20, 15), SelectedStreet.ito.ToString());
                    if (GUI.Button(new Rect(105, 20, 15, 15), "<")) {
                        SelectedStreet.ifrom = Mathf.Clamp((SelectedStreet.ifrom - 1), 0, 10);
                        SelectedStreet.Recalculate();
                        script.graphData.ReCalculate();
                    }
                    if (GUI.Button(new Rect(120, 20, 15, 15), ">")) {
                        SelectedStreet.ifrom = Mathf.Clamp((SelectedStreet.ifrom + 1), 0, 10);
                        SelectedStreet.Recalculate();
                        script.graphData.ReCalculate();
                    }
                    if (GUI.Button(new Rect(105, 40, 15, 15), "<")) {
                        SelectedStreet.ito = Mathf.Clamp((SelectedStreet.ito - 1), 0, 10);
                        SelectedStreet.Recalculate();
                        script.graphData.ReCalculate();
                    }
                    if (GUI.Button(new Rect(120, 40, 15, 15), ">")) {
                        SelectedStreet.ito = Mathf.Clamp((SelectedStreet.ito + 1), 0, 10);
                        SelectedStreet.Recalculate();
                        script.graphData.ReCalculate();
                    }
                    //przyjezdni przybysz delegat / dom mieszkañcy / sklep miejsca handlowe / praca miejsca pracy / wyje¿d¿aj¹cy emigranci 
                    string[] Describe = new string[5] { "Przyjezdni", "Mieszkañcy", "Hiejsca handlowe", "Miejsca pracy", "Wyje¿d¿aj¹cy" };
                    Color[] col = new Color[5] { new Color(0.9f, 0.9f, 0.9f), new Color(0, 1, 0), new Color(0, 0.75f, 1), new Color(1, 1, 0), new Color(1, 0.58f, 0.62f) };
                    for (int i = 0; i < 5; i++) {
                        GUI.color = col[i];
                        GUI.Label(new Rect(0, 60 + i * 20, 120, 15), Describe[i]);
                        string tmp = GUI.TextField(new Rect(110, 60 + i * 20, 25, 15), SelectedStreet.Spawns[i].ToString(), 25);
                        tmp = tmp.Length == 0 ? "0" : tmp;
                        try {
                            SelectedStreet.Spawns[i] = int.Parse(tmp);
                        }
                        catch (FormatException) { }
                    }
                    GUI.color = Color.white;
                    EditorGUILayout.EndHorizontal();
                }, "Select Street");
            }
            if (sceneMode == SceneMode.SelectJunction && SelectedJunction != null) {
                GUI.Window(5, new Rect(100f, 25f, 140f, 160f),
                delegate (int windowID) {
                    GUI.color = Color.white;
                    EditorGUILayout.BeginHorizontal();
                    GUI.Label(new Rect(0, 20, 70, 15), "Rondo");
                    SelectedJunction.Rondo = GUI.Toggle(new Rect(110, 20, 20, 15), SelectedJunction.Rondo, "");
                    if (SelectedJunction.Rondo == false) {
                        Color[] col = new Color[4] { Color.red, Color.green, Color.blue, Color.yellow };
                        GUI.Label(new Rect(0, 60, 120, 15), "Timers");
                        for (int i = 0; i < SelectedJunction.Timers.Length; i++) {
                            GUI.color = col[i];
                            string tmp = GUI.TextField(new Rect(110, 60 + i * 20, 25, 15), SelectedJunction.Timers[i].ToString(), 25);
                            tmp = tmp.Length == 0 ? "0" : tmp;
                            try {
                                SelectedJunction.Timers[i] = float.Parse(tmp);
                            }
                            catch (FormatException) { }
                        }
                    }
                    GUI.color = Color.white;
                    EditorGUILayout.EndHorizontal();
                }, "Select Junction");
            }

            if (sceneMode == SceneMode.AddStreet) {
                GUI.Window(4, new Rect(100f, 25f, 140f, 60f), delegate (int windowID) {
                    EditorGUILayout.BeginHorizontal();
                    GUI.color = Color.white;
                    GUI.Label(new Rect(0, 20, 70, 15), "Pasy ruchu");
                    GUI.Label(new Rect(0, 40, 70, 15), "Pasy ruchu");
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
        }


        private void DrawNodes(Color color, bool canMove = false, Node selectedNode = null, Color colorForSelected = default(Color)) {
            Handles.color = color;
            foreach (var node in script.graphData.nodes) {
                if (selectedNode != null && node == selectedNode)
                    Handles.color = colorForSelected;
                else
                    Handles.color = color;

                //if (canMove)
                //    node.SetPosition(Handles.FreeMoveHandle(node.Position, Quaternion.identity, script.graphData.nodeSize, Vector3.zero, Handles.SphereCap));
                //else
                //    Handles.SphereCap(0, node.Position, Quaternion.identity, script.graphData.nodeSize);
            }
            Handles.color = Color.white;
            DrawGUIDisplayForNodes();
            Handles.color = Color.white;
        }

        private void DrawPathLine() {
            if (script.graphData.nodes == null)
                return;

            ColoredTexture Colors = new ColoredTexture();
            //List<Path> paths = script.graphData.Paths;
            Vector3 currNode;
            Vector2 guiPosition;

            //for (Path pd = script.graphData.getnext(); pd != null; pd = script.graphData.getnext()) {
            foreach (Path pd in script.graphData.Paths) {
                switch (pd.Block) {
                    case 0:
                        Handles.color = Color.red;
                        break;
                    case 1:
                        Handles.color = Color.green;
                        break;
                    default:
                        Handles.color = script.graphData.lineColor;
                        break;
                }
                if (script.drawPaths && sceneMode != SceneMode.SelectJunction)
                    Handles.DrawLine(pd.a.Position, pd.b.Position);

                Handles.BeginGUI();
                currNode = (pd.a.Position + pd.b.Position) / 2;
                guiPosition = HandleUtility.WorldToGUIPoint(currNode);
                string str = "";
                if (script.showPathId)
                    str += "<Color=" + pathGUITextColor + ">" + pd.autoGeneratedID.ToString() + "</Color>";
                if (script.showCosts) {
                    if (!string.IsNullOrEmpty(str))
                        str += "<Color=" + "#ffffff" + ">" + "  Cost: " + "</Color>";
                    str += "<Color=" + costGUITextColor + ">" + pd.Cost.ToString() + "</Color>";
                }

                if (!string.IsNullOrEmpty(str))
                    GUI.Label(new Rect(guiPosition.x - 10, guiPosition.y - 30, 40, 20), str, CustomGUI.GetStyleWithRichText());

                Handles.EndGUI();
            }
            if (script.showSpawns) {
                Handles.BeginGUI();
                foreach (Street s in script.graphData.AllStreets) {
                    guiPosition = HandleUtility.WorldToGUIPoint(s.center.Position);
                    int i = 0;
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
            if (script.showNodeId) {
                Handles.BeginGUI();
                foreach (Node n in script.graphData.nodes) {
                    guiPosition = HandleUtility.WorldToGUIPoint(n.Position);
                    GUI.Label(new Rect(guiPosition.x - 10, guiPosition.y - 30, 20, 20), "<Color=" + nodeGUITextColor + ">" + n.ID.ToString() + "</Color>", CustomGUI.GetStyleWithRichText());
                }
                Handles.EndGUI();
            }


            if (sceneMode == SceneMode.SelectJunction && SelectedJunction != null && SelectedJunction.Rondo == false) {
                Color[] colors = new Color[4] { Color.red, Color.green, Color.blue, Color.yellow };
                for (int i = 0; i < SelectedJunction.phases.Length; i++) {
                    Handles.color = colors[i];
                    Vector3 v = new Vector3(0, i * 0.1f, 0);
                    foreach (int j in SelectedJunction.phases[i].Routes) {
                        Handles.DrawLine(SelectedJunction.paths[j].a.Position + v, SelectedJunction.paths[j].b.Position + v);
                    }
                }
            }


            if (script.showSpawns) {
                Handles.BeginGUI();
                foreach (Junction j in script.graphData.AllJunctions) {
                    if (j.phases != null && j.phases.Length > 0 && j.Rondo == false) {
                        guiPosition = HandleUtility.WorldToGUIPoint(j.transform.position);
                        GUI.Label(new Rect(guiPosition.x - 20, guiPosition.y - 20, 40, 20), "<Color=" + junctionGUITextColor + ">" + j.TimeToPhase.ToString("0.0") + "</Color>", CustomGUI.GetStyleWithRichText());
                    }
                }
                Handles.EndGUI();
            }
            Handles.color = Color.white;
        }

        private void DrawGUIDisplayForNodes() {
            if (!script.showNodeId)
                return;

            Node currNode;
            Vector2 guiPosition;
            Handles.BeginGUI();

            for (int i = 0; i < script.graphData.nodes.Count; i++) {
                currNode = script.graphData.nodes[i];
                guiPosition = HandleUtility.WorldToGUIPoint(currNode.Position);
                GUI.Label(new Rect(guiPosition.x - 10, guiPosition.y - 30, 20, 20), "<Color=" + nodeGUITextColor + ">" + currNode.ID.ToString() + "</Color>", CustomGUI.GetStyleWithRichText());
            }
            Handles.EndGUI();
        }

        #endregion

        #region Input Method

        void UpdateMouseInput() {
            Event e = Event.current;
            if (e.type == EventType.MouseDown) {
                if (e.button == 0)
                    OnMouseClick(e.mousePosition);
            } else if (e.type == EventType.MouseUp) {
                MarkThisDirty();
                SceneView.RepaintAll();
            }
        }

        void OnMouseClick(Vector2 mousePos) {
            LayerMask backgroundLayerMask = 1 << LayerMask.NameToLayer(script.graphData.groundColliderLayerName);
            Ray ray = HandleUtility.GUIPointToWorldRay(mousePos);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100000f, backgroundLayerMask)) {
                if (sceneMode == SceneMode.AddJunction) {
                    Vector3 hitPos = hit.point;
                    hitPos += (-ray.direction.normalized) * script.graphData.heightFromTheGround;
                    CreateJunction(hitPos);
                }
                if (sceneMode == SceneMode.SelectJunction) {
                    if (hit.transform.name == "Junction(Clone)") {
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
            }
        }
        public void CreateJunction(Vector3 pos) {
            GameObject go = Instantiate(script.graphData.JunctionPrefab, pos, Quaternion.identity, script.transform);
            Junction ju = go.AddComponent<Junction>();
            script.graphData.AllJunctions.Add(ju);
            script.graphData.ReCalculate();
        }
        public void CreateStreet(Junction a, Junction b) {
            GameObject go = new GameObject("Street");
            go.transform.parent = script.transform;
            Street st = go.AddComponent<Street>();
            st.Init(a, b, ifro, ito);
            script.graphData.AllStreets.Add(st);
            script.graphData.ReCalculate();
            a.AddStreet(st);
            b.AddStreet(st, 1);
            script.graphData.ReCalculate();
        }

        #endregion


        #region Node and Path methods

        void AddNode(Vector3 position, int addIndex = -1) {
            Node nodeAdded = new Node(position);
            if (addIndex == -1)
                script.graphData.nodes.Add(nodeAdded);
            else
                script.graphData.nodes.Insert(addIndex, nodeAdded);

            script.graphData.ReGenerateIDs();

            Logger.LogInfo("Node with ID:" + nodeAdded.ID + " Added!");
        }

        void DeleteNode(int removeIndex = -1) {
            List<Node> nodeList = script.graphData.nodes;
            if (nodeList == null || nodeList.Count == 0)
                return;

            if (removeIndex == -1)
                removeIndex = nodeList.Count - 1;

            Node nodeRemoved = nodeList[removeIndex];
            nodeList.RemoveAt(removeIndex);
            script.graphData.ReGenerateIDs();

            Logger.LogInfo("Node with ID:" + nodeRemoved.ID + " Removed!");
        }

        void DeleteNode(Node Index) {
            List<Node> nodeList = script.graphData.nodes;
            if (nodeList == null || nodeList.Count == 0)
                return;

            nodeList.Remove(Index);
            script.graphData.ReGenerateIDs();
        }

        void ClearNodes() {
            script.graphData.nodes.Clear();
            script.graphData.center.Clear();
        }
        void ClearPaths() {
            foreach (Street s in script.graphData.AllStreets) {
                DestroyImmediate(s.gameObject);
            }
            foreach (Junction s in script.graphData.AllJunctions) {
                DestroyImmediate(s.gameObject);
            }
            script.graphData.AllStreets.Clear();
            script.graphData.AllJunctions.Clear();
            //script.graphData.nodesSorted.Clear();
            //script.graphData.pathsSorted.Clear();
        }

        #endregion

        #region PRIVATE

        private void OnEnable() {
            sceneMode = SceneMode.None;
            script = FindObjectOfType<PathFinder>();
            script.graphData.ReGenerateIDs();
            //Debug.Log("OnEnable: registering playModeStateChanged");
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }
        private void OnDisable() {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }
        void OnPlayModeStateChanged(PlayModeStateChange state) {
            if (state == PlayModeStateChange.EnteredEditMode) {
                script.graphData.loadTimers();
            }
            if (state == PlayModeStateChange.ExitingPlayMode) {
                script.graphData.saveTimers();
            }

        }

        // When anything in inspector is changed, this will mark the scene or the prefab dirty
        void MarkThisDirty() {
            if (Application.isPlaying)
                return;

            if (PrefabUtility.GetCorrespondingObjectFromSource(script.gameObject) != null) {
                Logger.LogInfo("Prefab for PathFinder found! Marked it Dirty ( Modified )");
                EditorUtility.SetDirty(script);
            } else {
                //Logger.LogInfo ( "Prefab for PathFinder Not found! Marked the scene as Dirty ( Modified )");
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
        }


        private SceneMode sceneMode;
        private PathFinder script;


        const string nodeGUITextColor = "#ff00ffff";
        const string pathGUITextColor = "#00ffffff";
        const string costGUITextColor = "#0000ffff";
        const string junctionGUITextColor = "#00ff00ff";

        //private int selectedNodeForConnectNodesMode = -1;

        #endregion
    }
}