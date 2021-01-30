using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.Events;
public class AircraftControlSimple : MonoBehaviour
{
    public Transform[] LeftAilerons, RightAilerons, HorizontalAilerons, VerticalAilerons;
    public AirFoil[] foilsWithFlap;
    public Thruster[] thrusters;

    public WheelCollider[] wheels;
    public float[] wheelSteerings;
    public float brakeTorquePerWheel = 4000;
    public float reverseTorquePerWheel = 100;

    public Rigidbody body;
    public float rollSensitivity = 15f;
    public float pitchSensitivity = 15f;
    public float yawSensitivity = 15f;
    public bool ejectOnStart = false;
    public float ejectSpeed = 0f;
    public AirFoil[] aoaWings;
    AirFoil[] airFoils;

    [field: Header("Inputs")]
    [field: SerializeField, Range(0, 1)]
    public float throttleInput { get; set; }
    [field: SerializeField, Range(-1, 1)]
    public float pitchInput { get; set; }
    [field: SerializeField, Range(-1, 1)]
    public float yawInput { get; set; }
    [field: SerializeField, Range(-1, 1)]
    public float rollInput { get; set; }
    [field: SerializeField]
    public bool flapInput { get; set; }
    [field: SerializeField]
    public bool wheelReverseInput { get; set; }
    public void ToggleWheelReverse() { wheelReverseInput = !wheelReverseInput; }

    [Serializable] public class UpdateFloat : UnityEvent<float> { }
    [Serializable] public class UpdateString : UnityEvent<string> { }
    [Serializable]
    public class UpdateOutputs
    {
        public UpdateFloat updateEngineRPM;
        public UpdateFloat updateEngineThrottle;
        public UpdateString updateDebugText;
    }
    [Header("Outputs")]
    public UpdateOutputs updateOutputs;

    [Multiline(12)]
    public string debug_text;
    public bool debug_input = false;

    private void Start()
    {
        airFoils = GetComponentsInChildren<AirFoil>();
        if (ejectOnStart)
            body.velocity = transform.forward * ejectSpeed;
    }
    private void FixedUpdate()
    {
        throttleInput = Mathf.Clamp(throttleInput, 0, 1);
        rollInput = Mathf.Clamp(rollInput, -1, 1);
        pitchInput = Mathf.Clamp(pitchInput, -1, 1);
        yawInput = Mathf.Clamp(yawInput, -1, 1);

        foreach (var thruster in thrusters)
            thruster.throttle = throttleInput;
        foreach (var a in LeftAilerons)
            a.localRotation = Quaternion.Euler(rollInput * rollSensitivity, 0, 0);
        foreach (var a in RightAilerons)
            a.localRotation = Quaternion.Euler(-rollInput * rollSensitivity, 0, 0);
        foreach (var a in HorizontalAilerons)
            a.localRotation = Quaternion.Euler(-pitchInput * pitchSensitivity, 0, 0);
        foreach (var a in VerticalAilerons)
            a.localRotation = Quaternion.Euler(-yawInput * yawSensitivity, 0, 0);
        for (int i = 0; i < foilsWithFlap.Length; ++i)
        {
            foilsWithFlap[i].isFlap = flapInput;
        }
        for (int i = 0; i < wheels.Length; ++i)
        {
            wheels[i].steerAngle = wheelSteerings[i] * yawInput;
            wheels[i].brakeTorque = throttleInput < .01f && !wheelReverseInput ? brakeTorquePerWheel : 0;
            //Some known bugs of wheelcolliders, should not put zero
            wheels[i].motorTorque = wheelReverseInput ? -reverseTorquePerWheel : 1e-7f;
        }
        float maxRPM = 0;
        foreach (var thruster in thrusters)
            maxRPM = Mathf.Max(maxRPM, thruster.rpm);
        updateOutputs.updateEngineRPM.Invoke(maxRPM);
        updateOutputs.updateEngineThrottle.Invoke(throttleInput);
    }
    private void Update()
    {
        if (debug_input)
        {
            /*
            throttleInput = Mathf.Clamp01(throttleInput + Input.GetAxis("Vertical") * Time.deltaTime / 2f);
            yawInput = Input.GetAxis("Horizontal");
            rollInput = -Input.GetAxis("Mouse X");
            pitchInput = -Input.GetAxis("Mouse Y");
            */
            if (Input.GetKeyDown(KeyCode.W))
                throttleInput = Mathf.Clamp01(throttleInput + 0.1f);
            if (Input.GetKeyDown(KeyCode.S))
                throttleInput = Mathf.Clamp01(throttleInput - 0.1f);
            if (Input.GetKeyDown(KeyCode.F))
                flapInput = !flapInput;

            yawInput = Input.GetAxis("Horizontal");
            rollInput = -Mathf.Clamp((Input.mousePosition.x * 2 - Screen.width) / Screen.height, -1, 1);
            pitchInput = Mathf.Clamp(Input.mousePosition.y / Screen.height * 2 - 1, -1, 1);


        }

        Vector3 force = Vector3.zero;
        foreach (var f in airFoils)
            force += f.force;
        string aoa = "";
        if (aoaWings.Length > 0) foreach (var af in aoaWings) aoa += string.Format("{0:F0}, ", af.angleOfAttack);
        string rpm = "";
        float totalThrust = 0;
        foreach (var t in thrusters)
        {
            rpm += $"{t.rpm:F0}, ";
            totalThrust += t.thrust;
        }

        debug_text = string.Format(
            $"Throttle: {throttleInput:P0} {totalThrust / body.mass / 9.81:F1}G\n" +
            $"RPM: {rpm}\n" +
            $"AirSpeed: {Vector3.Dot(transform.forward, body.velocity) * 2.23693629f:F0} MPH ({Vector3.Dot(transform.forward, body.velocity):F1} m/s)\n" +
            $"Climb Rate:{body.velocity.y:F1}\n" +
            $"Altitude:{body.position.y:F1}\n" +
            $"Overload:{Vector3.Dot(transform.up, force) / body.mass / 9.81f:F1}G\n" +
            $"Slide: {Vector3.Dot(transform.right, body.velocity):F1}m/s\n" +
            $"Flaps: {(flapInput ? "on" : "off")}\n" +
            $"AngleOfAttack: {aoa}\n"
            );
        updateOutputs.updateDebugText.Invoke(debug_text);
    }
}
