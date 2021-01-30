using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdateCheck : MonoBehaviour
{
    float lastRotation;
    private void Update()
    {
        float rotation = transform.rotation.eulerAngles.y;
        Debug.Log(rotation - lastRotation);
        lastRotation = rotation;
    }
}
