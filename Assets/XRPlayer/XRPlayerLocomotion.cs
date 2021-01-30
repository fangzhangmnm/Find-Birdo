using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.Events;
using PhysicsEX = UnityEngine.Physics;
//using PhysicsEX = PhysicsDebugCast;

//TODO Attach, Mount, Climb
[DefaultExecutionOrder(-10)]
public class XRPlayerLocomotion : MonoBehaviour
{

    [Header("References")]
    public Transform head;
    public SphereCollider headCollider;
    public Transform trackingSpace;
    public Transform inputRef;
    public Transform leftHand, rightHand;
    public TeleportLine teleportLine;

    [Header("Raycast Layers")]
    public LayerMask environmentLayers = -1;
    public LayerMask groundLayers = 0;

    [Header("Head Alignment")]
    public float expectedPlayerHeight = 1.6f;
    public float headLeanDistance = 1f;
    public float headTeleportDistance = .6f;
    public float trackingLostDistance = .3f;
    public float minHeadHeight = .3f;
    public float maxHeadHeight = 2.2f;
    public float trackedHeightBias = 0f;

    [Header("Movement Setting")]
    public float speed = 3f;
    public float dashSpeed = 7f;
    public float rotateSnap = 45f;
    public float rotateSnapTime = .2f;
    public float stepHeight = .3f;
    public float slopeLimit = 60f;
    public float jumpSpeed = 4.5f;

    [Header("Confort Setting")]
    public float rotateSmoothTime = .1f;

    [Header("Teleport Mode Setting")]
    public bool teleportMode = false;
    public float stepDistance = 1f;
    public float teleportTime = .2f;
    public float stepTime = .4f;

    [Header("Events")]
    public UnityEvent onSicknessMovement = new UnityEvent();
    public UnityEvent onTeleport = new UnityEvent();
    [HideInInspector]public UnityEvent onLeaveSeat = new UnityEvent();
    [System.Serializable] public class UpdateTransform : UnityEvent<Transform> { }
    public UpdateTransform updateAttach = new UpdateTransform();

    bool mouseLook;

    State GroundedState, FallingState, TeleportModeState, SeatedState;

    Vector2 inputStick;
    Vector2 inputStick2;
    Vector3 fallingVelocity;
    public Vector3 fallingVelocityReference;
    bool inputDash;
    bool inputJump;
    int inputTurn;
    bool jumpReleased = true;
    float transportCD = 0;
    int lastTransportAction = -1;
    bool teleportPressed;

    [HideInInspector] public bool InputOverriden = false;

    public static XRPlayerLocomotion instance;

    private void OnEnable()
    {
        Debug.Log("OnEnable");
        instance = this;
        
    }
    private void OnDisable()
    {
        Debug.Log("OnDisable");
        TransitionState(GroundedState);
    }
    private void OnValidate()
    {
        Debug.Assert(headCollider != null);
        capsuleCollider = GetComponent<CapsuleCollider>();
        //Debug.Assert(((1 << capsuleCollider.gameObject.layer) & environmentLayers) == 0);
        //Debug.Assert(((1 << headCollider.gameObject.layer) & environmentLayers) == 0);
        Debug.Assert(Time.fixedDeltaTime <= 1 / 60f);
    }
    private void Start()
    {
        capsuleCollider = GetComponent<CapsuleCollider>();
        foreach (var c in GetComponentsInChildren<CapsuleCollider>())
            if (c != capsuleCollider)
                Physics.IgnoreCollision(c, capsuleCollider);

        //Setting Tracking Origin to Floor, default device on Oculus Quest
        var xrInputSubsystems = new List<XRInputSubsystem>();
        SubsystemManager.GetInstances<XRInputSubsystem>(xrInputSubsystems);
        foreach (var ss in xrInputSubsystems) if (ss.running) ss.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor);

        teleportLine.enabled = false;

        GroundedState = new State("Grounded");
        GroundedState.OnEnter = EnterGrounded;
        GroundedState.OnExit = ExitGrounded;
        GroundedState.Update = UpdateGrounded;

        FallingState = new State("Falling");
        FallingState.Update = UpdateFalling;

