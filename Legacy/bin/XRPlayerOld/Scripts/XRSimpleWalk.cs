using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//TODO crouch height adjustment

/* Input Settings
 * Donnot forget deadzones!
 * Horizontal X
 * Vertical Y
 * XR Look 4
 * XR Jump 5 Inverted
 * Dash joystick button8
 * Menu joystick button6
 */
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(Rigidbody))]
public class XRSimpleWalk : MonoBehaviour
{
    public Transform inputRef;
    public float speed = 3f;
    public float dashSpeed = 7f;
    public float rotateSnap = 45f;
    public float rotateSnapTime = .2f;
    public float stepHeight = .3f;
    public float slopeLimit = 60f;
    public float jumpSpeed = 4.5f;
    public LayerMask environmentLayers = -1;

    Rigidbody body;
    CapsuleCollider capsuleCollider;
    float R => capsuleCollider.radius;
    float H => capsuleCollider.height;
    float scale => transform.lossyScale.x;
    Vector3 GravityUp => transform.up;
    Vector3 TP(float x, float y, float z) => transform.TransformPoint(new Vector3(x, y, z));
    Vector3 TV(float x, float y, float z) => transform.TransformVector(new Vector3(x, y, z));
    Vector3 TD(float x, float y, float z) => transform.TransformDirection(new Vector3(x, y, z));
    private void Start()
    {
        body = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        var physicsMaterial = new PhysicMaterial("character");
        physicsMaterial.bounciness = 0;
        physicsMaterial.dynamicFriction = 0;
        physicsMaterial.staticFriction = 2;
        physicsMaterial.frictionCombine = PhysicMaterialCombine.Minimum;
        capsuleCollider.sharedMaterial = physicsMaterial;
        body.drag = 1f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }
    void OnDisable()
    {
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        body.isKinematic = true;
    }

    void MoveCharacter(Vector3 moveVelocityWS, out bool isThisStepHit)
    {

        RaycastHit thisStepHit;
        isThisStepHit = DebugSphereCast(TP(0, .9f * R + stepHeight, 0), .9f * R * scale, TD(0, -1, 0), out thisStepHit, 2 * stepHeight * scale, refine: true);

        bool isNextStepHit;
        RaycastHit nextStepHit;
        isNextStepHit = DebugSphereCast(TP(0, .9f * R + stepHeight, 0) + moveVelocityWS.normalized * R * scale, .9f * R * scale, TD(0, -1, 0), out nextStepHit, 2 * stepHeight * scale, refine: true);


        if (moveVelocityWS.magnitude > 0 && isNextStepHit && Vector3.Dot(nextStepHit.normal, GravityUp) > Mathf.Cos(Mathf.Deg2Rad * slopeLimit))
        {
            Vector3 nextStep = TP(0, .9f * R + stepHeight, 0) + moveVelocityWS.normalized * R * scale + nextStepHit.distance * TD(0, -1, 0) + TV(0, -.9f * R, 0);
            body.velocity = (nextStep - transform.position).normalized * moveVelocityWS.magnitude;
            body.AddForce(-body.mass * Physics.gravity);
        }
        else if (isNextStepHit || isThisStepHit)
        {
            //FallBack Grounded control
            //body.velocity = moveVelocity;
            //body.AddForce(-body.mass * Physics.gravity);
            body.velocity = Vector3.Dot(body.velocity, GravityUp) * GravityUp + moveVelocityWS;
        }
        else
        {
            //Falling
        }
    }
    Vector2 inputStick;
    Vector2 inputStick2;
    bool inputDash;
    bool inputJump;
    private void Update()
    {
        inputStick = Vector2.ClampMagnitude(new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")), 1);
        inputStick2 = new Vector2(Input.GetAxis("XR Look"), Input.GetAxis("XR Jump"));
        inputDash = Input.GetButton("Dash");
        inputJump = inputStick2.y > .5f;



        if (Mathf.Abs(inputStick2.x) > .5f)
        {
            rotateSnapCD -= Time.deltaTime;
            if (rotateSnapCD <= 0)
            {
                rotateSnapCD = rotateSnapTime;
                body.rotation = body.rotation * Quaternion.AngleAxis(rotateSnap * Mathf.Sign(inputStick2.x), Vector3.up);
            }
        }
        else
            rotateSnapCD = 0;
    }
    float rotateSnapCD = 0;
    private void FixedUpdate()
    {
        body.isKinematic = false;
        body.collisionDetectionMode = CollisionDetectionMode.Continuous;
        Vector3 inputVectorW = Vector3.ProjectOnPlane(inputRef.TransformDirection(new Vector3(inputStick.x, 0, inputStick.y)), Vector3.up).normalized * inputStick.magnitude;

        float curSpeed = inputDash ? dashSpeed : speed;
        bool isThisStepHit;
        MoveCharacter(inputVectorW * curSpeed * scale, out isThisStepHit);
        //MoveCharacter(inputVectorW * speed+headBias/Time.fixedDeltaTime, .3f, 60f, Vector3.up);//Jittering?



        if (inputJump && isThisStepHit)
        {
            body.velocity = Vector3.ProjectOnPlane(body.velocity, Vector3.up) + Vector3.up * jumpSpeed;
        }
    }
    #region DebugCasts

    Collider[] clds = new Collider[1];
    bool DebugSphereCast(Vector3 origin, float radius, Vector3 dir, out RaycastHit hitInfo, float maxDist, bool refine = false, bool detectInside = false, Color? color = null)
    {
        dir = dir.normalized;
        bool rtval = Physics.SphereCast(origin, radius, dir, out hitInfo, maxDist, environmentLayers);
        DebugDrawSphere(origin, radius, color ?? Color.white);
        DebugDrawSphere(origin + maxDist * dir, radius, color ?? Color.white);
        Debug.DrawLine(origin, origin + dir * (maxDist), color ?? Color.white);
        if (rtval)
        {
            if (refine)
                RefineSphereCastNormal(origin, radius, dir, ref hitInfo);
            DebugDrawSphere(origin + hitInfo.distance * dir, radius, Color.red);
            Debug.DrawLine(hitInfo.point, hitInfo.point + hitInfo.normal, Color.red);
        }
        if (detectInside)
            if (Physics.OverlapSphereNonAlloc(origin, radius, clds) > 0)
                rtval = true;
        return rtval;
    }
    void RefineSphereCastNormal(Vector3 origin, float radius, Vector3 dir, ref RaycastHit hitInfo)
    {
        hitInfo.normal = Vector3.zero;
        hitInfo.point = Vector3.zero;
        var hits = new RaycastHit[5];
        int n = Physics.SphereCastNonAlloc(origin, radius, dir, hits, hitInfo.distance + 0.1f * radius, environmentLayers);
        int nn = 0;
        for (int i = 0; i < n; ++i)
        {
            if (hits[i].distance > 0)
            {
                hitInfo.normal += hits[i].normal;
                hitInfo.point += hits[i].point;
                nn += 1;
            }
        }
        hitInfo.normal = hitInfo.normal.normalized;
        hitInfo.point /= nn;
    }
    void DebugDrawSphere(Vector3 pos, float radius, Color color)
    {
#if UNITY_EDITOR
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
#endif
    }
    Vector3 debugVec;
    #endregion
}
