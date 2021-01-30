using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.SpatialTracking;
using UnityEngine.Events;

//TODO Check Enable working properly
//TODO UI Interation
//TODO Keyboard
//TODO Bows, Guns, Joysticks, Staffs, Sword

[DefaultExecutionOrder(-5)]
public class XRHand : MonoBehaviour
{
    public Transform playerRoot;
    public Transform trackedHand;
    public XRNode whichHand;
    public LayerMask interactionLayer;
    public SphereCollider grabSphere;
    public float pickUpDistTS = 1f;
    public HapticSettings hapticSettings;

    public Rigidbody bodyToAttach;

    public Behaviour[] enableWhenEmpty;

    public Vector3 position => transform.position;//shoud not use body's
    public Quaternion rotation => transform.rotation;
    public InputDevice device => InputDevices.GetDeviceAtXRNode(whichHand);

    public static XRHand leftHand, rightHand;
    public XRHand otherHand => this == leftHand ? rightHand : leftHand;

    public XRInteractable attached { get; private set; }
    [HideInInspector] public XRInteractable hovering;
    [HideInInspector] public Vector3 hoveringHitPosition;
    [HideInInspector] public Quaternion hoveringHitRotation;
    [HideInInspector] public bool isEmpty =>!attached;
    [HideInInspector] public Vector3 lastVelocity;
    Vector3 lastPos;
    bool mouseLook = false;



    private void OnEnable()//should not use awake for network compatibility
    {
        if (whichHand == XRNode.LeftHand) leftHand = this;
        if (whichHand == XRNode.RightHand) rightHand = this;
    }
    private void Start()
    {
        lastPos = position;
        InitIgnoreColliders();

        var xrDisplaySubsystems = new List<XRDisplaySubsystem>();
        SubsystemManager.GetInstances<XRDisplaySubsystem>(xrDisplaySubsystems);
        mouseLook = !xrDisplaySubsystems.Exists(x => x.running);
    }
    private void OnDisable()
    {
        DetachIfAny();
        ClearAllIgnoredColliders();
    }

