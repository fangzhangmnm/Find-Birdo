using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class RoadNet
{
    [Serializable]
    public class Node
    {
        public Vector3 position;
        public RoadStyle style;

        [SerializeReference]
        public List<Road> roads = new List<Road>();
    }
    [Serializable]
    public class Road
    {
        public float length;
        public RoadStyle style;
        public bool hasControlPoint = false;
        public Vector3 controlPoint;

        [SerializeReference]
        public Node start, end;
    }
    [SerializeReference]
    public List<Node> nodes = new List<Node>();
    [SerializeReference]
    public List<Road> roads = new List<Road>();

    public Node AddNode(Vector3 localPos,RoadStyle style)
    {
        Debug.Assert(style != null);

        Node n = new Node();
        n.position = localPos;
        n.style = style;
        nodes.Add(n);
        return n;
    }
    public Road AddRoad(Node start, Node end, RoadStyle style)
    {
        Debug.Assert(start!=null);
        Debug.Assert(end != null);
        Debug.Assert(style != null);
        Debug.Assert(nodes.Contains(start));
        Debug.Assert(nodes.Contains(end));

        Road r = new Road();
        r.start = start;
        r.end = end;
        r.style = style;

        start.roads.Add(r);
        end.roads.Add(r);
        
        roads.Add(r);
        return r;
    }
    public Node SplitRoad(Road r, Vector3 localPos)
    {
        var style = r.style;
        var start = r.start;
        var end = r.end;
        var middle = AddNode(localPos, style);
        RemoveRoad(r);
        AddRoad(start, middle,style);
        AddRoad(middle, end, style);
        return middle;
    }
    public void RemoveRoad(Road r)
    {
        Debug.Assert(roads.Contains(r));

        r.start.roads.Remove(r);
        r.end.roads.Remove(r);
        roads.Remove(r);
        r.start = null;
        r.end = null;
    }
    public void RemoveNode(Node n)
    {
        Debug.Assert(nodes.Contains(n));
        for (int i = n.roads.Count - 1; i >= 0; --i)
            RemoveRoad(n.roads[i]);
        nodes.Remove(n);
    }
    public void Check()
    {
        foreach (var n in nodes)
            foreach (var r in n.roads)
                Debug.Assert(roads.Contains(r));
        foreach(var r in roads)
        {
            Debug.Assert(nodes.Contains(r.start));
            Debug.Assert(nodes.Contains(r.end));
        }    
    }
}
