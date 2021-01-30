using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.Events;

[DefaultExecutionOrder(-3)]//That's weird
public class XRHand : MonoBehaviour
{
    public XRNode whichHand;

    public Transform playerRoot;
    public Animator bareHandAnimator;
    public Rigidbody bareHandBody;
    public SphereCollider sphereCollider;

    public LayerMask interactionLayer;
    public float maxPickupDist = 1f;
    public float strength = 500f,torqueStrength=50f;

    public HapticSettings hapticSettings;

    public Behaviour[] disableWhenAttaching;
    public UnityEvent onTriggerDown, onTriggerUp, onGripDown, onGripUp;



    Transform trackingSpace => transform.parent;
    float trackingScale => trackingSpace.lossyScale.x;

    [HideInInspector] public static XRHand leftHand, rightHand;
    [HideInInspector] public XRHand otherHand => this == leftHand ? rightHand : leftHand;

    [HideInInspector] public bool isEmpty => !attached;

    [HideInInspector] public Vector3 position { get { return transform.position; } }
    [HideInInspector] public Quaternion rotation { get { return transform.rotation; } }
    [HideInInspector] public Vector3 trackedPosition { get { return transform.localPosition; } }
    [HideInInspector] public Quaternion trackedRotation { get { return transform.localRotation; } }
    [HideInInspector] public Vector3 velocity, angularVelocity;
    [HideInInspector] public Vector3 trackedVelocity, trackedAngularVelocity;

    [HideInInspector] public bool primaryAxisOverride => false;
    [HideInInspector] public float input_trigger, input_grip;
    [HideInInspector] public bool input_primary_button, input_secondary_button,input_primary_axis_click;
    [HideInInspector] public bool input_trigger_button, input_grip_button;
    [HideInInspector] public Vector2 input_primary_axis;
    [HideInInspector] public XRInteractable attached { get; private set; }
    [HideInInspector] public Vector3 pickUpDir => transform.forward;

    private void Awake()
    {
        if (whichHand == XRNode.LeftHand) leftHand = this;
        if (whichHand == XRNode.RightHand) rightHand = this;
    }
    
    private void Start()
    {
        SetupBareHand();
    }
    private void OnDisable()
    {
        RemoveHovering();
        RemoveBareHandIgnoreColliders();
        DetachIfAny();
    }

