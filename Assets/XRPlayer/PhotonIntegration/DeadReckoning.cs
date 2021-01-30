using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeadReckoningVector
{
    Vector3 shadowPos, currentPos, oldPos;
    public Vector3 velocity;
    public Vector3 lastVelocity,decayedVelocity,lastPosition;
    bool isReset = true;
    public bool isReady => !isReset;
    public float velocitySmoothTime = .300f;
    public float followTime = .100f;//Typical Send Interval
    public void FlagReset() { isReset = true; }
    public void NetworkUpdate(Vector3 newPosition, Vector3 newVelocity, float lag, bool isReset)
    {
        if (this.isReset)
            isReset = true;
        this.isReset = false;
        lastVelocity= decayedVelocity = newVelocity;
        lastPosition = newPosition;
        //velocity = Mathf.Exp(-lag / velocitySmoothTime) * newVelocity;
        shadowPos = newPosition + lag * velocity;
        if (isReset) currentPos = shadowPos;
    }
    public Vector3 Update(float dt)
    {
        Debug.Assert(isReady);

        decayedVelocity = Mathf.Max(0, 1 - dt / velocitySmoothTime) * decayedVelocity;
        velocity = Vector3.Lerp(velocity, decayedVelocity, dt / velocitySmoothTime);
        shadowPos = dt * velocity + shadowPos;
        currentPos = Vector3.Lerp(currentPos, shadowPos, dt / followTime);
        return currentPos;
    }
    public Vector3 EstimateVelocity(Vector3 newPosition, float dt, bool isReset)
    {
        if (isReset) oldPos = newPosition;
        Vector3 velocity = (newPosition - oldPos) / dt;
        oldPos = newPosition;
        return velocity;
    }
}
public class DeadReckoningQuaternion
{
    Quaternion shadowRot, currentRot, oldRotation;
    public Vector3 angularVelocity;
    public Vector3 lastAngularVelocity,decayedAngularVelocity;
    public Quaternion lastRot;
    bool isReset = true;
    public bool isReady => !isReset;
    public float velocitySmoothTime = .300f;
    public float followTime = .100f;//Typical Send Interval
    public void FlagReset() { isReset = true; }
    public void NetworkUpdate(Quaternion newRotation, Vector3 newAngularVelocity, float lag, bool isReset)
    {
        if (this.isReset) 
            isReset = true; 
        this.isReset = false;
        lastAngularVelocity = decayedAngularVelocity= newAngularVelocity;
        lastRot = newRotation;
        //angularVelocity = Mathf.Exp(-lag / velocitySmoothTime) * newAngularVelocity;
        shadowRot = GetRotationQuaternion(lag * angularVelocity) * newRotation;
        if (isReset) currentRot = shadowRot;
    }

    public Quaternion Update(float dt)
    {
        Debug.Assert(isReady);

        decayedAngularVelocity = Mathf.Max(0, 1 - dt / velocitySmoothTime) * decayedAngularVelocity;
        angularVelocity = Vector3.Lerp(angularVelocity, decayedAngularVelocity, dt / velocitySmoothTime);
        shadowRot = GetRotationQuaternion(dt * angularVelocity) * shadowRot;
        currentRot = Quaternion.Slerp(currentRot, shadowRot, dt / followTime);

        return currentRot;
    }
    public Vector3 EstimateVelocity(Quaternion newRotation, float dt, bool isReset)
    {
        if (isReset) oldRotation = newRotation;
        Vector3 angularVelocity = GetRotationVector(oldRotation, newRotation) / dt;
        oldRotation = newRotation;
        return angularVelocity;
    }
    static Vector3 GetRotationVector(Quaternion from, Quaternion to)
    {
        (to * Quaternion.Inverse(from)).ToAngleAxis(out float angle, out Vector3 axis);
        if (angle >= 360) return Vector3.zero;
        if (angle > 180) angle -= 360;
        return angle * Mathf.Deg2Rad * axis;
        //Only (-1,0,0,0) will return NaN
    }
    static Quaternion GetRotationQuaternion(Vector3 axis)
    {
        return Quaternion.AngleAxis(axis.magnitude * Mathf.Rad2Deg, axis.normalized);
    }
    static bool CheckQuaternion(Quaternion q)
    {
        if (q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w < .7f)
        {
            Debug.Log(q);
            return true;
        }
        return false;
    }
}