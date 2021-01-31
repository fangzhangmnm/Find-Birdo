using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateFX : MonoBehaviour
{
    public float angularSpeed;
    void FixedUpdate()
    {
        transform.Rotate(Vector3.forward * angularSpeed, Space.Self);
    }
}
