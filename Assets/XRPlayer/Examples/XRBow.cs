using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(XRGrabable))]
[RequireComponent(typeof(Animator))]
public class XRBow : MonoBehaviourPun, IPunObservable
{
    XRGrabable grabable;Animator animator;
    public Transform knob, minKnobRef, maxKnobRef, arrowRef;
    public SkinnedMeshRenderer skinnedMeshRenderer;
    public string animatorFieldName = "Value";
    public string arrowPrefabPath;
    public float maxShotSpeed=20f;
    [Tooltip("set to 0 to disable auto destroy")]
    public float arrowLifeTime = 10f;
    static GameObject arrowPrefab;
    Collider[] bowColliders;
    GameObject attachedArrow;
    float value;

    private void Awake()
    {
        grabable = GetComponent<XRGrabable>();
        animator = GetComponent<Animator>();
        if (!arrowPrefab) arrowPrefab = Resources.Load(arrowPrefabPath) as GameObject;
        bowColliders = GetComponentsInChildren<Collider>();
    }
    private void FixedUpdate()
    {
        Vector3 p0 = minKnobRef.position;
        Vector3 p1 = maxKnobRef.position;
        if (photonView.IsMine)
        {
            if (!grabable.hand2)
            {
                if (attachedArrow)
                {
                    float speed = value * maxShotSpeed;
                    DropArrow(attachedArrow.transform.position, attachedArrow.transform.rotation, speed);
                }
                value = 0;
            }
            else
            {
                Vector3 p = grabable.hand2.position;
                value = Mathf.Clamp01(Vector3.Dot(p - p0, p1 - p0) / (p1 - p0).sqrMagnitude);
                if (!attachedArrow)
                    AttachArrow();
            }
        }
        knob.transform.position = Vector3.Lerp(p0, p1, value);
        if (attachedArrow)
        {
            attachedArrow.transform.position = arrowRef.position + (p1 - p0) * value;
            attachedArrow.transform.rotation = arrowRef.rotation;
        }
        animator.SetFloat(animatorFieldName, value);
    }
    [PunRPC]
    public void AttachArrow()
    {
        if (photonView.IsMine)
            photonView.RPC("AttachArrow", RpcTarget.Others);
        if (attachedArrow) Destroy(attachedArrow);
        attachedArrow = Instantiate(arrowPrefab, arrowRef.position, arrowRef.rotation);
        foreach (var c in attachedArrow.GetComponentsInChildren<Collider>())
        {
            foreach (var cc in bowColliders)
                Physics.IgnoreCollision(c, cc);
            if(grabable.hand)
                foreach (var cc in grabable.hand.handColliders)
                    Physics.IgnoreCollision(c, cc);
            if (grabable.hand2)
                foreach (var cc in grabable.hand2.handColliders)
                    Physics.IgnoreCollision(c, cc);
        }
        attachedArrow.GetComponent<Rigidbody>().isKinematic = true;
    }
    
    [PunRPC]
    public void DropArrow(Vector3 arrowPosition, Quaternion arrowRotation, float speed)
    {
        if (photonView.IsMine)
            photonView.RPC("DropArrow",RpcTarget.Others, arrowPosition, arrowRotation, speed);
        if (attachedArrow)
        {
            var arrowBody = attachedArrow.GetComponent<Rigidbody>();
            attachedArrow.transform.position = arrowPosition;
            attachedArrow.transform.rotation = arrowRotation;
            arrowBody.isKinematic = false;
            arrowBody.velocity = transform.forward * speed;
            arrowBody.angularVelocity = Vector3.zero;
            if (arrowLifeTime > 0)
            {
                Destroy(attachedArrow, arrowLifeTime);
            }
            attachedArrow = null;
        }
    }
    void IPunObservable.OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(value);
        }
        else
        {
            value = (float)stream.ReceiveNext();
        }
    }
}
