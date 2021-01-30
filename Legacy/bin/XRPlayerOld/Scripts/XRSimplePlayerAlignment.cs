using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CapsuleCollider))]
public class XRSimplePlayerAlignment : MonoBehaviour
{
    public Transform head;
    public SphereCollider headCollider;
    public Transform trackingSpace;
    public float headLeanDistance = .3f;
    public float trackingLostDistance = .3f;
    public float preventFallDistance = .3f;
    public float slopeLimit = 60f;
    public float minHeadHeight = .3f;
    public float maxHeadHeight = 2.2f;

    public LayerMask environmentLayers = -1;
    Rigidbody body;
    CapsuleCollider capsuleCollider;
    float R => capsuleCollider.radius;
    float H => capsuleCollider.height;
    float scale => transform.lossyScale.x;
    Vector3 up => transform.up;
    Vector3 GravityUp => transform.up;
    Vector3 TP(float x, float y, float z) => transform.TransformPoint(new Vector3(x, y, z));
    Vector3 TV(float x, float y, float z) => transform.TransformVector(new Vector3(x, y, z));
    Vector3 TD(float x, float y, float z) => transform.TransformDirection(new Vector3(x, y, z));
    private void Start()
    {
        capsuleCollider = GetComponent<CapsuleCollider>();
        body = GetComponent<Rigidbody>();
        Debug.Assert(headCollider != null);
    }
    Vector3 oldHeadTracking = Vector3.zero;
    RaycastHit[] hitInfos = new RaycastHit[20];
    private void FixedUpdate()
    {
        //Align Rotation
        Vector3 lookForward = Vector3.ProjectOnPlane(head.forward, up).normalized;
        if (lookForward.magnitude > 0)
        {
            float angle = Vector3.SignedAngle(transform.forward, lookForward, transform.up);
            transform.Rotate(transform.up, angle);
            trackingSpace.RotateAround(head.position, transform.up, -angle);
        }

        //Setup Collider
        float actualHeadHeightLS = transform.InverseTransformPoint(head.position).y;
        float headHeightLS = Mathf.Clamp(actualHeadHeightLS, minHeadHeight, maxHeadHeight);
        capsuleCollider.height = headHeightLS + headCollider.radius;
        capsuleCollider.center = new Vector3(0, capsuleCollider.height / 2, 0);

        //Align Position
        Vector3 headBiasWS = Vector3.ProjectOnPlane(head.position - transform.position, up);
        Vector3 headBiasNonImposedWS = Vector3.ClampMagnitude(headBiasWS, headLeanDistance);
        Vector3 headBiasImposedWS = headBiasWS - headBiasNonImposedWS;

        //Check Falling
        if (headBiasNonImposedWS.magnitude > 0)
        {
            //RaycastHit hitInfo;bool isHit;
            //if (body)
            //    isHit=body.SweepTest(headBiasNonImposedWS, out hitInfo, headBiasWS.magnitude);
            //else

            Vector3 point1 = transform.TransformPoint(capsuleCollider.center + (H / 2 - R) * GravityUp);
            Vector3 point2 = transform.TransformPoint(capsuleCollider.center - (H / 2 - R) * GravityUp);
            int hitNum = Physics.CapsuleCastNonAlloc(point1, point2, R * scale * .9f,
                headBiasNonImposedWS.normalized, hitInfos, headBiasWS.magnitude + R * scale * .2f, environmentLayers);

            bool isBlocked = false;
            for (int i = 0; i < hitNum; ++i)
            {
                Debug.DrawRay(hitInfos[i].point, hitInfos[i].normal, Color.red, Time.fixedDeltaTime);
                if (Vector3.Angle(GravityUp, hitInfos[i].normal) > slopeLimit)
                    isBlocked = true;
            }
            if (isBlocked)
                headBiasNonImposedWS = Vector3.zero;
        }
        //Check Falling

        if (!Physics.Raycast(transform.position + headBiasWS + TV(0, headHeightLS / 2, 0),
                        -GravityUp,
                        (headHeightLS / 2 + preventFallDistance) * scale, environmentLayers))
            headBiasNonImposedWS = Vector3.zero;

        trackingSpace.position -= headBiasImposedWS + headBiasNonImposedWS;

        if (Vector3.Distance(oldHeadTracking, head.localPosition) < trackingLostDistance)
        {
            transform.position += headBiasImposedWS + headBiasNonImposedWS;
        }
        oldHeadTracking = head.localPosition;
    }
}
