using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class RoadNetObject : MonoBehaviour
{
    [HideInInspector]public RoadNet roadNet;

    public static void GenerateRoadPoints(RoadNet.Road road, out Vector3[] resultsLS)
    {
        Vector3 p1 = road.start.position;
        Vector3 p3 = road.end.position;
        Vector3 p2;
        if (road.hasControlPoint)
            p2 = road.controlPoint;
        else
            p2 = (p1 + p3) / 2;
        GenerateRoadPoints(p1, p2, p3, out resultsLS, out _, 6);
    }   

    public static void GenerateJunctionProfile(Vector3[] points, float[] widths, out Vector3[] results)
    {
        int n = points.Length;
        points = (Vector3[])points.Clone();
        widths = (float[])widths.Clone();

        for (int i = 0; i < n - 1; ++i)
            for (int j = 0; j < n - i - 1; ++j)
            {
                if (Mathf.Atan2(points[j].z, points[j].x) > Mathf.Atan2(points[j + 1].z, points[j + 1].x))
                {
                    Vector3 tmp2 = points[j]; points[j] = points[j + 1]; points[j + 1] = tmp2;
                    float tmp = widths[j]; widths[j] = widths[j + 1]; widths[j + 1] = tmp;
                }
            }
        results = new Vector3[3 * n];
        for (int i = 0; i < n; ++i)
        {
            int ii = (i + 1) % n;
            float sinTh = -Vector3.Cross(points[i].normalized, points[ii].normalized).y;
            float cosTh = Vector2.Dot(points[i].normalized, points[ii].normalized);
            float rightLength = sinTh == 0 ? 0 : points[i].magnitude - (widths[i] / 2 / sinTh * cosTh + widths[ii] / 2 / sinTh);
            Vector3 right = Vector3.Cross(points[i], Vector3.up).normalized;
            results[3 * i] = points[i] - widths[i] / 2 * right;
            results[3 * i + 1] = points[i] + widths[i] / 2 * right;
            results[3 * i + 2] = results[3 * i + 1] - points[i].normalized * rightLength;
        }
    }
    public static void SmoothJunctionProfile(Vector3[] points, out Vector3[] results, int segments)
    {
        int n = points.Length / 3;
        results = new Vector3[(2 * segments + 1) * n];
        int ptr = 0;
        for (int i = 0; i < n; ++i)
        {
            int ii = (i + 1) % n;
            results[ptr++] = points[3 * i];
            results[ptr++] = points[3 * i + 1];
            for (int j = 1; j < 2 * segments; ++j)
            {
                results[ptr++] = QuadraticSpline(points[3 * i + 1], points[3 * i + 2], points[3 * ii], (float)j / (2 * segments));
            }
        }
    }
    public static void GenerateRoadPoints(Vector3 p1, Vector3 p2, Vector3 p3, out Vector3[] results, out Vector3[] resultTangents, int nseg)
    {
        List<Vector3> rtval = new List<Vector3>();
        List<Vector3> rtval2 = new List<Vector3>();
        for (int j = 0; j <= nseg + 1; ++j)
        {
            float t = (float)j / (nseg + 1);
            rtval.Add(QuadraticSpline(p1, p2, p3, t));
            rtval2.Add(QuadraticSplineTangent(p1, p2, p3, t));
        }
        results = rtval.ToArray();
        resultTangents = rtval2.ToArray();
    }
    public static Vector3 QuadraticSpline(Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        return p1 + t * 2 * (p2 - p1) + t * t * (p1 - 2 * p2 + p3);
    }
    public static Vector3 QuadraticSplineTangent(Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        return 2 * (p2 - p1) + 2 * t * (p1 - 2 * p2 + p3);
    }
    public static float RayLineDist(Ray r, Vector3 p1, Vector3 p2)
    {
        Vector3 d1 = Vector3.ProjectOnPlane(p1 - r.origin, r.direction);
        Vector3 d2 = Vector3.ProjectOnPlane(p2 - r.origin, r.direction);
        float t = Vector3.Dot(-d1, d2 - d1) / (d2 - d1).sqrMagnitude;
        return Vector3.Lerp(d1, d2, Mathf.Clamp01(t)).magnitude;
    }
    public static Vector3 RayLineHit(Ray r, Vector3 p1, Vector3 p2)
    {
        Vector3 d1 = Vector3.ProjectOnPlane(p1 - r.origin, r.direction);
        Vector3 d2 = Vector3.ProjectOnPlane(p2 - r.origin, r.direction);
        float t = Vector3.Dot(-d1, d2 - d1) / (d2 - d1).sqrMagnitude;
        return Vector3.Lerp(p1, p2, Mathf.Clamp01(t));
    }
}
