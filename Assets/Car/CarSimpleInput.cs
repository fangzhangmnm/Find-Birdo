using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarSimpleInput : MonoBehaviour
{
    void Update()
    {
        var pcar = GetComponent<PhysicalCar>();
        pcar.throttleInput = Input.GetAxis("Vertical");
        pcar.steeringInput = Input.GetAxis("Horizontal");
        pcar.normalizeSteering = true;
    }
    private void OnDisable()
    {
        var pcar = GetComponent<PhysicalCar>();
        pcar.throttleInput = 0;
        pcar.steeringInput = 0;
    }
}
