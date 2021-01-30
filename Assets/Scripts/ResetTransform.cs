using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class ResetTransform : MonoBehaviourPun
{
    public Transform searchHierarchy;
    public List<Transform> registeredGameObjects;
    Vector3[] positions;
    Quaternion[] rotations;
    void Start()
    {
        if (searchHierarchy)
        {
            int nn = searchHierarchy.childCount;
            for (int i = 0; i < nn; ++i)
                registeredGameObjects.Add(searchHierarchy.GetChild(i));
        }
        int n = registeredGameObjects.Count;
        positions = new Vector3[n];
        rotations = new Quaternion[n];
        for (int i = 0; i < n; ++i)
        {
            positions[i] = registeredGameObjects[i].position;
            rotations[i]= registeredGameObjects[i].rotation;
        }
    }
    public void DoResetTransform()
    {
        int n = registeredGameObjects.Count;
        for (int i = 0; i < n; ++i)
            if (registeredGameObjects[i] != null)
            {
                registeredGameObjects[i].position = positions[i];
                registeredGameObjects[i].rotation = rotations[i];
                var body = registeredGameObjects[i].GetComponent<Rigidbody>();
                if (body)
                {
                    body.velocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                }
            }
    }
}
