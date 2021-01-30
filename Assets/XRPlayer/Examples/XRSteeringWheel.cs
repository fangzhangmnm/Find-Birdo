using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class XRSteeringWheel : XRInteractable
{
    public float maxRotation = 170f;
    public Transform pivot, steeringWheel;//, leftRef, rightRef;
    [System.Serializable]public class UpdateFloat : UnityEvent<float> { }
    [System.Serializable] public class UpdateInputs { public UpdateFloat updateSteering, updateThrottle; }
    public UpdateInputs updateInputs;
    public UnityEvent onTakeover;
    XRHand attachedLeftHand, attachedRightHand;
    [ReadOnly] public float currentRotation=0;
    Vector3 leftRef, rightRef;
    
    public override bool CanInteract(XRHand hand, out int priority)
    {
        return CanAttach(hand, out priority);
    }
    public override bool CanAttach(XRHand hand, out int priority)
    {
        priority = 1;
        if (!isActiveAndEnabled) return false;
        return true;
    }
    public override void OnAttach(XRHand emptyHand, Vector3 attachPositionWS, Quaternion attachRotationWS)
    {
        if (emptyHand.whichHand == UnityEngine.XR.XRNode.LeftHand)
        {
            attachedLeftHand = emptyHand;
            leftRef = pivot.InverseTransformPoint(emptyHand.position);
        }
        else if (emptyHand.whichHand == UnityEngine.XR.XRNode.RightHand)
        {
            attachedRightHand = emptyHand;
            rightRef = pivot.InverseTransformPoint(emptyHand.position);
        }
        onTakeover.Invoke();
    }
    public override void OnDetach(XRHand handAttachedMe)
    {
        if (handAttachedMe.whichHand == UnityEngine.XR.XRNode.LeftHand)
            attachedLeftHand = null;
        else if (handAttachedMe.whichHand == UnityEngine.XR.XRNode.RightHand)
            attachedRightHand = null;
    }
    private void FixedUpdate()
    {
        float totalRotation = 0, totalWeight = 0;
        if (attachedLeftHand)
        {
            Vector3 p1 = pivot.InverseTransformPoint(attachedLeftHand.position);
            Vector3 p2 = leftRef;
            p1.z = 0;p2.z = 0;
            if (p1.sqrMagnitude > 0 && p2.sqrMagnitude>0)
            {

                totalRotation += Vector3.SignedAngle(p2, p1, Vector3.forward);
                totalWeight += 1;
            }
            leftRef = p1;
        }
        if (attachedRightHand)
        {
            Vector3 p1 = pivot.InverseTransformPoint(attachedRightHand.position);
            Vector3 p2 = rightRef;
            p1.z = 0; p2.z = 0;
            if (p1.sqrMagnitude > 0 && p2.sqrMagnitude > 0)
            {
                totalRotation += Vector3.SignedAngle(p2, p1, Vector3.forward);
                totalWeight += 1;
            }
            rightRef = p1;
        }
        
        float rotation = totalWeight>0?totalRotation / totalWeight:0;
        currentRotation = Mathf.Clamp(currentRotation + rotation, -maxRotation, maxRotation);
        steeringWheel.localRotation = Quaternion.Euler(0, 0, currentRotation);

        float throttle = 0, brake = 0;
        if(attachedRightHand)
            throttle = attachedRightHand.input_trigger;
        if (attachedLeftHand)
            brake = attachedLeftHand.input_trigger;
        if (brake > .1f) throttle = -brake;
        float steering = Mathf.Clamp(currentRotation / maxRotation, -1, 1);
        updateInputs.updateSteering.Invoke(steering);
        updateInputs.updateThrottle.Invoke(throttle);

        if (attachedLeftHand && attachedLeftHand.input_grip < .5f)
            attachedLeftHand.DetachIfAny();
        if (attachedRightHand && attachedRightHand.input_grip < .5f)
            attachedRightHand.DetachIfAny();
    }
    private void Awake()
    {
        if (outline) outline.enabled = false;
    }
    public override void SetHovering(bool isHovering)
    {
        if (outline) outline.enabled = isHovering;
    }
    private void OnEnable()
    {
        if (outline) outline.enabled = false;
    }
    private void OnDisable()
    {
        if (attachedLeftHand) attachedLeftHand.DetachIfAny();
        if (attachedRightHand) attachedRightHand.DetachIfAny();
        if (outline) outline.enabled = false;
    }
    public Behaviour outline;
}
