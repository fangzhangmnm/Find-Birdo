using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Thruster : MonoBehaviour
{
    //https://wenku.baidu.com/view/f3701926854769eae009581b6bd97f192279bf9f.html
    //https://www.zhihu.com/question/316834572
    //https://www.grc.nasa.gov/WWW/K-12/airplane/propanl.html
    //https://www.grc.nasa.gov/WWW/K-12/airplane/propth.html
    public float airDensity = 1.225f;
    public float diameter = 0.2f;//8 inch
    public float pitch = 0.15f;//6 inch
    public float propellerInertia = 0.01f *0.15f*0.15f / 12;
    public int rotationDirection = 1;
    public float propellerEfficiency = 0.9f;

    public float maxRPM = 6000f;//15 m/s
    public float maxPower = 10f;
    public float maxEngineTorque = float.PositiveInfinity;

    public bool simulationTorque = true;
    public bool throttleControlRPM = false;
    [Range(0, 1)] public float throttle = 0;

    [ReadOnly, SerializeField] private float geometricalMaxSpeed;
    [ReadOnly, SerializeField] private float area;
    [ReadOnly, SerializeField] private float airSpeed;
    [ReadOnly, SerializeField] private float geometricalSpeed;
    [ReadOnly, SerializeField] public float rpm;
    [ReadOnly, SerializeField] public float thrust;
    [ReadOnly, SerializeField] public float engineTorque;
    [ReadOnly, SerializeField] public float dragTorque;
    [ReadOnly, SerializeField] public float precessionTorque;
    [ReadOnly, SerializeField] public float currentPower;
    [ReadOnly, SerializeField] public Vector3 totalTorque;
    

    float getGeometricSpeed(float rpm)
    {
        return rpm / 60 * pitch;
    }
    float getThrust(float rpm,float airSpeed)
    {
        float vp = getGeometricSpeed(rpm);
        return airDensity * area * vp * 2 * (vp - airSpeed);
    }
    float getEnginePower(float rpm,float airSpeed)
    {
        float vp = getGeometricSpeed(rpm);
        return vp * getThrust(rpm, airSpeed)/propellerEfficiency;
    }
    float getEngineTorque(float rpm,float airSpeed)
    {
        float vp = getGeometricSpeed(rpm);
        return vp * (vp - airSpeed) * pitch * airDensity * area / (Mathf.PI * propellerEfficiency);
    }
    float enginePowerToRPM(float power,float airSpeed)
    {
        if (float.IsInfinity(power)) return float.PositiveInfinity;

        float P = propellerEfficiency * power / (2 * airDensity * area);
        float v0 = airSpeed;
        float s332 = Mathf.Sqrt(3) * 3 / 2;
        float v03 = v0 * v0 * v0;
        float b = Mathf.Pow(s332 * Mathf.Sqrt(P * (27 * P + 4 * v03)) + v03 + 27 * P / 2, 1f / 3);
        float vp = (v0 + v0 * v0 / b + b) / 3;
        if (float.IsNaN(vp) || float.IsInfinity(vp)) vp = 0;
        return 60 * vp / pitch;
    }
    float engineTorqueToRPM(float torque,float airSpeed)
    {
        if (float.IsInfinity(torque)) return float.PositiveInfinity;

        float P = Mathf.PI * torque * propellerEfficiency / (pitch * airDensity * area);
        float v0 = airSpeed;
        float vp = (v0 + Mathf.Sqrt(4 * P + v0 * v0)) / 2;
        if (float.IsNaN(vp) || float.IsInfinity(vp)) vp = 0;
        return 60 * vp / pitch;
    }



    Rigidbody body;
    private void Start()
    {
        body = GetComponentInParent<Rigidbody>();
        OnValidate();
    }
    private void OnValidate()
    {
        geometricalMaxSpeed = getGeometricSpeed(maxRPM);
        area = Mathf.PI * diameter * diameter / 4;
    }
    private void FixedUpdate()
    {
        Vector3 pointVelocity = body.GetPointVelocity(transform.position);
        Vector3 airVelocity = Vector3.zero;
        Vector3 localFlow = transform.InverseTransformDirection(airVelocity - pointVelocity);
        airSpeed = -localFlow.z;
        area = Mathf.PI * diameter * diameter / 4;

        float rpm1 = throttleControlRPM ? maxRPM * throttle : maxRPM;
        float rpm2 = throttleControlRPM ? enginePowerToRPM(maxPower,airSpeed) : enginePowerToRPM(maxPower * throttle, airSpeed);
        float rpm3 = engineTorqueToRPM(maxEngineTorque,airSpeed);

        //Debug.Log($"{rpm1}, {rpm2}, {rpm3}");
        rpm = Mathf.Min(rpm1, Mathf.Min(rpm2, rpm3));

        //suppose rpm is large enough so aoa is small enough that outflow speend is just geometricalSpeed
        //also suppose 
        geometricalSpeed = getGeometricSpeed(rpm);
        thrust = getThrust(rpm, airSpeed);
        //thrust can be lesser than zero
        
        dragTorque = getEngineTorque(rpm,airSpeed) * -rotationDirection;
        engineTorque = getEngineTorque(rpm, airSpeed);

        currentPower = getEnginePower(rpm,airSpeed);



        Vector3 angularMomentum = (rpm/30*Mathf.PI) * propellerInertia* rotationDirection* transform.forward;
        Vector3 precessionTorqueVector= -Vector3.Cross(body.angularVelocity, angularMomentum);
        precessionTorque = precessionTorqueVector.magnitude;
        totalTorque = precessionTorqueVector + dragTorque * transform.forward;

        body.AddForceAtPosition(transform.forward * thrust, transform.position);
        if(simulationTorque)
            body.AddTorque(totalTorque);
    }
    private void OnDrawGizmos()
    {
        if (body)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + thrust / body.mass * transform.forward);
            Vector3 v = Quaternion.Inverse(body.rotation * body.inertiaTensorRotation) * totalTorque;
            v.x /= body.inertiaTensor.x;
            v.y /= body.inertiaTensor.y;
            v.z /= body.inertiaTensor.z;
            v = body.rotation * body.inertiaTensorRotation * v;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + v);
        }
    }
}
