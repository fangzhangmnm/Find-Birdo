using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

public class PathGraph : MonoBehaviour, ISerializationCallbackReceiver
{
    #region DataStruct
    [System.Serializable]
    public class Node
    {
        [System.NonSerialized] public List<Path> paths = new List<Path>();
        public Vector3 pos;
        public int s_ID; public List<int> s_paths = new List<int>();
        public float[] angles, dists, biAngles, biDists, biRadius;

    }
    [System.Serializable]
    public class Path
    {

        [System.NonSerialized] public Node node1, node2;
        public float width;
        public int s_ID, s_node1, s_node2;
        public float dist1, dist2;

        public Node Other(Node n) { return n == node2 ? node1 : node2; }
        public bool Contains(Node n) { return n == node1 || n == node2; }
        public bool IsSame(Node a, Node b) { return a == node1 && b == node2 || a == node2 && b == node1; }
        public float length { get { return Vector3.Distance(node1.pos, node2.pos); } }
        public float slope { get { return (node2.pos.y - node1.pos.y) / Vector3.ProjectOnPlane(node2.pos - node1.pos, Vector3.up).magnitude; } }
        public Vector3 tangent { get { return (node2.pos - node1.pos).normalized; } }
        public Vector3 planarTangent { get { return Vector3.ProjectOnPlane(node2.pos - node1.pos, Vector3.up).normalized; } }
        public Vector3 normal { get { return Vector3.Cross(Vector3.up, tangent).normalized; } }
    }
    [HideInInspector] public List<Path> paths = new List<Path>();
    [HideInInspector] public List<Node> nodes = new List<Node>();
    [System.NonSerialized] public Node selectedNode;
    [System.NonSerialized] public Path selectedPath;
    [SerializeField, HideInInspector] int s_selectedNode = -1, s_selectedPath = -1;
    public void OnBeforeSerialize()
    {
        for (int i = 0; i < paths.Count; ++i) paths[i].s_ID = i;
        for (int i = 0; i < nodes.Count; ++i) nodes[i].s_ID = i;
        foreach (var p in paths) { p.s_node1 = p.node1.s_ID; p.s_node2 = p.node2.s_ID; }
        foreach (var n in nodes) { n.s_paths.Clear(); foreach (var p in n.paths) n.s_paths.Add(p.s_ID); }
        s_selectedNode = selectedNode == null ? -1 : selectedNode.s_ID;
        s_selectedPath = selectedPath == null ? -1 : selectedPath.s_ID;
    }
    public void OnAfterDeserialize()
    {
        foreach (var p in paths) { p.node1 = nodes[p.s_node1]; p.node2 = nodes[p.s_node2]; }
        foreach (var n in nodes) { n.paths.Clear(); foreach (var s_p in n.s_paths) n.paths.Add(paths[s_p]); }
        selectedNode = s_selectedNode == -1 ? null : nodes[s_selectedNode];
        selectedPath = s_selectedPath == -1 ? null : paths[s_selectedPath];
        Debug.Log("OnAfterDeserialize Called");
    }
    public Node addNode(Vector3 pos)
    {
        Node n = new Node();
        n.pos = pos;
        nodes.Add(n);
        return n;
    }
    public Path connectNodes(Node a, Node b)
    {
        if (a == b) return null;
        foreach (var p in a.paths)
            if (p.IsSame(a, b))
                return p;
        Path p1 = new Path();
        p1.width = 1f;
        p1.node1 = a; p1.node2 = b;
        a.paths.Add(p1); b.paths.Add(p1);
        paths.Add(p1);
        return p1;
    }
    public void disconnectNodes(Node a, Node b)
    {
        foreach (var p in a.paths)
            if (p.IsSame(a, b))
            {
                removePath(p);
                return;
            }
    }
    public void removePath(Path p)
    {
        p.node1.paths.Remove(p);
        p.node2.paths.Remove(p);
        paths.Remove(p);
        p.node1 = p.node2 = null;
    }
    public void removeNode(Node n)
    {
        foreach (var p in n.paths)
            if (p.Contains(n))
            {
                p.Other(n).paths.Remove(p);
                paths.Remove(p);
                p.node1 = p.node2 = null;

            }
        n.paths.Clear();
        nodes.Remove(n);
    }
    public bool isConneted(Node a, Node b)
    {
        foreach (var p in a.paths)
            if (p.IsSame(a, b))
                return true;
        return false;
    }
    public void Clear()
    {
        foreach (var n in nodes)
            n.paths.Clear();
        foreach (var p in paths)
            p.node1 = p.node2 = null;
        nodes.Clear();
        paths.Clear();
        selectedNode = null;
        selectedPath = null;
    }
    #endregion
    #region Contor
    void SortAll()
    {
        foreach (var n in nodes)
            Sort(n);
    }
    public void Sort(Node n)
    {
        n.paths.Sort(delegate (Path a, Path b)
        {
            Vector3 v1 = a.Other(n).pos - n.pos;
            float f1 = Mathf.Atan2(v1.z, v1.x);
            Vector3 v2 = b.Other(n).pos - n.pos;
            float f2 = Mathf.Atan2(v2.z, v2.x);
            return f1.CompareTo(f2);
        });
        int N = n.paths.Count;
        if (N == 0) return;
        //will it work for n=1 case?
        n.angles = new float[N]; n.dists = new float[N]; n.biAngles = new float[N]; n.biDists = new float[N]; n.biRadius = new float[N];
        for (int i = 0; i < N; ++i)
        {
            Vector3 v1 = n.paths[i].Other(n).pos - n.pos;
            n.angles[i] = Mathf.Atan2(v1.z, v1.x);
        }
        for (int i = 0; i < N - 1; ++i)
            n.biAngles[i] = (n.angles[i] + n.angles[i + 1]) / 2;
        n.biAngles[N - 1] = (n.angles[N - 1] + n.angles[0] + 2 * Mathf.PI) / 2;
        for (int i = 0; i < N; ++i)
            n.dists[i] = 0;
        for (int i = 0; i < N; ++i)
        {
            float a1 = n.biAngles[i] - n.angles[i];//0-pi
            float a = a1 < Mathf.PI / 2 ? a1 : Mathf.Max(0, Mathf.PI - a1);
            float sign = a1 < Mathf.PI / 2 ? 1 : -1;

            float d = 1 / Mathf.Sin(a); if (float.IsNaN(d) || float.IsInfinity(d)) d = float.PositiveInfinity;
            float d1 = Mathf.Max(0, .5f * n.paths[i].length / Mathf.Cos(a)); if (float.IsNaN(d1) || float.IsInfinity(d1)) d1 = float.PositiveInfinity;
            float d2 = Mathf.Max(0, .5f * n.paths[(i + 1) % N].length / Mathf.Cos(a)); if (float.IsNaN(d2) || float.IsInfinity(d2)) d2 = float.PositiveInfinity;
            d = Mathf.Min(Mathf.Min(d, d1), d2);
            if (N == 1) d = 0;
            n.biDists[i] = sign * d;
            n.biRadius[i] = sign * d * Mathf.Sin(a);
            n.dists[i] = Mathf.Max(n.dists[i], d * Mathf.Cos(a));
            n.dists[(i + 1) % N] = Mathf.Max(n.dists[(i + 1) % N], d * Mathf.Cos(a));
        }
        for (int i = 0; i < N; ++i)
        {
            if (n.paths[i].node1 == n)
                n.paths[i].dist1 = n.dists[i];
            else
                n.paths[i].dist2 = n.dists[i];
        }
    }
    List<Vector3> getNodeContor(Node n)
    {
        List<Vector3> rtval = new List<Vector3>();
        for (int i = 0; i < n.paths.Count; ++i)
        {
            int i1 = (i + 1) % n.paths.Count;

            float a0 = n.angles[i];
            float a1 = i < n.angles.Length - 1 ? n.angles[i1] : n.angles[i1] + 2 * Mathf.PI;
            float r0 = n.biRadius[i] - n.paths[i].width / 2;
            float r1 = n.biRadius[i] - n.paths[i1].width / 2;
            Vector3 c1 = n.biDists[i] * new Vector3(Mathf.Cos(n.biAngles[i]), 0, Mathf.Sin(n.biAngles[i]));

            Vector3 rd0 = n.pos + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * n.dists[i] - n.paths[i].width / 2 * n.paths[i].normal * (n.paths[i].node1 == n ? 1 : -1);
            rd0 += Vector3.Dot(rd0 - n.pos, n.paths[i].planarTangent) * n.paths[i].slope * Vector3.up;
            Vector3 rd1 = n.pos + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * n.dists[i1] + n.paths[i1].width / 2 * n.paths[i1].normal * (n.paths[i1].node1 == n ? 1 : -1);
            rd1 += Vector3.Dot(rd1 - n.pos, n.paths[i1].planarTangent) * n.paths[i1].slope * Vector3.up;

            Gizmos.color = Color.yellow;
            rtval.Add(rd0);
            for (int j = 0; j <= 10; ++j)
            {
                float t = j / 10f;
                float at = Mathf.Lerp(a0 - .5f * Mathf.PI, a1 - 1.5f * Mathf.PI, t);
                Vector3 ast = n.pos + c1 + Mathf.Lerp(r0, r1, t) * new Vector3(Mathf.Cos(at), 0, Mathf.Sin(at));
                float h0 = Vector3.Dot(ast - n.pos, n.paths[i].planarTangent) * n.paths[i].slope;
                float h1 = Vector3.Dot(ast - n.pos, n.paths[i1].planarTangent) * n.paths[i1].slope;
                ast += Vector3.up * Mathf.Lerp(h0, h1, t);
                rtval.Add(ast);
            }
            rtval.Add(rd1);
        }
        return rtval;

    }
    #endregion
    [Button]
    public void SpawnExample()
    {
        Clear();
        var n1 = addNode(new Vector3(0, 0, 0));
        var n2 = addNode(new Vector3(0, 0, 10));
        var n3 = addNode(new Vector3(10, 0, 10));
        var n4 = addNode(new Vector3(10, 0, 0));
        var n5 = addNode(new Vector3(11, 0, 5));
        var n6 = addNode(new Vector3(0, 0, 20));

        connectNodes(n1, n2);
        connectNodes(n2, n3);
        connectNodes(n3, n4);
        connectNodes(n4, n1);
        connectNodes(n1, n3);
        connectNodes(n4, n5);
        connectNodes(n2, n6);
    }
    void OnDrawGizmos()
    {
        SortAll();
        foreach (var n in nodes)
        {
            Gizmos.color = Color.yellow;
            //Gizmos.DrawWireSphere(transform.TransformPoint(n.pos), transform.lossyScale.x);
            var vertexs = getNodeContor(n);
            for (int i = 0; i < vertexs.Count; ++i)
            {
                int i1 = (i + 1) % vertexs.Count;
                Gizmos.color = Color.white;
                Gizmos.DrawLine(transform.TransformPoint(vertexs[i]), transform.TransformPoint(vertexs[i1]));
            }
        }
        foreach (var p in paths)
        {
            Gizmos.color = Color.yellow;
            Vector3 p1 = p.node1.pos + p.dist1 * p.planarTangent + Vector3.up * p.dist1 * p.slope;
            Vector3 p2 = p.node2.pos - p.dist2 * p.planarTangent - Vector3.up * p.dist2 * p.slope;
            Gizmos.DrawLine(
                transform.TransformPoint(p1 + p.normal * p.width / 2),
                transform.TransformPoint(p2 + p.normal * p.width / 2));
            Gizmos.DrawLine(
                transform.TransformPoint(p1 - p.normal * p.width / 2),
                transform.TransformPoint(p2 - p.normal * p.width / 2));
        }
    }
}
#if UNITY_EDITOR
[CustomEditor(typeof(PathGraph))]
public class PathGraphEditor : Editor
{
    enum CustomEditorTool { FreeMove };
    CustomEditorTool currentTool = CustomEditorTool.FreeMove;
    public override void OnInspectorGUI()
    {
        var pathGraph = target as PathGraph;
        var transform = pathGraph.transform;
        DrawDefaultInspector();
        if (GUILayout.Button("Spawn Example"))
        {
            Undo.RecordObject(pathGraph, "Spawn Example");
            pathGraph.SpawnExample();
        }
    }
    PathGraph.Path highLightPath;
    PathGraph.Node highLightNode;
    static float RayLine(Ray r, Vector3 p1, Vector3 p2)
    {
        Vector3 d1 = Vector3.ProjectOnPlane(p1 - r.origin, r.direction);
        Vector3 d2 = Vector3.ProjectOnPlane(p2 - r.origin, r.direction);
        float t = Vector3.Dot(-d1, d2 - d1) / (d2 - d1).sqrMagnitude;
        return Vector3.Lerp(d1, d2, Mathf.Clamp01(t)).magnitude;
    }
    static float RayLinePos(Ray r, Vector3 p1, Vector3 p2)
    {
        Vector3 d1 = Vector3.ProjectOnPlane(p1 - r.origin, r.direction);
        Vector3 d2 = Vector3.ProjectOnPlane(p2 - r.origin, r.direction);
        float t = Vector3.Dot(-d1, d2 - d1) / (d2 - d1).sqrMagnitude;
        return t;
    }
    static float RayPoint(Ray r, Vector3 p)
    {
        return Vector3.ProjectOnPlane(p - r.origin, r.direction).magnitude;
    }
    public void OnSceneGUI()
    {
        Event ev = Event.current;
        int sca = (ev.shift ? 4 : 0) + (ev.control ? 2 : 0) + (ev.alt ? 1 : 0);
        Ray mouseRay = HandleUtility.GUIPointToWorldRay(ev.mousePosition);


        var pathGraph = target as PathGraph;
        var transform = pathGraph.transform;
        if (pathGraph.selectedNode != null && !pathGraph.nodes.Contains(pathGraph.selectedNode)) { pathGraph.selectedNode = null; Debug.LogWarning("Selected Node Wild Reference"); }

        Vector3 mousePos = Vector3.zero; bool mousePosHit;
        Plane plane = new Plane(transform.up, transform.position);
        float enter;
        if (mousePosHit = plane.Raycast(mouseRay, out enter))
            mousePos = mouseRay.origin + mouseRay.direction * enter;

        PathGraph.Node pointingNode = null;
        PathGraph.Path pointingPath = null;

        foreach (var p in pathGraph.paths)
        {
            var v1 = transform.TransformPoint(p.node1.pos);
            var v2 = transform.TransformPoint(p.node2.pos);
            float handleSize = p.width / 2;

            if (ev.type == EventType.MouseMove || ev.type == EventType.MouseDown)
                if (RayLine(mouseRay, v1, v2) < handleSize)
                {
                    pointingPath = p;
                }
            if (pathGraph.selectedPath == p)
                Handles.color = Color.red;
            else if (highLightPath == p)
                Handles.color = Color.gray;
            else
                Handles.color = Color.white;
            Handles.DrawLine(v1, v2);
        }

        foreach (var n in pathGraph.nodes)
        {
            Vector3 pos = transform.TransformPoint(n.pos);
            float handleSize = .5f * transform.lossyScale.x;

            if (ev.type == EventType.MouseMove || ev.type == EventType.MouseDown)
                if (RayPoint(mouseRay, pos) < handleSize)
                {
                    pointingNode = n;
                    pointingPath = null;
                }

            if (n == pathGraph.selectedNode)
                Handles.color = Color.red;
            else if (n == highLightNode)
                Handles.color = Color.grey;
            else
                Handles.color = Color.white;

            Handles.DrawWireDisc(pos, transform.up, handleSize);

        }

        if (pathGraph.selectedNode != null)
        {
            Vector3 oldPos = transform.TransformPoint(pathGraph.selectedNode.pos);
            Vector3 newPos = Handles.PositionHandle(oldPos, transform.rotation);
            if (oldPos != newPos)
            {
                Undo.RecordObject(pathGraph, "Move Point");
                pathGraph.selectedNode.pos = transform.InverseTransformPoint(newPos);
                ev.Use();
            }
        }

        if (ev.type == EventType.MouseMove || ev.type == EventType.MouseDown)
        {
            highLightNode = pointingNode;
            highLightPath = pointingPath;
        }


        if (ev.type == EventType.MouseDown && ev.button == 0 && sca == 0 && pointingNode != null)
        {
            Undo.RecordObject(pathGraph, "Select Node");
            pathGraph.selectedPath = null;
            pathGraph.selectedNode = pointingNode;
            ev.Use();
        }
        if (ev.type == EventType.MouseDown && ev.button == 0 && sca == 0 && pointingPath != null)
        {
            Undo.RecordObject(pathGraph, "Select Path");
            pathGraph.selectedPath = pointingPath;
            pathGraph.selectedNode = null;
            ev.Use();
        }
        if (ev.type == EventType.MouseDown && ev.button == 0 && sca == 0 && pointingNode == null && pointingPath == null)
        {
            Undo.RecordObject(pathGraph, "Disselect Node");
            pathGraph.selectedNode = null;
            ev.Use();
        }
        if (ev.type == EventType.MouseDown && ev.button == 0 && sca == 4 && pointingNode != null && pathGraph.selectedNode != null)
        {
            if (!pathGraph.isConneted(pathGraph.selectedNode, pointingNode))
            {
                Undo.RecordObject(pathGraph, "Connect Nodes");
                pathGraph.connectNodes(pathGraph.selectedNode, pointingNode);
                pathGraph.selectedNode = pointingNode;
                pathGraph.selectedPath = null;
            }
            ev.Use();
        }
        if (ev.type == EventType.MouseDown && ev.button == 0 && sca == 4 && pointingNode == null && pointingPath == null && mousePosHit && pathGraph.selectedNode == null)
        {
            Undo.RecordObject(pathGraph, "Add Node");
            PathGraph.Node n = pathGraph.addNode(transform.InverseTransformPoint(mousePos));
            pathGraph.selectedNode = n;
            pathGraph.selectedPath = null;
            ev.Use();
        }
        if (ev.type == EventType.MouseDown && ev.button == 0 && sca == 4 && pointingNode == null && pointingPath == null && mousePosHit && pathGraph.selectedNode != null)
        {
            Undo.RecordObject(pathGraph, "Add Node and Connect");
            PathGraph.Node n = pathGraph.addNode(transform.InverseTransformPoint(mousePos));
            pathGraph.connectNodes(pathGraph.selectedNode, n);
            pathGraph.selectedNode = n;
            pathGraph.selectedPath = null;
            ev.Use();
        }
        if (ev.type == EventType.MouseDown && ev.button == 0 && sca == 4 && pointingPath != null && pathGraph.selectedNode == null)
        {
            Undo.RecordObject(pathGraph, "Split Path");

            PathGraph.Node n = pathGraph.addNode(transform.InverseTransformPoint(mousePos));
            pathGraph.connectNodes(pointingPath.node1, n);
            pathGraph.connectNodes(n, pointingPath.node2);
            pathGraph.removePath(pointingPath);
            pathGraph.selectedNode = n;
            pathGraph.selectedPath = null;
            ev.Use();
        }
        if (ev.type == EventType.MouseDown && ev.button == 0 && sca == 4 && pointingPath != null && pathGraph.selectedNode != null)
        {
            Undo.RecordObject(pathGraph, "Split Path and Connect");

            PathGraph.Node n = pathGraph.addNode(transform.InverseTransformPoint(mousePos));
            pathGraph.connectNodes(pointingPath.node1, n);
            pathGraph.connectNodes(n, pointingPath.node2);
            pathGraph.removePath(pointingPath);
            pathGraph.connectNodes(pathGraph.selectedNode, n);
            pathGraph.selectedNode = n;
            pathGraph.selectedPath = null;
            ev.Use();
        }
        if (ev.type == EventType.KeyDown && ev.keyCode == KeyCode.X && sca == 0 && pathGraph.selectedNode != null)
        {
            Undo.RecordObject(pathGraph, "Remove Node");
            pathGraph.removeNode(pathGraph.selectedNode);
            pathGraph.selectedNode = null;
        }
        if (ev.type == EventType.KeyDown && ev.keyCode == KeyCode.X && sca == 0 && pathGraph.selectedPath != null)
        {
            Undo.RecordObject(pathGraph, "Remove Path");
            pathGraph.removePath(pathGraph.selectedPath);
            pathGraph.selectedPath = null;
        }



        if (ev.type == EventType.KeyDown && ev.keyCode == KeyCode.F)
        {
            if (pathGraph.selectedNode != null)
            {
                Bounds b = new Bounds(pathGraph.selectedNode.pos, transform.lossyScale.x * 10 * Vector3.one);
                SceneView.lastActiveSceneView.Frame(b, instant: false);
            }
            ev.Use();
        }

        HandleUtility.AddDefaultControl(0);
        Tools.current = Tool.None;
    }
}
#endif
//https://www.synnaxium.com/en/2019/01/unity-custom-map-editor-part-1/

