using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "AirFoilSettings", menuName = "Physics/AirFoilSettings")]
public class AirFoilSettings : ScriptableObject
{
    public AnimationCurve liftCoefficient;
    public AnimationCurve dragCoefficient;
    public AnimationCurve pitchCoefficient;
    public float bonusLiftWithFlap;
    public float bonusDragWithFlap;
    public float stallWarning = 10f;
}