    private void FixedUpdate()
    {
        ReadDeviceInputs();
        ReadDeviceTracking();
        UpdateVelocity(Time.fixedDeltaTime);
        UpdateInputs();
        UpdateBareHand(Time.fixedDeltaTime);
        UpdateBareHandIgnoreColliders();
        UpdateHoverings();
        UpdateHaptic(Time.fixedUnscaledDeltaTime);
    }
    public void OnPlayerTeleport()
    {
        attached?.OnPlayerTeleport();
    }
    #region Attach
    public void DetachIfAny()
    {
        if (attached)
            _Detach();
    }
    public void Attach(XRInteractable interactable)
    {
        if (attached != interactable)
        {
            DetachIfAny();
            _Attach(interactable, 0, interactable.transform.InverseTransformPoint(position));
        }
    }
    void _Attach(XRInteractable interactable, float pickUpDistance, Vector3 pickUpPointLS)
    {
        if (interactable.CanAttach(this, pickUpDistance, out int priority))
        {
            RemoveHovering();
            attached = interactable;//Be careful with the order
            foreach (var b in disableWhenAttaching) b.enabled = false;
            foreach (var c in interactable.GetComponentsInChildren<Collider>(includeInactive:true))
                BareHandIgnoreCollider(c);
            SendHapticImpulse(hapticSettings.attachStrength, hapticSettings.attachDuration);
            interactable.OnAttach(this, pickUpPointLS);//Be careful with the order
        }
    }
    void _Detach()
    {
        Debug.Assert(attached);
        SendHapticImpulse(hapticSettings.detachStrength, hapticSettings.detachDuration);
        attached.OnDetach(this);//Be careful with the order
        foreach (var b in disableWhenAttaching) b.enabled = true;
        attached = null;//Be careful with the order
    }
    #endregion
    #region Hovering
    //readonly List<(XRInteractable, float)> hoveringInteractables = new List<(XRInteractable, float)>();
    //public bool isHovering(XRInteractable x) => hoveringInteractables.Exists(y => y.Item1 == x);
    XRInteractable hoveringInteractable=null;
    float hoveringPickUpDist=0;
    Vector3 hoveringPointLS;
    void UpdateHoverings()
    {
        if (attached)
        {
            RemoveHovering();
            return;
        }
        //Get hovering from overlapsphere and spherecast
        int maxPriority = int.MinValue, tmpPriority;float minDist = float.MaxValue;
        XRInteractable newHovering = null; float newPickUpDistance = 0;
        var colliders = Physics.OverlapSphere(transform.TransformPoint(sphereCollider.center), trackingScale * sphereCollider.radius, interactionLayer);
        foreach (var c in colliders)
        {
            var interactable = c.attachedRigidbody ? c.attachedRigidbody.GetComponent<XRInteractable>() : c.GetComponent<XRInteractable>();
            float tmpDist = Vector3.Distance(interactable.transform.position, position);

            if (interactable && interactable.CanHover(this,0, out tmpPriority))
                if (tmpPriority > maxPriority || tmpPriority==maxPriority && tmpDist<minDist)
                {
                    maxPriority = tmpPriority;newHovering = interactable;newPickUpDistance = 0;minDist = tmpDist;
                    hoveringPointLS = interactable.transform.InverseTransformPoint(position);
                }
        }
        if (!newHovering)
        {
            if (Physics.SphereCast(transform.position, trackingScale * sphereCollider.radius, transform.forward, out RaycastHit hitInfo, trackingScale * (maxPickupDist - sphereCollider.radius), interactionLayer))
            {
                float pickUpDist = Mathf.Max(0, hitInfo.distance);
                var interactable = hitInfo.rigidbody ? hitInfo.rigidbody.GetComponent<XRInteractable>() : hitInfo.collider.GetComponent<XRInteractable>();
                if (interactable &&  interactable.CanHover(this,pickUpDist,out tmpPriority))
                    if (tmpPriority > maxPriority)
                    {
                        float dist = Mathf.Max(0, hitInfo.distance);
                        newHovering = interactable; newPickUpDistance = dist;
                        hoveringPointLS = interactable.transform.InverseTransformPoint(hitInfo.point);
                    }
            }
        }
        //exit hovering
        if (hoveringInteractable != newHovering)
        {
            if (newHovering == null)
            {
                if(hoveringInteractable && otherHand.hoveringInteractable!=hoveringInteractable)
                    hoveringInteractable.OnHoverExit();
                hoveringInteractable = null;
                SendHapticImpulse(hapticSettings.hoveringExitStrength, hapticSettings.hoveringExitDuration);

            }
            else
            {
                if (hoveringInteractable && otherHand.hoveringInteractable != hoveringInteractable)
                    hoveringInteractable.OnHoverExit();
                hoveringInteractable = newHovering;
                hoveringPickUpDist = newPickUpDistance;
                if (otherHand.hoveringInteractable != hoveringInteractable)
                    hoveringInteractable.OnHoverEnter();
                SendHapticImpulse(hapticSettings.hoveringEnterStrength, hapticSettings.hoveringEnterDuration);
            }
        }
    }
    void RemoveHovering()
    {
        if (hoveringInteractable && otherHand.hoveringInteractable != hoveringInteractable)
            hoveringInteractable.OnHoverExit();
        hoveringInteractable = null;
    }
    #endregion
    #region BareHand
    Collider[] bareHandColliders;
    List<Collider> bareHandIgnoredColliders = new List<Collider>();
    void SetupBareHand()
    {
        Debug.Assert(!bareHandBody.useGravity);
        bareHandColliders = bareHandBody.GetComponentsInChildren<Collider>(includeInactive: true);
        foreach (var c1 in playerRoot.GetComponentsInChildren<Collider>(includeInactive: true))
            foreach (var c2 in bareHandColliders)
                if (c1 != c2)
                    Physics.IgnoreCollision(c1, c2);
    }
    void UpdateBareHand(float dt)
    {
        if (!isEmpty)
        {
            if(attached && attached.TryGetHandPosition(this, out Vector3 handPosition, out Quaternion handRotation))
            {
                if (!bareHandBody.gameObject.activeSelf)
                    bareHandBody.gameObject.SetActive(true);
                bareHandBody.isKinematic = true;
                bareHandBody.transform.position = handPosition;
                bareHandBody.transform.rotation = handRotation;

            }
            else
            {
                if (bareHandBody.gameObject.activeSelf)
                    bareHandBody.gameObject.SetActive(false);
            }
        }
        else
        {
            bareHandBody.isKinematic = false;
            if (!bareHandBody.gameObject.activeSelf)
                bareHandBody.gameObject.SetActive(true);

            bareHandAnimator.SetFloat("Grip", input_grip);

            if (Vector3.Distance(bareHandBody.position, position) > .3f * trackingScale)
            {
                bareHandBody.transform.position = position;//set rigidbody.position instead will cause hand will not teleport when player is moving
                bareHandBody.transform.rotation = rotation;
                bareHandBody.velocity = Vector3.zero;
                bareHandBody.angularVelocity = Vector3.zero;
            }
            else
            {
                Vector3 desiredVelocity = (position - bareHandBody.position) / dt;
                Vector3 desiredAngularVelocity = GetRotationVector(bareHandBody.rotation, rotation) / dt;
                desiredVelocity = Vector3.Lerp(bareHandBody.velocity, desiredVelocity, dt / .01f);
                desiredAngularVelocity = Vector3.Lerp(bareHandBody.angularVelocity, desiredAngularVelocity, dt / .01f);
                Vector3 applyForce = (desiredVelocity - bareHandBody.velocity) / Time.fixedDeltaTime * bareHandBody.mass;
                Vector3 applyTorque = ApplyTensor(desiredAngularVelocity - bareHandBody.angularVelocity, bareHandBody.inertiaTensor, bareHandBody.rotation * bareHandBody.inertiaTensorRotation) / Time.fixedDeltaTime;
                applyForce = Vector3.ClampMagnitude(applyForce, strength);
                applyTorque = Vector3.ClampMagnitude(applyTorque, torqueStrength);
                bareHandBody.AddForce(applyForce);
                bareHandBody.AddTorque(applyTorque);
            }
        }
    }
    void BareHandIgnoreCollider(Collider c)
    {
        if (!bareHandIgnoredColliders.Exists(cc => cc == c))
        {
            bareHandIgnoredColliders.Add(c);
            foreach (var cc in bareHandColliders)
            {
                //Debug.Log($"{cc} {c}");
                Physics.IgnoreCollision(c, cc, true);
            }
        }
    }
    void UpdateBareHandIgnoreColliders()
    {
        //var colliders = Physics.OverlapSphere(transform.TransformPoint(sphereCollider.center), trackingScale * sphereCollider.radius, interactionLayer);
        for(int i = bareHandIgnoredColliders.Count - 1; i >= 0; --i)
        {
            var c = bareHandIgnoredColliders[i];

            if (attached && c.GetComponentInParent<XRInteractable>() == attached)
                continue;

            bool found = false;
            //foreach (var cc in colliders) if (c == cc) found = true;
            foreach(var cc in bareHandColliders)
                if(cc.enabled && cc.gameObject.activeInHierarchy)
                    if(Physics.ComputePenetration(c,c.transform.position,c.transform.rotation,cc,cc.transform.position,cc.transform.rotation, out Vector3 dir, out float dist))
                    {
                        found = true;break;
                    }

            if (!found)
            {
                bareHandIgnoredColliders.RemoveAt(i);
                foreach(var cc in bareHandColliders)
                    Physics.IgnoreCollision(c, cc, false);
            }
        }
    }
    void RemoveBareHandIgnoreColliders()
    {
        for (int i = bareHandIgnoredColliders.Count - 1; i >= 0; --i)
        {
            var c = bareHandIgnoredColliders[i];
            bareHandIgnoredColliders.RemoveAt(i);
            foreach (var cc in bareHandColliders)
                Physics.IgnoreCollision(c, cc, false);
        }
    }
    #endregion
    #region XRDevices and Inputs
    readonly List<XRNodeState> xrNodeStates = new List<XRNodeState>();
    public void ReadDeviceTracking()
    {
        InputTracking.GetNodeStates(xrNodeStates);
        foreach (var s in xrNodeStates)
        {
            if (s.nodeType == whichHand)
            {
                if (s.TryGetPosition(out Vector3 pos)) transform.position = trackingSpace.TransformPoint(pos);
                if (s.TryGetRotation(out Quaternion rot)) transform.rotation = trackingSpace.rotation * rot;
            }
        }
    }
    public void ReadDeviceInputs()
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(whichHand);
        if (device != null)
        {
            if (!device.TryGetFeatureValue(CommonUsages.trigger, out input_trigger)) input_trigger = 0;
            if (!device.TryGetFeatureValue(CommonUsages.grip, out input_grip)) input_grip = 0;
            if (!device.TryGetFeatureValue(CommonUsages.primaryButton, out input_primary_button)) input_primary_button = false;
            if (!device.TryGetFeatureValue(CommonUsages.secondaryButton, out input_secondary_button)) input_secondary_button = false;
            if (!device.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out input_primary_axis_click)) input_primary_axis_click = false;
            if (!device.TryGetFeatureValue(CommonUsages.primary2DAxis, out input_primary_axis)) input_primary_axis = Vector2.zero;
        }
    }
    Vector3 oldTrackedPosition; Quaternion oldTrackedRotation;
    void UpdateVelocity(float dt, float smoothTime = .1f, float clamp = 100)
    {
        trackedVelocity = Vector3.Lerp(trackedVelocity, Vector3.ClampMagnitude((trackedPosition - oldTrackedPosition) / dt, clamp), dt / smoothTime);
        trackedAngularVelocity = Vector3.Lerp(trackedAngularVelocity, Vector3.ClampMagnitude(GetRotationVector(oldTrackedRotation, trackedRotation) / dt, clamp), dt / smoothTime);
        oldTrackedPosition = trackedPosition;
        oldTrackedRotation = trackedRotation;
        velocity = trackingSpace.TransformVector(trackedVelocity);
        angularVelocity = trackingSpace.TransformDirection(trackedAngularVelocity);
    }
    #endregion
    #region Inputs
    bool input_trigger_lastButton, input_grip_lastButton;
    void UpdateInputs()
    {

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
        if (!attached)
            if (hoveringInteractable)
            {
                _Attach(hoveringInteractable, hoveringPickUpDist,hoveringPointLS);
            }
        attached?.OnGripDown(this);
        onGripDown.Invoke();
    }
    void _OnGripUp() { attached?.OnGripUp(this); onGripUp.Invoke(); }
    void _OnTriggerDown() { attached?.OnTriggerDown(this);onTriggerDown.Invoke(); }
    void _OnTriggerUp() { attached?.OnTriggerUp(this);onTriggerUp.Invoke(); }
    #endregion
    #region Haptic
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
    void UpdateHaptic(float unscaledDt)
    {
        hapticRefractoryCD -= unscaledDt;
    }
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

