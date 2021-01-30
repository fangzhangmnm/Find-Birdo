using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/* Input Settings
 * Donnot forget deadzones!
 * Horizontal X
 * Vertical Y
 * XR Look 4
 */
[RequireComponent(typeof(CapsuleCollider))]
public class XRSimpleTeleport : MonoBehaviour
{
    public Transform inputRef;

    public LayerMask teleportGroundLayers=-1;
    public LayerMask environmentLayers=-1;
    public float teleportMaxSlope = 60f;

    public float teleportTrajectoryVelocity=5;
    public float teleportTrajectoryGravity=5;
    public float teleportTrajectoryTime=3;
    public int teleportTrajectoryNPoints=30;

    public float rotateSnap = 45f;

    public float stepDistance = .5f;

    public float rotateTime = .2f;
    public float teleportTime = .2f;
    public float stepTime = .4f;
    //TODO FOV Blur and Time
    float transportCD = 0;

    public LineRenderer teleportTrajectoryRenderer;
    public GameObject teleportTargetRenderer;
    public Gradient teleportTrajectoryValidColor, teleportTrajectoryInvalidColor;

    Vector3[] teleportTrajectoryPoints;
    bool canTeleport;
    Vector3 teleportPoint;

    
    CapsuleCollider capsuleCollider;
    float R { get { return capsuleCollider.radius; } }
    float H { get { return capsuleCollider.height; } }
    float scale => transform.lossyScale.x;
    Vector3 GravityUp => transform.up;
    Vector3 TP(float x, float y, float z) => transform.TransformPoint(new Vector3(x, y, z));
    Vector3 TV(float x, float y, float z) => transform.TransformVector(new Vector3(x, y, z));
    Vector3 TD(float x, float y, float z) => transform.TransformDirection(new Vector3(x, y, z));
    private void Start()
    {
        capsuleCollider = GetComponent<CapsuleCollider>();
    }

    void CalculateTrack()
    {

        if (teleportTrajectoryPoints is null) teleportTrajectoryPoints = new Vector3[teleportTrajectoryNPoints];
        Vector3 r = inputRef.position;
        Vector3 v = inputRef.forward * teleportTrajectoryVelocity;
        float dt = teleportTrajectoryTime / teleportTrajectoryNPoints;
        canTeleport = false;

        teleportTrajectoryPoints[0] = r;
        for (int i = 1; i < teleportTrajectoryNPoints; ++i)
        {
            Vector3 r0 = r;r += v * dt;
            v += Vector3.down * teleportTrajectoryGravity * dt;
            RaycastHit hitInfo;
            if(Physics.Raycast(r0, r-r0, out hitInfo, Vector3.Distance(r0, r), environmentLayers))
            {
                if((teleportGroundLayers.value & (1 << hitInfo.collider.gameObject.layer)) > 0)
                    if(CheckTeleportTarget(hitInfo.point))
                        canTeleport = true;
                teleportPoint = r = hitInfo.point;
                for (; i < teleportTrajectoryNPoints; ++i)
                    teleportTrajectoryPoints[i] = r;
            }
            else
            {
                teleportTrajectoryPoints[i] = r;
            }
        }
    }
    bool CheckTeleportTarget(Vector3 targetPoint)
    {
        RaycastHit hitInfo;
        if (Physics.SphereCast(targetPoint+TV(0, R, 0), .9f * R*scale, transform.up, out hitInfo, (H-1.8f*R) * scale, environmentLayers))
            return false;
        if (!Physics.SphereCast(targetPoint + TV(0, H / 2, 0), .9f * R * scale, -transform.up, out hitInfo, H / 2 * scale, environmentLayers))
            return false;
        if ((teleportGroundLayers.value & (1 << hitInfo.collider.gameObject.layer)) == 0)
            return false;
        if (hitInfo.normal.y < Mathf.Cos(teleportMaxSlope * Mathf.Deg2Rad))
            return false;
        return true;
    }
    void TeleportTo(Vector3 targetPoint)
    {
        //Vector3 oldFoot = head.position - Vector3.Dot(head.position - transform.position, Vector3.up) * Vector3.up;
        Vector3 oldFoot = transform.position;
        transform.position += targetPoint - oldFoot;
    }


    int state = 0;
    Vector2 inputStick;
    Vector2 inputStick2;
    private void Update()
    {
        inputStick = Vector2.ClampMagnitude(new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")), 1);
        inputStick2 = new Vector2(Input.GetAxis("XR Look"), 0);

        transportCD = Mathf.Max(0,transportCD-Time.deltaTime);

        if (Mathf.Abs(inputStick2.x) > .5f)
        {
            if (transportCD <= 0)
            {
                transportCD = rotateTime;
                transform.rotation = transform.rotation * Quaternion.AngleAxis(rotateSnap * Mathf.Sign(Input.GetAxis("XR Look")), Vector3.up);
            }
        }

        Vector3 stepVector = Vector3.zero;
        if (inputStick.x > .5f) stepVector.x += 1; 
        if (inputStick.x < -.5f)stepVector.x -=1; 
        if (inputStick.y < -.5f)stepVector.z -= 1;
        stepVector *= stepDistance * scale;
        if (inputStick.y > .5f) stepVector = Vector3.zero;

        if (stepVector.magnitude > 0)
        {
            if (transportCD <= 0)
            {
                transportCD = stepTime;
                RaycastHit stepHitInfo;
                if (Physics.Raycast(TP(0, H/2, 0)+stepVector, -transform.up, out stepHitInfo, 1.0f * H * scale, environmentLayers))
                {
                    if (CheckTeleportTarget(stepHitInfo.point))
                        TeleportTo(stepHitInfo.point);
                }
            }
        }


        if (inputStick.y > .5f)
            state = 1;
        else
        {
            if(state==1)
            {
                if (transportCD <= 0)
                {
                    transportCD = teleportTime;
                    if (canTeleport)
                    {
                        TeleportTo(teleportPoint);
                    }
                }

                state = 0;
            }
        }
        if (state == 1)
        {
            CalculateTrack();
            teleportTrajectoryRenderer.gameObject.SetActive(true);
            teleportTrajectoryRenderer.useWorldSpace = true;
            teleportTrajectoryRenderer.positionCount = teleportTrajectoryPoints.Length;
            teleportTrajectoryRenderer.SetPositions(teleportTrajectoryPoints);
            teleportTrajectoryRenderer.colorGradient = canTeleport ? teleportTrajectoryValidColor : teleportTrajectoryInvalidColor;

            teleportTargetRenderer.transform.position = teleportPoint;
            teleportTargetRenderer.SetActive(canTeleport);
        }
        else
        {
            teleportTrajectoryRenderer.gameObject.SetActive(false);
            teleportTargetRenderer.SetActive(false);
        }

    }


}
