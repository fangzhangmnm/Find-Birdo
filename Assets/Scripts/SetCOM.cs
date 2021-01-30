using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetCOM : MonoBehaviour
{
    public Transform reference;
    void Awake()
    {
        //centerOfMass is relative to the transform's position and rotation, but will not reflect the transform's scale!
        GetComponent<Rigidbody>().centerOfMass = Quaternion.Inverse(transform.rotation) * (reference.position - transform.position);
    }
}
