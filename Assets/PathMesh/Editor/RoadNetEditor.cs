using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RoadNetObject))]
public class RoadNetEditor : Editor
{
    static RoadNet.Node selectedNode;
    static RoadNet.Road selectedRoad;
    static RoadStyle selectedStyle;
    static float elevation = 0;

    enum State {Inactive, Editing, AddOrSelectPoint, SetSegmentEnd, DragSegmentControl}
    static State state = State.Inactive;

    private void OnSceneGUI()
    {
        if (state == State.Inactive) return;

        var target = (this.target) as RoadNetObject;
        var roadNet = target.roadNet;
        var transform = target.transform;
        var scale = transform.lossyScale.x;
        Vector3 TP(Vector3 p) => transform.TransformPoint(p);
        Vector3 ITP(Vector3 p) => transform.InverseTransformPoint(p);
        Vector3 up = transform.up;

        var ev = Event.current;
        Ray mouseRay = HandleUtility.GUIPointToWorldRay(ev.mousePosition);
        bool isMouseHit = new Plane(transform.up, transform.TransformPoint(new Vector3(0, elevation, 0))).Raycast(mouseRay, out float enter);
        Vector3 point = isMouseHit? ITP(mouseRay.origin + enter * mouseRay.direction):Vector3.zero;
        Handles.color = Color.white;
        bool NodeButton(RoadNet.Node n) => Handles.Button(TP(n.position), transform.rotation * Quaternion.Euler(90, 0, 0), n.style.width / 2 * scale, n.style.width / 2 * scale, Handles.CircleHandleCap);
        Vector3 RoadButtonHit = Vector3.zero;
        bool RoadButton(RoadNet.Road r)
        {
            RoadNetObject.GenerateRoadPoints(r, out Vector3[] results);
            for (int i = 0; i < results.Length; ++i) results[i] = TP(results[i]);
            bool mouseOver = false;
            float minDist = Mathf.Infinity;
            for (int i = 0; i < results.Length - 1; ++i)
            {
                float dist = RoadNetObject.RayLineDist(mouseRay, results[i], results[i + 1]);
                if ( dist< r.style.width / 2)
                {
                    mouseOver = true;
                }
                if (dist < minDist) { minDist = dist;RoadButtonHit = RoadNetObject.RayLineHit(mouseRay, results[i], results[i + 1]); }
            }
            var tmp = Handles.color;
            if (!mouseOver) Handles.color /= 2;
            Handles.DrawAAPolyLine(results);
            Handles.color = tmp;
            if(mouseOver && ev.type==EventType.MouseUp && ev.button == 0)
            {
                ev.Use();
                return true;
            }
            return false;
        }
        void SelectNode(RoadNet.Node n) { selectedNode = n;selectedRoad = null; }
        void SelectRoad(RoadNet.Road r) { selectedNode = null; selectedRoad = r; }

        if (!roadNet.nodes.Contains(selectedNode)) selectedNode = null;
        if (!roadNet.roads.Contains(selectedRoad)) selectedRoad = null;

        Tools.current = Tool.None;

        if (state == State.Editing)
        {
            foreach(var n in roadNet.nodes)
                if (NodeButton(n))
                {
                    SelectNode(n);
                    if (!selectedStyle)
                        selectedStyle = selectedNode.style;
                }
            foreach (var r in roadNet.roads)
                if (RoadButton(r))
                {
                    SelectRoad(r);
                    if (!selectedStyle)
                        selectedStyle = selectedRoad.style;
                }
            if (selectedNode != null)
            {
                EditorGUI.BeginChangeCheck();
                Vector3 newPos = ITP(Handles.PositionHandle(TP(selectedNode.position), transform.rotation));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, "Move Node");
                    selectedNode.position = newPos;
                }
            }
            if (selectedRoad != null)
            {

            }
        }
        else if (state == State.AddOrSelectPoint)
        {
            foreach (var n in roadNet.nodes)
                if(NodeButton(n) && selectedStyle)
                {
                    SelectNode(n);
                    state = State.SetSegmentEnd;
                }
            foreach (var r in roadNet.roads)
                if (RoadButton(r))
                {
                    Undo.RegisterCompleteObjectUndo(target, "Split Road");
                    SelectNode(roadNet.SplitRoad(r, ITP(RoadButtonHit)));
                    state = State.SetSegmentEnd;
                    break;
                }

            if (isMouseHit && selectedStyle)
            {
                Handles.DrawWireDisc(TP(point), transform.up, selectedStyle.width / 2 * scale);
                if(ev.type == EventType.MouseUp && ev.button == 0)
                {
                    ev.Use();
                    Undo.RegisterCompleteObjectUndo(target, "Add Node");
                    SelectNode(roadNet.AddNode(point, selectedStyle));
                    state = State.SetSegmentEnd;
                }
            }
        }
        else if (state == State.SetSegmentEnd)
        {
            foreach (var n in roadNet.nodes)
                if (NodeButton(n) && selectedStyle)
                {
                    Undo.RegisterCompleteObjectUndo(target, "Connect Nodes");
                    roadNet.AddRoad(selectedNode, n, selectedStyle);
                    SelectNode(n);
                    state = State.SetSegmentEnd;
                }
            foreach (var r in roadNet.roads)
                if (RoadButton(r))
                {
                    Undo.RegisterCompleteObjectUndo(target, "Split Road and Connect Nodes");
                    RoadNet.Node n = roadNet.SplitRoad(r, ITP(RoadButtonHit));
                    roadNet.AddRoad(selectedNode, n, selectedStyle);
                    SelectNode(n);
                    state = State.SetSegmentEnd;
                    break;
                }
            if (isMouseHit && selectedStyle != null)
            {
                Handles.DrawWireDisc(TP(point), transform.up, selectedStyle.width / 2 * scale);
                if (ev.type == EventType.MouseUp && ev.button == 0)
                {
                    ev.Use();
                    Undo.RegisterCompleteObjectUndo(target, "Add Node");
                    var n = roadNet.AddNode(point, selectedStyle);
                    roadNet.AddRoad(selectedNode, n, selectedStyle);
                    selectedNode = n;
                    state = State.SetSegmentEnd;
                }
            }
        }


        if (ev.type == EventType.KeyUp && ev.keyCode == KeyCode.Escape)
        {
            if (state != State.Editing)
            {
                selectedNode = null;
                selectedRoad = null;
                state = State.Editing;
            }
            else
            {
                state = State.Inactive;
                Tools.current = Tool.Move;
            }
            ev.Use();
        }
        if (ev.type == EventType.KeyUp && ev.keyCode == KeyCode.A)
        {
            if (selectedNode == null)
                state = State.AddOrSelectPoint;
            else
                state = State.SetSegmentEnd;
            ev.Use();
        }
        if(ev.type==EventType.KeyUp && ev.keyCode == KeyCode.X)
        {
            if(selectedNode!=null)
            {
                roadNet.RemoveNode(selectedNode);
                selectedNode = null;
                if (!roadNet.roads.Contains(selectedRoad))
                    selectedRoad = null;
            }
            if (selectedRoad != null)
            {
                roadNet.RemoveRoad(selectedRoad);
                selectedRoad = null;
            }
            ev.Use();
        }

        Repaint();
        SceneView.RepaintAll();
        HandleUtility.AddDefaultControl(0);
    }
    public override void OnInspectorGUI()
    {
        var target = (this.target) as RoadNetObject;
        var roadNet = target.roadNet;

        GUILayout.Label($"Nodes: {roadNet.nodes.Count}");
        GUILayout.Label($"Roads: {roadNet.roads.Count}");

        if (state == State.Inactive)
            if (GUILayout.Button("Start Editing"))
            {
                state = State.Editing;
                SceneView.RepaintAll();
            }

        GUILayout.Label(state.ToString());
        selectedStyle = EditorGUILayout.ObjectField("roadStyle",selectedStyle,typeof(RoadStyle),allowSceneObjects:false) as RoadStyle;

        if (GUILayout.Button("Check Data Structure"))
        {
            roadNet.Check();
            Debug.Log("Check Done");
        }
    }
}
