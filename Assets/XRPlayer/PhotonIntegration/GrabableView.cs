using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.Events;

[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(Rigidbody))]
public class GrabableView : MonoBehaviourPun, IPunObservable
{
    public UnityEvent onOtherPlayerTake;
    public Behaviour[] disableOnOtherPlayerTake;

    [Header("Debug Exposed")]
    public bool isLocalPlayerPickup;
    public bool isOtherPlayerPickup;
    public bool useDR;
    DeadReckoningVector DRV = new DeadReckoningVector();
    DeadReckoningQuaternion DRQ = new DeadReckoningQuaternion();

    void TransfreOwnershipBrutal(Player player)
    {
        photonView.TransferOwnership(player);
        photonView.SetOwnerInternal(player, player.ActorNumber);
        if (photonView.IsMine)
        {
            useDR = isOtherPlayerPickup = false;
        }
    }

    public void OnLocalPlayerPickup()
    {
        isLocalPlayerPickup = true;
        TransfreOwnershipBrutal(PhotonNetwork.LocalPlayer);
    }
    public void OnLocalPlayerDrop()
    {
        isLocalPlayerPickup = false;
    }
    void FixedUpdate()
    {
        colLevel = _colLevel;_colLevel = ColLevel.None;
        if (body.IsSleeping()) colLevel = ColLevel.GroundOrSleep;
        float E = .5f * body.velocity.sqrMagnitude + .5f * Vector3.Dot(body.angularVelocity, ApplyTensor(body.angularVelocity, body.inertiaTensor, body.rotation * body.inertiaTensorRotation)) / body.mass;


        if (isActiveAndEnabled && PhotonNetwork.IsConnectedAndReady)
        {
            if (isOtherPlayerPickup)
            {
                if (isLocalPlayerPickup)
                    onOtherPlayerTake.Invoke();
            }
            else if (isLocalPlayerPickup)
            {
                //Let Grabable Script handle the all
            }
            else if(photonView.IsMine)
            {
                if (colLevel == ColLevel.GroundOrSleep)
                {
                    if (E < Physics.sleepThreshold)
                        if (!PhotonNetwork.IsMasterClient)
                        {
                            //send stopped body
                            Vector3 position = body.position;
                            Quaternion rotation = body.rotation;
                            ToAttached(ref position, ref rotation);

                            DRV.NetworkUpdate(position, Vector3.zero, 0, true);
                            DRQ.NetworkUpdate(rotation, Vector3.zero, 0, true);
                            useDR = true;
                            TransfreOwnershipBrutal(PhotonNetwork.MasterClient);
                        }
                }
                if (colLevel == ColLevel.OtherOwned)
                {
                    if (!PhotonNetwork.IsMasterClient)
                    {
                        //send stopped body
                        Vector3 position = body.position;
                        Quaternion rotation = body.rotation;
                        ToAttached(ref position, ref rotation);

                        DRV.NetworkUpdate(position, Vector3.zero, 0, true);
                        DRQ.NetworkUpdate(rotation, Vector3.zero, 0, true);
                        useDR = true;
                        TransfreOwnershipBrutal(PhotonNetwork.MasterClient);
                    }
                }
            }
            else
            {
                if (colLevel == ColLevel.LocalPickedup)
                    TransfreOwnershipBrutal(PhotonNetwork.LocalPlayer);
            }
        }
        if (photonView.IsMine) useDR = false;
        if (useDR)
        {
            if (DRV.isReady && DRQ.isReady)
            {
                Vector3 position = DRV.Update(Time.fixedDeltaTime);
                Quaternion rotation = DRQ.Update(Time.fixedDeltaTime);
                Vector3 velocity = DRV.velocity;
                Vector3 angularVelocity = DRQ.angularVelocity;
                FromAttached(ref position, ref rotation, ref velocity, ref angularVelocity);
                body.MovePosition(position);
                body.velocity = velocity;
                body.MoveRotation(rotation);
                body.angularVelocity = angularVelocity;
            }
            if (body.useGravity) body.AddForce(-body.mass * Physics.gravity);
        }
        //body.isKinematic = useDR;//TODOTODOTODO Do it after teleportation to wakeup stacked objects

        //DebugColor();
    }



    void IPunObservable.OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        float sendInterval = 1.0f / PhotonNetwork.SerializationRate;

