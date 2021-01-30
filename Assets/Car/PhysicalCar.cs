//https://www.youtube.com/watch?v=SKXqWcaoTGE&t=1499s

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody))]
public class PhysicalCar : MonoBehaviour
{
    [Header("Car Parameters")]
    public float maxSteering = 30f;
    [ReadOnly, SerializeField] private float minTurnRadius;// <=6
    public float speedLimit = 50f;//112mph
    [ReadOnly] public float idleEngineRPM;
    public float maxEngineRPM = 8000f;
    public float[] gearRatios = { 3.92f,2.14f,1.28f };
    public float finalDriveRatio = 4f;
    public AnimationCurve engineTorqueByRPM;//typical value: 150@0, 270@1750(max torque), 203@4000(max power), 0@8000
    public AnimationCurve idleEngineTorqueByRPM;//typical value: 0@800
    public float enginePowerMultiplier = 1f;
    public float brakeTorquePerWheel = 4000;
    public float dragCoefficientPerMass = 0.0007f;
    public float liftCoefficientPerMass = 0.005f;//typical lift-drag ratio 10
    public float additionalDownForcePerMass = 0f;


    [Header("Car Control System")]
    public bool normalizeSteering = true;
    public bool AutomaticTransmission = true;
    public float AutomaticTransmissionInterval = 1f;
    public float AutoGearUpRPM = 5000;
    public float AutoGearDownRPM = 2000;
    
    public bool ABS = true;
    public bool ESP = false;//It only adds instability when drifting :(
    public float ABSInterval = .5f;
    public float steeringLimit = 2f;
    public bool canTurnAround = true;


    [Header("Car Parts")]
    public Transform centerOfMass;//As low as possible
    public WheelCollider[] steeringWheels;
    public WheelCollider[] drivingWheels;
    public WheelCollider[] handBrakeWheels;
    public WheelCollider[] allWheels;
    Rigidbody body;
    [field:Header("Inputs")]
    [field:SerializeField,Range(-1,1)]
    public float throttleInput { get; set; }
    [field: SerializeField, Range(-1, 1)]
    public float steeringInput { get; set; }
    [field: SerializeField]
    public bool handBrakeInput { get; set; }

    [Serializable] public class UpdateFloat : UnityEvent<float> { }
    [Serializable] public class UpdateString : UnityEvent<string> { }
    [Serializable] public class UpdateOutputs
    {
        public UpdateFloat updateEngineRPM;
        public UpdateFloat updateEngineThrottle;
        public UpdateString updateDebugText;
    }
    [Header("Outputs")]
    public UpdateOutputs updateOutputs;

    [Header("Dev Info")]
    [ReadOnly] public float currentSpeed;
    [ReadOnly] public float currentEngineRPM;
    float currentEngineInputRPM;
    [ReadOnly] public float currentTorquePerWheel;
    [ReadOnly] public float currentThrottle = 0;
    float currentBrake = 0;
    [ReadOnly] public int currentGear = 0;
    [ReadOnly] public bool absActive;
    [ReadOnly] public bool espActive;
    bool[] wheelGrounded;
    float[] forwardSlip;
    float[] sidewaysSlip;
    [ReadOnly] public bool[] isSkid;
    public bool headLamp;
    [ReadOnly] public bool rearLamp;
    private bool[] ABSNoBrake, ESPBrake; private float ABSCD, TransmissionCD;
    [Multiline(3)]
    public string debug_text;

    #region Validate
    [Serializable]public struct CarInfo
    {
        //Use default values for wheelcollider, except doubling the Suspension Spring
        public Vector3 carSize;// 3.8-4.3, 1.6-2
        public float carMass;//1500
        public float wheelRadius;//0.3
        public float maxSpeed;
        public float maxSpeedRPM;
        public float maxSpeedGear;
        public float maxPerWheelTorque;//300
        public float maxTorqueRPM;//4600-6000
        public float maxEnginePower;//1000000
        public float maxPowerRPM;

        public float maxTorqueAcceleration;//3-5
        public float maxTireAcceleration;//9.8
        public float maxTorqueBrakeAcceleration;// 5-7
        public float maxTireBrakeAcceleration;
        public float maxTireTurnAcceleration;
        public float maxTireFullTurnSpeed;
        public float doubleDragSpeed;
        /*
        [ReadOnly] public float maxTorqueSpeed;
        */
    }
    [ReadOnly] public CarInfo devInfo;
    private const float RPMUnit = 2 * 3.14159265f / 60;
    private const float MPHUnit = 0.44704f;

