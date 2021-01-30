using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-20)]
public class DebugMovingPlat : MonoBehaviour
{
    float speed;

    private void FixedUpdate()
    {
        float targetSpeed = 0;
        if (Input.GetKey(KeyCode.R))
            targetSpeed = 40;
        if (Input.GetKey(KeyCode.F))
            targetSpeed = -40;
        speed += Mathf.Clamp(targetSpeed - speed, -Time.fixedDeltaTime * 50, Time.fixedDeltaTime * 50);
        transform.position += speed * transform.forward * Time.fixedDeltaTime;
    }
}
