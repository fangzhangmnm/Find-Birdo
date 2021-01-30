using UnityEngine;

public class FallRespawn : MonoBehaviour
{
    public float respawnHeight = -20f;
    public float respawnDistanceToWorldOrigin = float.PositiveInfinity;
    public Transform respawnRef;
    public bool recordPointAtStart=true;
    Vector3 respawnPoint=Vector3.zero;
    

    void Start()
    {
        if (respawnRef == null)
        {
            respawnRef = transform;
            recordPointAtStart = true;
        }
        if (recordPointAtStart)
            respawnPoint = respawnRef.position;

    }

    // Update is called once per frame
    void Update()
    {
        if (transform.position.y < respawnHeight || transform.position.magnitude > respawnDistanceToWorldOrigin)
        {
            transform.position = recordPointAtStart ? respawnPoint : respawnRef.position;
            var body = transform.GetComponent<Rigidbody>();
            if (body)
            {
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

        }
    }
}
