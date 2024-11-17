using Core.Meshing;
using MyEditor.Train.Data;
using Rail;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MyEditor.Train
{
    public class RailSceneManager : EditorWindow
    {
        private Railroad _currentSelectedRail;
        private bool _editingTerrain;
        private bool _editingMesh;
        private bool _railSelected;
        private bool _junctionMenu;
        private Vector2 _scrollPosition;

        private RailResources _data;

        [MenuItem("Tools/Rail Scene Manager")]
        public static void ShowWindow()
        {
            GetWindow<RailSceneManager>();
        }

        private void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChanged;
            _data = Resources.Load("Data/RailResources") as RailResources;

            _railMesh = _data.Railmesh;
            _railMaterial = _data.RailMaterial;
            _ballastMesh = _data.BallastMesh;
            _ballastMaterial = _data.BallastMaterial;
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
        }

        private void OnSelectionChanged()
        {
            _editingTerrain = false;
            _editingMesh = false;
            _junctionMenu = false;
            GameObject go = Selection.activeGameObject;

            if (go != null)
            {
                _railSelected = go.TryGetComponent(out Railroad selectedRail);
                _currentSelectedRail = selectedRail;
                Repaint();
                return;
            }
            _currentSelectedRail = null;
            _railSelected = false;
            Repaint();
        }

        private void OnGUI()
        {
            GUILayout.BeginHorizontal();
            {
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
                DrawRailMenu();
                EditorGUILayout.EndScrollView();
            }
            GUILayout.EndHorizontal();
        }

        private void DrawRailMenu()
        {
            GUILayout.BeginVertical();
            {
                GUILayout.Label("Rail Spawn Options");

                _globalHeight = EditorGUILayout.FloatField("Spawn Height", _globalHeight);

                if (!_railSelected)
                {
                    if (GUILayout.Button("+ Add new rail"))
                    {
                        GameObject rail = Instantiate(_data.Rail);
                        Undo.RegisterCreatedObjectUndo(rail, " new rail");
                        SceneView.lastActiveSceneView.MoveToView(rail.transform);
                        rail.transform.position = new Vector3(rail.transform.position.x, _globalHeight, rail.transform.position.z);
                        Selection.activeGameObject = rail;
                        rail.name = $"Rail({rail.GetInstanceID()})";
                        UpdateRailMesh(rail.GetComponent<Railroad>());
                    }
                }

                if (_railSelected)
                {
                    if (GUILayout.Button(" - Remove selected rail"))
                    {
                        DestroyImmediate(_currentSelectedRail.gameObject);
                    }

                    if (_currentSelectedRail.GetBRail())
                    {
                        EditorGUILayout.LabelField("Current rail has next rail", EditorStyles.centeredGreyMiniLabel);
                    }
                    else
                    {
                        if (GUILayout.Button("+ Add rail next to selected"))
                        {
                            AddStraightRailConnected();
                        }

                        FancySeparator(Color.blue);
                        DrawJunctionSpawnMenu();
                    }

                    FancySeparator(Color.blue);

                    DrawRailMergeOptions();
                }

                FancySeparator(Color.gray);

                GUILayout.Label("Rail Cosmetics Options");

                if (_currentSelectedRail != null)
                {
                    if (GUILayout.Button("Update Rail Mesh"))
                    {
                        _junctionMenu = false;
                        _editingMesh = !_editingMesh;
                        _editingTerrain = false;
                    }

                    if (GUILayout.Button("Conform Terrain to Rail"))
                    {
                        _junctionMenu = false;
                        _editingMesh = false;
                        _editingTerrain = !_editingTerrain;
                    }

                    if (_editingTerrain)

                    {
                        FancySeparator(Color.green);
                        DrawTerrainConformingTool();
                    }

                    if (_editingMesh)
                    {
                        FancySeparator(Color.red);
                        DrawRailMeshTool();
                    }
                }
                else EditorGUILayout.LabelField("Select rail", EditorStyles.centeredGreyMiniLabel);
            }
            GUILayout.EndVertical();
        }

        private Railroad _targetRail;

        private void DrawRailMergeOptions()
        {
            _railMergerCollapse = EditorGUILayout.BeginFoldoutHeaderGroup(_railMergerCollapse, "Rail Merger Options");

            if (_railMergerCollapse)
            {
                _targetRail = EditorGUILayout.ObjectField(("Target Rail"), _targetRail, typeof(Railroad), true) as Railroad;

                if (_targetRail == _currentSelectedRail)
                {
                    EditorGUILayout.HelpBox("Can't merge rail to itself", MessageType.Error);
                    return;
                }

                if (_targetRail)
                {
                    GUILayout.BeginVertical();
                    if (GUILayout.Button("Connect B point"))
                    {
                        RailData data = _targetRail.GetRailDataFromPoint(_currentSelectedRail.GetRailDataFromTime(1).NearestPosition);

                        data.NearestPosition = _currentSelectedRail.transform.InverseTransformPoint(data.NearestPosition);

                        if (data.Time >= 0.5f)
                        {
                            _currentSelectedRail.ActiveSplineContainer[0].SetKnot(0, new UnityEngine.Splines.BezierKnot(data.NearestPosition));
                        }
                        else
                        {
                            int length = _currentSelectedRail.ActiveSplineContainer[0].Knots.ToArray().Length;
                            _currentSelectedRail.ActiveSplineContainer[0].SetKnot(length - 1, new UnityEngine.Splines.BezierKnot(data.NearestPosition));
                        }
                        _currentSelectedRail.SetBRail(_targetRail);
                        _targetRail.SetARail(_currentSelectedRail);
                    }

                    if (GUILayout.Button("Connect A point"))
                    {
                        RailData data = _targetRail.GetRailDataFromPoint(_currentSelectedRail.GetRailDataFromTime(0).NearestPosition);
                        data.NearestPosition = _currentSelectedRail.transform.InverseTransformPoint(data.NearestPosition);

                        if (data.Time >= 0.5f)
                        {
                            _currentSelectedRail.ActiveSplineContainer[0].SetKnot(0, new UnityEngine.Splines.BezierKnot(data.NearestPosition));
                        }
                        else
                        {
                            int length = _currentSelectedRail.ActiveSplineContainer[0].Knots.ToArray().Length;
                            _currentSelectedRail.ActiveSplineContainer[0].SetKnot(length - 1, new UnityEngine.Splines.BezierKnot(data.NearestPosition));
                        }
                        _currentSelectedRail.SetARail(_targetRail);
                        _targetRail.SetBRail(_currentSelectedRail);
                    }

                    if (_targetRail is RailroadJunction)
                    {
                        if (GUILayout.Button("Connect to C point"))
                        {
                        }
                    }

                    GUILayout.EndVertical();
                }
                else EditorGUILayout.HelpBox("Assign Rail to merge to", MessageType.Warning);
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawJunctionSpawnMenu()
        {
            EditorGUILayout.BeginVertical();

            EditorGUILayout.LabelField("Rail Junction Creation", EditorStyles.largeLabel);

            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Add split left"))
                {
                    RailroadJunction rail = Instantiate(_data.JunctionOutLeft).GetComponent<RailroadJunction>();
                    rail.SetARail(_currentSelectedRail);
                    rail.transform.position = _currentSelectedRail.GetRailDataFromTime(1).NearestPosition;
                    rail.transform.forward = _currentSelectedRail.GetRailDataFromTime(1).Tangent.normalized;
                    rail.name = $"RailJunction({rail.GetInstanceID()})";
                    _currentSelectedRail.SetBRail(rail);

                    Railroad b = ExtendRailJunction(rail);
                    Railroad c = ExtendRailJunction(rail);

                    rail.SetCRail(c);
                    rail.SetBRail(b);

                    c.transform.position = rail.GetRailDataFromTime(1, 1).NearestPosition;
                    c.transform.forward = rail.GetRailDataFromTime(1, 1).Tangent.normalized;

                    UpdateRailMesh(b);
                    UpdateRailMesh(c);

                    Undo.RegisterCreatedObjectUndo(rail, " new rail junction");
                    Undo.RegisterCreatedObjectUndo(b, " new rail junction");
                    Undo.RegisterCreatedObjectUndo(c, " new rail junction");

                }
                
                if (GUILayout.Button("Add split right"))
                {
                    RailroadJunction rail = Instantiate(_data.JunctionOutRight).GetComponent<RailroadJunction>();
                    rail.SetARail(_currentSelectedRail);
                    rail.transform.position = _currentSelectedRail.GetRailDataFromTime(1).NearestPosition;
                    rail.transform.forward = _currentSelectedRail.GetRailDataFromTime(1).Tangent;
                    rail.name = $"RailJunction({rail.GetInstanceID()})";
                    _currentSelectedRail.SetBRail(rail);

                    Railroad b = ExtendRailJunction(rail);
                    Railroad c = ExtendRailJunction(rail);
                    rail.SetCRail(c);
                    rail.SetBRail(b);

                    c.transform.position = rail.GetRailDataFromTime(1, 1).NearestPosition;
                    c.transform.forward = rail.GetRailDataFromTime(1, 1).Tangent.normalized;

                    UpdateRailMesh(b);
                    UpdateRailMesh(c);
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Add merge left"))
                {
                    RailroadJunction rail = Instantiate(_data.JunctionInLeft).GetComponent<RailroadJunction>();

                    rail.SetARail(_currentSelectedRail);

                    rail.transform.position = _currentSelectedRail.GetRailDataFromTime(1).NearestPosition;
                    rail.transform.forward = _currentSelectedRail.GetRailDataFromTime(1).Tangent.normalized;
                    rail.name = $"RailJunction({rail.GetInstanceID()})";
                    _currentSelectedRail.SetBRail(rail);

                    Railroad b = ExtendRailJunction(rail);
                    Railroad c = ExtendRailJunction(rail, true);
                    rail.SetCRail(c);
                    c.SetBRail(rail);

                    c.transform.position = rail.GetRailDataFromTime(0, 1).NearestPosition;
                    c.transform.forward = -rail.GetRailDataFromTime(0, 1).Tangent.normalized;
                    c.FlipSpline(0);

                    UpdateRailMesh(b);
                    UpdateRailMesh(c);
                }

                if (GUILayout.Button("Add merge right"))
                {
                    RailroadJunction rail = Instantiate(_data.JunctionInRight).GetComponent<RailroadJunction>();

                    rail.transform.position = _currentSelectedRail.GetRailDataFromTime(1).NearestPosition;
                    rail.transform.forward = _currentSelectedRail.GetRailDataFromTime(1).Tangent.normalized;
                    rail.name = $"RailJunction({rail.GetInstanceID()})";
                    _currentSelectedRail.SetBRail(rail);

                    Railroad b = ExtendRailJunction(rail);
                    Railroad c = ExtendRailJunction(rail, true);

                    rail.SetARail(_currentSelectedRail);
                    rail.SetBRail(b);
                    rail.SetCRail(c);

                    c.SetBRail(rail);
                    b.SetARail(rail);

                    c.transform.position = rail.GetRailDataFromTime(0, 1).NearestPosition;
                    c.transform.forward = -rail.GetRailDataFromTime(0, 1).Tangent.normalized;
                    c.FlipSpline(0);

                    UpdateRailMesh(b);
                    UpdateRailMesh(c);
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawRailMeshTool()
        {
            GUILayout.BeginVertical();
            GUILayout.Label("Rail Mesh options");
            DrawRailMeshProperties();

            if (_allMeshOptionsSet)
            {
                if (GUILayout.Button("Update Mesh"))
                {
                    UpdateRailMesh(_currentSelectedRail);
                    _editingMesh = false;
                }
            }
            else
                EditorGUILayout.HelpBox("Not all fields are set", MessageType.Warning);

            GUILayout.EndVertical();
        }

        private bool _allMeshOptionsSet => _railMesh != null && _railMaterial != null && _ballastMesh != null && _ballastMaterial != null;
        private RailShapeMesh2D _railMesh;
        private Material _railMaterial;
        private RailShapeMesh2D _ballastMesh;
        private Material _ballastMaterial;
        private int _resolutionDividend = 1;
        private bool _generateCollider;

        private void DrawRailMeshProperties()
        {
            GUILayout.Label("Rail", EditorStyles.boldLabel);
            _railMesh = EditorGUILayout.ObjectField("Rail Mesh", _railMesh, typeof(RailShapeMesh2D), false) as RailShapeMesh2D;
            _railMaterial = EditorGUILayout.ObjectField("Rail Material", _railMaterial, typeof(Material), false) as Material;
            EditorGUILayout.Separator();

            GUILayout.Label("Ballast", EditorStyles.boldLabel);
            _ballastMesh = EditorGUILayout.ObjectField("Ballast Mesh", _ballastMesh, typeof(RailShapeMesh2D), false) as RailShapeMesh2D;
            _ballastMaterial = EditorGUILayout.ObjectField("Ballast Material", _ballastMaterial, typeof(Material), false) as Material;
            EditorGUILayout.Separator();

            GUILayout.Label("Mesh Quality", EditorStyles.boldLabel);
            GUILayout.Label("Increase this value to get more detail", EditorStyles.helpBox);
            _resolutionDividend = EditorGUILayout.IntField("Resolution Dividend", _resolutionDividend);
            EditorGUILayout.Separator();
            GUILayout.Label("Collider", EditorStyles.boldLabel);
            _generateCollider = EditorGUILayout.Toggle("Generate Collider", _generateCollider);
        }

        private GameObject UpdateRailMesh(Railroad rail)
        {
            if (rail.transform.childCount > 0)
            {
                DestroyImmediate(rail.transform.GetChild(0).gameObject);
            }
            GameObject visual = new GameObject("RailMesh");
            visual.transform.SetParent(rail.transform, false);
            RailroadExtrudeMesh extruder = visual.AddComponent<RailroadExtrudeMesh>();
            extruder.SetParamenters(_railMesh, _railMaterial, _ballastMesh, _ballastMaterial, _resolutionDividend, _generateCollider);
            extruder.GenerateRailMesh();
            DestroyImmediate(extruder);
            Undo.RegisterCreatedObjectUndo(visual, "Create Mesh");
            return visual;
        }

        private void DrawTerrainConformingTool()
        {
            GUILayout.BeginVertical();
            GUILayout.Label("Terrain Conforming Options");

            DrawTerrainProperties();

            if (_terrain != null)
            {
                if (GUILayout.Button("Conform Terrain (NO UNDO) "))
                {
                    ConformTerrain(_currentSelectedRail);
                    _editingTerrain = false;
                }
            }
            else EditorGUILayout.HelpBox("Terrain is not assigned", MessageType.Warning);

            GUILayout.EndVertical();
        }

        private Terrain _terrain;
        private AnimationCurve _curve = AnimationCurve.Linear(0, 1, 1, 0);
        private float _brushSpacing = 1;
        private float _verticalOffset = 0;
        private int[] _initialPassRadii = new int[] { 20, 7, 2 };
        private bool _arrayOpen = true;
        private float _globalHeight = 0;
        private bool _flipped;
        private bool _railMergerCollapse;

        private void DrawTerrainProperties()
        {
            _terrain = EditorGUILayout.ObjectField("Terrain", _terrain, typeof(Terrain), true) as Terrain;
            _curve = EditorGUILayout.CurveField("Conform Profile", _curve);
            _brushSpacing = EditorGUILayout.FloatField("Spacing", _brushSpacing);
            _verticalOffset = EditorGUILayout.FloatField("Vertical Offset", _verticalOffset);
            _initialPassRadii = EditorUtils.IntArrayField("Radius Passes", ref _arrayOpen, _initialPassRadii);
        }

        private void ConformTerrain(Railroad currentSelectedRail)
        {
            RailroadTerrainBrush brush = currentSelectedRail.gameObject.AddComponent<RailroadTerrainBrush>();
            brush.SetProperties(_terrain, _curve, _brushSpacing, _verticalOffset, _initialPassRadii);
            brush.ShapeTerrain();
            DestroyImmediate(brush);
        }

        private static void FancySeparator(Color color)
        {
            EditorGUILayout.Separator();
            Rect r = EditorGUILayout.BeginVertical();
            EditorGUI.DrawRect(r, color);
            GUILayout.Space(3);
            EditorGUILayout.EndVertical();
        }

        private void AddStraightRailConnected()
        {
            GameObject newrail = Instantiate(_data.Rail);
            Railroad rail = newrail.GetComponent<Railroad>();
            rail.SetARail(_currentSelectedRail);
            _currentSelectedRail.SetBRail(rail);
            Selection.activeGameObject = newrail;
            newrail.transform.position = _currentSelectedRail.GetRailDataFromTime(1).NearestPosition;
            newrail.transform.rotation = Quaternion.LookRotation(_currentSelectedRail.GetRailDataFromTime(1).Tangent);
            newrail.name = $"Rail({newrail.GetInstanceID()})";
            UpdateRailMesh(rail);
            //TODO: Connect Rail
        }

        private Railroad ExtendRailJunction(Railroad target, bool merge = false)
        {
            //TODO: CONTECT RAIL TO PREVIOUS
            GameObject newrail = Instantiate(PrefabUtility.LoadPrefabContents("Assets/Prefabs/Train/Rails/Rail.prefab"));
            Railroad rail = newrail.GetComponent<Railroad>();

            if (merge)
            {
                rail.SetBRail(target);
                target.SetARail(rail);
            }
            else
            {
                rail.SetARail(target);
                target.SetBRail(rail);
            }

            newrail.transform.position = target.GetRailDataFromTime(1).NearestPosition;
            newrail.transform.rotation = Quaternion.LookRotation(target.GetRailDataFromTime(1).Tangent);
            newrail.name = $"Rail({newrail.GetInstanceID()})";

            return rail;
            //TODO: Connect Rail
        }

        private enum JunctionType
        {
            IN_LEFT,
            IN_RIGHT,
            OUT_LEFT,
            OUT_RIGHT
        }

        private enum RailMergeType
        {
            NEXT,
            PREVIOUS,
            JUNCTION,
        }
    }
}