    private void OnValidate()
    {
        body = GetComponent<Rigidbody>();
        body.centerOfMass = transform.InverseTransformPoint(centerOfMass.position);
        //body.interpolation = RigidbodyInterpolation.Interpolate;//For Better VR Performance
        //NO, will cause trouble if you stick to update everything in fixedupdate

        allWheels = GetComponentsInChildren<WheelCollider>();

        if (engineTorqueByRPM == null || engineTorqueByRPM.keys.Length==0)
        {
            engineTorqueByRPM = new AnimationCurve();
            engineTorqueByRPM.AddKey(0, 150);
            engineTorqueByRPM.AddKey(1750, 270);
            engineTorqueByRPM.AddKey(4000, 203);
            engineTorqueByRPM.AddKey(8000, 0);
        }
        if (idleEngineTorqueByRPM == null || idleEngineTorqueByRPM.keys.Length == 0)
        {
            idleEngineTorqueByRPM = new AnimationCurve();
            idleEngineTorqueByRPM.AddKey(0, 50);
            idleEngineTorqueByRPM.AddKey(800, 0);
            idleEngineTorqueByRPM.AddKey(8000, -60);
        }

        float steeringPos = 0, nonSteeringPos = 0;
        foreach (var w in steeringWheels)
        {
            float pos = Vector3.Dot(transform.forward, w.transform.position - centerOfMass.position);
            steeringPos += pos / steeringWheels.Length;
            nonSteeringPos -= pos / (allWheels.Length - steeringWheels.Length);
        }
        foreach (var w in allWheels)
        {
            float pos = Vector3.Dot(transform.forward, w.transform.position - centerOfMass.position);
            nonSteeringPos += pos / (allWheels.Length - steeringWheels.Length);
        }
        minTurnRadius = (steeringPos - nonSteeringPos) / (maxSteering * Mathf.Deg2Rad);

        devInfo.carSize = GetComponentInChildren<Renderer>().bounds.size;
        devInfo.carMass = body.mass;
        devInfo.wheelRadius = 0;
        foreach (var w in allWheels)
            devInfo.wheelRadius += w.radius*w.transform.lossyScale.x / allWheels.Length;

        devInfo.maxPerWheelTorque = 0;
        devInfo.maxEnginePower = 0;
        idleEngineRPM = 0;
        for (float rpm=0;rpm<maxEngineRPM;rpm+=maxEngineRPM/100f)
        {

            float torque = GetTorquePerWheel(rpm, 1.0f,1, drivingWheels.Length);
            if (torque > devInfo.maxPerWheelTorque)
            {
                devInfo.maxPerWheelTorque = torque;
                devInfo.maxTorqueRPM = rpm;
            }
            float power = GetEngineTorque(rpm, 1.0f) * rpm* RPMUnit;
            if (power > devInfo.maxEnginePower)
            {
                devInfo.maxEnginePower = power;
                devInfo.maxPowerRPM = rpm;
            }
            if (idleEngineTorqueByRPM.Evaluate(rpm) >= 0)
                idleEngineRPM = rpm;
        }

        devInfo.maxSpeed = 0;
        for(int gear=1;gear<=gearRatios.Length;++gear)
            for (float rpm = 0; rpm < maxEngineRPM; rpm += maxEngineRPM / 100f)
            {
                float perWheelTorque = GetTorquePerWheel(rpm, 1.0f, gear, drivingWheels.Length);
                float speed = GetWheelRPM(rpm, gear)*RPMUnit * devInfo.wheelRadius;

                float downForcePerMass = Physics.gravity.magnitude + liftCoefficientPerMass * speed * speed+ additionalDownForcePerMass;
                float acceleration = -dragCoefficientPerMass * speed * speed;


                foreach (var w in drivingWheels)
                {
                    float torqueForcePerMass = perWheelTorque / (w.radius * w.transform.lossyScale.x) / body.mass;
                    float tireForcePerMass = downForcePerMass * body.mass / allWheels.Length * w.forwardFriction.extremumValue / body.mass;
                    acceleration += Mathf.Min(torqueForcePerMass, tireForcePerMass);
                }
                if (acceleration >= 0)
                {
                    if (speed > devInfo.maxSpeed)
                    {
                        devInfo.maxSpeed = speed;
                        devInfo.maxSpeedGear = gear;
                        devInfo.maxSpeedRPM = rpm;
                    }
                }

            }

                devInfo.maxTorqueAcceleration = 0;
        devInfo.maxTireAcceleration = 0;
        foreach (var w in drivingWheels)
        {
            devInfo.maxTorqueAcceleration += devInfo.maxPerWheelTorque / (w.radius * w.transform.lossyScale.x) / body.mass;
            devInfo.maxTireAcceleration += Physics.gravity.magnitude * body.mass / allWheels.Length * w.forwardFriction.extremumValue / body.mass;
        }
        devInfo.maxTorqueBrakeAcceleration = 0;
        devInfo.maxTireBrakeAcceleration = 0;
        devInfo.maxTireTurnAcceleration = 0;
        foreach (var w in allWheels)
        {
            devInfo.maxTorqueBrakeAcceleration += brakeTorquePerWheel / (w.radius * w.transform.lossyScale.x) / body.mass;
            devInfo.maxTireBrakeAcceleration += Physics.gravity.magnitude * w.forwardFriction.extremumValue / allWheels.Length;
            devInfo.maxTireTurnAcceleration += Physics.gravity.magnitude * w.sidewaysFriction.extremumValue / allWheels.Length;
        }
        devInfo.maxTireFullTurnSpeed = Mathf.Sqrt(minTurnRadius * devInfo.maxTireTurnAcceleration);
        devInfo.doubleDragSpeed = Mathf.Sqrt(Physics.gravity.magnitude / liftCoefficientPerMass);

        /*
        devInfo.maxTorqueSpeed = Mathf.Sqrt(maxTorqueAcceleration * body.mass / (dragCoefficientPerMass * body.mass));
        */
    }
    #endregion
    #region Gears
    float GetGearRatios(int gear)
    {
        if (gear == 0)
            return 0;
        else if (gear == -1)
            return -gearRatios[0];
        else if (gear - 1 < gearRatios.Length)
            return gearRatios[gear - 1];
        else
            return 0;
    }
    float GetWheelRPM(float engineRPM,int gear)
    {
        return engineRPM / GetGearRatios(gear) / finalDriveRatio;
    }
    float GetEngineTorque(float engineRPM,float throttle)
    {
        throttle = Mathf.Clamp01(throttle);
        if (engineRPM > maxEngineRPM)
            return 0;
        float idleWeight = Mathf.Clamp01(1 - throttle / .25f);
        float val=engineTorqueByRPM.Evaluate(engineRPM) * enginePowerMultiplier *Mathf.Clamp01(throttle);
        float idleVal= idleEngineTorqueByRPM.Evaluate(engineRPM) * enginePowerMultiplier;
        if (idleVal > 0) idleVal *= throttle;
        return Mathf.Lerp(val, idleVal, idleWeight);
    }
    float GetTorquePerWheel(float engineRPM,float throttle,int gear,float drivingWheelCount)
    {

        return GetEngineTorque(engineRPM,throttle) * GetGearRatios(gear) * finalDriveRatio / drivingWheelCount;
    }
    float GetEngineInputRPM(float averageDrivingWheelRPM,int gear)
    {
        if (gear == 0)
            return idleEngineRPM;
        else
            return Mathf.Abs(averageDrivingWheelRPM * finalDriveRatio * GetGearRatios(gear));
    }
    #endregion
    private void Awake()
    {
        OnValidate();

        /*
        allWheels = GetComponentsInChildren<WheelCollider>();
        allWheelMeshes = new Transform[allWheels.Length];
        allWheelAudioSources = new AudioSource[allWheels.Length];
        allWheelTrails = new TrailRenderer[allWheels.Length];
        */
        forwardSlip = new float[allWheels.Length];
        sidewaysSlip = new float[allWheels.Length];
        wheelGrounded = new bool[allWheels.Length];
        isSkid = new bool[allWheels.Length];
        /*
        for (int i = 0; i < allWheels.Length; ++i)
        {
            allWheelMeshes[i] = allWheels[i].GetComponentInChildren<Renderer>().transform;
            allWheelAudioSources[i] = allWheels[i].GetComponentInChildren<AudioSource>();
            if (allWheelAudioSources[i] == null) allWheelAudioSources[i] = allWheelMeshes[i].gameObject.AddComponent<AudioSource>();
            allWheelAudioSources[i].spatialBlend = 1;
            allWheelAudioSources[i].loop = false;
                /*
                allWheelTrails[i]=Instantiate(skidTrailPrefab,
                    allWheels[i].transform.TransformPoint(Vector3.down*allWheels[i].radius*.5f),
                    Quaternion.LookRotation(transform.up,transform.forward), 
                    allWheels[i].transform).GetComponent<TrailRenderer>();
                    /
        }
        if (engineSoundSource!=null)
        {
            engineSoundSource.spatialBlend = 1;
            engineSoundSource.loop = true;
        }*/
        allWheels[0].ConfigureVehicleSubsteps(5f, 12, 15);

        ABSNoBrake = new bool[allWheels.Length];
        ESPBrake = new bool[allWheels.Length];
        ABSCD = 0;
        TransmissionCD = 0;
    }
    private void ABSUpdate()
    {
        espActive = false;
        absActive = false;
        for (int i = 0; i < allWheels.Length; ++i)
        {
            ABSNoBrake[i] = ESPBrake[i] = false;
        }
        if (ESP)
        {
            float slipping = 0;//positive -> need acceleration x negative(left)
            for (int i = 0; i < allWheels.Length; ++i)
            {
                if (Mathf.Abs(sidewaysSlip[i]) > allWheels[i].sidewaysFriction.extremumSlip)
                {
                    slipping += sidewaysSlip[i];
                }
            }
            for (int i = 0; i < allWheels.Length; ++i)
            {
                float rightOrLeft = Mathf.Sign(Vector3.Dot(allWheels[i].transform.position - centerOfMass.position, transform.right));//positive slipping right
                if (rightOrLeft * slipping < 0)
                {
                    espActive = true;
                    ESPBrake[i] = true;
                }
            }
        }
        if (ABS)//&& throttleInput>-.99f
        {
            for (int i = 0; i < allWheels.Length; ++i)
                if (Mathf.Abs(forwardSlip[i]) > allWheels[i].forwardFriction.extremumSlip)//positive slipping forward
                {
                    absActive = true;
                    ABSNoBrake[i] = true;
                }
        }
    }
    void UpdateInput()
    {
        //throttleInput = Input.GetAxis("Throttle");
        //steeringInput = Input.GetAxis("Horizontal");
        //handBrakeInput = Input.GetButton("HandBrake");
        //rearLampStatus = false;
        //if (Input.GetButtonDown("CarHeadLamp"))
        //    headLampStatus = !headLampStatus;
        throttleInput = Mathf.Clamp(throttleInput, -1, 1);
        steeringInput = Mathf.Clamp(steeringInput, -1, 1);

        rearLamp = false;
        if (Mathf.Abs(currentSpeed) < 1f && throttleInput == 0)
        {
            currentGear = 0;
            currentThrottle = 0;
            currentBrake = 1;
        }
        else if (currentSpeed > 1f || throttleInput>0 && currentSpeed>-1f)
        {
            if (AutomaticTransmission == true)
            {
                if (currentGear < 1)
                    currentGear = 1;
            }
            else
            {
                if (currentGear < 1)
                    currentGear = 1;
                //if (Input.GetButtonUp("GearUp"))
                //    currentGear += 1;
                //if (Input.GetButtonUp("GearDown"))
                //    currentGear -= 1;
                //
            }
            currentGear = Mathf.RoundToInt(Mathf.Clamp(currentGear, 1, gearRatios.Length));
            if (throttleInput > 0)
            {
                currentThrottle = Mathf.Abs(throttleInput);
                currentBrake = 0;
            }
            else if (throttleInput < 0)
            {
                currentThrottle = 0;
                currentBrake = Mathf.Abs(throttleInput);
                rearLamp = true;
                //rearLampStatus = true;
            }
            else
            {
                currentThrottle = 0;
                currentBrake = 0;
            }
        }
        else
        {
            currentGear = -1;
            rearLamp = true;
            //rearLampStatus = true;
            if (throttleInput < 0)
            {
                currentThrottle = Mathf.Abs(throttleInput);
                currentBrake = 0;
            }
            else if (throttleInput > 0)
            {
                currentThrottle = 0;
                currentBrake = Mathf.Abs(throttleInput);
            }
            else
            {
                currentThrottle = 0;
                currentBrake = 0;
            }
        }

        if (Mathf.Abs(currentSpeed) > speedLimit)
            currentThrottle = 0;
        if (currentEngineRPM > maxEngineRPM)
            currentThrottle = 0;
    }

