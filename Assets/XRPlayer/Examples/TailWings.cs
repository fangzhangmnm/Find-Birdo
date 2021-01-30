using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TailWings : MonoBehaviour
{
    Rigidbody body;
    public float airDensity = 1.225f;
    public float CD = .15f,  CLa = 1.4f;
    public float frontAreaLocal = .01f * .01f;
    public float wingAreaLocal = .02f * .05f;
    float frontArea => frontAreaLocal * transform.lossyScale.x * transform.lossyScale.x;
    float wingArea => wingAreaLocal * transform.lossyScale.x * transform.lossyScale.x;
    private void Awake()
    {
        body = GetComponent<Rigidbody>();
    }
    private void FixedUpdate()
    {
        if (!body.isKinematic && !body.IsSleeping())
        {
            Vector3 v = body.velocity, w = body.angularVelocity;
            Vector3 drag = -.5f * CD * airDensity * frontArea * v.magnitude * v;
            drag = Vector3.ClampMagnitude(drag, v.magnitude * body.mass * Time.fixedDeltaTime);//Prevent Jittering
            Vector3 liftTorque = .5f * airDensity * wingArea * v.sqrMagnitude * CLa * Vector3.Cross(transform.forward, v);
            body.AddForce(drag);
            body.AddTorque(liftTorque);
        }
    }
}
