using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.Events;

[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(Rigidbody))]
public class XRHand : MonoBehaviour
{
    Transform trackingSpace { get { return transform.parent; } }
    public XRNode whichHand;
    public LayerMask interactionLayer;
    public float pickUpDistance = 1f;
    public float teleportThredhold = .3f;
    public HapticSettings hapticSettings;
    public static XRHand leftHand, rightHand;
    JointSettings handBodyJoint = new JointSettings();
    JointSettings gripJoint = new JointSettings();

    [HideInInspector] public Quaternion rotation { get { return transform.rotation; } }
    [HideInInspector] public Vector3 position { get { return transform.position; } }
    [HideInInspector] public float input_trigger, input_grip;
    [HideInInspector] public bool input_trigger_button, input_grip_button;
    [HideInInspector] public Rigidbody attached;
    bool input_trigger_lastButton, input_grip_lastButton;

    SphereCollider sphereCollider;
    Rigidbody body;
    public Rigidbody playerBody;
    public XRHand otherHand => whichHand == XRNode.LeftHand ? rightHand : leftHand;
    float scale => transform.lossyScale.x;

    private void OnValidate()
    {
        if (Time.fixedDeltaTime > 1f / 60)
            Debug.LogWarning("Physics Update Rate should be greater than frame rate to avoid jittering");
    }
    private void Awake()
    {
        Debug.Assert(trackingSpace && playerBody);
        if (whichHand == XRNode.LeftHand) leftHand = this;
        if (whichHand == XRNode.RightHand) rightHand = this;
        sphereCollider = GetComponent<SphereCollider>();
        body = GetComponent<Rigidbody>();
        body.useGravity = false;
        body.maxAngularVelocity = 100f;
    }
    private void Start()
    {
        bodyJoint = SetupJoint(body, playerBody, Vector3.zero, handBodyJoint, out bodyJointInitialLocalRotation, out bodyJointAxisLocalRotation);
        /*
        if (this == leftHand)
            foreach (var c in GetComponentsInChildren<Collider>())
                foreach (var cc in rightHand.GetComponentsInChildren<Collider>())
                    Physics.IgnoreCollision(c, cc);
                    */
    }
    Vector3 targetVelocity, targetAngularVelocity, oldTargetPosition;
    Quaternion oldTargetRotation;
    private void FixedUpdate()
    {
        UpdateNodeState();
        if(Vector3.Distance(targetPosition,body.position)< teleportThredhold*scale)
        {
            targetVelocity = (targetPosition - oldTargetPosition) / Time.fixedDeltaTime;
            targetAngularVelocity = GetTargetAngularVelocity(oldTargetRotation, targetRotation, Time.fixedDeltaTime);
        }
        else
        {
            targetVelocity = Vector3.zero;
            targetAngularVelocity = Vector3.zero;
            body.position = targetPosition;
            body.rotation = targetRotation;
        }
        oldTargetPosition = targetPosition;
        oldTargetRotation = targetRotation;
        SetJointTarget(bodyJoint, targetPosition, targetRotation, targetVelocity-playerBody.velocity, targetAngularVelocity-playerBody.velocity, bodyJointInitialLocalRotation, bodyJointAxisLocalRotation);
    }


    private void Update()
    {
        hapticRefractoryCD -= Time.unscaledDeltaTime;
        UpdateNodeState();
        UpdateInputState();
        UpdateHoveringInteractable();

        input_trigger_lastButton = input_trigger_button;
        input_trigger_button = input_trigger > .9f;
        if (input_trigger_lastButton && !input_trigger_button) _OnTriggerUp();
        if (!input_trigger_lastButton && input_trigger_button) _OnTriggerDown();

        input_grip_lastButton = input_grip_button;
        input_grip_button = input_grip > .9f;
        if (input_grip_lastButton && !input_grip_button) _OnGripUp();
        if (!input_grip_lastButton && input_grip_button) _OnGripDown();
    }
    void _OnGripDown()
    {
        var h = GetHovered();
        Debug.Assert(!attached);
        if (h)
        {
            Debug.Log(h);
            _Attach(h);
        }
    }
    void _OnGripUp()
    {
        if (attached)
            _Detach();
    }
    void _OnTriggerDown()
    {
    }
    void _OnTriggerUp()
    {
    }
    void _Attach(Rigidbody target)
    {
        Debug.Assert(!attached || pickUpJoint);
        Vector3 localAnchor = target.transform.InverseTransformPoint(position);
        pickUpJoint=SetupJoint(target, body, localAnchor, gripJoint, out pickUpJointInitialLocalRotation, out pickUpJointAxisLocalRotation);
        attached = target;
    }
    void _Detach()
    {
        Debug.Assert(attached && pickUpJoint);
        Debug.Log(pickUpJoint);
        Destroy(pickUpJoint);
        attached = null;
        pickUpJoint = null;
    }

