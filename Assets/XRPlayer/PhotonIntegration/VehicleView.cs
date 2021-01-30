using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

[DefaultExecutionOrder(-20)]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PhotonView))]
public class VehicleView : MonoBehaviourPun, IPunObservable
{
    DeadReckoningVector DRV = new DeadReckoningVector();
    DeadReckoningQuaternion DRQ = new DeadReckoningQuaternion();
    Rigidbody body;
    public float velocitySmoothTime = .5f;
    public float followTime = .5f;

    public void TakeOver()
    {
        photonView.RequestOwnership();
    }
    void Awake()
    {
        body = GetComponent<Rigidbody>();
    }
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(body.position);
            stream.SendNext(body.velocity);
            stream.SendNext(body.rotation);
            stream.SendNext(body.angularVelocity);

            DRV.FlagReset(); DRQ.FlagReset();
        }
        if(stream.IsReading)
        {
            Vector3 newPosition = (Vector3)stream.ReceiveNext();
            Vector3 newVelocity = (Vector3)stream.ReceiveNext();
            Quaternion newRotation = (Quaternion)stream.ReceiveNext();
            Vector3 newAngularVelocity = (Vector3)stream.ReceiveNext();
            DRV.NetworkUpdate(newPosition, newVelocity, 0, false);
            DRQ.NetworkUpdate(newRotation, newAngularVelocity, 0, false);

        }
    }
    void FixedUpdate()
    {
        DRV.velocitySmoothTime = velocitySmoothTime;
        DRQ.velocitySmoothTime = velocitySmoothTime;
        DRV.followTime = followTime;
        DRQ.followTime = followTime;

        if (!photonView.IsMine)
        {
            body.isKinematic = true;
            if (DRV.isReady)
            {
                body.transform.position=DRV.Update(Time.fixedDeltaTime);
                body.velocity = DRV.velocity;
            }
            if (DRQ.isReady)
            {
                body.transform.rotation=DRQ.Update(Time.fixedDeltaTime);
                body.angularVelocity = DRQ.lastAngularVelocity;
            }
            if (body.useGravity) body.AddForce(-body.mass * Physics.gravity);
        }
        else
        {
            body.isKinematic = false;
        }
    }
    void OnValidate()
    {
        Debug.Assert(photonView.OwnershipTransfer == OwnershipOption.Takeover);
        body = GetComponent<Rigidbody>();
        Debug.Assert(body.interpolation==RigidbodyInterpolation.None);
    }
}
