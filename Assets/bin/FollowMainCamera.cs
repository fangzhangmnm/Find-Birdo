using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowMainCamera : MonoBehaviour
{
    public float forward = 10f;

    [Button]
    void Follow()
    {
        transform.position = Camera.main.transform.position + Camera.main.transform.forward * forward;
    }
    void Update()
    {
        Follow();
    }
}