    #region Joint
    ConfigurableJoint bodyJoint, pickUpJoint;
    Quaternion bodyJointInitialLocalRotation;
    Quaternion bodyJointAxisLocalRotation;
    Quaternion pickUpJointInitialLocalRotation;
    Quaternion pickUpJointAxisLocalRotation;
    [System.Serializable]
    public class JointSettings
    {
        public float jointSpring = 50000;
        public float jointDamping = 2000;
        public float jointMaxForce = 1000;//human single hand push strength//grip strength 600
        public float jointSpringAngular = 50000;
        public float jointDampingAngular = 2000;
        public float jointMaxForceAngular = 1000;//times one meter squared
    }
    static ConfigurableJoint SetupJoint(Rigidbody body, Rigidbody parentBody, Vector3 localAnchor, JointSettings jointSettings, out Quaternion jointInitialLocalRotation, out Quaternion jointAxisLocalRotation)
    {
        var joint = body.gameObject.AddComponent<ConfigurableJoint>();
        joint.connectedBody = parentBody;
        joint.enableCollision = false;
        joint.autoConfigureConnectedAnchor = false;
        joint.anchor = localAnchor;
        joint.connectedAnchor = joint.connectedBody.transform.InverseTransformPoint(body.transform.TransformPoint(localAnchor));
        joint.targetPosition = Vector3.zero;
        joint.xMotion = joint.yMotion = joint.zMotion = ConfigurableJointMotion.Free;
        joint.angularXMotion = joint.angularYMotion = joint.angularZMotion = ConfigurableJointMotion.Free;
        joint.xDrive = joint.yDrive = joint.zDrive =
            new JointDrive { positionSpring = jointSettings.jointSpring, positionDamper = jointSettings.jointDamping, maximumForce = jointSettings.jointMaxForce };
        joint.rotationDriveMode = RotationDriveMode.Slerp;
        joint.slerpDrive =
            new JointDrive { positionSpring = jointSettings.jointSpringAngular, positionDamper = jointSettings.jointDampingAngular, maximumForce = jointSettings.jointMaxForceAngular };

        jointInitialLocalRotation = Quaternion.Inverse(parentBody.rotation) * body.rotation;
        jointAxisLocalRotation = Quaternion.LookRotation(Vector3.Cross(joint.axis, joint.secondaryAxis), joint.secondaryAxis);
        return joint;
    }
    static void SetJointTarget(ConfigurableJoint joint, Vector3 targetWorldPosition, Quaternion targetWorldRotation, Vector3 targetWorldVelocity, Vector3 targetWorldAngularVelocity, Quaternion jointInitialLocalRotation, Quaternion jointAxisLocalRotation)
    {

        joint.connectedAnchor = joint.connectedBody.transform.InverseTransformPoint(targetWorldPosition);
        joint.targetPosition = Vector3.zero;
        joint.targetRotation =
                        Quaternion.Inverse(jointAxisLocalRotation)
                        * Quaternion.Inverse(targetWorldRotation)
                        * joint.connectedBody.rotation
                        * jointInitialLocalRotation
                        * jointAxisLocalRotation;
            
        joint.targetVelocity = Quaternion.Inverse(targetWorldRotation*jointAxisLocalRotation) * targetWorldVelocity;
        joint.targetAngularVelocity = Quaternion.Inverse(targetWorldRotation * jointAxisLocalRotation) * targetWorldAngularVelocity;
    }
    #endregion
    #region Hovering
    struct HoveringDesc
    {
        public int priority;
        public Rigidbody interactable;
        public float pickUpDistance;
    }
    readonly List<HoveringDesc> hoveringInteractables = new List<HoveringDesc>();
    readonly List<HoveringDesc> newHovering = new List<HoveringDesc>();
    Rigidbody GetHovered()
    {
        Rigidbody rtval = null;
        foreach(var d in hoveringInteractables)
        {
            rtval = d.interactable;
        }

        return rtval;
    }
    void UpdateHoveringInteractable()
    {
        newHovering.Clear();
        var colliders = Physics.OverlapSphere(transform.TransformPoint(sphereCollider.center), scale * sphereCollider.radius, interactionLayer);
        foreach (var c in colliders)
        {
            var interactable = c.attachedRigidbody;
            if (interactable && !newHovering.Exists(x => x.interactable == interactable))
                newHovering.Add(new HoveringDesc() { interactable = interactable, pickUpDistance = 0, priority = 0 });
        }
        if (newHovering.Count == 0)
        {
            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hitInfo, scale * pickUpDistance , interactionLayer))
            {
                var interactable = hitInfo.rigidbody;
                if (interactable && !newHovering.Exists(x => x.interactable == interactable))
                {
                    float dist = Mathf.Max(0, hitInfo.distance);
                    newHovering.Add(new HoveringDesc() { interactable = interactable, pickUpDistance = dist });
                }
            }
        }
        foreach (var desc in hoveringInteractables)
            if (!newHovering.Exists(x => x.interactable == desc.interactable))
            {
                //desc.interactable._OnHoveringExitHand(this);
                if (!attached)
                    SendHapticImpulse(hapticSettings.hoveringExitStrength, hapticSettings.hoveringExitDuration);
            }
        foreach (var desc in newHovering)
            if (!hoveringInteractables.Exists(x => x.interactable == desc.interactable))
            {
                //desc.interactable._OnHoveringEnterHand(this);
                if (!attached)
                    SendHapticImpulse(hapticSettings.hoveringEnterStrength, hapticSettings.hoveringEnterDuration);
            }
        hoveringInteractables.Clear();
        foreach (var interactable in newHovering)
            hoveringInteractables.Add(interactable);
    }
    #endregion
    #region XRDevices
    readonly List<XRNodeState> xrNodeStates = new List<XRNodeState>();
    Vector3 targetPosition=Vector3.zero; 
    Quaternion targetRotation=Quaternion.identity; 
    public void UpdateNodeState()
    {
        InputTracking.GetNodeStates(xrNodeStates);
        bool got = false;
        foreach (var s in xrNodeStates)
        {
            if (s.nodeType == whichHand)
            {
                if (s.TryGetPosition(out Vector3 pos)) targetPosition = trackingSpace.TransformPoint(pos);
                if (s.TryGetRotation(out Quaternion rot)) targetRotation = trackingSpace.rotation * rot;
                got = true;
            }
        }
        if(!got)
            targetPosition = trackingSpace.TransformPoint(new Vector3(this == leftHand ? -.3f : .3f, 1, 0));
    }
    public void UpdateInputState()
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(whichHand);
        if (device != null)
        {
            if (!device.TryGetFeatureValue(CommonUsages.trigger, out input_trigger)) input_trigger = 0;
            if (!device.TryGetFeatureValue(CommonUsages.grip, out input_grip)) input_grip = 0;
        }
    }
    [System.Serializable]
    public class HapticSettings
    {
        public float hoveringEnterStrength = .2f;
        public float hoveringEnterDuration = .05f;
        public float hoveringExitStrength = .1f;
        public float hoveringExitDuration = .05f;
        public float attachStrength = .2f;
        public float attachDuration = .05f;
        public float detachStrength = .5f;
        public float detachDuration = .05f;
        public float refractoryPeriod = .5f;
    }
    float hapticRefractoryCD = 0f, hapticRefractoryLastStrength = 0f;
    public void SendHapticImpulse(float strength, float duration)
    {
        if (hapticRefractoryCD > 0 && strength > hapticRefractoryLastStrength)
            hapticRefractoryCD = 0;
        if (hapticRefractoryCD > 0) return;
        hapticRefractoryCD = hapticSettings.refractoryPeriod;
        hapticRefractoryLastStrength = strength;

        var device = InputDevices.GetDeviceAtXRNode(whichHand);
        if (device != null) device.SendHapticImpulse(0, strength, duration);
    }
    #endregion
    #region Util
    static Vector3 GetTargetAngularVelocity(Quaternion from, Quaternion to, float dt, float max = float.PositiveInfinity)
    {
        Quaternion q = to * Quaternion.Inverse(from);
        if (Mathf.Abs(q.w) >0.99999f) return Vector3.zero;
        q.ToAngleAxis(out float angle, out Vector3 axis);
        if (angle > 180) angle -= 360;
        Vector3 rtval= Vector3.ClampMagnitude(axis * angle * Mathf.Deg2Rad / dt, max);
#if UNITY_EDITOR
        if(float.IsNaN(rtval.magnitude) || float.IsInfinity(rtval.magnitude))
        {
            Debug.LogError($"NAN! {from} {to} {q}");
        }
#endif
        return rtval;
    }
    #endregion
}
