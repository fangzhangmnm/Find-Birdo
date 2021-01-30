using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class XRGrabable : XRInteractable
{
    public Behaviour hoveringHint;

    public enum MoveMode { Transform, Joint, None };
    public enum AttachMode { FreeGrab, FreeGrabTwoHanded, Handle, PrimarySecondaryHandle, Pole };
    bool secondaryHandEnabled => attachMode == AttachMode.FreeGrabTwoHanded
                                || attachMode == AttachMode.PrimarySecondaryHandle
                                || attachMode == AttachMode.Pole;
    bool secondaryHandAutoDrop => attachMode == AttachMode.PrimarySecondaryHandle;
    public MoveMode moveMode = MoveMode.Transform;
    public AttachMode attachMode = AttachMode.Handle;

    [System.Serializable]
    public class JointSettings
    {
        public float spring = 360000;
        public float damper = 120000;
        public float maxForce = 1200;
        public float angularSpring = 40000;
        public float angularDamper = 1000;
        public float angularMaxForce = 150;
    }
    public JointSettings jointSettings;


    public Transform attachRef;
    public Transform attachSecondaryRef;

    [HideInInspector]public XRHand attachedHand, attachedSecondaryHand;

    Rigidbody body;
    ConfigurableJoint joint;

    private void Start()
    {
        if (hoveringHint) hoveringHint.enabled = false;
        body = GetComponent<Rigidbody>();
        if (!attachRef) attachRef = transform;
    }
    private void FixedUpdate()
    {
        UpdateDesiredMovement(Time.fixedDeltaTime);
        UpdateMovement(Time.fixedDeltaTime);
    }
    public void DropIfAttached() {attachedSecondaryHand?.DetachIfAny(); attachedHand?.DetachIfAny(); }
    public virtual void OnPickup() { }
    public virtual void OnDrop() { }
    public override void OnPlayerTeleport()
    {
        base.OnPlayerTeleport();
        UpdateDesiredMovement(0);
        UpdateMovement(0);
    }
    public override void OnValidate()
    {
        body = GetComponent<Rigidbody>();
        if (attachMode == AttachMode.PrimarySecondaryHandle) Debug.Assert(attachSecondaryRef);
        if (GetComponentsInChildren<Collider>().Length > GetComponents<Collider>().Length && !body) Debug.LogError("Add Rigidbody to Receive Hand Collision");
    }
    #region Moving
    Vector3 desiredPosition,attachedPositionLS,attachedSecondaryPositionLS;
    Quaternion desiredRotation, attachedRotationLS, attachedSecondaryRotationLS;
    void RegisterHand(XRHand hand,Vector3 pickUpPointLS)
    {
        attachedHand = hand;
        attachedPositionLS = pickUpPointLS;
        attachedRotationLS = Quaternion.Inverse(transform.rotation) * hand.rotation;
    }
    void RegisterSecondaryHand(XRHand hand, Vector3 pickUpPointLS)
    {
        attachedSecondaryHand = hand;
        attachedSecondaryPositionLS = pickUpPointLS;
        attachedSecondaryRotationLS = Quaternion.Inverse(transform.rotation) * hand.rotation;
    }
    void UpdateDesiredMovement(float dt)
    {
        if (attachedSecondaryHand && attachMode == AttachMode.PrimarySecondaryHandle)
        {
            attachedSecondaryPositionLS = transform.InverseTransformPoint(attachSecondaryRef.position);
            attachedSecondaryRotationLS = Quaternion.Inverse(transform.rotation) * attachSecondaryRef.rotation;
        }
        if (attachedHand && (attachMode == AttachMode.PrimarySecondaryHandle || attachMode == AttachMode.Handle))
        {
            attachedPositionLS = transform.InverseTransformPoint(attachRef.position);
            attachedRotationLS = Quaternion.Inverse(transform.rotation) * attachRef.rotation;
        }

        if (attachedHand && !attachedSecondaryHand)
        {
            desiredRotation = attachedHand.rotation * Quaternion.Inverse(attachedRotationLS);
            desiredPosition = attachedHand.position - desiredRotation * Quaternion.Inverse(transform.rotation) * transform.TransformVector(attachedPositionLS);
        }
        else if (attachedHand && attachedSecondaryHand)
        {
            if (attachMode == AttachMode.PrimarySecondaryHandle)
                desiredRotation = attachedHand.rotation * Quaternion.Inverse(attachedRotationLS);
            else
            {
                var rot1= attachedHand.rotation * Quaternion.Inverse(attachedRotationLS);
                var rot2 = attachedSecondaryHand.rotation * Quaternion.Inverse(attachedSecondaryRotationLS);
                desiredRotation=Quaternion.Slerp(rot1, rot2, .5f);
            }
            var targetTargetAxisWS = transform.TransformVector(attachedSecondaryPositionLS - attachedPositionLS);
            var handHandAxisWS = attachedSecondaryHand.position - attachedHand.position;
            var desiredDeltaRotation = desiredRotation * Quaternion.Inverse(transform.rotation);
            var alignRotation = Quaternion.FromToRotation(desiredDeltaRotation * targetTargetAxisWS, handHandAxisWS);
            desiredRotation = alignRotation * desiredRotation;

            if (attachMode==AttachMode.PrimarySecondaryHandle)
            {
                desiredPosition= attachedHand.position - desiredRotation * Quaternion.Inverse(transform.rotation) * transform.TransformVector(attachedPositionLS);
            }
            else if (attachMode == AttachMode.FreeGrabTwoHanded)
            {
                //TODO BUGS
                desiredPosition = (attachedHand.position+ attachedSecondaryHand.position)/2
                    - desiredRotation * Quaternion.Inverse(transform.rotation) * transform.TransformVector((attachedPositionLS+attachedSecondaryPositionLS)/2);

            }
            else throw new System.NotImplementedException();
        }
    }
    void UpdateMovement(float dt)
    {

        if (!attachedHand) return;
        if (moveMode == MoveMode.Transform)
        {
            transform.position = desiredPosition;
            transform.rotation = desiredRotation;
            if (body && dt>0 && !oldTransformDirtyFlag)
            {
                //Smoothing is essential for correct throw velocity;
                estimatedTransformVelocity = Vector3.Lerp(estimatedTransformVelocity, (transform.position - oldTransformPosition) / dt, dt / .05f);
                estimatedTransformAngularVelocity = Vector3.Lerp(estimatedTransformAngularVelocity, GetRotationVector(oldTransformRotation,transform.rotation) / dt, dt / .05f);
            }
            oldTransformDirtyFlag = false;
            oldTransformPosition = transform.position;
            oldTransformRotation = transform.rotation;
        }
        else if (moveMode == MoveMode.None)
        {
            //Do Nothing
        }
        else if (moveMode == MoveMode.Joint)
        {
            
        }
        /*
        else if(moveMode==MoveMode.ApplyForce)
        {
            if(attachedHand && !attachedSecondaryHand)
            {
                //desiredPosition = attachedHand.position - transform.rotation * Quaternion.Inverse(transform.rotation) * transform.TransformVector(attachedPositionLS);

                Vector3 desiredVelocity = (desiredPosition - transform.position) / dt;
                Vector3 desiredAngularVelocity = GetRotationVector(transform.rotation, desiredRotation) / dt;
                desiredVelocity = Vector3.Lerp(body.velocity, desiredVelocity, dt / .01f);
                desiredAngularVelocity = Vector3.Lerp(body.angularVelocity, desiredAngularVelocity, dt / .01f);

                Vector3 applyForce = (desiredVelocity - body.velocity) / Time.fixedDeltaTime * body.mass;
                if (body.useGravity) applyForce -= Physics.gravity * body.mass;

                Vector3 forceInducedTorque = Vector3.Cross(attachedHand.position - transform.position, applyForce);


                Vector3 applyTorque = -forceInducedTorque+ ApplyTensor(desiredAngularVelocity - body.angularVelocity, body.inertiaTensor, body.rotation * body.inertiaTensorRotation) / Time.fixedDeltaTime;
                
                applyForce = Vector3.ClampMagnitude(applyForce, attachedHand.strength);
                applyTorque = Vector3.ClampMagnitude(applyTorque, attachedHand.torqueStrength);

                body.AddForceAtPosition(applyForce,attachedHand.position);
                body.AddTorque(applyTorque);
            }
            else if (attachedHand && attachedSecondaryHand)
            {
                
                throw new System.NotImplementedException();
            }
            else
                throw new System.NotImplementedException();
        }*/
        else
            throw new System.NotImplementedException();
    }
    bool oldTransformDirtyFlag = true;
    Vector3 oldTransformPosition, estimatedTransformVelocity, estimatedTransformAngularVelocity; Quaternion oldTransformRotation;
    #endregion
    #region Joint
    Quaternion jointSetupAttachedRotation, jointSetupBodyRotation;
    void CreateJoint(Rigidbody attachedRigidbody)
    {
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
            new JointDrive { positionSpring = jointSettings.spring, positionDamper = jointSettings.damper, maximumForce = jointSettings.maxForce };
        joint.rotationDriveMode = RotationDriveMode.Slerp;
        joint.slerpDrive =
            new JointDrive { positionSpring = jointSettings.angularSpring, positionDamper = jointSettings.angularDamper, maximumForce = jointSettings.angularMaxForce };

        jointSetupAttachedRotation = attachedRigidbody.rotation;
        jointSetupBodyRotation = body.rotation;
    }
    #endregion
    #region Pickup Events
    bool oldIsKinematic =false;
    void _OnPickup()
    {
        if (moveMode==MoveMode.Transform && body) 
        {
            estimatedTransformVelocity = body.velocity;
            estimatedTransformAngularVelocity = body.angularVelocity;
            oldTransformDirtyFlag = true;
            oldIsKinematic = body.isKinematic; 
            body.isKinematic = true; 
        }
        if (moveMode == MoveMode.Joint)
        {

        }


        OnPickup();
    }
    void _OnDrop()
    {
        if (moveMode == MoveMode.Transform && body)
        {
            body.isKinematic = oldIsKinematic;
            body.velocity = estimatedTransformVelocity;
            body.angularVelocity = estimatedTransformAngularVelocity;
        }
        if (joint)
        {
            Destroy(joint);joint = null;
        }
        OnDrop();
    }
    #endregion
    #region Hand Position
    public override bool TryGetHandPosition(XRHand hand, out Vector3 position, out Quaternion rotation)
    {
        if (hand == attachedHand)
        {
            position = transform.TransformPoint(attachedPositionLS);
            rotation = transform.rotation * attachedRotationLS;
            return true;
        }
        else if (hand == attachedSecondaryHand)
        {
            position = transform.TransformPoint(attachedSecondaryPositionLS);
            rotation = transform.rotation * attachedSecondaryRotationLS;
            return true;
        }
        else
        {
            position = Vector3.zero; rotation = Quaternion.identity; return false;
        }
    }
    #endregion
    #region Attach Logic
    public override bool CanAttach(XRHand emptyHand, float pickUpDistance, out int priority)
    {
        if (secondaryHandEnabled)
        {
            priority = -1;
            if (attachedSecondaryHand == null) return true;
            else return false;
        }
        else
        {
            priority = 0;
            return true;
        }
    }

    public override void OnAttach(XRHand handAttachedMe, Vector3 pickUpPointLS)
    {
        if (attachedHand)
        {
            if (secondaryHandEnabled)
            {
                if(attachedSecondaryHand)
                    attachedSecondaryHand.DetachIfAny();
                RegisterSecondaryHand(handAttachedMe, pickUpPointLS);
            }
            else
            {
                attachedHand.DetachIfAny();
                RegisterHand(handAttachedMe, pickUpPointLS);
                _OnPickup();
            }
        }
        else
        {
            RegisterHand(handAttachedMe, pickUpPointLS);
            _OnPickup();
        }
    }

    public override void OnDetach(XRHand handWillDetachMe)
    {
        if (attachedHand == handWillDetachMe)
        {
            if (attachedSecondaryHand)
            {
                if (secondaryHandAutoDrop)
                {
                    attachedSecondaryHand.DetachIfAny();
                    _OnDrop();
                    attachedHand = null;
                }
                else
                {
                    var tmp = attachedSecondaryHand;
                    attachedSecondaryHand.DetachIfAny();
                    _OnDrop();
                    attachedHand = null;
                    tmp.Attach(this);
                }
            }
            else
            {
                _OnDrop();
                attachedHand = null;
            }
        }
        else if (attachedSecondaryHand == handWillDetachMe)
        {
            attachedSecondaryHand = null;
        }
    }
    public override void OnGripUp(XRHand hand)
    {
        base.OnGripUp(hand);
        if(hand==attachedHand || hand==attachedSecondaryHand)
            hand.DetachIfAny();
    }
    #endregion
    #region HoveringEffect
    public override void OnHoverEnter() { if(hoveringHint)hoveringHint.enabled = true; }
    public override void OnHoverExit() { if (hoveringHint) hoveringHint.enabled = false; }
    #endregion
    #region Utils
    static Vector3 ApplyTensor(Vector3 vec, Vector3 tensorEigen, Quaternion tensorRotation)
    {
        vec = Quaternion.Inverse(tensorRotation) * vec;
        vec = new Vector3(vec.x * tensorEigen.x, vec.y * tensorEigen.y, vec.z * tensorEigen.z);
        return tensorRotation * vec;
    }
    static Vector3 GetRotationVector(Quaternion from, Quaternion to)
    {
        (to * Quaternion.Inverse(from)).ToAngleAxis(out float angle, out Vector3 axis);
        if (angle >= 360) return Vector3.zero;
        if (angle > 180) angle -= 360;
        return angle * Mathf.Deg2Rad * axis;
        //Only (-1,0,0,0) will return NaN
    }
    #endregion
}
