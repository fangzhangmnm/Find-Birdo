using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class PhysicsDebugCast : MonoBehaviour
{
    
    public static bool CapsuleCast(Vector3 P1, Vector3 P2, float radius, Vector3 dir, out RaycastHit hitInfo, float maxDist, int layers, Color? color = null)
    {
        bool rtval=Physics.CapsuleCast(P1, P2, radius, dir, out hitInfo, maxDist, layers);
#if UNITY_EDITOR
        dir = dir.normalized;
        DebugDrawSphere(P1, radius, color ?? Color.white);
        DebugDrawSphere(P2, radius, color ?? Color.white);
        Debug.DrawLine(P1, P2, color ?? Color.white);

        DebugDrawSphere(P1 + maxDist * dir, radius, color ?? Color.white);
        Debug.DrawLine(P1, P1 + dir * (maxDist), color ?? Color.white);
        DebugDrawSphere(P2 + maxDist * dir, radius, color ?? Color.white);
        Debug.DrawLine(P2, P2 + dir * (maxDist), color ?? Color.white);

        if (rtval)
        {
            DebugDrawSphere(P1 + hitInfo.distance * dir, radius, Color.red);
            DebugDrawSphere(P2 + hitInfo.distance * dir, radius, Color.red);
            Debug.DrawLine(hitInfo.point, hitInfo.point + hitInfo.normal, Color.red);
        }
#endif
        return rtval;
    }
    public static bool SphereCast(Vector3 origin, float radius, Vector3 dir, out RaycastHit hitInfo, float maxDist, int layers, Color? color = null)
    {
        bool rtval = Physics.SphereCast(origin, radius, dir, out hitInfo, maxDist, layers);
#if UNITY_EDITOR
        dir = dir.normalized;
        DebugDrawSphere(origin, radius, color ?? Color.white);
        DebugDrawSphere(origin + maxDist * dir, radius, color ?? Color.white);
        Debug.DrawLine(origin, origin + dir * (maxDist), color ?? Color.white);
        if (rtval)
        {
            DebugDrawSphere(origin + hitInfo.distance * dir, radius, Color.red);
            Debug.DrawLine(hitInfo.point, hitInfo.point + hitInfo.normal, Color.red);
        }
#endif
        return rtval;
    }
    public static void DebugDrawSphere(Vector3 pos, float radius, Color color)
    {
        for (int i = 0; i < 20; ++i)
        {
            float c = Mathf.Cos(i * Mathf.PI * 2 / 20);
            float s = Mathf.Sin(i * Mathf.PI * 2 / 20);
            float c1 = Mathf.Cos((i + 1) * Mathf.PI * 2 / 20);
            float s1 = Mathf.Sin((i + 1) * Mathf.PI * 2 / 20);
            Debug.DrawLine(pos + new Vector3(radius * c, radius * s, 0), pos + new Vector3(radius * c1, radius * s1, 0), color);
            Debug.DrawLine(pos + new Vector3(radius * c, 0, radius * s), pos + new Vector3(radius * c1, 0, radius * s1), color);
            Debug.DrawLine(pos + new Vector3(0, radius * c, radius * s), pos + new Vector3(0, radius * c1, radius * s1), color);
        }
    }
}
