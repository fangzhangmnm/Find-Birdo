using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class XRSimpleGrabable : XRSimpleInteractable
{
    [Tooltip("Set to null for free grabbing")]
    public Transform attachRef;
    public Transform secondaryHandRef;
    public bool deteachOnRelease = true;
    public float throwVelocityBoost = 1.25f;
    public float throwAngularVelocityBoost = 1f;
    public float massAttached = 3f;
    public float snapBeforeRenderDistance = .003f;
    public float breakDistance = .3f;
    public bool teleportWhenBreak = false;
    public bool useJoint = true;
    public float jointSpring = 50000;
    public float jointDamping = 1000;
    public float joingMaxForce = 1000;
    public float jointSpringAngular = 50000;
    public float jointDampingAngular = 1000;
    public float jointMaxForceAngular = 1000;
    public float smoothTime = .01f;
    public Behaviour hoverEffect;
    
    [HideInInspector] public XRSimpleHand attachedHand;
    [HideInInspector] public XRSimpleHand secondaryAttachedHand;
    Rigidbody attachedRigidbody;
    ConfigurableJoint joint;
    
    Vector3 attachPositionLS;Quaternion attachRotationLS;
    Rigidbody body;
    protected virtual void Awake()
    {
        body = GetComponent<Rigidbody>();
    }
    protected virtual void Start()
    {
        if (hoverEffect) hoverEffect.enabled = false;
    }
    protected virtual void OnDisable()
    {
        DropIfAttached();
    }
    public override void OnHoverEnter()
    {
        if (hoverEffect) hoverEffect.enabled = true;
    }
    public override void OnHoverExit()
    {
        if (hoverEffect) hoverEffect.enabled = false;
    }

    public void DropIfAttached() => attachedHand?.DetachIfAny();
    public virtual void OnPickup() { }
    public virtual void OnDrop() { }

    #region Pickup Logic
    public override void OnGripUp(XRSimpleHand hand)
    {
        Debug.Assert(hand && hand.attached == this);
        base.OnGripUp(hand);
        if (hand == secondaryAttachedHand)
            hand.DetachIfAny();
        else
            hand.DetachIfAny();
    }
    public override bool CanAttach(XRSimpleHand emptyHand, float pickUpDist, out int priority)
    {
        if (secondaryHandRef)
        {
            if (secondaryAttachedHand) { priority = 0; return false; }
            else { priority = -1; return true; }
        }
        else { priority = 0;return true; }
    }
    public override void OnAttach(XRSimpleHand handAttachedMe, float pickUpDist)
    {
        //Debug.Log($"OnAttach {handAttachedMe}");
        //Debug.Log($"Before Attached hands: {attachedHand}, {secondaryAttachedHand}");
        if (attachedHand)
        {
            Debug.Assert(attachedHand != handAttachedMe);
            if (secondaryHandRef)
            {
                if (secondaryAttachedHand)
                {
                    Debug.Assert(secondaryAttachedHand != handAttachedMe);
                    Debug.Assert(secondaryAttachedHand.attached == this);
                    secondaryAttachedHand.DetachIfAny();

                    Debug.Assert(secondaryAttachedHand == null);
                    secondaryAttachedHand = handAttachedMe;//Be careful with the order
                    _OnAttachSecondary();//Be careful with the order
                }
                else
                {
                    Debug.Assert(secondaryAttachedHand == null);
                    secondaryAttachedHand = handAttachedMe;//Be careful with the order
                    _OnAttachSecondary();//Be careful with the order
                }
            }
            else
            {
                Debug.Assert(attachedHand.attached == this);
                attachedHand.DetachIfAny();

                Debug.Assert(attachedHand == null);
                attachedHand = handAttachedMe;//Be careful with the order
                _OnPickup(pickUpDist);//Be careful with the order
            }
        }
        else
        {
            Debug.Assert(attachedHand == null);
            attachedHand = handAttachedMe;//Be careful with the order
            _OnPickup(pickUpDist);//Be careful with the order
        }
        //Debug.Log($"After Attached hands: {attachedHand}, {secondaryAttachedHand}");
    }
    public override void OnDetach(XRSimpleHand handWillDetachMe)
    {
        //Debug.Log($"OnDetach {handWillDetachMe}");
        //Debug.Log($"Before Attached hands: {attachedHand}, {secondaryAttachedHand}");
        if (attachedHand == handWillDetachMe)
        {
            Debug.Assert(attachedHand && attachedHand.attached == this);
            if (secondaryAttachedHand)
            {
                Debug.Assert(secondaryAttachedHand && secondaryAttachedHand.attached == this);
                secondaryAttachedHand.DetachIfAny();
                //Do not reset attachedSecondaryHand here
            }
            Debug.Assert(attachedHand.attached == this);

            Debug.Assert(attachedHand != null);
            _OnDrop();//Be careful with the order
            Debug.Assert(attachedHand != null);
            attachedHand = null;//Be careful with the order
        }
        else if (secondaryAttachedHand == handWillDetachMe)
        {
            Debug.Assert(secondaryAttachedHand.attached == this);

            Debug.Assert(secondaryAttachedHand != null);
            _OnDetachSecondary();//Be careful with the order
            Debug.Assert(secondaryAttachedHand != null);
            secondaryAttachedHand = null;//Be careful with the order
        }
        else
        {
            Debug.Assert(false);
        }
        //Debug.Log($"After Attached hands: {attachedHand}, {secondaryAttachedHand}");
    }
    Quaternion _jointSetupAttachedRotation, _jointSetupBodyRotation;
    void _OnPickup(float pickUpDist)
    {
        Debug.Assert(attachedHand && attachedHand.attached==this && !secondaryAttachedHand);
        Application.onBeforeRender += OnBeforeRender;
        if (attachRef == null)
        {
            //attachPositionLS = Quaternion.Inverse(body.rotation) * (attachedHand.position+attachedHand.transform.forward*pickUpDist - body.position);
            attachPositionLS = body.transform.InverseTransformPoint(attachedHand.position + attachedHand.transform.forward * pickUpDist);
            attachRotationLS = Quaternion.Inverse(body.rotation) * attachedHand.rotation;
        }
        else
        {
            //attachPositionLS = Quaternion.Inverse(body.rotation) * (attachRef.position - body.position);
            attachPositionLS = body.transform.InverseTransformPoint(attachRef.position);
            attachRotationLS = Quaternion.Inverse(body.rotation) * attachRef.rotation;
        }
        attachedRigidbody = attachedHand.GetComponentInParent<Rigidbody>();
        if (attachedRigidbody)
        {
            Debug.Assert(attachedRigidbody.mass > 2 * massAttached);
            attachedRigidbody.mass -= massAttached;
        }
        body.mass += massAttached;
        body.isKinematic = false;

        //Average inertia tensor for better rotational stability
        body.ResetInertiaTensor();
        float maxI = Mathf.Max(Mathf.Max(body.inertiaTensor.x, body.inertiaTensor.y), body.inertiaTensor.z);
        body.inertiaTensor = new Vector3(Mathf.Max(maxI * .1f, body.inertiaTensor.x), Mathf.Max(maxI * .1f, body.inertiaTensor.y), Mathf.Max(maxI * .1f, body.inertiaTensor.z));

        if (useJoint)
        {
            Debug.Assert(attachedRigidbody);
            joint = gameObject.AddComponent<ConfigurableJoint>();
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedBody = attachedRigidbody;

            joint.xMotion = ConfigurableJointMotion.Free;
            joint.yMotion = ConfigurableJointMotion.Free;
            joint.zMotion = ConfigurableJointMotion.Free;
            joint.angularXMotion = ConfigurableJointMotion.Free;
            joint.angularYMotion = ConfigurableJointMotion.Free;
            joint.angularZMotion = ConfigurableJointMotion.Free;
            joint.xDrive = joint.yDrive = joint.zDrive =
                new JointDrive { positionSpring = jointSpring, positionDamper = jointDamping, maximumForce = joingMaxForce };
            joint.rotationDriveMode = RotationDriveMode.Slerp;
            joint.slerpDrive =
                new JointDrive { positionSpring = jointSpringAngular, positionDamper = jointDampingAngular, maximumForce = jointMaxForceAngular };

            _jointSetupAttachedRotation = attachedRigidbody.rotation;
            _jointSetupBodyRotation = body.rotation;
        }

        CalcPos(out Vector3 targetPosition, out Quaternion targetRotation);

        if (transform.InverseTransformVector(targetPosition - body.position).magnitude > breakDistance)
        {
            body.position = targetPosition;
            body.rotation = targetRotation;
            lastPosition = body.position;
        }

        OnPickup();
    }
    void _OnDrop()
    {
        Debug.Assert(attachedHand && attachedHand.attached == this && !secondaryAttachedHand);

        Application.onBeforeRender -= OnBeforeRender;

        if (joint) { Destroy(joint); joint = null; }

        body.velocity *= throwVelocityBoost;
        body.angularVelocity *= throwAngularVelocityBoost;
        if(attachedRigidbody)
            attachedRigidbody.mass += massAttached;
        body.mass -= massAttached;
        body.ResetInertiaTensor();
        attachedRigidbody = null;


        OnDrop();
    }
    void _OnAttachSecondary()
    {
        Debug.Assert(attachedHand && attachedHand.attached == this && secondaryAttachedHand && secondaryAttachedHand.attached==this);
    }
    void _OnDetachSecondary()
    {
        Debug.Assert(attachedHand && attachedHand.attached == this && secondaryAttachedHand && secondaryAttachedHand.attached == this);
    }
    #endregion
    

    private void CalcPos(out Vector3 targetPosition, out Quaternion targetRotation)
    {
        Debug.Assert(attachedHand != null);
        attachedHand.UpdateNodeState();
        if (attachRef)
        {
            attachPositionLS = body.transform.InverseTransformPoint(attachRef.position);
            attachRotationLS = Quaternion.Inverse(body.rotation) * attachRef.rotation;
        }
        targetRotation = attachedHand.rotation * Quaternion.Inverse(attachRotationLS);
        targetPosition = attachedHand.position - targetRotation * Quaternion.Inverse(body.rotation) * body.transform.TransformVector(attachPositionLS);
        if (secondaryAttachedHand)
        {
            secondaryAttachedHand.UpdateNodeState();
            var R2 = secondaryAttachedHand.position;
            var r3 = Quaternion.Inverse(body.rotation) * (secondaryHandRef.position-body.position);
            var R3 = targetRotation * r3 + targetPosition;
            var R1 = targetPosition+targetRotation*attachPositionLS;
            var q = Quaternion.FromToRotation(R3 - R1, R2 - R1);
            targetRotation = q * targetRotation;
            targetPosition = attachedHand.position - targetRotation * (attachPositionLS);
        }

    }
    Vector3 lastPosition,lastVelocity;
    protected virtual void FixedUpdate()
    {
        if (attachedHand)
        {

            CalcPos(out Vector3 targetPosition, out Quaternion targetRotation);
            lastVelocity = Vector3.Lerp(lastVelocity, (lastPosition - targetPosition) / Time.fixedDeltaTime, Time.fixedDeltaTime / smoothTime);
            lastPosition = targetPosition;
            if (float.IsInfinity(lastVelocity.magnitude) || float.IsNaN(lastVelocity.magnitude))
                Debug.Assert(false);
            
            if (transform.InverseTransformVector(targetPosition - body.position).magnitude > breakDistance)
            {
                if (teleportWhenBreak)
                {
                    body.position = targetPosition;
                    body.rotation = targetRotation;
                    lastPosition = body.position;
                }
                else
                {
                    DropIfAttached();
                    return;
                }
            }
            if (useJoint)
            {
                Debug.Assert(joint);
                Debug.Assert(attachedRigidbody);
                joint.connectedAnchor = attachedRigidbody.transform.InverseTransformPoint(attachedHand.position);
                //targetposition + targetRotation * Quaternion.Inverse(transform.rotation) * transform.TransformVector(joint.anchor) == attachedHand.position;
                joint.anchor = transform.InverseTransformVector(transform.rotation * Quaternion.Inverse(targetRotation) * (attachedHand.position - targetPosition));

                joint.targetPosition = Vector3.zero;
                //joint.targetRotation = Quaternion.Inverse(Quaternion.Inverse(_jointSetupBodyRotation)*_jointSetupAttachedRotation *Quaternion.Inverse(attachedRigidbody.rotation) * targetRotation);
                joint.targetRotation = Quaternion.Inverse(targetRotation) * attachedRigidbody.rotation * Quaternion.Inverse(_jointSetupAttachedRotation) * _jointSetupBodyRotation;
                joint.targetVelocity = Quaternion.Inverse(targetRotation) * (lastVelocity + attachedRigidbody.velocity);
            }
            else
            {
                Debug.Assert(!joint);
                (targetRotation * Quaternion.Inverse(body.rotation)).ToAngleAxis(out float angleInDegree, out Vector3 axis);
                Vector3 estimatedAngularVelocity = angleInDegree * Mathf.Deg2Rad / Time.fixedDeltaTime * axis;
                estimatedAngularVelocity = Vector3.ClampMagnitude(estimatedAngularVelocity, 10f);
                Vector3 estimatedVelocity = (targetPosition - body.position) / Time.fixedDeltaTime;
                estimatedVelocity = Vector3.ClampMagnitude(estimatedVelocity, 10f);
                
                Vector3 newVelocity = Vector3.Lerp(body.velocity, estimatedVelocity, Time.fixedDeltaTime / smoothTime);
                Vector3 newAngularVelocity = Vector3.Lerp(body.angularVelocity, estimatedAngularVelocity, Time.fixedDeltaTime / smoothTime);
                Vector3 applyForce = (newVelocity - body.velocity) / Time.fixedDeltaTime * body.mass;
                if (body.useGravity) applyForce -= Physics.gravity * body.mass;
                Vector3 applyTorque = (newAngularVelocity - body.angularVelocity) / Time.fixedDeltaTime;
                applyTorque = Quaternion.Inverse(body.rotation * body.inertiaTensorRotation) * applyTorque;
                applyTorque = new Vector3(applyTorque.x * body.inertiaTensor.x, applyTorque.y * body.inertiaTensor.y, applyTorque.z * body.inertiaTensor.z);
                applyTorque = body.rotation * body.inertiaTensorRotation * applyTorque;
                /*if (attachedRigidbody)
                {
                    float m = body.mass; float M = attachedRigidbody.mass;
                    float massRatio = (1 / m) / (1 / m + 1 / M);//Not physically correct, wrong for torque redistribution 
                    applyForce *= massRatio;
                    applyTorque *= massRatio;
                }*/
                    
                /*if (attachedRigidbody)
                {
                    attachedRigidbody.AddForceAtPosition(-applyForce, body.position);
                    attachedRigidbody.AddTorque(-applyTorque);
                }*/
                body.AddForce(applyForce);
                body.AddTorque(applyTorque);
                //body.velocity = Vector3.Lerp(body.velocity,estimatedVelocity,Time.fixedDeltaTime/smoothTime);
                //body.angularVelocity = Vector3.Lerp(body.angularVelocity,estimatedAngularVelocity, Time.fixedDeltaTime / smoothTime);
                //if (body.useGravity) body.AddForce(-Physics.gravity * body.mass);

                
            }
            
        }
    }
    void OnBeforeRender()
    {
        
        if (attachedHand)
        {
            CalcPos(out Vector3 targetPosition, out Quaternion targetRotation);
            if (Vector3.Distance(transform.position, targetPosition) < snapBeforeRenderDistance)
            {
                transform.position = body.position = targetPosition;
                transform.rotation = body.rotation = targetRotation;
            }
        }
        
    }

    #region DEBUG
    #endregion
}
