using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Frisbee : MonoBehaviour
{
    public float airDensity = 1.225f;
    public float localRadius = .26f;
    public float CD0 = .08f, CDa = 2.72f, CL0 = .15f, CLa = 1.4f;
    Rigidbody body;
    float radius => localRadius * transform.lossyScale.x;
    private void Awake()
    {
        body = GetComponent<Rigidbody>();
    }
    private void FixedUpdate()
    {
        if (!body.isKinematic && !body.IsSleeping())
        {
            //https://scripts.mit.edu/~womens-ult/frisbee_physics.pdf

            float r = radius;
            Vector3 v = body.velocity, w = body.angularVelocity;
            float aoa = -Mathf.Asin(Mathf.Clamp( Vector3.Dot(v.normalized, transform.up),-1,1));
            float CD = CD0 + CDa * aoa * aoa;//TODO use cos, sin to match the taylor
            float CL = CL0 + CLa * aoa;
            Vector3 drag = -.5f*CD * airDensity * Mathf.PI * r * r * v.magnitude * v;
            drag = Vector3.ClampMagnitude(drag, v.magnitude * body.mass * Time.fixedDeltaTime);//Prevent Jittering
            Vector3 lift = .5f * airDensity * v.sqrMagnitude * Mathf.PI * r * r * CL*transform.up;
            body.AddForce(drag + lift);
        }
    }
    private void OnValidate()
    {
        body = GetComponent<Rigidbody>();
        if (body.drag > .0001f)
        {
            Debug.LogWarning(gameObject.name + ": Rigidbody's builtin  linear drag are disabled.");
            body.drag = .0001f;
        }
    }
}