    private void FixedUpdate()
    {
        if (body.isKinematic) return;

        UpdateInput();

        currentSpeed = Vector3.Dot(body.velocity, transform.forward);
        body.AddForce(-transform.forward * dragCoefficientPerMass * body.mass * currentSpeed* currentSpeed*Mathf.Sign(currentSpeed));
        body.AddForce(-transform.up * liftCoefficientPerMass * body.mass * currentSpeed * currentSpeed);

       

        float averageDrivingWheelRPM = 0;
        foreach (var w in drivingWheels)
            averageDrivingWheelRPM += w.rpm / drivingWheels.Length;
        
        currentEngineInputRPM = GetEngineInputRPM(averageDrivingWheelRPM, currentGear);
        float targetEngineRPM = currentEngineInputRPM;
        currentEngineRPM = Mathf.Lerp(currentEngineRPM, targetEngineRPM, Time.fixedDeltaTime / .5f);


        TransmissionCD -= Time.deltaTime;
        if (AutomaticTransmission && TransmissionCD <= 0)
        {
            if (currentGear + 1 <= gearRatios.Length && currentEngineRPM > AutoGearUpRPM)
            {
                TransmissionCD = AutomaticTransmissionInterval;
                ++currentGear;
            }
            else if (currentGear - 1 >= 1 && currentEngineRPM < AutoGearDownRPM)
            {
                TransmissionCD = AutomaticTransmissionInterval;
                --currentGear;
            }
        }


        if (Mathf.Abs(currentSpeed) > speedLimit)
            currentThrottle = 0;
        if (currentEngineRPM > maxEngineRPM)
            currentThrottle = 0;

        currentTorquePerWheel = GetTorquePerWheel(currentEngineRPM, currentThrottle, currentGear, drivingWheels.Length);
        foreach (var w in allWheels)
            w.brakeTorque = brakeTorquePerWheel * currentBrake;
        foreach (var w in drivingWheels)
            w.motorTorque = currentTorquePerWheel;

        WheelHit hit = new WheelHit();
        for (int i = 0; i < allWheels.Length; ++i)
        {
            if (allWheels[i].GetGroundHit(out hit))
            {
                forwardSlip[i] = hit.forwardSlip;
                sidewaysSlip[i] = hit.sidewaysSlip;
                wheelGrounded[i] = true;
            }else
                wheelGrounded[i] = false;
        }

        ABSCD -= Time.fixedDeltaTime;
        if (ABSCD <= 0)
            ABSUpdate();
        for (int i=0;i<allWheels.Length;++i)
        {
            if (ESPBrake[i])
            {
                allWheels[i].brakeTorque = brakeTorquePerWheel;
                allWheels[i].motorTorque = 0;
            }
            if (ABSNoBrake[i])
            {
                allWheels[i].brakeTorque = 0;
                allWheels[i].motorTorque = 0;
            }
        }
        foreach(var w in handBrakeWheels)
        {
            if (handBrakeInput)
                w.brakeTorque = brakeTorquePerWheel;
        }

        float downForcePerMass = -Vector3.Dot(transform.up, Physics.gravity) + liftCoefficientPerMass * currentSpeed * currentSpeed+additionalDownForcePerMass;
        float noSlipTurnRadius = currentSpeed * currentSpeed / downForcePerMass;

        //float steering = maxSteering*steeringInput * Mathf.Pow(1 / Mathf.Max(1,currentSpeed/maxTireFullTurnSpeed/Mathf.Sqrt(steeringLimit)),2);
        float steering = steeringInput * maxSteering;
        if(normalizeSteering)
            steering *= Mathf.Clamp01(steeringLimit * minTurnRadius / noSlipTurnRadius);

        foreach (var w in steeringWheels)
        {
            w.steerAngle =  steering;
        }
        if (canTurnAround)
        {
            int noGrounded = 0;
            for (int i = 0; i < allWheels.Length; ++i)
                if (!wheelGrounded[i]) noGrounded+=1;
            if (noGrounded>=allWheels.Length/2 && body.velocity.magnitude<1f&& body.angularVelocity.magnitude<1f )
            {
                body.AddTorque(transform.forward * -steeringInput * Mathf.Abs(Vector3.Dot(Vector3.forward, body.inertiaTensorRotation * body.inertiaTensor)) * 10f);
            }
        }

        updateOutputs.updateEngineRPM.Invoke(currentEngineRPM);
        updateOutputs.updateEngineThrottle.Invoke(currentThrottle);

    }

    private void Update()
    {
        debug_text = string.Format("{0:F0} MPH", currentSpeed / MPHUnit) + "\n"
            + string.Format("{0:F0} RPM", currentEngineRPM) + "\n"
            + string.Format("Gear {0:F0}", currentGear);
        updateOutputs.updateDebugText.Invoke(debug_text);
    }
}
