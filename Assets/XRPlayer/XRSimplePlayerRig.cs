using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class XRSimplePlayerRig : MonoBehaviour
{
    public Transform head, body;

    public Transform trackedHead, playerRoot;
    public float neckLength = 0,neckLength2=.4f;
    float scale => playerRoot.lossyScale.x;

    void CopyPosRot(Transform from, Transform to) { to.position = from.position;to.rotation = from.rotation; }
    //[Button]
    private void Update()
    {
        CopyPosRot(trackedHead, head);
        Vector3 bodyPos = head.position - head.up * neckLength-playerRoot.up* neckLength2;
        body.position = bodyPos;
        body.rotation = playerRoot.rotation;
    }
}
