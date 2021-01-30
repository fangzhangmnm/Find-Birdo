using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AircraftControlSimple))]
public class AircraftAI : MonoBehaviour
{
    public Transform target;
    public float maxRoll = 35f;
    public float maxClimbAngle = 25f;
    public float maxPitchUp = 25f;
    public float maxPitchDown = 45f;
    public float turnBackYaw = 30f;
    public PIDController rollController, pitchController, yawController;
    public PIDController climbToPitchController, yawToRollController;

    //[ReadOnly]
    public float targetClimbAngle,targetYaw;
    [ReadOnly]
    public float climbAngle,yawSpeed, targetRoll,targetPitch,targetYawSpeed;
    [ReadOnly]
    public Vector3 targetPosition,eulers,output;

    AircraftControlSimple control;
    Rigidbody body;
    private void Start()
    {
        control = GetComponent<AircraftControlSimple>();
        body=GetComponent<Rigidbody>();
    }
    static float SafeAsin(float y,float r)
    {
        return r <= 0 ? 0 : Mathf.Asin(y / r);
    }
    void FixedUpdate()
    {
        eulers=transform.rotation.eulerAngles;
        if(target)targetPosition = Quaternion.Euler(0,-eulers.y,0)*( target.position - transform.position);
        eulers.x = (eulers.x <= 180 ? eulers.x : eulers.x - 360);
        eulers.z = (eulers.z <= 180 ? eulers.z : eulers.z - 360);
        eulers.y = 0;
        yawSpeed = body.angularVelocity.y*Mathf.Rad2Deg;
        climbAngle = SafeAsin(body.velocity.y, body.velocity.magnitude) * Mathf.Rad2Deg;

        if (target) targetYaw = Mathf.Atan2(targetPosition.x, targetPosition.z)*Mathf.Rad2Deg;
        if (target) targetClimbAngle = SafeAsin(targetPosition.y ,  targetPosition.magnitude) * Mathf.Rad2Deg;
        if (targetClimbAngle > maxClimbAngle)
        {
            targetClimbAngle = maxClimbAngle;
            //targetYaw = targetYaw + 90;
        }
        if (Mathf.Abs(targetYaw) > 120)
            targetYaw = turnBackYaw * Mathf.Sign(targetYaw);

        
        targetRoll = -maxRoll * yawToRollController.Step((targetYaw-eulers.y)/90f, Time.fixedDeltaTime);
        targetPitch = -90 * climbToPitchController.Step((targetClimbAngle - climbAngle)/90f, Time.fixedDeltaTime);
        targetPitch = Mathf.Clamp(targetPitch, -maxPitchUp, maxPitchDown);

            
        output.x = pitchController.Step((targetPitch - eulers.x) / 180, Time.fixedDeltaTime);
        output.y = yawController.Step((targetYaw - eulers.y) / 180, Time.fixedDeltaTime);
        output.z = rollController.Step((targetRoll - eulers.z) / 180, Time.fixedDeltaTime);

        if (!control.debug_input)
        {
            control.pitchInput = output.x;
            control.yawInput = output.y;
            control.rollInput = output.z;
            control.throttleInput = 1;
        }
    }
}