        TeleportModeState = new State("TeleportMode");
        TeleportModeState.OnEnter = EnterTeleportMode;
        TeleportModeState.OnExit = ExitTeleportMode;
        TeleportModeState.Update = UpdateTeleportMode;

        SeatedState = new State("Seated");
        SeatedState.OnEnter = EnterSeated;
        SeatedState.OnExit = ExitSeated;
        SeatedState.Update = UpdateSeated;
        SeatedState.LateUpdate = LateUpdateSeated;

        TransitionState(GroundedState);

        var xrDisplaySubsystems = new List<XRDisplaySubsystem>();
        SubsystemManager.GetInstances<XRDisplaySubsystem>(xrDisplaySubsystems);
        mouseLook = !xrDisplaySubsystems.Exists(x => x.running);

        teleportCheck = transform.position;
    }
    private void Update()
    {
        GetInput();
        if (mouseLook)
            DealMouseLook();
    }
    private void LateUpdate()
    {
        LateUpdateState();
    }
    Vector3 teleportCheck;
    private void FixedUpdate()
    {
        if (Vector3.Distance(transform.position, teleportCheck) > .0001f)
            _OnTeleport();
        transportCD = Mathf.Max(0, transportCD - Time.fixedDeltaTime);
        UpdateState(Time.fixedDeltaTime);
        teleportCheck = transform.position;
    }
    void _OnTeleport()
    {
        //Debug.Log("Player Teleported");
        onTeleport.Invoke();
    }
    void _OnEnforcedMovement()
    {
        onSicknessMovement.Invoke();
    }
    public void SetTeleportMode(bool enableTeleportMode) { teleportMode = enableTeleportMode; }

        //different amount of fixedupdate will be executed between screen renders. Which result in Stuttering rotation. Smooth it

    #region Grounded and Falling
    void UpdateGrounded(float dt)
    {
        if (teleportMode == true) { TransitionState(TeleportModeState); return; }

        //enforced movement from attached
        Vector3 attachedDelta = GetAttachedTranslation();
        transform.position += attachedDelta;

        //height and rotation
        AdjustColliderAndHead();
        DealInputRotation();
        DealTrackedRotation();

        //Head Movement
        Vector3 trackedDelta = Vector3.ProjectOnPlane(head.position - transform.position, up);
        if (trackedDelta.magnitude > headTeleportDistance)
        {
            trackingSpace.position -= trackedDelta;
            trackedDelta = Vector3.zero;
        }

        //Input Movement
        Vector3 inputVectorW = Vector3.ProjectOnPlane(inputRef.TransformDirection(new Vector3(inputStick.x, 0, inputStick.y)), Vector3.up).normalized * inputStick.magnitude;
        float curSpeed = inputDash ? dashSpeed : speed;
        Vector3 inputDelta = inputVectorW * curSpeed * scale * dt;

        //Start Moving Character
        Vector3 delta = inputDelta + trackedDelta;

        //Check Next Step
        delta = ProjectStepOnGround(delta, out bool isNextStepHit);


        //Check Falling
        bool isThisStep = PhysicsEX.SphereCast(TP(0, .9f * R + stepHeight, 0),
            .9f * R * scale, TD(0, -1, 0), out RaycastHit groundHit, 2 * stepHeight * scale, groundLayers,QueryTriggerInteraction.Ignore);
        float groundDist = isThisStep ? groundHit.distance - stepHeight * scale : float.PositiveInfinity;
        if (!isThisStep)
        {
            fallingVelocity = delta / dt;
            TransitionState(FallingState);
        }
        else
        {
            fallingVelocityReference = Vector3.Lerp(fallingVelocityReference, attachedDelta / dt, dt / .2f);

            if (inputJump)
            {
                if (jumpReleased)
                {
                    jumpReleased = false;
                    fallingVelocity = Vector3.ProjectOnPlane(inputDelta, up)/dt + up * jumpSpeed+ fallingVelocityReference;
                    //fallingVelocityReference = attachedDelta / dt;
                    TransitionState(FallingState);
                }
            }
            else
                jumpReleased = true;
        }
        //Should not introduce falling if the player is not inputting movement
        if (inputDelta.magnitude == 0 && trackedDelta.magnitude < headLeanDistance * scale)
        {
            if (!isNextStepHit)
            {
                delta = trackedDelta = Vector3.zero;
            }
        }

        //Move the Character and camera
        {
            delta = SweepCollider(delta, slide: true);
            transform.position += delta;
            if (inputDelta.magnitude > 0)
            {
                trackingSpace.position -= trackedDelta;
                //Debug.Log("A" + trackedDelta / dt);
            }
            else if (trackedDelta.magnitude > 0)
            {
                float forward = Vector3.Dot(delta, trackedDelta.normalized);
                if (forward < 0) forward = 0;
                trackingSpace.position -= forward * trackedDelta.normalized;
                //Debug.Log("B" + forward * trackedDelta.normalized / dt);
            }
            //if (inputDelta.magnitude > 0) 
            ResolveCollision();
        }

        //Update Attach
        if (isThisStep)
            SetAttach(groundHit.transform, Vector3.zero);
    }
    void UpdateFalling(float dt)
    {
        if (teleportMode == true) { TransitionState(TeleportModeState); return; }

        //height and rotation
        AdjustColliderAndHead();
        DealInputRotation();
        DealTrackedRotation();

        //Head Movement
        Vector3 trackedDelta = Vector3.ProjectOnPlane(head.position - transform.position, up);
        if (trackedDelta.magnitude > headTeleportDistance)
        {
            trackingSpace.position -= trackedDelta;
            trackedDelta = Vector3.zero;
        }

        //Input Movement
        Vector3 inputVectorW = Vector3.ProjectOnPlane(inputRef.TransformDirection(new Vector3(inputStick.x, 0, inputStick.y)), Vector3.up).normalized * inputStick.magnitude;
        float curSpeed = inputDash ? dashSpeed : speed;
        Vector3 inputDelta = inputVectorW * curSpeed * scale * dt;

        //Start Moving Character
        fallingVelocity += Physics.gravity * dt;
        fallingVelocity = Vector3.Dot(fallingVelocity-fallingVelocityReference, up) * up+fallingVelocityReference + (inputDelta + trackedDelta) / dt;
        Vector3 delta = fallingVelocity * dt;

        //Check Grounded
        bool isThisStep = PhysicsEX.SphereCast(TP(0, .9f * R + stepHeight, 0),
            .9f * R * scale, TD(0, -1, 0), out RaycastHit groundHit, 2 * stepHeight * scale, groundLayers, QueryTriggerInteraction.Ignore);
        float groundDist = isThisStep ? groundHit.distance - stepHeight * scale : float.PositiveInfinity;
        if (isThisStep && groundDist + Vector3.Dot(fallingVelocity * dt, up) <= .1f * R * scale)
            TransitionState(GroundedState);

        //Move the Character and camera
        {
            delta = SweepCollider(delta, slide: true);
            transform.position += delta;
            trackingSpace.position -= trackedDelta;
            //if (inputDelta.magnitude > 0) 
            ResolveCollision();
        }
    }
    void EnterGrounded()
    {
        SetAttach(null, Vector3.zero);
    }
    void ExitGrounded()
    {
        SetAttach(null, Vector3.zero);
    }
    #endregion
    #region Teleport
    void EnterTeleportMode()
    {
        teleportPressed = false;
        SetAttach(null, Vector3.zero);
    }
    void ExitTeleportMode()
    {
        teleportLine.enabled = false;
        SetAttach(null, Vector3.zero);
    }

    void UpdateTeleportMode(float dt)
    {
        if (!teleportMode) { TransitionState(GroundedState); return; }

        //enforced movement from attached
        transform.position += GetAttachedTranslation();

        //height and rotation
        AdjustColliderAndHead();
        DealInputRotation();
        DealTrackedRotation();

        //Head Movement
        Vector3 trackedDelta = Vector3.ProjectOnPlane(head.position - transform.position, up);
        if (trackedDelta.magnitude > headTeleportDistance)
        {
            trackingSpace.position -= trackedDelta;
            trackedDelta = Vector3.zero;
        }

        //Snap Input
        Vector3 inputVector = Vector3.zero;
        if (inputStick.x > .5f && Mathf.Abs(inputStick.x) > Mathf.Abs(inputStick.y)) inputVector.x += 1;
        if (inputStick.x < -.5f && Mathf.Abs(inputStick.x) > Mathf.Abs(inputStick.y)) inputVector.x -= 1;
        if (inputStick.y < -.5f && Mathf.Abs(inputStick.y) > Mathf.Abs(inputStick.x)) inputVector.z -= 1;
        if (inputStick.y > .5f && Mathf.Abs(inputStick.y) > Mathf.Abs(inputStick.x)) inputVector = Vector3.zero;

        Vector3 stepDelta = Vector3.zero;
        if (inputVector.magnitude > 0)
        {
            if (transportCD <= 0)
            {

                stepDelta = transform.rotation * inputVector * stepDistance * scale;
                transportCD = stepTime;
                lastTransportAction = 2;
            }
        }
        else if (lastTransportAction == 2)
        {
            transportCD = 0;
            lastTransportAction = -1;
        }

        //Start Moving Character
        Vector3 delta = trackedDelta + stepDelta;
        delta = ProjectStepOnGround(delta, out bool isNextStepHit);
        if (!isNextStepHit)
        {
            delta = Vector3.zero;
            trackedDelta = Vector3.zero;
        }

        //Move the Character and camera
        {
            delta = SweepCollider(delta, slide: true);
            transform.position += delta;
            trackingSpace.position -= stepDelta.magnitude > 0 ? trackedDelta : Vector3.ProjectOnPlane(delta, up);
            var enforcedDelta = ResolveCollision();
            if (enforcedDelta.magnitude / scale > .005f)
                _OnEnforcedMovement();
        }
        if (stepDelta.magnitude > 0) _OnTeleport();

        //Process Teleport
        if (inputStick.y > .5f && Mathf.Abs(inputStick.y) > Mathf.Abs(inputStick.x))
        {
            teleportPressed = true;
        }
        else
        {
            if (teleportPressed)
            {
                if (teleportLine.intersectPointFound)
                    TeleportTo(teleportLine.intersectPoint);
            }
            teleportPressed = false;
        }
        if (teleportLine.enabled != teleportPressed)
            teleportLine.enabled = teleportPressed;
        teleportLine.environmentLayers = environmentLayers;
        teleportLine.groundLayers = groundLayers;
        teleportLine.inputRef = inputRef;
        teleportLine.CheckTeleportPoint = CheckTeleportTarget;


        //Update Attach
        bool isThisStep = PhysicsEX.SphereCast(TP(0, .9f * R + stepHeight, 0),
            .9f * R * scale, TD(0, -1, 0), out RaycastHit groundHit, 2 * stepHeight * scale, groundLayers, QueryTriggerInteraction.Ignore);
        if (isThisStep)
            SetAttach(groundHit.transform, Vector3.zero);

    }
    bool CheckTeleportTarget(RaycastHit hitInfo)
    {
        Vector3 targetPoint = hitInfo.point;
        RaycastHit hitInfo1;
        //if (Physics.SphereCast(targetPoint + TV(0, R, 0), .9f * R * scale, transform.up, out hitInfo1, (H - 1.8f * R) * scale, environmentLayers,QueryTriggerInteraction.Ignore))
        if (Physics.OverlapCapsuleNonAlloc(targetPoint + TV(0, R, 0), targetPoint + TV(0, H - 1.8f * R, 0), .9f * R * scale, colliderBuffer, environmentLayers, QueryTriggerInteraction.Ignore) > 0)
            return false;
        if (!Physics.SphereCast(targetPoint + TV(0, 1.4f * R, 0), .9f * R * scale, -transform.up, out hitInfo1, 1f * R * scale, environmentLayers, QueryTriggerInteraction.Ignore))
            return false;
        if ((groundLayers & (1 << hitInfo1.collider.gameObject.layer)) == 0)
            return false;
        if (hitInfo1.normal.y < Mathf.Cos(slopeLimit * Mathf.Deg2Rad))
            return false;
        return true;
    }
    void TeleportTo(Vector3 targetPoint)
    {
        //Vector3 oldFoot = head.position - Vector3.Dot(head.position - transform.position, Vector3.up) * Vector3.up;
        Vector3 oldFoot = transform.position;
        transform.position += targetPoint - oldFoot;
        _OnTeleport();
    }
    #endregion
    #region Seated
    float seatHeadBias;
    public void Sit(Transform seat)
    {
        if (currentState == SeatedState)
            TransitionState(GroundedState);
        //onLeaveSeat.Invoke();

        if (!isActiveAndEnabled) return;
        seatTransform = seat;
        float trackedHeadHeight = trackingSpace.InverseTransformPoint(head.position).y + trackedHeightBias;
        trackedHeadHeight = Mathf.Clamp(trackedHeadHeight, minHeadHeight, maxHeadHeight);
        seatHeadBias = -(trackedHeadHeight - expectedPlayerHeight *.4f);

        transform.rotation = seatTransform.rotation;
        transform.position = seatTransform.position + trackingSpace.TransformVector(Vector3.up * seatHeadBias);
        if (attachedBody)
        {
            //fast moving vehicles, predict the position after the physics frame
            transform.position += attachedBody.velocity * Time.fixedDeltaTime;
        }
        _OnTeleport();
        TransitionState(SeatedState);
    }
    public void LeaveSeat()
    {
        if (!this || !isActiveAndEnabled) return;
        if(currentState==SeatedState)
            TransitionState(GroundedState);
    }
    Transform seatTransform;
    void EnterSeated()
    {
        SetAttach(seatTransform, Vector3.zero);
        lastRotate = seatTransform.rotation;
    }
    Quaternion lastRotate;
    void LateUpdateSeated()
    {
        //Fixed the sluttering to the environment, but get sluttering to the vehicle :(
        //lastRotate = transform.rotation = Quaternion.Slerp(lastRotate, transform.rotation, Time.deltaTime / rotateSmoothTime);
    }
    void UpdateSeated(float dt)
    {
        if (seatTransform == null)
        {
            LeaveSeat(); return;
        }
        if(inputJump)
        {
            LeaveSeat(); return;
        }
        GetAttachedTranslation(dt);

        //different amount of fixedupdate will be executed between screen renders. Which result in jiggy rotation. Smooth it
        //transform.rotation = Quaternion.Slerp(transform.rotation, seatTransform.rotation, Time.fixedDeltaTime / seatRotateSmoothTime);
        Vector3 delta = seatTransform.position + trackingSpace.TransformVector(Vector3.up * seatHeadBias) - transform.position;

        transform.rotation = seatTransform.rotation;
        transform.position = seatTransform.position + trackingSpace.TransformVector(Vector3.up * seatHeadBias);
        if (attachedBody) {
            //fast moving vehicles, predict the position after the physics frame
            transform.position += attachedBody.velocity * Time.fixedDeltaTime;
            delta+= attachedBody.velocity * Time.fixedDeltaTime;
        }

        fallingVelocityReference = Vector3.Lerp(fallingVelocityReference, delta / dt, dt / .2f);
    }
    void ExitSeated()
    {
        SetAttach(null, Vector3.zero);
        onLeaveSeat.Invoke();
        AlignRotationToGravity();
    }
    void AlignRotationToGravity()
    {
        Vector3 gup = -Physics.gravity.normalized;if (gup.magnitude == 0) gup = Vector3.up;
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, gup).normalized;if (forward.magnitude == 0) forward = -Vector3.ProjectOnPlane(transform.up, gup).normalized;
        transform.LookAt(transform.position+forward, gup);
    }
    #endregion
    #region Input and XR Input
    public void SetInputStick(Vector2 value) => inputStick = value;
    public void SetInputStick2(Vector2 value) => inputStick2 = value;

    void GetInput()
    {
        inputStick = Vector3.zero;
        inputStick2 = Vector3.zero;
        inputDash = false;
        inputJump = false;
        inputTurn = 0;
        if (!InputOverriden)
        {
            //TODO
            inputStick = Vector2.ClampMagnitude(new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")), 1);
            inputStick2 = new Vector2(Input.GetAxis("XR Look"), Input.GetAxis("XR Jump"));
            inputDash = Input.GetButton("Dash");
            inputJump = (inputStick2.y > .5f && inputStick2.y > Mathf.Abs(inputStick2.x)) || Input.GetButton("Jump");
            if (Mathf.Abs(inputStick2.x) > .5f && Mathf.Abs(inputStick2.x) > Mathf.Abs(inputStick2.y))
                inputTurn = inputStick2.x > 0 ? 1 : -1;
        }
    }
    void DealMouseLook()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(2))
            Cursor.lockState = CursorLockMode.Locked;
        if (Input.GetKeyDown(KeyCode.Escape))
            Cursor.lockState = CursorLockMode.None;

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            float rotationDX = Input.GetAxis("Mouse X") * 6;
            float rotationX = head.localEulerAngles.y+ rotationDX;//Must in Update
            float rotationY = head.localEulerAngles.x - Input.GetAxis("Mouse Y") * 6;
            if (rotationY > 180) rotationY -= 360;
            rotationY = Mathf.Clamp(rotationY, -80, 80);
            if (rotationX > 180) rotationX -= 360;
            if (currentState != SeatedState)
            {
                head.localEulerAngles = new Vector3(rotationY, 0, 0);
                transform.Rotate(Vector3.up, rotationDX, Space.Self);
                leftHand.localEulerAngles = new Vector3(rotationY, 0, 0);
                rightHand.localEulerAngles = new Vector3(rotationY, 0, 0);
            }
            else
            {
                head.localEulerAngles = new Vector3(rotationY, rotationX, 0);
                leftHand.localEulerAngles = new Vector3(rotationY, 0, 0);
                rightHand.localEulerAngles = new Vector3(rotationY, 0, 0);
            }

        }
    }
    void DealInputRotation()
    {
        //Input Rotation
        if (inputTurn != 0)
        {
            if (transportCD <= 0)
            {
                lastTransportAction = 1;
                transportCD = rotateSnapTime;
                transform.rotation = transform.rotation * Quaternion.AngleAxis(rotateSnap * inputTurn, Vector3.up);
                _OnTeleport();
            }
        }
        else if (lastTransportAction == 1)
        {
            transportCD = 0;
            lastTransportAction = -1;
        }
    }
    void DealTrackedRotation()
    {
        //Head Rotation
        Vector3 lookForward = Vector3.ProjectOnPlane(head.forward, up).normalized;
        if (lookForward.magnitude > 0)
        {
            float angle = Vector3.SignedAngle(transform.forward, lookForward, transform.up);
            transform.RotateAround(head.position, transform.up, angle);
            trackingSpace.RotateAround(head.position, transform.up, -angle);
        }
    }
    void AdjustColliderAndHead()
    {
        //Setup Collider
        float trackedHeadHeight = trackingSpace.InverseTransformPoint(head.position).y + trackedHeightBias;
        trackedHeadHeight = Mathf.Clamp(trackedHeadHeight, minHeadHeight, maxHeadHeight);
        //H=h+R; dst=H-2R=h-R
        bool isHeadHit = PhysicsEX.SphereCast(transform.position + Vector3.ProjectOnPlane(head.position - transform.position, up) + TV(0, R, 0),
            R * .9f, up, out RaycastHit headHit, Mathf.Max(0, trackedHeadHeight - R) * scale, environmentLayers, QueryTriggerInteraction.Ignore);
        float allowedHeadHeight = isHeadHit ? headHit.distance / scale + R : trackedHeadHeight;
        if (allowedHeadHeight < minHeadHeight) allowedHeadHeight = minHeadHeight;

        float oldHeadHeight = capsuleCollider.height - R * 1.1f;
        float headHeight = isHeadHit ? Mathf.Max(oldHeadHeight, allowedHeadHeight) : allowedHeadHeight;

        capsuleCollider.height = headHeight + R * 1.1f;
        capsuleCollider.center = new Vector3(0, capsuleCollider.height / 2, 0);
        /*
        if (isHeadHit && headHit.distance / scale + R >= minHeadHeight)
            capsuleCollider.height = headHit.distance / scale + 2.1f * R;
        else
            capsuleCollider.height = trackedHeadHeight + 1.1f * R;
        capsuleCollider.center = new Vector3(0, capsuleCollider.height / 2, 0);*/

        //Setup Camera Height
        //float deltaY = Vector3.Dot(TP(0, capsuleCollider.height - R, 0) - head.position, up);
        //trackingSpace.position +=deltaY * up;
    }
    #endregion
    #region Attach
    Transform attached;
    Rigidbody attachedBody;
    Vector3 attachAnchorPositionLS = Vector3.zero, attachedPositionLS;
    Vector3 attachedVelocity = Vector3.zero;
    //TODO Attach
    public Vector3 GetAttachedTranslation(float dt = 0, float smoothTime = .1f)
    {
        if (attached)
        {
            Vector3 translation = attached.TransformPoint(attachedPositionLS) - transform.TransformPoint(attachAnchorPositionLS);
            if (attachedBody) translation += attachedBody.velocity * Time.fixedDeltaTime;//fast moving vehicles, predict the position after the physics frame
            if (dt > 0)
                attachedVelocity = Vector3.Lerp(attachedVelocity, translation / dt, dt / smoothTime);
            return translation;
        }
        else return Vector3.zero;
    }
    void SetAttach(Transform attached, Vector3 attachAnchorPositionLS)
    {
        this.attached = attached;
        this.attachAnchorPositionLS = attachAnchorPositionLS;
        if (attached) attachedBody = attached.GetComponentInParent<Rigidbody>();
        if (attachedBody && attachedBody.isKinematic && attachedBody.transform.parent)
        {
            var parentBody = attachedBody.transform.parent.GetComponentInParent<Rigidbody>();
            if (parentBody) attachedBody = parentBody;
        }
        Vector3 attachedPointWS = transform.TransformPoint(attachAnchorPositionLS);
        if (attachedBody) attachedPointWS -= attachedBody.velocity * Time.fixedDeltaTime;//Compensate the correlation in GetAttachedTranslation
        if (attached) attachedPositionLS = attached.InverseTransformPoint(attachedPointWS);
        updateAttach.Invoke(this.attached);
    }
    void SetAttach(Transform attached, Vector3 attachedPositionLS, Vector3 attachAnchorPositionLS)
    {
        SetAttach(attached, attachedPositionLS);
        this.attachAnchorPositionLS = attachAnchorPositionLS;
    }
    #endregion
    #region CapsuleCollider
    CapsuleCollider capsuleCollider;
    float R => capsuleCollider.radius;
    float H => capsuleCollider.height;
    float scale => transform.lossyScale.x;
    Vector3 up => transform.up;
    Vector3 GravityUp => transform.up;
    Vector3 TP(float x, float y, float z) => transform.TransformPoint(new Vector3(x, y, z));
    Vector3 TV(float x, float y, float z) => transform.TransformVector(new Vector3(x, y, z));
    Vector3 TD(float x, float y, float z) => transform.TransformDirection(new Vector3(x, y, z));
    Vector3 P1 => TP(0, R, 0);
    Vector3 P2 => TP(0, H - R, 0);
    Vector3 ProjectStepOnGround(Vector3 delta, out bool isNextStepHit)
    {
        float stepDist = Mathf.Max(delta.magnitude, R * scale);
        isNextStepHit = PhysicsEX.SphereCast(TP(0, .9f * R + stepHeight, 0) + delta.normalized * Mathf.Max(.9f * R * scale, delta.magnitude),
            .9f * R * scale, TD(0, -1, 0), out RaycastHit nextStepHit, 2 * stepHeight * scale, groundLayers, QueryTriggerInteraction.Ignore);
        if (isNextStepHit)
        {
            Vector3 nextStep = TP(0, .9f * R + stepHeight, 0) + delta.normalized * Mathf.Max(.9f * R * scale, delta.magnitude) + nextStepHit.distance * TD(0, -1, 0) + TV(0, -.9f * R, 0);
            if (Vector3.Dot(nextStepHit.point - transform.position, GravityUp) < Mathf.Sin(slopeLimit * Mathf.Deg2Rad))
                delta = (nextStep - transform.position).normalized * delta.magnitude;
            else
                delta = Vector3.zero;
        }
        return delta;
    }
    Vector3 SweepCollider(Vector3 delta, bool slide)
    {
        //Sweep
        bool isHit = PhysicsEX.CapsuleCast(P1, P2, .9f * R * scale, delta.normalized, out RaycastHit hit, delta.magnitude + .1f * R * scale, environmentLayers, QueryTriggerInteraction.Ignore);
        if (isHit)
        {
            Vector3 delta1 = delta.normalized * Mathf.Clamp(hit.distance - .2f * R * scale, 0, delta.magnitude);//Skinning is needed 
            //Additional Skinning to fix collider default skinning, otherwise will result to sliding when head-leaning
            if (slide)
                return delta1;
            else
            {
                Vector3 delta2 = Vector3.ProjectOnPlane(delta - delta1, hit.normal);
                bool isHit2 = PhysicsEX.CapsuleCast(P1 + delta1, P2 + delta1, .9f * R, delta2.normalized, out RaycastHit hit2, delta2.magnitude + .1f * R * scale, environmentLayers, QueryTriggerInteraction.Ignore);
                if (isHit2)
                {
                    return delta1 + delta2.normalized * Mathf.Clamp(hit2.distance - .1f * R * scale, 0, delta2.magnitude);
                }
                else
                {
                    return delta1 + delta2;
                }
            }
        }
        else
            return delta;
    }
    Vector3 ResolveCollision(bool resolveHead = true)
    {
        Vector3 totalMoved = Vector3.zero;
        //Head Collision Resolving(?)
        int overlapCount; bool tmp;
        if (resolveHead)
        {
            overlapCount = Physics.OverlapSphereNonAlloc(headCollider.transform.position, headCollider.radius * scale, colliderBuffer, environmentLayers, QueryTriggerInteraction.Ignore);
            tmp = headCollider.enabled;
            headCollider.enabled = true;
            for (int i = 0; i < overlapCount; ++i)
            {
                var c = colliderBuffer[i];
                if (c == capsuleCollider || c == headCollider) continue;
                if (Physics.ComputePenetration(headCollider, headCollider.transform.position, headCollider.transform.rotation, c, c.transform.position, c.transform.rotation,
                    out Vector3 resolveDir, out float resolveDist))
                {
                    transform.position += resolveDir * resolveDist;
                    totalMoved += resolveDir * resolveDist;
                }
            }
            headCollider.enabled = tmp;
        }

        //Body Collision Resolving
        overlapCount = Physics.OverlapCapsuleNonAlloc(P1, P2, R * scale * 1.1f, colliderBuffer, environmentLayers, QueryTriggerInteraction.Ignore);
        tmp = capsuleCollider.enabled;
        capsuleCollider.enabled = true;
        for (int i = 0; i < overlapCount; ++i)
        {
            var c = colliderBuffer[i];
            if (c == capsuleCollider || c == headCollider) continue;
            if (Physics.ComputePenetration(capsuleCollider, capsuleCollider.transform.position, capsuleCollider.transform.rotation, c, c.transform.position, c.transform.rotation,
                out Vector3 resolveDir, out float resolveDist))
            {
                transform.position += resolveDir * resolveDist;
                totalMoved += resolveDir * resolveDist;
            }
        }
        capsuleCollider.enabled = tmp;
        return totalMoved;
    }
    Collider[] colliderBuffer = new Collider[20];
    #endregion
    #region StateMachine
    protected delegate void StateEvent();
    protected delegate void StateUpdate(float dt);
    protected class State
    {
        public State(string name) { this.name = name; }
        public string name;
        public float time;
        public bool isFirstUpdate;
        public StateEvent OnEnter, OnExit, LateUpdate;
        public StateUpdate Update;
    }
    private State _currentState;
    protected State currentState => _currentState;
    void TransitionState(State newState, bool triggerEventsIfEnterSame=true)
    {
        if (newState == _currentState && !triggerEventsIfEnterSame) return;
        _currentState?.OnExit?.Invoke();
        newState.time = 0; newState.isFirstUpdate = true;
        newState.OnEnter?.Invoke();
        _currentState = newState;
#if UNITY_EDITOR
        debug_Statename = _currentState.name;
#endif
    }
    void UpdateState(float dt)
    {
        _currentState.time += dt; _currentState.isFirstUpdate = false;
        _currentState.Update?.Invoke(dt);
    }
    void LateUpdateState()
    {
        _currentState.LateUpdate?.Invoke();
    }
#if UNITY_EDITOR
    [SerializeField] private string debug_Statename;
#endif
    #endregion    
    static Quaternion GetRotationQuaternion(Vector3 axis)
    {
        return Quaternion.AngleAxis(axis.magnitude* Mathf.Rad2Deg, axis.normalized);
    }
}