public abstract class XRInteractable : MonoBehaviour
{
    public abstract bool CanAttach(XRHand emptyHand, float pickUpDistance, out int priority);
    public abstract void OnAttach(XRHand handAttachedMe, Vector3 pickUpPointLS);
    public abstract void OnDetach(XRHand handWillDetachMe);

    public virtual void OnGripDown(XRHand hand) { }
    public virtual void OnGripUp(XRHand hand) { }
    public virtual void OnTriggerDown(XRHand hand) { }
    public virtual void OnTriggerUp(XRHand hand) { }

    public virtual void OnHoverEnter() { }
    public virtual void OnHoverExit() { }
    public virtual bool CanHover(XRHand hand, float pickUpDistance, out int priority) { priority = 0; return true; }
    public virtual void OnValidate(){
        if (gameObject.layer == 0)Debug.LogWarning("Did you forgot to set the layer for this interactable?");
        Debug.Assert(Physics.defaultMaxAngularSpeed >= 50);
        Debug.LogWarning("Did you enabled Physics Adaptive Force?");
    }
    public virtual void OnPlayerTeleport() { }
    public virtual bool TryGetHandPosition(XRHand hand, out Vector3 position, out Quaternion rotation) { position = Vector3.zero;rotation = Quaternion.identity; return false; }
}