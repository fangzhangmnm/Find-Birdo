using UnityEngine;

public class FallRespawn : MonoBehaviour
{
    public float respawnHeight = -100f;
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
        if (transform.position.y < respawnHeight)
            transform.position = recordPointAtStart ? respawnPoint : respawnRef.position;
    }
}