#if UNITY_EDITOR

public class GridPaintEditor : EditorWindow
{
    private bool paintMode = false;
    private string path = "Assets/GridPaintPalette";
    [SerializeField]
    private List<GameObject> palette = new List<GameObject>();
    private List<GUIContent> paletteIcons = new List<GUIContent>();
    [SerializeField]
    private int paletteIndex;
    private Vector2 scrollPosition;

    private Transform container;
    private int selectedY;
    private float cellHeight = 1f;
    private float cellSize = 1f;
    private int rotate = 0;
    private bool halfInteger = false;
    private GameObject previewObject;
    private Bounds lastPlace = new Bounds();
    private Vector3 oldA = Vector3.zero;
    private bool mouseMoved = true;
    [MenuItem("Window/GridPaint")]
    private static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(GridPaintEditor));
    }
    void OnFocus()
    {
        SceneView.duringSceneGui -= this.OnSceneGUI;
        SceneView.duringSceneGui += this.OnSceneGUI;
    }
    void OnDestroy()
    {
        SceneView.duringSceneGui -= this.OnSceneGUI;
    }

    private void LoadPalette()
    {
        palette.Clear();
        paletteIcons.Clear();
        string[] prefabFiles = System.IO.Directory.GetFiles(path, "*.prefab");
        foreach (string prefabFile in prefabFiles)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath(prefabFile, typeof(GameObject)) as GameObject;
            if (prefab.GetComponentInChildren<Renderer>() != null)
            {
                palette.Add(prefab);
                var guic = new GUIContent(prefab.name);
                guic.image = null;
                paletteIcons.Add(guic);
            }
        }
        RefreshPaletteIcons(0, 40);
        paletteIndex = 0;
        EnterPaintModeAndFocus();
    }
    private void RefreshPaletteIcons(int begin, int end)
    {
        for (int i = Mathf.Max(0, begin); i < Mathf.Min(palette.Count, end); ++i)
            paletteIcons[i].image = AssetPreview.GetAssetPreview(palette[i]);
    }
    private void OnGUI()
    {
        GUILayout.BeginHorizontal();
        container = EditorGUILayout.ObjectField("Container", container, typeof(Transform), true) as Transform;
        /*if (GUILayout.Button("Clear"))
        {
            for (int i = container.childCount - 1; i >= 0; --i)
            {
                Undo.DestroyObjectImmediate(container.GetChild(i));
            }
        }*/
        if (container == null)
            if (GUILayout.Button("Create"))
                container = new GameObject("container").transform;
        GUILayout.EndHorizontal();
        cellSize = EditorGUILayout.FloatField("Grid Size", cellSize);
        cellHeight = EditorGUILayout.FloatField("Grid Height", cellHeight);
        selectedY = EditorGUILayout.IntField("Height Layer[Alt+MouseWheel]", selectedY);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Align to Center?[C]");
        halfInteger = GUILayout.Toggle(halfInteger, "");
        GUILayout.Label("Rotation[R]");
        rotate = EditorGUILayout.IntSlider("", rotate, 0, 3);
        GUILayout.EndHorizontal();

        bool oldPaintMode = paintMode;
        paintMode = GUILayout.Toggle(paintMode, (paintMode ? "End Paint[1]" : "Start Paint[1]"), "Button", GUILayout.Height(60f));
        if (container == null) paintMode = false;
        if (!oldPaintMode && paintMode)
            EnterPaintModeAndFocus();

        GUILayout.Space(10);



        GUILayout.Label("Prefab Palette Folder");
        GUILayout.BeginHorizontal();
        path = GUILayout.TextField(path);
        if (GUILayout.Button("Load")) LoadPalette();
        GUILayout.EndHorizontal();
        if (paletteIndex < palette.Count)
        {
            GUILayout.Label(palette[paletteIndex].name);
        }
        else
        {
            GUILayout.Label("");
        }

        scrollPosition = GUILayout.BeginScrollView(scrollPosition);
        int xCount = Mathf.Max(1, Mathf.FloorToInt((EditorGUIUtility.currentViewWidth - 30) / 128));
        RefreshPaletteIcons(Mathf.FloorToInt(scrollPosition.y / 135) * xCount - 10, Mathf.FloorToInt(scrollPosition.y / 135) * xCount + 30);

        GUIStyle guis = new GUIStyle(GUI.skin.button);
        guis.fixedWidth = 128;
        guis.fixedHeight = 135;
        guis.wordWrap = true;
        guis.imagePosition = ImagePosition.ImageAbove;
        guis.padding = new RectOffset(3, 3, 3, 3);

        int newIndex = GUILayout.SelectionGrid(paintMode ? paletteIndex : -1, paletteIcons.ToArray(), xCount, guis, GUILayout.Width(xCount * 128));

        if (newIndex != -1)
        {
            paintMode = true;
            if (paletteIndex != newIndex)
                EnterPaintModeAndFocus();
            paletteIndex = newIndex;
        }
        HidePreviewObject();
        GUILayout.EndScrollView();

    }
    void Rotate(int rotate, ref float x, ref float z)
    {
        float x0 = x, z0 = z;
        switch (rotate)
        {
            case 0: x = x0; z = z0; break;
            case 1: x = z0; z = -x0; break;
            case 2: x = -x0; z = -z0; break;
            case 3: x = -z0; z = x0; break;
        }
    }
    static Bounds getCompoundGameobjectBound(GameObject g)
    {
        var a = g.GetComponentsInChildren<Renderer>();
        if (a.Length == 0) return new Bounds();
        Bounds b = a[0].bounds; //new Bounds contains original point dont use that
        for (int i = 0; i < a.Length; ++i)
            b.Encapsulate(a[i].bounds);
        return b;
    }
    bool ShowPreviewObject(out Vector3 placePos, out Bounds bounds)
    {
        placePos = Vector3.zero;
        bounds = new Bounds();
        if (paletteIndex >= palette.Count) { paintMode = false; return false; }
        if (container == null) return false;

        if (previewObject == null)
        {
            previewObject = PrefabUtility.InstantiatePrefab(palette[paletteIndex]) as GameObject;
            previewObject.name = "[PREVIEW]";
        }
        Bounds prefabBounds = getCompoundGameobjectBound(palette[paletteIndex]);
        float bias = halfInteger ? 0.5f : 0;

        float plx = (Mathf.RoundToInt(prefabBounds.min.x / cellSize + bias - 0.1f) - bias + 0.1f) * cellSize;
        float plz = (Mathf.RoundToInt(prefabBounds.min.z / cellSize + bias - 0.1f) - bias + 0.1f) * cellSize;
        float phx = (Mathf.RoundToInt(prefabBounds.max.x / cellSize + bias + 0.1f) - bias - 0.1f) * cellSize;
        float phz = (Mathf.RoundToInt(prefabBounds.max.z / cellSize + bias + 0.1f) - bias - 0.1f) * cellSize;
        int pwx = Mathf.RoundToInt((phx - plx) / cellSize);
        int pwz = Mathf.RoundToInt((phz - plz) / cellSize);
        if (pwx == 0) { pwx = 1; phx += cellSize; }
        if (pwz == 0) { pwz = 1; phz += cellSize; }
        float pmx = (plx + phx) / 2;
        float pmz = (plz + phz) / 2;
        float prmx = pmx, prmz = pmz; Rotate(rotate, ref prmx, ref prmz);


        int paintWidthX = pwx, paintWidthZ = pwz;
        if (rotate == 1 || rotate == 3) { paintWidthX = pwz; paintWidthZ = pwx; }

        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        Plane plane = new Plane(container.up, container.position + container.up * cellHeight * selectedY);
        float enter;
        if (!plane.Raycast(ray, out enter))
        {
            //HidePreviewObject();
            return false;
        }
        Vector3 intersection = ray.origin + enter * ray.direction;
        Vector3 coord = container.InverseTransformPoint(intersection);

        int x0 = Mathf.RoundToInt(coord.x / cellSize - paintWidthX / 2f);
        int z0 = Mathf.RoundToInt(coord.z / cellSize - paintWidthZ / 2f);

        Vector3 newA = container.TransformPoint(new Vector3(x0 * cellSize, 0, z0 * cellSize));
        if (mouseMoved) oldA = newA;
        Vector3 a = oldA + container.up * cellHeight * selectedY;

        Vector3 b = container.right * cellSize * paintWidthX;
        Vector3 c = container.forward * cellSize * paintWidthZ;
        Vector3 d = new Vector3(prmx, 0, prmz);
        Handles.DrawLine(a, a + b);
        Handles.DrawLine(a + b, a + b + c);
        Handles.DrawLine(a + b + c, a + c);
        Handles.DrawLine(a + c, a);

        bounds.min = new Vector3(x0 * cellSize, 0, z0 * cellSize);
        bounds.max = new Vector3((x0 + paintWidthX) * cellSize, 1, (z0 + paintWidthZ) * cellSize);
        bounds.size = bounds.size - Vector3.one * cellSize * 0.01f;

        placePos = a + (b + c) / 2 - d;

        previewObject.transform.position = placePos;
        previewObject.transform.rotation = Quaternion.Euler(0, 90 * rotate, 0);
        previewObject.transform.parent = null;
        return true;
    }
    void HidePreviewObject()
    {
        if (previewObject != null)
        {
            DestroyImmediate(previewObject);
            previewObject = null;
        }
    }
    private void OnSceneGUI(SceneView sceneView)
    {
        Event ev = Event.current;
        int sca = (ev.shift ? 4 : 0) + (ev.control ? 2 : 0) + (ev.alt ? 1 : 0);
        if (ev.type == EventType.MouseMove)
            mouseMoved = true;
        if (paintMode)
        {
            if (ev.type == EventType.Layout)
                HandleUtility.AddDefaultControl(0); // Consume the event
            Vector3 placePos = Vector3.zero;
            Bounds bounds = new Bounds();
            if (true || sca == 0)
            {
                if (ShowPreviewObject(out placePos, out bounds))
                {
                    if (ev.type == EventType.MouseDown && ev.button == 0 && sca == 0)
                    {
                        GameObject gameObject = PrefabUtility.InstantiatePrefab(palette[paletteIndex]) as GameObject;
                        gameObject.transform.position = placePos;
                        gameObject.transform.rotation = Quaternion.Euler(0, 90 * rotate, 0);
                        gameObject.transform.parent = container;
                        lastPlace = bounds;
                        Undo.RegisterCreatedObjectUndo(gameObject, "");
                        ev.Use();
                    }
                    if (ev.type == EventType.MouseDrag && ev.button == 0 && sca == 0)
                    {
                        if (!bounds.Intersects(lastPlace))
                        {
                            GameObject gameObject = PrefabUtility.InstantiatePrefab(palette[paletteIndex]) as GameObject;
                            gameObject.transform.position = placePos;
                            gameObject.transform.rotation = Quaternion.Euler(0, 90 * rotate, 0);
                            gameObject.transform.parent = container;
                            lastPlace = bounds;
                            Undo.RegisterCreatedObjectUndo(gameObject, "");
                            ev.Use();
                        }
                    }
                }
                else
                    HidePreviewObject();
            }
            else
                HidePreviewObject();
            if (ev.type == EventType.KeyDown && ev.keyCode == KeyCode.R && sca == 0)
            {
                rotate = (rotate + 1) % 4;
                ev.Use();
                Repaint();
            }
            if (ev.type == EventType.KeyDown && ev.keyCode == KeyCode.C && sca == 0)
            {
                halfInteger = !halfInteger;
                ev.Use();
                Repaint();
            }
            if (ev.type == EventType.KeyDown && ev.keyCode == KeyCode.Alpha1 && sca == 0)
            {
                paintMode = false;
                HidePreviewObject();
                ev.Use();
                Repaint();
            }
            if (ev.type == EventType.ScrollWheel && sca == 1)
            {
                if (ev.delta.y < 0)
                    selectedY += 1;
                if (ev.delta.y > 0)
                    selectedY -= 1;
                mouseMoved = false;
                ev.Use();
                Repaint();
            }
        }
        else
        {
            HidePreviewObject();
            if (ev.type == EventType.KeyDown && ev.keyCode == KeyCode.Alpha1)
            {
                EnterPaintModeAndFocus();
                ev.Use();
                Repaint();
            }
        }

        string path1 = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (path1 != "")
        {
            if (Path.GetExtension(path1) != "")
                path1 = path1.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
            if (path != path1)
            {

                path = path1;
                Repaint();
            }
        }
    }
    void EnterPaintModeAndFocus()
    {

        (SceneView.sceneViews[0] as SceneView).Focus();
        Selection.SetActiveObjectWithContext(null, null);
        paintMode = true;
    }
}
#endif