        if (stream.IsWriting)
        {
            byte statCode = 0;
            if (isLocalPlayerPickup) statCode |= 0x1;
            if (isLocalPlayerPickup) statCode |= 0x2;
            if (colLevel == ColLevel.GroundOrSleep) statCode |= 0x2;
            if (colLevel == ColLevel.LocalOwned) statCode |= 0x2;
            if (colLevel == ColLevel.LocalPickedup) statCode |= 0x2;

            Vector3 position = body.position;
            Quaternion rotation = body.rotation;
            Vector3 velocity = body.velocity;
            Vector3 angularVelocity = body.angularVelocity;
            if (useDR)
            {
                velocity = DRV.EstimateVelocity(position, sendInterval, firstTake);
                angularVelocity = DRQ.EstimateVelocity(rotation, sendInterval, firstTake);
                firstTake = false;
            }

            ToAttached(ref position, ref rotation, ref velocity,ref angularVelocity);

            stream.SendNext(statCode);
            stream.SendNext(attachedPhotonViewID);
            stream.SendNext(position);
            stream.SendNext(rotation);
            stream.SendNext(velocity);
            stream.SendNext(angularVelocity);

            DRV.FlagReset(); DRQ.FlagReset();
        }
        if(stream.IsReading && !photonView.IsMine)
        {
            byte statCode = (byte)stream.ReceiveNext();

            int newAttachedPhotonViewID=(int)stream.ReceiveNext();

            isOtherPlayerPickup = (statCode & 0x1)!=0;
            useDR = (statCode & 0x2) != 0;
            Vector3 newPosition = (Vector3)stream.ReceiveNext();
            Quaternion newRotation = (Quaternion)stream.ReceiveNext();
            Vector3 newVelocity = (Vector3)stream.ReceiveNext();
            Vector3 newAngularVelocity = (Vector3)stream.ReceiveNext();

            SetAttachFromRemote(newAttachedPhotonViewID);

            if (useDR)
            {
                DRV.NetworkUpdate(newPosition, newVelocity, 0, firstTake);
                DRQ.NetworkUpdate(newRotation, newAngularVelocity, 0, firstTake);
            }
            else
            {
                FromAttached(ref newPosition, ref newRotation, ref newVelocity, ref newAngularVelocity);
                //if ((newPosition - body.position).magnitude / sendInterval > 2*Mathf.Max(1, newVelocity.magnitude))
                {
                    body.MovePosition(newPosition);
                    body.velocity = newVelocity;
                }
                //else//TODO
                {
                    //body.velocity = newVelocity + (newPosition - body.position) / sendInterval;
                }
                body.angularVelocity = newAngularVelocity;
                body.MoveRotation(newRotation);
                DRV.FlagReset(); DRQ.FlagReset();
            }
            firstTake = false;
        }
    }
    #region AttachLogic
    bool firstTake;

    public void MarkTeleport() { firstTake = true; }

    [Header("Debug Exposed")]
    public int attachedPhotonViewID = 0;
    public Transform attachedTransform;
    public void SetAttach(Transform attachedChildTransform)
    {
        var attachedPhotonView = attachedChildTransform ? attachedChildTransform.GetComponentInParent<PhotonView>() : null;
        Transform oldAttachedTransform = attachedTransform;
        if (attachedPhotonView)
        {
            attachedPhotonViewID = attachedPhotonView.ViewID;
            attachedTransform = attachedPhotonView.transform;
        }
        else
        {
            attachedPhotonViewID = 0;//No View Found
            attachedTransform = null;
        }
        if (oldAttachedTransform != attachedTransform)
            MarkTeleport();
    }
    void SetAttachFromRemote(int newAttachedPhotonViewID)
    {
        attachedPhotonViewID = newAttachedPhotonViewID;


        Transform oldAttachedTransform = attachedTransform;

        if (attachedPhotonViewID == 0)
            attachedTransform = null;
        else
            attachedTransform = PhotonView.Find(attachedPhotonViewID).transform;
        if (oldAttachedTransform != attachedTransform)
            MarkTeleport();
    }
    void ToAttached(ref Vector3 position, ref Quaternion rotation)
    {
        if (attachedTransform)
        {
            position = attachedTransform.InverseTransformPoint(position);
            rotation = Quaternion.Inverse(attachedTransform.rotation) * rotation;
            //velocity = Quaternion.Inverse(attachedTransform.rotation) * velocity;
            //angularVelocity = Quaternion.Inverse(attachedTransform.rotation) * angularVelocity;
        }
    }
    void ToAttached(ref Vector3 position, ref Quaternion rotation, ref Vector3 velocity, ref Vector3 angularVelocity)
    {
        if (attachedTransform)
        {
            position = attachedTransform.InverseTransformPoint(position);
            rotation = Quaternion.Inverse(attachedTransform.rotation) * rotation;
            velocity = Quaternion.Inverse(attachedTransform.rotation) * velocity;
            angularVelocity = Quaternion.Inverse(attachedTransform.rotation) * angularVelocity;
        }
    }
    void FromAttached(ref Vector3 position, ref Quaternion rotation, ref Vector3 velocity, ref Vector3 angularVelocity)
    {
        if (attachedTransform)
        {
            position = attachedTransform.TransformPoint(position);
            rotation = attachedTransform.rotation * rotation;
            velocity = attachedTransform.rotation * velocity;
            angularVelocity = attachedTransform.rotation * angularVelocity;
        }
    }
    #endregion
    #region Tracking Collision State
    enum ColLevel { None, GroundOrSleep, LocalOwned, OtherOwned, LocalPickedup, OtherPickedup }
    ColLevel colLevel, _colLevel;
    void UpdateColLevel(Rigidbody body)
    {
        if (body)
        {
            var gv = body.GetComponent<GrabableView>();
            if (gv)
            {
                if (!gv.photonView.IsMine && gv.isOtherPlayerPickup)
                    _colLevel = _colLevel < ColLevel.OtherPickedup ? ColLevel.OtherPickedup : _colLevel;
                else if (gv.photonView.IsMine && gv.isLocalPlayerPickup)
                    _colLevel = _colLevel < ColLevel.LocalPickedup ? ColLevel.LocalPickedup : _colLevel;
                else if (!gv.photonView.IsMine)
                    _colLevel = _colLevel < ColLevel.OtherOwned ? ColLevel.OtherOwned : _colLevel;
                else
                    _colLevel = _colLevel < ColLevel.LocalOwned ? ColLevel.LocalOwned : _colLevel;
            }
            else
            {
                if (body.GetComponent<LocalPlayerColliderMarker>())
                    _colLevel = _colLevel < ColLevel.LocalPickedup ? ColLevel.LocalPickedup : _colLevel;
            }
        }
        else
            _colLevel = _colLevel < ColLevel.GroundOrSleep ? ColLevel.GroundOrSleep : _colLevel;
    }
    void OnCollisionExit(Collision collision)
    {
        UpdateColLevel(collision.rigidbody);
    }
    void OnCollisionStay(Collision collision)
    {
        UpdateColLevel(collision.rigidbody);
    }
    #endregion
    Rigidbody body;
    void Awake()
    {
        body = GetComponent<Rigidbody>();
    }
    void OnValidate()
    {
        //photonView.Synchronization = ViewSynchronization.Unreliable;
        photonView.Synchronization = ViewSynchronization.UnreliableOnChange;
        photonView.OwnershipTransfer = OwnershipOption.Takeover;
        body=GetComponent<Rigidbody>();
        if (body.drag == 0 || body.angularDrag == 0)
            Debug.LogWarning(gameObject.name+": Add drag to the body to make it stop when player is not interacting with for better network performance");
    }
    void DebugColor()
    {
        if (photonView.IsMine)
            GetComponent<MeshRenderer>().material.color = Color.green;
        else
            GetComponent<MeshRenderer>().material.color = Color.red;
        if (colLevel>0)
            GetComponent<MeshRenderer>().material.color = GetComponent<MeshRenderer>().material.color / 2;
        if(body.IsSleeping())
            GetComponent<MeshRenderer>().material.color = GetComponent<MeshRenderer>().material.color / 3;
    }
    static Vector3 ApplyTensor(Vector3 vec, Vector3 eigen, Quaternion rot)
    {
        var a = Quaternion.Inverse(rot) * vec;
        return rot * new Vector3(eigen.x * a.x, eigen.y * a.y, eigen.z * a.z);
    }
}
