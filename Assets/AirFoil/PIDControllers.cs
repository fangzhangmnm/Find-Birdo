using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DampingController
{
    public float dampingTime = 0.02f;
    float oldInput = float.NaN;
    public void Reset() { oldInput = float.NaN; }
    public float Step(float input, float dt)
    {
        if (float.IsNaN(oldInput)) oldInput = input;
        return oldInput = Mathf.Lerp(oldInput, input, dt / (dt + dampingTime));
    }
}
[System.Serializable]
public class DifferentialController
{
    public float dampingTime = 0.02f;
    private DampingController damping = new DampingController();
    float oldRawInput = float.NaN;
    public void Reset() { oldRawInput = float.NaN; }
    public float Step(float rawInput, float dt)
    {
        damping.dampingTime = dampingTime;
        if (float.IsNaN(oldRawInput)) oldRawInput = rawInput;
        float output = damping.Step((rawInput - oldRawInput) / dt, dt);
        oldRawInput = rawInput;
        return output;
    }
}
[System.Serializable]
public class PIDController
{
    public float KP = 1, KI = 0, KD = 0;
    public DampingController dampP, dampIInput;
    public DifferentialController diffD;
    public float maxDifferential = 10, maxIntegrate = 10;
    [ReadOnly]
    public float input, differential, integrate = 0, output;

    public void Reset()
    {
        integrate = 0;
    }
    public float Step(float rawInput, float dt)
    {
        input = Mathf.Clamp(dampP.Step(rawInput, dt), -1, 1);
        differential = diffD.Step(rawInput, dt);
        differential = Mathf.Clamp(differential, -maxDifferential, maxDifferential);
        integrate += Mathf.Clamp(dampIInput.Step(rawInput, dt), -1, 1) * dt;
        integrate = Mathf.Clamp(integrate, -maxIntegrate, maxIntegrate);

        return output = Mathf.Clamp(KP * input + KI * integrate + KD * differential, -1, 1);
    }
}