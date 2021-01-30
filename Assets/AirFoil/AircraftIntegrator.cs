using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AircraftIntegrator : MonoBehaviour
{
    public float airDensity = 1.225f;
    public float maxSpeed = Mathf.Infinity;
    public float maxDeltaTime = 1 / 500f;
    public float subStep = 10;
    Rigidbody body;
    private Vector3 oldBodyVelocity;
    private Vector3 debug_fix_BodyCOMPosition;
    private Vector3 oldBodyAngularVelocity;
    private Quaternion oldBodyRotation;
    public Transform CenterOfMass;
    public bool calculate_pitch_momentum = true;
    public bool debug_apply_force_torque = true;
    public Vector3 generatedForce, generatedTorque;
    public Vector3 externalForce, externalTorque;
    public Vector3 debug_wind_vector;
    public bool debug_wind_local = false;
    public bool debug_fix_position = false;
    public bool debug_draw_gizmos = false;
    AirFoil[] foils;
    void Start()
    {
        body = GetComponent<Rigidbody>();
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        if (CenterOfMass)
        {
            body.centerOfMass = Quaternion.Inverse(body.rotation) * (CenterOfMass.position - body.position);
            body.ResetInertiaTensor();
        }
        body.drag = .01f;
        body.angularDrag = .1f;
        //body.interpolation = RigidbodyInterpolation.Extrapolate;//works best for vr//No

        oldBodyVelocity = body.velocity;
        debug_fix_BodyCOMPosition = body.position + body.rotation * body.centerOfMass;
        oldBodyAngularVelocity = body.angularVelocity;
        oldBodyRotation = body.rotation;

    }
    private void OnValidate()
    {
        subStep = Mathf.Max(1, Mathf.CeilToInt(Time.fixedDeltaTime / maxDeltaTime));
    }
    void calcAirfoilForce(AirFoil foil, Quaternion bodyWorldRotation,Vector3 bodyWorldVelocity,Vector3 bodyWorldAngularVelocity,Vector3 airVelocity, float airDensity, float maxSpeed, out Vector3 worldForce,out Vector3 worldTorque)
    {
        worldForce = Vector3.zero;
        worldTorque = Vector3.zero;
        foil.angleOfAttack = 0;
        foil.drag = 0;
        foil.lift = 0;
        foil.planarFlowSpeed = 0;
        foil.force = Vector3.zero;


        for (int seg = 0; seg < foil.segmentation; ++seg)
        {
            float segmentDelta = foil.sectionLength * (-.5f + (.5f + seg) / foil.segmentation);
            Quaternion foilWorldRotation = bodyWorldRotation * foil.foilBodyRotation;
            Vector3 foilWorldDeltaPosition = bodyWorldRotation * foil.foilBodyCOMPositionNoScale + foilWorldRotation * Vector3.right * segmentDelta;
            Vector3 foilWorldVelocity = bodyWorldVelocity + Vector3.Cross(bodyWorldAngularVelocity, foilWorldDeltaPosition);

            Vector3 localFlow = Quaternion.Inverse(foilWorldRotation) * (airVelocity - foilWorldVelocity);
            localFlow.x = 0;

            float planarFlowSpeed = localFlow.magnitude;
            float planarFlowSpeedClamped = Mathf.Clamp(planarFlowSpeed, 0, maxSpeed);
            float angleOfAttack = planarFlowSpeed > 0 ? -Mathf.Atan2(-localFlow.y, -localFlow.z) * Mathf.Rad2Deg : 0;


            float coeff = 0.5f * airDensity * planarFlowSpeedClamped * planarFlowSpeedClamped * (foil.chordLength) * (foil.sectionLength/foil.segmentation);

            float dragCoeff = foil.airFoilSettings.dragCoefficient.Evaluate(angleOfAttack);
            float liftCoeff = foil.airFoilSettings.liftCoefficient.Evaluate(angleOfAttack);
            float pitchCoeff = foil.airFoilSettings.pitchCoefficient.Evaluate(angleOfAttack);
            if (foil.isFlap)
            {
                dragCoeff += foil.airFoilSettings.bonusDragWithFlap;
                liftCoeff += foil.airFoilSettings.bonusLiftWithFlap;
            }


            float drag = dragCoeff * coeff;
            float lift = liftCoeff * coeff;
            float torque = pitchCoeff * coeff * foil.chordLength;

            Vector3 segLocalForce = Quaternion.Euler(angleOfAttack, 0, 0) * new Vector3(0, lift, -drag);
            Vector3 segWorldForce = foilWorldRotation * segLocalForce;

            worldForce += segWorldForce;
            worldTorque += Vector3.Cross(foilWorldDeltaPosition, segWorldForce);
            if(calculate_pitch_momentum)
                worldTorque += -foil.transform.right * torque;


            foil.segAngleOfAttack[seg] = angleOfAttack;
            foil.segDrag[seg] = drag;
            foil.segLift[seg] = lift;
            foil.segPlanarFlowSpeed[seg] = planarFlowSpeed;
            foil.segForce[seg] = segWorldForce;

            foil.angleOfAttack += angleOfAttack / foil.segmentation;
            foil.drag += drag;
            foil.lift += lift;
            foil.planarFlowSpeed += planarFlowSpeed / foil.segmentation;
            foil.force += segWorldForce;
        }
    }
    private void FixedUpdate()
    {
        if (body.isKinematic) return;

        foils = GetComponentsInChildren<AirFoil>();//foils may rotate!
        for (int i = 0; i < foils.Length; ++i)
        {
            foils[i].foilBodyCOMPositionNoScale = Quaternion.Inverse(transform.rotation) * (foils[i].transform.position - (transform.position + transform.rotation * body.centerOfMass));
            foils[i].foilBodyRotation = Quaternion.Inverse(transform.rotation) * foils[i].transform.rotation;
        }
        Vector3 totalWorldForce = Vector3.zero;
        Vector3 totalWorldTorque = Vector3.zero;
        if (foils.Length > 0)
        {
            Vector3 bodyCOMWorldPosition = body.position + body.rotation * body.centerOfMass;


            Quaternion subBodyRotation = Quaternion.AngleAxis(-body.angularVelocity.magnitude * Mathf.Rad2Deg * Time.fixedDeltaTime, body.angularVelocity.normalized) * body.rotation;
            Vector3 subBodyAngularVelocity = oldBodyAngularVelocity;
            Vector3 subBodyVelocity = oldBodyVelocity;

            externalForce = (body.velocity - oldBodyVelocity) / Time.fixedDeltaTime*body.mass;
            externalTorque = (body.angularVelocity - oldBodyAngularVelocity) / Time.fixedDeltaTime;
            externalTorque = Quaternion.Inverse(subBodyRotation * body.inertiaTensorRotation) * externalTorque;
            externalTorque.x *= body.inertiaTensor.x;
            externalTorque.y *= body.inertiaTensor.y;
            externalTorque.z *= body.inertiaTensor.z;
            externalTorque = subBodyRotation * body.inertiaTensorRotation * externalTorque;
            
            for (int step = 0; step < subStep; ++step)
            {
                float dt = Time.fixedDeltaTime / subStep;

                totalWorldForce = externalForce;
                totalWorldTorque = externalTorque;

                for (int i = 0; i < foils.Length; ++i)
                    if (foils[i].isActiveAndEnabled)
                    {
                        Vector3 foilWorldPosition = bodyCOMWorldPosition + subBodyRotation * foils[i].foilBodyCOMPositionNoScale;
                        Vector3 airVelocity = debug_wind_local? subBodyRotation*debug_wind_vector : debug_wind_vector;

                        Vector3 worldForce, worldTorque;
                        calcAirfoilForce(foil: foils[i],
                            bodyWorldRotation: subBodyRotation,
                            bodyWorldVelocity: subBodyVelocity,
                            bodyWorldAngularVelocity: subBodyAngularVelocity,
                            airVelocity: airVelocity,
                            airDensity: airDensity,
                            maxSpeed: maxSpeed,
                            worldForce: out worldForce,
                            worldTorque: out worldTorque
                            );
                        if (debug_apply_force_torque)
                        {

                            totalWorldForce += worldForce;
                            totalWorldTorque += worldTorque;
                        }
                    }

                Vector3 worldAngularAcceleration = Quaternion.Inverse(subBodyRotation * body.inertiaTensorRotation) * totalWorldTorque;
                worldAngularAcceleration.x = body.inertiaTensor.x == 0 ? 0 : worldAngularAcceleration.x / body.inertiaTensor.x;
                worldAngularAcceleration.y = body.inertiaTensor.y == 0 ? 0 : worldAngularAcceleration.y / body.inertiaTensor.y;
                worldAngularAcceleration.z = body.inertiaTensor.z == 0 ? 0 : worldAngularAcceleration.z / body.inertiaTensor.z;
                worldAngularAcceleration = subBodyRotation * body.inertiaTensorRotation * worldAngularAcceleration;

                subBodyRotation = Quaternion.AngleAxis(subBodyAngularVelocity.magnitude * dt * Mathf.Rad2Deg, subBodyAngularVelocity.normalized) * subBodyRotation;
                subBodyAngularVelocity += worldAngularAcceleration * dt;
                subBodyVelocity += totalWorldForce * dt / body.mass;
            }

            //body.rotation = subBodyRotation;//This will lead instability
            //if (subBodyAngularVelocity.magnitude < .1f*Time.fixedDeltaTime)
            //    subBodyAngularVelocity = Vector3.zero;
            //if (subBodyVelocity.magnitude < .1f * Time.fixedDeltaTime)
            //    subBodyVelocity = Vector3.zero;
            body.angularVelocity = subBodyAngularVelocity;
            body.velocity = subBodyVelocity;
            if (debug_fix_position)
                transform.position = debug_fix_BodyCOMPosition-body.rotation*body.centerOfMass-Time.fixedDeltaTime*body.velocity;
        }//if (foils.Length > 0)
        oldBodyRotation = body.rotation;
        //oldBodyCOMPosition = body.position + body.rotation * body.centerOfMass;
        oldBodyAngularVelocity = body.angularVelocity;
        oldBodyVelocity = body.velocity;
        generatedForce = totalWorldForce;
        generatedTorque = totalWorldTorque;

    }
    private void OnDrawGizmos()
    {
        if (body && debug_draw_gizmos)
        {
            var foils = GetComponentsInChildren<AirFoil>();
            foreach (var foil in foils)
                if(foil.isActiveAndEnabled)
                {
                    for(int seg = 0; seg < foil.segmentation; ++seg)
                    {

                        float segmentDelta = foil.sectionLength * (-.5f + (.5f + seg) / foil.segmentation);
                        Vector3 p = foil.transform.position + foil.transform.right * segmentDelta;
                        var q = Quaternion.AngleAxis(foil.segAngleOfAttack[seg], foil.transform.right);
                        Gizmos.color = Mathf.Abs(foil.segAngleOfAttack[seg])>foil.airFoilSettings.stallWarning ?Color.red: Color.blue;
                        Gizmos.DrawLine(p, p + q * (foil.transform.up * foil.segLift[seg] / body.mass));
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawLine(p, p - q * (foil.transform.forward * foil.segDrag[seg] / body.mass));
                    }
                }
            
        }

    }
}
