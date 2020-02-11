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
            AddNode,
            EditNode,
            SelectStreet,
            AddStreet,
            None
        }
        Junction last;
        Street SelectedStreet;
        int ifro = 2, ito = 2;
        int win = 0;

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

        public override void OnInspectorGUI() {
            showDefaultInspector = EditorGUILayout.Toggle("Show Default inspector", showDefaultInspector);
            if (showDefaultInspector) {
                DrawDefaultInspector();
            } else {
                CustomGUI.DrawSeparator(Color.gray);
                //ShowNodesAndPathInInspector();
            }

        }

        private void ShowNodesAndPathInInspector() {
            script.graphData.nodeSize = EditorGUILayout.Slider("Node gizmo Size", script.graphData.nodeSize, 0.1f, 3f);
            script.graphData.lineColor = EditorGUILayout.ColorField("Path Color", script.graphData.lineColor);
            script.graphData.lineType = (PathLineType)EditorGUILayout.EnumPopup("Path Type", script.graphData.lineType);
            script.graphData.heightFromTheGround = EditorGUILayout.FloatField("Offset from ground( Height )", script.graphData.heightFromTheGround);
            script.graphData.groundColliderLayerName = EditorGUILayout.TextField("Ground collider layer name", script.graphData.groundColliderLayerName);
            EditorGUILayout.Space();
            GUILayout.Label("<size=12><b>Nodes</b></size>", CustomGUI.GetStyleWithRichText());

            if (script.graphData.nodes.Count > 0) {
                showNodeIDsInTheScene = EditorGUILayout.Toggle("Show Node IDs in scene", showNodeIDsInTheScene);

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

            showPathIDsInTheScene = EditorGUILayout.Toggle("Show Path IDs in scene", showPathIDsInTheScene);
            drawPathsInTheScene = EditorGUILayout.Toggle("Draw Paths", drawPathsInTheScene);
            showCostsInTheScene = EditorGUILayout.Toggle("Show Path Costs in scene", showCostsInTheScene);

            //for (Path pd = script.graphData.getnext(); pd != null; pd = script.graphData.getnext()) {
            List<Path> paths = script.graphData.Paths;
            foreach (Path pd in paths) {
                GUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("\t" + "Path <Color=" + pathGUITextColor + ">" + pd.autoGeneratedID + "</Color>", CustomGUI.GetStyleWithRichText(), GUILayout.Width(120f));

                    EditorGUILayout.LabelField("From", EditorStyles.miniLabel, GUILayout.Width(30f)); pd.IDOfA = EditorGUILayout.IntField(pd.IDOfA, GUILayout.Width(50f));
                    EditorGUILayout.LabelField("To", EditorStyles.miniLabel, GUILayout.Width(25f)); pd.IDOfB = EditorGUILayout.IntField(pd.IDOfB, GUILayout.Width(50f));
                    EditorGUILayout.LabelField("<Color=" + costGUITextColor + ">" + "Cost" + "</Color>", CustomGUI.GetStyleWithRichText(EditorStyles.miniLabel), GUILayout.Width(30f)); pd.Cost = EditorGUILayout.IntField(pd.Cost, GUILayout.Width(50f));

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

            //if (sceneMode == SceneMode.AddNode) {
            //    DrawNodes(Color.green);
            //} else if (sceneMode == SceneMode.EditNode) {
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
                                                                sceneMode = (SceneMode)GUILayout.SelectionGrid((int)sceneMode, new string[] { "Add Node",
                                                                    "Move Node", "SelectStreet", "AddStreet", "None" }, 1);
                                                                if (sceneMode == SceneMode.SelectStreet) {
                                                                    win = 1;
                                                                } else if (sceneMode == SceneMode.AddStreet) {
                                                                    win = 2;
                                                                } else {
                                                                    win = 0;
                                                                    if (SelectedStreet)
                                                                        SelectedStreet.Select(false);
                                                                }
                                                                GUI.color = Color.white;
                                                                EditorGUILayout.EndHorizontal();
                                                            }
                            , "Mode");
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
                                                            }
                                , "");
            if (win != 0) {
                GUI.Window(3, new Rect(100f, 25f, 100f, 80f),
                                                                delegate (int windowID) {
                                                                EditorGUILayout.BeginHorizontal();
                                                                    if (win == 1 && SelectedStreet) {
                                                                        GUI.Label(new Rect(0, 20, 50, 15), "Route " + SelectedStreet.ifrom);
                                                                        GUI.Label(new Rect(0, 40, 50, 15), "Route " + SelectedStreet.ito);
                                                                        if (GUI.Button(new Rect(65, 20, 15, 15), "<")) {
                                                                            SelectedStreet.ifrom = Mathf.Clamp((SelectedStreet.ifrom - 1), 0, 10);
                                                                            SelectedStreet.Recalculate();
                                                                            script.graphData.ReCalculate();
                                                                        }
                                                                        if (GUI.Button(new Rect(80, 20, 15, 15), ">")) {
                                                                            SelectedStreet.ifrom = Mathf.Clamp((SelectedStreet.ifrom + 1), 0, 10);
                                                                            SelectedStreet.Recalculate();
                                                                            script.graphData.ReCalculate();
                                                                        }
                                                                        if (GUI.Button(new Rect(65, 40, 15, 15), "<")) {
                                                                            SelectedStreet.ito = Mathf.Clamp((SelectedStreet.ito - 1), 0, 10);
                                                                            SelectedStreet.Recalculate();
                                                                            script.graphData.ReCalculate();
                                                                        }
                                                                        if (GUI.Button(new Rect(80, 40, 15, 15), ">")) {
                                                                            SelectedStreet.ito = Mathf.Clamp((SelectedStreet.ito + 1), 0, 10);
                                                                            SelectedStreet.Recalculate();
                                                                            script.graphData.ReCalculate();
                                                                        }
                                                                    }
                                                                    if (win == 2) {
                                                                        GUI.Label(new Rect(0, 20, 50, 15), "Route " + ifro);
                                                                        GUI.Label(new Rect(0, 40, 50, 15), "Route " + ito);
                                                                        if (GUI.Button(new Rect(65, 20, 15, 15), "<")) {
                                                                            ifro = Mathf.Clamp((ifro - 1), 0, 10);
                                                                        }
                                                                        if (GUI.Button(new Rect(80, 20, 15, 15), ">")) {
                                                                            ifro = Mathf.Clamp((ifro + 1), 0, 10);
                                                                        }
                                                                        if (GUI.Button(new Rect(65, 40, 15, 15), "<")) {
                                                                            ito = Mathf.Clamp((ito - 1), 0, 10);
                                                                        }
                                                                        if (GUI.Button(new Rect(80, 40, 15, 15), ">")) {
                                                                            ito = Mathf.Clamp((ito + 1), 0, 10);
                                                                        }
                                                                    }
                                                                    GUI.color = Color.white;
                                                                    EditorGUILayout.EndHorizontal();
                                                                }
                                , "Mode");
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
            //List<Path> paths = script.graphData.Paths;
            Vector3 currNode;
            Vector2 guiPosition;

            if (script.graphData.nodes == null)
                return;


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
                if (drawPathsInTheScene)
                    Handles.DrawLine(pd.a.Position, pd.b.Position);

                Handles.BeginGUI();
                {
                    currNode = (pd.a.Position + pd.b.Position) / 2;
                    guiPosition = HandleUtility.WorldToGUIPoint(currNode);
                    string str = "";
                    if (showPathIDsInTheScene)
                        str += "<Color=" + pathGUITextColor + ">" + pd.autoGeneratedID.ToString() + "</Color>";
                    if (showCostsInTheScene) {
                        if (!string.IsNullOrEmpty(str))
                            str += "<Color=" + "#ffffff" + ">" + "  Cost: " + "</Color>";
                        str += "<Color=" + costGUITextColor + ">" + pd.Cost.ToString() + "</Color>";
                    }

                    if (!string.IsNullOrEmpty(str))
                        GUI.Label(new Rect(guiPosition.x - 10, guiPosition.y - 30, 40, 20), str, CustomGUI.GetStyleWithRichText());
                }
                Handles.EndGUI();
            }
            Handles.color = Color.white;
        }

        private void DrawGUIDisplayForNodes() {
            if (!showNodeIDsInTheScene)
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
                if (sceneMode == SceneMode.AddNode) {

                    Vector3 hitPos = hit.point;
                    hitPos += (-ray.direction.normalized) * script.graphData.heightFromTheGround;
                    //AddNode(hitPos);
                    CreateJunction(hitPos);
                }
                if (sceneMode == SceneMode.SelectStreet) {
                    if (hit.transform.parent.name == "Street") {
                        if (SelectedStreet) {
                            SelectedStreet.Select(false);
                        }
                        SelectedStreet = hit.transform.parent.GetComponent<Street>();
                        SelectedStreet.Select();
                    }

                } else {
                    if (SelectedStreet) {
                        SelectedStreet.Select(false);
                    }
                }
                if (sceneMode == SceneMode.AddStreet) {
                    if (last == null) {
                        last = hit.transform.gameObject.GetComponent<Junction>();
                    } else {
                        Junction b = hit.transform.gameObject.GetComponent<Junction>();
                        if (b != null) {
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
            a.AddStreet(st);
            b.AddStreet(st, 1);
            script.graphData.AllStreets.Add(st);
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
            Logger.LogWarning("All Nodes are cleared!");
            script.graphData.Spawn.Clear();
            script.graphData.Target.Clear();

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
            script.graphData.nodesSorted.Clear();
            script.graphData.pathsSorted.Clear();

        }

        #endregion

        #region PRIVATE

        private void OnEnable() {
            sceneMode = SceneMode.None;
            script = FindObjectOfType<PathFinder>();
            script.graphData.ReGenerateIDs();
        }

        // When anything in inspector is changed, this will mark the scene or the prefab dirty
        private void MarkThisDirty() {
            if (Application.isPlaying)
                return;

            if (PrefabUtility.GetCorrespondingObjectFromSource(script.gameObject) != null) {
                //Logger.LogInfo ( "Prefab for PathFinder found! Marked it Dirty ( Modified )");
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

        //private int selectedNodeForConnectNodesMode = -1;
        private bool showNodeIDsInTheScene = true;
        private bool showPathIDsInTheScene = true;
        private bool drawPathsInTheScene = true;
        private bool showCostsInTheScene = false;
        private bool showDefaultInspector = true;

        #endregion
    }
}