    bool lastgrip = false,lasttrigger=false;
    private void Update()
    {
        if (mouseLook && whichHand == XRNode.RightHand)
        {
            if (Input.GetMouseButtonDown(0))
                input_grip = input_grip > .5 ? 0 : 1;
            input_trigger = Input.GetMouseButton(1) ? 1 : 0;
        }
    }
    private void FixedUpdate()
    {
        lastVelocity = (trackedHand.position - lastPos) / Time.fixedDeltaTime;
        lastPos = trackedHand.position;

        ReadDeviceInputs();
        UpdateHovering();
        UpdateIgnoreCollision();
        if(input_grip>.5f && isEmpty && hovering && !lastgrip )
        {
            if(hovering.CanAttach(this, out _))
                Attach(hovering, hoveringHitPosition, hoveringHitRotation);
        }
        lastgrip = input_grip > .5f;
        if(input_trigger>.5f && isEmpty && hovering && !lasttrigger)
        {
            if (hovering.CanInteract(this, out _))
                hovering.OnInteract();
        }


        bool isEmpty_tmp = isEmpty;
        foreach (var b in enableWhenEmpty)
            if (b && b.enabled!= isEmpty_tmp)
                b.enabled = isEmpty_tmp;
    }
    public void Attach(XRInteractable interactable, Vector3 handAttachPositionWS, Quaternion handAttachRotationWS)
    {
        if (!interactable.CanAttach(this, out _)) return;
        DetachIfAny();

        attached = interactable;
        foreach (var c in attached.GetComponentsInChildren<Collider>())
            IgnoreCollider(c);

        interactable.OnAttach(this, handAttachPositionWS, handAttachRotationWS);
        SendHapticImpulse(hapticSettings.attachStrength, hapticSettings.attachDuration);
    }
    public void DetachIfAny()
    {
        if (attached)
        {
            attached.OnDetach(this);
            attached = null;
            SendHapticImpulse(hapticSettings.detachStrength, hapticSettings.detachDuration);
        }
        attached = null;
    }
    public void TransforAttached(XRHand other)
    {
        if (attached)
        {
            other.DetachIfAny();
            other.attached = attached;
            foreach (var c in attached.GetComponentsInChildren<Collider>())
                other.IgnoreCollider(c);
            attached = null;


            SendHapticImpulse(hapticSettings.detachStrength, hapticSettings.detachDuration);
            other.SendHapticImpulse(other.hapticSettings.attachStrength, other.hapticSettings.attachDuration);
        }
    }
    public void OnTeleport()
    {
        attached?.OnTeleport();
    }
    public bool isHovering(XRInteractable interactable)
    {
        int grabHitNum = Physics.SphereCastNonAlloc(grabSphere.transform.position - grabSphere.radius * grabSphere.transform.localScale.x * grabSphere.transform.forward,
                            grabSphere.radius * grabSphere.transform.localScale.x,
                            grabSphere.transform.forward,
                            _hits,
                            (grabSphere.center.z + grabSphere.radius) * grabSphere.transform.localScale.x,
                            interactionLayer);

        for (int i = 0; i < grabHitNum; ++i)
        {
            var hit = _hits[i];
            var it = hit.collider.attachedRigidbody ? hit.collider.attachedRigidbody.GetComponent<XRInteractable>() : hit.collider.GetComponent<XRInteractable>();
            if (it == interactable) return true;
        }
        return false;
    }
    void UpdateHovering()
    {
        XRInteractable newHovering = null;
        if (isEmpty)
        {
            /*int grabHitNum = Physics.SphereCastNonAlloc(grabSphere.transform.position - grabSphere.radius * grabSphere.transform.localScale.x * grabSphere.transform.forward,
                                grabSphere.radius * grabSphere.transform.localScale.x,
                                grabSphere.transform.forward,
                                _hits,
                                (grabSphere.center.z + grabSphere.radius) * grabSphere.transform.localScale.x,
                                interactionLayer,
                                QueryTriggerInteraction.Collide);*/
            /*PhysicsEX.SphereCast(grabSphere.transform.position - grabSphere.radius * grabSphere.transform.localScale.x * grabSphere.transform.forward,
                                grabSphere.radius * grabSphere.transform.localScale.x,
                                grabSphere.transform.forward,
                                out RaycastHit junk, 
                                (grabSphere.center.z + grabSphere.radius) * grabSphere.transform.localScale.x, 
                                interactionLayer);*/
            int grabHitNum = Physics.OverlapSphereNonAlloc(grabSphere.transform.TransformPoint(grabSphere.center),
                                grabSphere.radius * grabSphere.transform.localScale.x,
                                colliderBuffer,
                                interactionLayer,
                                QueryTriggerInteraction.Collide);
            int maxP = int.MinValue; float minD = float.MaxValue;
            for (int i = 0; i < grabHitNum; ++i)
            {
                /*
                var hit = _hits[i];
                if (hit.distance==0 ||  Vector3.Dot(grabSphere.transform.forward, hit.point - grabSphere.transform.position) < 0) continue;
                var it = hit.collider.attachedRigidbody ? hit.collider.attachedRigidbody.GetComponent<XRInteractable>() : hit.collider.GetComponent<XRInteractable>();*/
                var cld = colliderBuffer[i];
                var it = cld.attachedRigidbody ? cld.attachedRigidbody.GetComponent<XRInteractable>() : cld.GetComponent<XRInteractable>();
                if (!it) continue;
                Vector3 clostestPoint = cld.ClosestPoint(grabSphere.transform.position);
                if (Vector3.Dot(grabSphere.transform.forward, clostestPoint - grabSphere.transform.position) < 0) continue;
                float distance = Vector3.Distance(clostestPoint, grabSphere.transform.position);
                if (!it.isActiveAndEnabled) continue;
                if (it.CanInteract(this, out int p))
                {
                    if (p > maxP || p == maxP && distance < minD)
                    {
                        maxP=p; minD = distance; 
                        newHovering = it;
                        hoveringHitPosition = clostestPoint;
                        hoveringHitRotation = rotation;
                        /*
                        float cutoff = grabSphere.radius * grabSphere.transform.lossyScale.x*.5f;
                        if (hit.distance < cutoff)//TODO better judgement?
                        {
                            hoveringHitPosition = position;
                            hoveringHitRotation = rotation;
                        }
                        else
                        {
                            hoveringHitPosition = position+ grabSphere.transform.forward*(hit.distance- cutoff);
                            hoveringHitRotation = rotation;//TODO adjust it by hit.normal?
                        }
                        */
                    }
                }
            }
            if (!newHovering)
            {
                if (Physics.SphereCast(grabSphere.transform.TransformPoint(grabSphere.center)+transform.forward* grabSphere.radius * grabSphere.transform.localScale.x*.0f,
                    grabSphere.radius * grabSphere.transform.localScale.x,
                    transform.forward,
                    out RaycastHit hit2,
                    playerRoot.lossyScale.x * pickUpDistTS,
                    interactionLayer,
                    QueryTriggerInteraction.Collide))
                {
                    var it = hit2.collider.attachedRigidbody ? hit2.collider.attachedRigidbody.GetComponent<XRInteractable>() : hit2.collider.GetComponent<XRInteractable>();
                    if (it && it.isActiveAndEnabled && it.CanInteract(this, out _))
                    {
                        newHovering = it;
                        hoveringHitPosition = hit2.point;
                        hoveringHitRotation = rotation;
                    }
                }
            }
        }

        if (hovering != newHovering && hovering != null && hovering != otherHand.hovering)
            hovering.SetHovering(false);

        if (newHovering)
            newHovering.SetHovering(true);

        if (hovering != newHovering)
        {
            if (newHovering == null)
                SendHapticImpulse(hapticSettings.hoveringExitStrength, hapticSettings.hoveringEnterDuration);
            else
                SendHapticImpulse(hapticSettings.hoveringEnterStrength, hapticSettings.hoveringEnterDuration);
        }

        hovering = newHovering;
    }
    #region IgnoreCollision
    List<Collider> ignoredColliders=new List<Collider>();
    Collider[] colliderBuffer = new Collider[50];
    [HideInInspector]public Collider[] handColliders;
    void InitIgnoreColliders()
    {
        handColliders = GetComponentsInChildren<Collider>();
        foreach (var c in playerRoot.GetComponentsInChildren<Collider>())
            foreach (var cc in handColliders)
                Physics.IgnoreCollision(c, cc, true);
    }
    void IgnoreCollider(Collider c)
    {
        if(!ignoredColliders.Exists(x=>x==c))
        {
            ignoredColliders.Add(c);

            foreach (var cc in handColliders)
            {
                //Debug.Log($"IgnoreCollision {c} {cc}");
                Physics.IgnoreCollision(cc, c, true);
            }
        }    
    }
    void UpdateIgnoreCollision()
    {
        int n = Physics.OverlapSphereNonAlloc(grabSphere.transform.TransformPoint(grabSphere.center), grabSphere.transform.lossyScale.x * grabSphere.radius, colliderBuffer, interactionLayer);
        for (int i = ignoredColliders.Count - 1; i >= 0; --i)
        {
            var c = ignoredColliders[i];
            if (c == null) continue;
            bool found = false;
            for(int j = 0; j < n; ++j) if (c == colliderBuffer[j]) { found = true;break; }
            if (!found && attached && c.GetComponentInParent<XRInteractable>() == attached && attached.isActiveAndEnabled) continue;
            if (!found)
            {
                foreach(var cc in handColliders)
                {
                    Physics.IgnoreCollision(cc, ignoredColliders[i], false);
                    //Debug.Log($"UnIgnoreCollision {c} {cc}");
                }
                ignoredColliders.RemoveAt(i);
            }
        }
    }
    void ClearAllIgnoredColliders()
    {
        foreach(var c in ignoredColliders)
            if(c)
                foreach (var cc in handColliders)
                    Physics.IgnoreCollision(cc, c, false);
        ignoredColliders.Clear();
    }
    #endregion
    #region Inputs
    [HideInInspector] public float input_grip, input_trigger;
    [HideInInspector] public bool input_primary_button, input_secondary_button, input_primary_axis_click;
    [HideInInspector] public Vector2 input_primary_axis;
    public void ReadDeviceInputs()
    {
        if (mouseLook) return;
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
    #endregion
    #region Haptics
    [System.Serializable]
    public class HapticSettings
    {
        public float hoveringEnterStrength = .1f;
        public float hoveringEnterDuration = .01f;
        public float hoveringExitStrength = 0f;
        public float hoveringExitDuration = .01f;
        public float attachStrength = .5f;
        public float attachDuration = .01f;
        public float detachStrength = .5f;
        public float detachDuration = .01f;
        public float collisionEnterMaxStrength = 1f;
        public float collisionEnterMaxStrengthSpeed = 5f;
        public float collisionEnterDuration = .05f;
    }
    public void SendHapticImpulse(float strength, float duration)
    {
        var device = InputDevices.GetDeviceAtXRNode(whichHand);
        if (device != null) device.SendHapticImpulse(0, strength, duration);
    }
    #endregion
    RaycastHit[] _hits = new RaycastHit[50];
    private void OnValidate()
    {
        Debug.Assert(Physics.defaultMaxAngularSpeed >= 50);
    }
    public XRInteractable debug_attach;
    [Button]
    public void Debug_Attach()
    {
        if (debug_attach.CanAttach(this, out _))
        {
            Attach(debug_attach, position, rotation);
            input_grip = 1;
        }
    }
}
public abstract class XRInteractable : MonoBehaviour
{
    public virtual bool CanInteract(XRHand hand, out int priority) { priority = 0;return true; }
    public virtual bool CanAttach(XRHand hand, out int priority) { priority = 0;return false; }
    public virtual void OnAttach(XRHand emptyHand, Vector3 attachPositionWS, Quaternion attachRotationWS) { }
    public virtual void OnDetach(XRHand handAttachedMe) { }
    public virtual bool GetAttachPosition(XRHand hand, out Vector3 position, out Quaternion rotation) { position = hand.position;rotation= hand.rotation;return false; }
    public virtual void SetHovering(bool isHovering) { }
    public virtual void OnTeleport() { }
    public virtual void OnInteract() { }
    private void OnValidate()
    {
        if (!LayerMask.LayerToName(gameObject.layer).StartsWith("Interactable"))
            Debug.LogError("The Layer for interactable objects must be \"InteractableXXX\"");
        Debug.Assert(gameObject.GetComponent<Rigidbody>());
    }
}
