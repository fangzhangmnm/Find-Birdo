using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Racket : MonoBehaviour
{
    public float smoothTime=.1f;
    [HideInInspector] public float smoothedSpeed, speed, smoothedAngularSped, angularSpeed;
    [HideInInspector] public Vector3 smoothedVelocity, smoothedAngularVelocity, smoothedPosition;
    [HideInInspector] public Quaternion smoothedRotation=Quaternion.identity;
    Rigidbody body;
    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        foreach (var c in GetComponentsInChildren<Collider>())
            c.contactOffset = 0.005f;
    }
    private void FixedUpdate()
    {
        smoothedVelocity = Vector3.Lerp(smoothedVelocity, body.velocity, Time.fixedDeltaTime / smoothTime);
        smoothedAngularVelocity = Vector3.Lerp(smoothedAngularVelocity, body.angularVelocity, Time.fixedDeltaTime / smoothTime);
        smoothedPosition = Vector3.Lerp(smoothedPosition, body.position, Time.fixedDeltaTime / smoothTime);
        smoothedRotation = Quaternion.Slerp(smoothedRotation, body.rotation, Time.fixedDeltaTime / smoothTime);
        smoothedSpeed = smoothedVelocity.magnitude;
        speed = body.velocity.magnitude;
        smoothedAngularSped = smoothedAngularVelocity.magnitude;
        angularSpeed = body.angularVelocity.magnitude;
    }
}
