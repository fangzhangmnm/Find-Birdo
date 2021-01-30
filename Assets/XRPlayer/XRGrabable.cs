using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody))]
public class XRGrabable : XRInteractable
{
    public enum AttachMode { FreeGrab, FreeGrabTwoHanded, Handle, PrimarySecondaryHandle, Pole };
    public bool canSwapHand = true;
    bool hand2Enabled => attachMode == AttachMode.FreeGrabTwoHanded
                        || attachMode == AttachMode.PrimarySecondaryHandle
                        || attachMode == AttachMode.Pole;
    bool hand2AutoDrop => false;// attachMode == AttachMode.PrimarySecondaryHandle;
    public AttachMode attachMode = AttachMode.FreeGrab;

    public Transform attachRef, attach2Ref;
    public XRJointSettings jointSettings;
    public float lostTrackDist = .3f;
    public float throwSmoothTime = .1f;
    public bool breakWhenLostTrack = true;

    public UnityEvent onPickUp, onDrop;
    [System.Serializable] public class UpdateTransform : UnityEvent<Transform> { }
    public UpdateTransform updateAttach = new UpdateTransform();

    public Behaviour outline;

    Rigidbody body;
    [HideInInspector]public XRHand hand, hand2;
    ConfigurableJoint joint, joint2;
    Quaternion jointBias, jointBias2;
    Vector3 attachedPositionLS, attached2PositionLS, desiredPosition;
    Quaternion attachedRotationLS, attached2RotationLS, desiredRotation;
    Vector3 smoothedVelocity, smoothedAngularVelocity;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        if (!attachRef) attachRef = transform;
        if (outline) outline.enabled = false;
    }

    private void OnDisable()
    {
        hand?.DetachIfAny();
        hand2?.DetachIfAny();
        onDrop.Invoke();
        if (outline) outline.enabled = false;
    }

    public void DetachIfAttached()
    {
        if (hand) hand.DetachIfAny();
    }
    public override bool CanInteract(XRHand hand, out int priority)
    {
        return CanAttach(hand, out priority);
    }
    public override bool CanAttach(XRHand hand, out int priority)
    {
        if (!isActiveAndEnabled) { priority = 0; return false; }
        if (hand2Enabled && this.hand && isActiveAndEnabled)
        {
            priority = -1;
            if (hand2) return false;
            else return true;
        }
        else
        {
            if (this.hand)
            {
                priority = -2;
                return canSwapHand;
            }
            else
            {
                priority = 0;
                return true;
            }
        }
    }

    public override void OnAttach(XRHand emptyHand, Vector3 attachPositionWS, Quaternion attachRotationWS)
    {
        if (!hand)
        {
            hand = emptyHand;
            (joint, jointBias) = BuildJoint(body, emptyHand.bodyToAttach, jointSettings);
            attachedPositionLS = transform.InverseTransformPoint(attachPositionWS);
            attachedRotationLS = Quaternion.Inverse(transform.rotation) * attachRotationWS;
            ResetMovement();
            updateAttach.Invoke(emptyHand.playerRoot);
            onPickUp.Invoke();
        }
        else
        {
            if (!hand2Enabled)
            {
                hand.TransforAttached(emptyHand);

                hand = emptyHand;
                (joint, jointBias) = BuildJoint(body, emptyHand.bodyToAttach, jointSettings);
                attachedPositionLS = transform.InverseTransformPoint(attachPositionWS);
                attachedRotationLS = Quaternion.Inverse(transform.rotation) * attachRotationWS;

                //TODO Test Logic
            }
            else
            {
                if (hand2) hand2.DetachIfAny();

                hand2 = emptyHand;
                (joint2, jointBias2) = BuildJoint(body, emptyHand.bodyToAttach, jointSettings);
                attached2PositionLS = transform.InverseTransformPoint(attachPositionWS);
                attached2RotationLS = Quaternion.Inverse(transform.rotation) * attachRotationWS;
            }
        }
    }

    public override void OnDetach(XRHand handAttachedMe)
    {
        if (handAttachedMe == hand2)
        {
            Destroy(joint2); joint2 = null; hand2 = null;
            if (hand.isHovering(this))
            {
                attachedPositionLS = transform.InverseTransformPoint(hand.position);
                attachedRotationLS = Quaternion.Inverse(transform.rotation) * hand.rotation;
            }
        }
        else
        {
            if (hand2)
            {
                if (hand2AutoDrop)
                {
                    hand2.DetachIfAny();
                    Destroy(joint); joint = null; hand = null;
                    _OnDrop();
                }
                else
                {
                    Destroy(joint); joint = null; hand = null;

                    hand = hand2; hand2 = null;
                    joint = joint2; joint2 = null;
                    jointBias = jointBias2;
                    attachedPositionLS = attached2PositionLS;
                    attachedRotationLS = attached2RotationLS;
                }
            }
            else
            {
                Destroy(joint); joint = null; hand = null;
                _OnDrop();
            }
        }

    }

    void _OnDrop()
    {
        body.velocity = smoothedVelocity;
        body.angularVelocity = smoothedAngularVelocity;
        onDrop.Invoke();
        updateAttach.Invoke(null);
    }
    /*
    public override (Vector3, Quaternion) GetAttachPosition(XRHand hand)
    {
        if (hand == this.hand) return (transform.TransformPoint(attachedPositionLS), transform.rotation * attachedRotationLS);
        else return (transform.TransformPoint(attached2PositionLS), transform.rotation * attached2RotationLS);
    }
    */
    public override void SetHovering(bool isHovering)
    {
        if (outline) outline.enabled = isHovering;
    }

    public override void OnTeleport()
    {
        ResetMovement();
    }

    private void FixedUpdate()
    {
        if (hand)
        {
            UpdateDesiredMovementAndAttachPosition();

            bool break1 = hand && Vector3.Distance(hand.position, transform.TransformPoint(attachedPositionLS)) > lostTrackDist * hand.playerRoot.lossyScale.x;
            bool break2 = hand2 && Vector3.Distance(hand2.position, transform.TransformPoint(attached2PositionLS)) > lostTrackDist * hand.playerRoot.lossyScale.x;

            if (break1 || break2)
            {
                if (breakWhenLostTrack)
                {
                    if (break1)
                    {
                        hand?.DetachIfAny();
                    }
                    else
                        hand2.DetachIfAny();
                }
                else
                    ResetMovement();
            }
            else
            {
                //transform.position = desiredPosition;
                //transform.rotation = desiredRotation;
                if (joint)
                    UpdateJoint(joint, jointBias, body, hand.bodyToAttach, desiredPosition, desiredRotation);
                if (joint2)
                    UpdateJoint(joint2, jointBias2, body, hand2.bodyToAttach, desiredPosition, desiredRotation);
            }
        }

        {
            smoothedVelocity = Vector3.Lerp(smoothedVelocity, body.velocity, Time.fixedDeltaTime / throwSmoothTime);
            smoothedAngularVelocity = Vector3.Lerp(smoothedAngularVelocity, body.angularVelocity, Time.fixedDeltaTime / throwSmoothTime);
        }

        if (hand && hand.input_grip < .5f)
            hand.DetachIfAny();
        if (hand2 && hand2.input_grip < .5f)
            hand2.DetachIfAny();
    }

    private void OnCollisionEnter(Collision collision)//Will be called before start
    {
        if (hand)
        {
            float speed = 1f;
            if (collision.contactCount > 0)
            {
                Vector3 point = collision.GetContact(0).point;
                Vector3 v1 = body.GetPointVelocity(point);
                Vector3 v2 = collision.rigidbody ? collision.rigidbody.GetPointVelocity(point) : Vector3.zero;
                speed = (v2 - v1).magnitude / hand.playerRoot.lossyScale.x;
            }
            float strength = Mathf.Lerp(0, hand.hapticSettings.collisionEnterMaxStrength, speed / hand.hapticSettings.collisionEnterMaxStrengthSpeed);
            hand.SendHapticImpulse(strength, hand.hapticSettings.collisionEnterDuration);
        }
    }

    void ResetMovement()
    {
        if (hand)
        {
            UpdateDesiredMovementAndAttachPosition();
            transform.position = desiredPosition;
            transform.rotation = desiredRotation;
            if (joint)
                UpdateJoint(joint, jointBias, body, hand.bodyToAttach, desiredPosition, desiredRotation);
            if (joint2)
                UpdateJoint(joint2, jointBias2, body, hand2.bodyToAttach, desiredPosition, desiredRotation);
        }

    }
    void UpdateDesiredMovementAndAttachPosition()
    {
        if (hand2 && attachMode == AttachMode.PrimarySecondaryHandle)
        {
            attached2PositionLS = transform.InverseTransformPoint(attach2Ref.position);
            attached2RotationLS = Quaternion.Inverse(transform.rotation) * attach2Ref.rotation;
        }
        if (hand && (attachMode == AttachMode.PrimarySecondaryHandle || attachMode == AttachMode.Handle))
        {
            attachedPositionLS = transform.InverseTransformPoint(attachRef.position);
            attachedRotationLS = Quaternion.Inverse(transform.rotation) * attachRef.rotation;
        }

        if (hand && !hand2)
        {
            desiredRotation = hand.rotation * Quaternion.Inverse(attachedRotationLS);
            desiredPosition = hand.position - desiredRotation * Quaternion.Inverse(transform.rotation) * transform.TransformVector(attachedPositionLS);
        }
        else if (hand && hand2)
        {
            if (attachMode == AttachMode.PrimarySecondaryHandle)
                desiredRotation = hand.rotation * Quaternion.Inverse(attachedRotationLS);
            else
            {
                var rot1 = hand.rotation * Quaternion.Inverse(attachedRotationLS);
                var rot2 = hand2.rotation * Quaternion.Inverse(attached2RotationLS);
                desiredRotation = Quaternion.Slerp(rot1, rot2, .5f);
            }
            var targetTargetAxisWS = transform.TransformVector(attached2PositionLS - attachedPositionLS);
            var handHandAxisWS = hand2.position - hand.position;
            var desiredDeltaRotation = desiredRotation * Quaternion.Inverse(transform.rotation);
            var alignRotation = Quaternion.FromToRotation(desiredDeltaRotation * targetTargetAxisWS, handHandAxisWS);
            desiredRotation = alignRotation * desiredRotation;

            if (attachMode == AttachMode.PrimarySecondaryHandle)
            {
                desiredPosition = hand.position - desiredRotation * Quaternion.Inverse(transform.rotation) * transform.TransformVector(attachedPositionLS);
            }
            else if (attachMode == AttachMode.FreeGrabTwoHanded)
            {
                var desiredPosition1 = hand.position - desiredRotation * Quaternion.Inverse(transform.rotation) * transform.TransformVector(attachedPositionLS);
                var desiredPosition2 = hand2.position - desiredRotation * Quaternion.Inverse(transform.rotation) * transform.TransformVector(attached2PositionLS);

                desiredPosition = (desiredPosition1 + desiredPosition2) / 2;

                //attachedPositionLS = transform.InverseTransformPoint(hand.position);
                //attachedRotationLS = Quaternion.Inverse(transform.rotation) * hand.rotation;
                //attached2PositionLS = transform.InverseTransformPoint(hand2.position);
                //attached2RotationLS = Quaternion.Inverse(transform.rotation) * hand2.rotation;
                //will introduce sliding
            }
            else
            {
                //need also attachposition update lol
                throw new System.NotImplementedException();
            }
        }
    }

    #region Joint
    static (ConfigurableJoint, Quaternion) BuildJoint(Rigidbody body, Rigidbody attachedRigidbody, XRJointSettings jointSettings)
    {
        Debug.Assert(Physics.defaultMaxAngularSpeed >= 50f);

        var joint = body.gameObject.AddComponent<ConfigurableJoint>();
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

        var jointSetupInverseAttachedTimesBodyRotation = Quaternion.Inverse(attachedRigidbody.rotation) * body.rotation;
        return (joint, jointSetupInverseAttachedTimesBodyRotation);
    }

    static void UpdateJoint(ConfigurableJoint joint, Quaternion jointSetupInverseAttachedTimesBodyRotation,
        Rigidbody body, Rigidbody attachedRigidbody, Vector3 targetPosition, Quaternion targetRotation)
    {

        joint.connectedAnchor = attachedRigidbody.transform.InverseTransformPoint(attachedRigidbody.transform.position);
        //Note targetposition + targetRotation * Quaternion.Inverse(transform.rotation) * transform.TransformVector(joint.anchor) == attachedHand.position;
        joint.anchor = joint.transform.InverseTransformVector(joint.transform.rotation * Quaternion.Inverse(targetRotation) * (attachedRigidbody.transform.position - targetPosition));

        joint.targetPosition = Vector3.zero;
        //Note joint.targetRotation == Quaternion.Inverse(Quaternion.Inverse(_jointSetupBodyRotation)*_jointSetupAttachedRotation *Quaternion.Inverse(attachedRigidbody.transform.rotation) * targetRotation);
        joint.targetRotation = Quaternion.Inverse(targetRotation) * attachedRigidbody.transform.rotation * jointSetupInverseAttachedTimesBodyRotation;
        //joint.targetVelocity = Quaternion.Inverse(targetRotation) * (lastVelocity)/2;//Magic number but works

        joint.targetVelocity = Vector3.zero;
        //Do not need these correlations now
        //joint.targetVelocity += Quaternion.Inverse(targetRotation) * (attachedRigidbody.velocity) * .5f;//Magic number but works
        //joint.targetVelocity += Quaternion.Inverse(targetRotation) * (targetPosition - body.position) / Time.fixedDeltaTime * -1f;
    }

    #endregion
}
