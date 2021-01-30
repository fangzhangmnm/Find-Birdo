using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class LookCamera : MonoBehaviour
{
    static Camera main;
    private void Update()
    {
        if (main == null || !main.isActiveAndEnabled)
            main = Camera.main;
        transform.LookAt(main.transform);
        transform.Rotate(Vector3.right, 90,Space.Self);
    }
}
