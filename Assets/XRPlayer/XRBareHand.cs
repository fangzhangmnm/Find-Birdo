using UnityEngine;
using UnityEngine.XR;

[DefaultExecutionOrder(-5)]
[RequireComponent(typeof(Rigidbody))]
public class XRBareHand : MonoBehaviour
{
    public XRHand hand;
    public Rigidbody attachedRigidbody;
    public XRJointSettings jointSetting;
    public float lostTrackDist = .3f;
    //float smoothVelocityTime = .01f; No Need of this. Hand tracking error too small, and it increase the responce time to player locomotion

    Rigidbody body; Animator animator;
    ConfigurableJoint joint;
    Quaternion jointSetupAttachedRotation, jointSetupBodyRotation;
    Vector3 attachedOldPosition;
    Vector3 estimatedVelocity;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        //if rigidbody is inside player transform, it will be moved when moving player transform.
        //so the rigidbody's velocity will not account for its real velocity. which will result trouble in attaching joints
        transform.parent = null;

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
            new JointDrive { positionSpring = jointSetting.spring, positionDamper = jointSetting.damper, maximumForce = jointSetting.maxForce };
        joint.rotationDriveMode = RotationDriveMode.Slerp;
        joint.slerpDrive =
            new JointDrive { positionSpring = jointSetting.angularSpring, positionDamper = jointSetting.angularDamper, maximumForce = jointSetting.angularMaxForce };

        jointSetupAttachedRotation = attachedRigidbody.rotation;
        jointSetupBodyRotation = body.rotation;
        attachedOldPosition = attachedRigidbody.position;

        Debug.Assert(Physics.defaultMaxAngularSpeed >= 50f);
        Debug.Assert(Physics.defaultMaxDepenetrationVelocity <= 1f);
        //Also Need to set Physics iterstions=>(10,10) and enableAdaptiveForce

        animator = GetComponentInChildren<Animator>();
    }

    private void FixedUpdate()
    {
        //estimatedVelocity = Vector3.Lerp(estimatedVelocity, (attachedOldPosition - attachedRigidbody.transform.position) / Time.fixedDeltaTime, Time.fixedDeltaTime / smoothVelocityTime);
        estimatedVelocity = (attachedOldPosition - attachedRigidbody.transform.position) / Time.fixedDeltaTime;
        attachedOldPosition = attachedRigidbody.transform.position;

        if (Vector3.Distance(body.transform.position, attachedRigidbody.transform.position) > lostTrackDist * hand.playerRoot.lossyScale.x)
        {
            ResetMovement(estimatedVelocity);
        }
        else
        {

            //rigidbody.position is not updated. use transform instead
            //Vector3 targetPosition = attachedRigidbody.transform.position;
            Quaternion targetRotation = attachedRigidbody.transform.rotation;

            //joint.connectedAnchor = attachedRigidbody.transform.InverseTransformPoint(attachedRigidbody.transform.position);
            //Note targetposition + targetRotation * Quaternion.Inverse(transform.rotation) * transform.TransformVector(joint.anchor) == attachedHand.position;
            //joint.anchor = transform.InverseTransformVector(transform.rotation * Quaternion.Inverse(targetRotation) * (attachedRigidbody.transform.position - targetPosition));
            joint.connectedAnchor = Vector3.zero;
            joint.anchor = Vector3.zero;

            joint.targetPosition = Vector3.zero;
            //Note joint.targetRotation == Quaternion.Inverse(Quaternion.Inverse(_jointSetupBodyRotation)*_jointSetupAttachedRotation *Quaternion.Inverse(attachedRigidbody.rotation) * targetRotation);
            //joint.targetRotation = Quaternion.Inverse(targetRotation) * attachedRigidbody.transform.rotation * Quaternion.Inverse(jointSetupAttachedRotation) * jointSetupBodyRotation;
            joint.targetRotation = Quaternion.Inverse(jointSetupAttachedRotation) * jointSetupBodyRotation;
            //joint.targetVelocity = Vector3.zero;
            //joint.targetVelocity = Quaternion.Inverse(targetRotation) * (lastVelocity) * 1f;
            joint.targetVelocity = Quaternion.Inverse(transform.rotation) * (estimatedVelocity) * 1f;//Magic number but works
                                                                                             //joint.targetVelocity += Quaternion.Inverse(targetRotation) * (targetPosition - body.position) / Time.fixedDeltaTime*-.5f;
                                                                                             //joint.targetVelocity = Vector3.zero;
        }
        animator.SetFloat(hand.whichHand==XRNode.LeftHand? "GripL":"GripR", hand.input_grip);
    }
    void ResetMovement(Vector3 lastVelocity)
    {
        //Debug.Log(lastVelocity);
        //body.MovePosition(attachedRigidbody.transform.position);
        //body.MoveRotation(attachedRigidbody.transform.rotation);
        transform.position = attachedRigidbody.transform.position+lastVelocity*Time.fixedDeltaTime;
        transform.rotation = attachedRigidbody.transform.rotation;
        //body.velocity = lastVelocity;
        body.angularVelocity = Vector3.zero;
        joint.targetPosition = Vector3.zero;
        joint.targetRotation = Quaternion.Inverse(jointSetupAttachedRotation) * jointSetupBodyRotation;
        joint.targetVelocity = Quaternion.Inverse(transform.rotation) * (lastVelocity) * 1f;
        attachedOldPosition = attachedRigidbody.transform.position;
    }
    public void OnTeleport()
    {
        ResetMovement(Vector3.zero);
    }

    private void OnCollisionEnter(Collision collision)//Will be called before start
    {
        float speed = 1f;
        if (collision.contactCount > 0)
        {
            Vector3 point = collision.GetContact(0).point;
            Vector3 v1 = body.GetPointVelocity(point);
            Vector3 v2 = collision.rigidbody ? collision.rigidbody.GetPointVelocity(point) : Vector3.zero;
            speed = (v2 - v1).magnitude/hand.playerRoot.lossyScale.x;
        }
        float strength = Mathf.Lerp(0, hand.hapticSettings.collisionEnterMaxStrength, speed / hand.hapticSettings.collisionEnterMaxStrengthSpeed);
        hand.SendHapticImpulse(strength, hand.hapticSettings.collisionEnterDuration);

        //if (collision.rigidbody)
        //{
        //    Debug.Log("Collide with body " + collision.rigidbody.name + body.velocity + " " + collision.rigidbody.velocity);
        //}
    }
    private void OnCollisionStay(Collision collision)
    {
        //if (collision.rigidbody)
        //{
        //    Debug.Log("Collide with body " + collision.rigidbody.name + body.velocity + " " + collision.rigidbody.velocity);
        //}
    }
    private void OnCollisionExit(Collision collision)
    {
    }
    private void OnValidate()
    {
        body = GetComponent<Rigidbody>();
        Debug.Assert(body.collisionDetectionMode == CollisionDetectionMode.ContinuousDynamic);
        //Otherwise will trigger false collision when in fast moving vehicles
    }
}
