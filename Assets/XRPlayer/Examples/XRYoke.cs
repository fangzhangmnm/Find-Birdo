using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class XRYoke : XRInteractable
{
    public enum Mode { GrabBall,OneHandRotation,Yoke3Dof,Yoke2Dof}
    public Mode mode=Mode.OneHandRotation;
    public bool twoHanded => mode != Mode.OneHandRotation;
    public Transform pivot;
    public Transform yoke;
    [System.Serializable] public class UpdateFloat : UnityEvent<float> { }
    [System.Serializable]public class UpdateCallbacks { public UpdateFloat updatePitch, updateYaw, updateRoll, updateX, updateY, updateZ, updateTrigger; }
    public UpdateCallbacks updateCallbacks;
    public UnityEvent onTakeover;
    public float maxRotation = 45f;
    public float maxTranslation = .25f;
    XRHand hand;
    XRHand hand2;
    Vector3 attachedPositionLS, attached2PositionLS;
    Quaternion attachedRotationLS,attached2RotationLS;
    //Output
    public Vector3 input_pitchyawroll, input_xyz;
    float input_trigger;

    public override bool CanInteract(XRHand hand, out int priority)
    {
        return CanAttach(hand, out priority);
    }
    public override bool CanAttach(XRHand hand, out int priority)
    {
        priority = 1;
        if (!isActiveAndEnabled) return false;
        if (this.hand && !twoHanded) return false;
        return true;
    }
    public override void OnAttach(XRHand emptyHand, Vector3 attachPositionWS, Quaternion attachRotationWS)
    {
        if (hand)
        {
            hand2 = emptyHand;
            attached2PositionLS = yoke.InverseTransformPoint(hand.position);
            attached2RotationLS = Quaternion.Inverse(yoke.rotation) * hand.rotation;
        }
        else
        {
            hand = emptyHand;
            attachedPositionLS = yoke.InverseTransformPoint(hand.position);
            attachedRotationLS = Quaternion.Inverse(yoke.rotation) * hand.rotation;
        }
        onTakeover.Invoke();
    }
    public override void OnDetach(XRHand handAttachedMe)
    {
        if (handAttachedMe == hand2)
        {
            hand2 = null;
        }
        else
        {
            hand = hand2;
            attachedPositionLS = attached2PositionLS;
            attachedRotationLS = attached2RotationLS;
            hand2 = null;
        }
        if (hand == null)
        {
            ResetYoke();
        }
    }
    private void FixedUpdate()
    {
        if (mode == Mode.OneHandRotation)
        {
            DealOneHandRotation();
        }
        updateCallbacks.updatePitch.Invoke(input_pitchyawroll.x);
        updateCallbacks.updateYaw.Invoke(input_pitchyawroll.y);
        updateCallbacks.updateRoll.Invoke(input_pitchyawroll.z);
        updateCallbacks.updateX.Invoke(input_xyz.x);
        updateCallbacks.updateY.Invoke(input_xyz.y);
        updateCallbacks.updateZ.Invoke(input_xyz.z);
        updateCallbacks.updateTrigger.Invoke(input_trigger);


        if (hand && hand.input_grip < .5f)
            hand.DetachIfAny();
        if (hand2 && hand2.input_grip < .5f)
            hand2.DetachIfAny();
    }
    void ResetYoke()
    {
        yoke.localPosition = Vector3.zero;
    }
    void DealOneHandRotation()
    {
        Quaternion desiredRotationWS = yoke.rotation;
        if(hand) desiredRotationWS = hand.rotation * Quaternion.Inverse(attachedRotationLS);
        Quaternion desiredRotationLS = Quaternion.Inverse(pivot.rotation) * desiredRotationWS;
        input_pitchyawroll= LimitRotation(desiredRotationLS, maxRotation, maxRotation, maxRotation)/(maxRotation>0?maxRotation:1);
        input_xyz = Vector3.zero;
        if (hand) input_trigger = hand.input_trigger;
        yoke.localRotation = FromEulerAngle_custom(input_pitchyawroll * maxRotation);
        if (hand) yoke.position = hand.position;
        else yoke.localPosition = Vector3.zero;
    }
    Vector3 ToEulerAngle_custom(Quaternion q)
    {
        //default unity order: xyz
        //let's use zxy
        Vector3 v = new Quaternion(q.z,q.x,q.y,q.w).eulerAngles;
        return new Vector3(v.y, v.z, v.x);
    }
    Quaternion FromEulerAngle_custom(Vector3 v)
    {
        Quaternion q = Quaternion.Euler(new Vector3(v.z, v.x, v.y));
        return new Quaternion(q.y, q.z, q.x, q.w);
    }
    Vector3 LimitRotation(Quaternion q, float maxRoll, float maxPitch, float maxYaw)
    {
        Vector3 v = ToEulerAngle_custom(q);
        if (v.x > 180) v.x -= 360;
        if (v.y > 180) v.y -= 360;
        if (v.z > 180) v.z -= 360;
        v.x = Mathf.Clamp(v.x, -maxPitch, maxPitch);
        v.y = Mathf.Clamp(v.y, -maxYaw, maxYaw);
        v.z = Mathf.Clamp(v.z, -maxRoll, maxRoll);
        return v;
    }




    private void OnEnable()
    {
        if (outline) outline.enabled = false;
    }
    private void OnDisable()
    {
        if (hand) hand.DetachIfAny();
        if (hand2) hand2.DetachIfAny();
        if (outline) outline.enabled = false;

    }
    private void Awake()
    {
        if (outline) outline.enabled = false;
    }
    public override void SetHovering(bool isHovering)
    {
        if (outline) outline.enabled = isHovering;
    }
    public Behaviour outline;
}
