using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleSpawner : MonoBehaviour
{
    public GameObject prefab;
    public float interval=5f;
    public float lifeTime=10f;
    public Vector3 initialVelocity;

    private void Start()
    {
        StartCoroutine(MainLoop());
    }
    IEnumerator MainLoop()
    {
        while (true)
        {
            var p = Instantiate(prefab);
            p.transform.position = transform.position;
            p.transform.rotation = transform.rotation;
            if (p.GetComponent<Rigidbody>())
                p.GetComponent<Rigidbody>().velocity = transform.TransformDirection(initialVelocity);
            Destroy(p, lifeTime);
            yield return new WaitForSeconds(interval);
        }
    }
}
