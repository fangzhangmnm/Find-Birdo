using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeleportLine : MonoBehaviour
{
    public float teleportTrajectoryVelocity = 5;
    public float teleportTrajectoryGravity = 5;
    public float teleportTrajectoryTime = 3;
    public int teleportTrajectoryNPoints = 30;

    public LineRenderer teleportTrajectoryRenderer;
    public GameObject teleportTargetRenderer;
    public Gradient teleportTrajectoryValidColor, teleportTrajectoryInvalidColor;

    public Transform inputRef;
    public LayerMask environmentLayers, groundLayers;

    public Vector3 intersectPoint;
    public bool intersectPointFound;

    public delegate bool CheckTeleportPointDelegate(RaycastHit hit);
    public CheckTeleportPointDelegate CheckTeleportPoint=(RaycastHit hit)=>true;

    private void OnDisable()
    {
        teleportTrajectoryRenderer.enabled = false;
        teleportTargetRenderer.SetActive(false);
    }
    private void Update()
    {
        CalculateTeleportTrack();

        teleportTrajectoryRenderer.enabled = true;
        teleportTrajectoryRenderer.useWorldSpace = true;
        teleportTrajectoryRenderer.positionCount = teleportTrajectoryPoints.Length;
        teleportTrajectoryRenderer.SetPositions(teleportTrajectoryPoints);
        teleportTrajectoryRenderer.colorGradient = intersectPointFound ? teleportTrajectoryValidColor : teleportTrajectoryInvalidColor;
        teleportTargetRenderer.transform.position = intersectPoint;
        teleportTargetRenderer.SetActive(intersectPointFound);
    }
    void CalculateTeleportTrack()
    {
        if (teleportTrajectoryPoints is null) teleportTrajectoryPoints = new Vector3[teleportTrajectoryNPoints];
        Vector3 r = inputRef.position;
        Vector3 v = inputRef.forward * teleportTrajectoryVelocity;
        float dt = teleportTrajectoryTime / teleportTrajectoryNPoints;
        intersectPointFound = false;

        teleportTrajectoryPoints[0] = r;
        for (int i = 1; i < teleportTrajectoryNPoints; ++i)
        {
            Vector3 r0 = r; r += v * dt;
            v += Vector3.down * teleportTrajectoryGravity * dt;
            RaycastHit hitInfo;
            if (Physics.Raycast(r0, r - r0, out hitInfo, Vector3.Distance(r0, r), environmentLayers))
            {
                if ((groundLayers & (1 << hitInfo.collider.gameObject.layer)) > 0)
                    if(CheckTeleportPoint(hitInfo))
                        intersectPointFound = true;
                intersectPoint = r = hitInfo.point;
                for (; i < teleportTrajectoryNPoints; ++i)
                    teleportTrajectoryPoints[i] = r;
                break;
            }
            else
            {
                teleportTrajectoryPoints[i] = r;
            }
        }
    }
    Vector3[] teleportTrajectoryPoints;
}
