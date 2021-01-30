using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;




public class JunctionProfile : MonoBehaviour
{




    public Vector3[] points;
    public float[] widths;
    public Vector3[] results;
    public int segments = 3;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        if (results!=null && results.Length>0)
        {
            for(int i = 0; i < results.Length; ++i)
            {
                Gizmos.DrawSphere(results[i], .1f);
                int ii = (i + 1) % results.Length;
                Gizmos.DrawLine(results[i], results[ii]);
            }
        }
        Gizmos.color = Color.yellow;
        if (points!=null && points.Length > 0)
        {
            for (int i = 0; i < points.Length; ++i)
            {
                Gizmos.DrawSphere(points[i], .2f);
            }
        }
    }
    [Button]
    void Generate()
    {
        GenerateJunctionProfile(points, widths, out results);
        SmoothJunctionProfile(results, out results, segments);
        //GenerateRoadPoints(points, out results, out _, 1f);
    }
    void GenerateJunctionProfile(Vector3[] points, float[] widths, out Vector3[] results)
    {
        int n = points.Length;
        points=(Vector3[])points.Clone();
        widths = (float[])widths.Clone();

        for (int i=0;i<n-1;++i)
            for(int j = 0; j < n - i - 1; ++j)
            {
                if (Mathf.Atan2(points[j].z,points[j].x)>Mathf.Atan2(points[j+1].z,points[j+1].x))
                {
                    Vector3 tmp2 = points[j];points[j] = points[j + 1];points[j + 1] = tmp2;
                    float tmp = widths[j]; widths[j] = widths[j + 1]; widths[j + 1] = tmp;
                }
            }
        results = new Vector3[3 * n];
        for(int i = 0; i < n; ++i)
        {
            int ii = (i + 1) % n;
            float sinTh = -Vector3.Cross(points[i].normalized, points[ii].normalized).y;
            float cosTh = Vector2.Dot(points[i].normalized, points[ii].normalized);
            float rightLength = sinTh==0? 0: points[i].magnitude - (widths[i] / 2 / sinTh * cosTh + widths[ii] / 2 / sinTh);
            Vector3 right = Vector3.Cross(points[i],Vector3.up).normalized;
            results[3 * i] = points[i] - widths[i] / 2 * right;
            results[3 * i+1] = points[i] + widths[i] / 2 * right;
            results[3 * i + 2] = results[3 * i + 1] - points[i].normalized * rightLength;
        }
    }
    void SmoothJunctionProfile(Vector3[] points, out Vector3[] results,int segments)
    {
        int n = points.Length / 3;
        results = new Vector3[(2*segments+1) * n];
        int ptr = 0;
        for(int i = 0; i < n; ++i)
        {
            int ii = (i + 1) % n;
            results[ptr++] = points[3*i];
            results[ptr++] = points[3 * i+1];
            for(int j = 1; j <2*segments; ++j)
            {
                results[ptr++] = QuadraticSpline(points[3 * i + 1], points[3 * i + 2], points[3 * ii], (float)j / (2 * segments));
            }
        }
    }

    void GenerateRoadPoints(Vector3[] controlPoints, out Vector3[] results, out Vector3[] resultTangents, float segmentDist)
    {
        List<Vector3> rtval = new List<Vector3>();
        List<Vector3> rtval2 = new List<Vector3>();
        for (int i = 0; i < controlPoints.Length - 2; i+=2)
        {
            float d = Vector3.Distance(controlPoints[i], controlPoints[i + 1]) + Vector3.Distance(controlPoints[i+1], controlPoints[i + 2]);
            int nseg = Mathf.CeilToInt(d / segmentDist);
            for(int j = 0; j <= (i == controlPoints.Length - 3 ? nseg + 1 : nseg); ++j)
            {
                float t = (float)j / (nseg+1);
                rtval.Add(QuadraticSpline(controlPoints[i], controlPoints[i+1], controlPoints[i + 2], t));
                rtval2.Add(QuadraticSplineTangent(controlPoints[i], controlPoints[i+1], controlPoints[i + 2], t));
            }
        }
        results = rtval.ToArray();
        resultTangents = rtval2.ToArray();
    }
    Vector3 CatmullRomSpline(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, float t,float tau=1f)
    {
        //https://www.cs.cmu.edu/~fp/courses/graphics/asst5/catmullRom.pdf
        Vector3 v1 = p2;
        Vector3 v2 = tau * (p3 - p1);
        Vector3 v3 = 2 * tau * p1 + (tau - 3) * p2 + (3 - 2 * tau) * p3 - tau * p4;
        Vector3 v4 = -tau * p1 + (2 - tau) * p2 + (tau - 2) * p3 + tau * p4;
        return v1 + t * v2 + t * t * v3 + t * t * t * v4;
    }
    Vector3 CatmullRomSplineTangent(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, float t, float tau = 0.5f)
    {
        //https://www.cs.cmu.edu/~fp/courses/graphics/asst5/catmullRom.pdf
        Vector3 v1 = p2;
        Vector3 v2 = tau * (p3 - p1);
        Vector3 v3 = 2 * tau * p1 + (tau - 3) * p2 + (3 - 2 * tau) * p3 - tau * p4;
        Vector3 v4 = -tau * p1 + (2 - tau) * p2 + (tau - 2) * p3 + tau * p4;
        return 3 * t * t * v4 + 2 * t * v3 + v2;
    }
    Vector3 QuadraticSpline(Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        return p1 + t* 2 * (p2 - p1) + t*t*(p1 - 2 * p2 + p3);
    }
    Vector3 QuadraticSplineTangent(Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        return 2 * (p2 - p1) + 2 * t * (p1 - 2 * p2 + p3);
    }
}
