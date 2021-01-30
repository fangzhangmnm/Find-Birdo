using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "XRJointSettings", menuName = "XR/XRJointSettings", order = 1)]
public class XRJointSettings : ScriptableObject
{
    public float spring = 50000;
    public float damper = 2000;
    public float maxForce = 1000;
    public float angularSpring = 50000;
    public float angularDamper = 2000;
    public float angularMaxForce = 1000;
}
