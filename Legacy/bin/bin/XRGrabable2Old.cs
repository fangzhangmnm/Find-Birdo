using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class XRGrabable2tmp:MonoBehaviour
{
    public enum AttachMode { FreeGrab, FreeGrabTwoHanded, Handle, PrimarySecondaryHandle, Pole };
    bool hand2Enabled => attachMode == AttachMode.FreeGrabTwoHanded
                        || attachMode == AttachMode.PrimarySecondaryHandle
                        || attachMode == AttachMode.Pole;
    bool hand2AutoDrop => attachMode == AttachMode.PrimarySecondaryHandle;
    public AttachMode attachMode = AttachMode.Handle;

    public Transform attachRef, attach2Ref;
    public JointSettings jointSettings;

    Rigidbody body;
    XRHand2 hand, hand2;
    ConfigurableJoint joint, joint2;
    Quaternion jointBias, jointBias2;
    Vector3 attachedPositionLS,attached2PositionLS,desiredPosition;
    Quaternion attachedRotationLS, attached2RotationLS,desiredRotation;


    public bool CanAttach(XRHand2 hand, out int priority)
    {
        if (hand2Enabled && this.hand)
        {
            priority = -1;
            if (hand2) return false;
            else return true;
        }
        else
        {
            priority = 0;
            return true;
        }
    }

    public void OnAttach(XRHand2 emptyHand, Vector3 attachPositionWS)
    {
        if (!hand)
        {
            hand = emptyHand;
            (joint, jointBias) = BuildJoint(body, emptyHand.bodyToAttach, jointSettings);
            attachedPositionLS = transform.InverseTransformPoint(attachPositionWS);
            attachedRotationLS = Quaternion.Inverse(transform.rotation) * emptyHand.bodyToAttach.rotation;
        }
        else
        {
            if (!hand2Enabled)
            {
                hand.DetachIfAny();

                hand = emptyHand;
                (joint, jointBias) = BuildJoint(body, emptyHand.bodyToAttach, jointSettings);
                attachedPositionLS = transform.InverseTransformPoint(attachPositionWS);
                attachedRotationLS = Quaternion.Inverse(transform.rotation) * emptyHand.bodyToAttach.rotation;
            }
            else
            {
                if (hand2) hand2.DetachIfAny();

                hand2 = emptyHand;
                (joint2, jointBias2) = BuildJoint(body, emptyHand.bodyToAttach, jointSettings);
                attached2PositionLS = transform.InverseTransformPoint(attachPositionWS);
                attached2RotationLS = Quaternion.Inverse(transform.rotation) * emptyHand.bodyToAttach.rotation;
            }
        }
    }

    public void OnDetach(XRHand2 handAttachedMe)
    {
        if (handAttachedMe == hand2)
        {
            Destroy(joint2);joint2 = null;hand2 = null;
        }
        else
        {
            if (hand2)
            {
                if (hand2AutoDrop)
                {
                    hand2.DetachIfAny();
                    Destroy(joint); joint = null; hand = null;
                }
                else
                {
                    Destroy(joint);

                    hand = hand2;hand2 = null;
                    joint = joint2;joint2=null;
                    jointBias = jointBias2;
                    attachedPositionLS = attached2PositionLS;
                    attachedRotationLS = attached2RotationLS;
                }
            }
            else
            {
                Destroy(joint); joint = null; hand = null;
            }
        }

    }

    public (Vector3, Quaternion) GetAttachPosition(XRHand2 hand)
    {
        if (hand == this.hand) return (transform.TransformPoint(attachedPositionLS), transform.rotation * attachedRotationLS);
        else return (transform.TransformPoint(attached2PositionLS), transform.rotation * attached2RotationLS);
    }

    void UpdateDesiredMovement()
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

            }
            else
            {
                throw new System.NotImplementedException();
            }
        }
    }


    #region Joint
    [System.Serializable]
    public class JointSettings
    {
        public float spring = 50000;
        public float damper = 500;
        public float maxForce = 1000;
        public float angularSpring = 50000;
        public float angularDamper = 500;
        public float angularMaxForce = 100;
    }
    static (ConfigurableJoint, Quaternion) BuildJoint(Rigidbody body, Rigidbody attachedRigidbody, JointSettings jointSettings)
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
        Rigidbody attachedRigidbody, Vector3 attachedVelocity, Vector3 targetPosition, Quaternion targetRotation)
    {

        joint.connectedAnchor = attachedRigidbody.transform.InverseTransformPoint(attachedRigidbody.position);
        //Note targetposition + targetRotation * Quaternion.Inverse(transform.rotation) * transform.TransformVector(joint.anchor) == attachedHand.position;
        joint.anchor = joint.transform.InverseTransformVector(joint.transform.rotation * Quaternion.Inverse(targetRotation) * (attachedRigidbody.position - targetPosition));

        joint.targetPosition = Vector3.zero;
        //Note joint.targetRotation == Quaternion.Inverse(Quaternion.Inverse(_jointSetupBodyRotation)*_jointSetupAttachedRotation *Quaternion.Inverse(attachedRigidbody.rotation) * targetRotation);
        joint.targetRotation = Quaternion.Inverse(targetRotation) * attachedRigidbody.rotation * jointSetupInverseAttachedTimesBodyRotation;
        joint.targetVelocity = Quaternion.Inverse(targetRotation) * attachedVelocity / 2;//Magic number but works
    }
    #endregion
}
