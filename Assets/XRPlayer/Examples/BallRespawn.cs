using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(XRGrabable))]
public class BallRespawn : MonoBehaviour
{
    public XRGrabable ball;
    XRGrabable grabable;
    private void Awake()
    {
        grabable = GetComponent<XRGrabable>();
    }
    private void FixedUpdate()
    {
        if (grabable.hand)
            if (grabable.hand.otherHand.input_grip > .5f && !grabable.hand.otherHand.attached)
                grabable.hand.otherHand.Attach(ball,ball.transform.position,ball.transform.rotation);
    }
    private void OnValidate()
    {
        grabable = GetComponent<XRGrabable>();
        if (grabable.canSwapHand)
            Debug.LogWarning("The Controller should not enable canSwapHand");
    }
}
