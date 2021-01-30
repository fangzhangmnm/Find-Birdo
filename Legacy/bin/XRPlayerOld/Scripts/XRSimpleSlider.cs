using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class XRSimpleSlider : XRSimpleInteractable
{
    public Transform startRef;
    public Transform endRef;
    public Transform handle;
    public float restoreSpeed = 0;

    public float value;
    XRSimpleHand attachedHand;
    Vector3 attachPositionLS; Quaternion attachRotationLS;

    public override bool CanAttach(XRSimpleHand emptyHand, float dist, out int priority)
    {
        priority = 0;
        return !attachedHand && dist==0;
    }
    public override void OnAttach(XRSimpleHand handAttachedMe, float dist)
    {
        Debug.Assert(!attachedHand);
        attachedHand = handAttachedMe;
        attachPositionLS = Quaternion.Inverse(transform.rotation) * (attachedHand.position - transform.position);
        attachRotationLS = Quaternion.Inverse(transform.rotation) * attachedHand.rotation;
    }
    public override void OnDetach(XRSimpleHand handAttachedMe)
    {
        Debug.Assert(handAttachedMe==attachedHand && handAttachedMe.attached==this);
        attachedHand = null;
    }
    public override void OnGripUp(XRSimpleHand hand)
    {
        base.OnGripUp(hand);
        hand.DetachIfAny();
    }

    public void Update()
    {
        if (attachedHand)
        {
            Quaternion targetRotation = attachedHand.rotation * Quaternion.Inverse(attachRotationLS);
            Vector3 targetPosition = attachedHand.position - targetRotation * (attachPositionLS);
            value = Mathf.Clamp01(Vector3.Dot(targetPosition - startRef.position, (endRef.position - startRef.position)) / (endRef.position - startRef.position).sqrMagnitude);
        }
        else
        {
            value = Mathf.Clamp01(value + restoreSpeed * Time.deltaTime);
        }
        handle.position = Vector3.Lerp(startRef.position, endRef.position, value);
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        if(startRef && endRef)
            Gizmos.DrawLine(startRef.position, endRef.position);
    }
}
