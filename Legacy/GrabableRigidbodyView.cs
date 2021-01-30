using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.Events;

[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(Rigidbody))]
public class GrabableRigidbodyView : MonoBehaviourPun, IPunObservable, IPunOwnershipCallbacks, IOnPhotonViewOwnerChange
{

    float minIgnoreOwnershipTime = 0.100f;
    float maxExtrapolationTime = 0.500f;
    float adjustBodyTime = 0.500f;
    public float teleportDist = 1.000f;
    public UnityEvent onOtherPlayerTake;
    bool debug_color = false;

    Rigidbody body;
    DeadReckoningVector DRV = new DeadReckoningVector();
    DeadReckoningQuaternion DRQ = new DeadReckoningQuaternion();
    [Header("Debug Exposed")]
    public float averageLag = 0;
    public int debug_sendReceive_blinker;
    public Vector3 debug_sentReceived_Position;
    public bool isPickedUpByMeLocally = false;
    public bool isPickedUpByOther = false;
    public float ignoreOwnshipCountdown = 0;
    bool firstTake;
    bool _isCollidingLastFrame;
    public bool isColliding=false;


    void ResetIgnoreOwnership()=> ignoreOwnshipCountdown = 0;
    void FlagIgnoreOwnership()=>ignoreOwnshipCountdown = Mathf.Clamp(averageLag * 3f, minIgnoreOwnershipTime,10);
    bool IsIgnoreOwnership() => ignoreOwnshipCountdown > 0;

    bool ownershipTransferPending = false;
    void IOnPhotonViewOwnerChange.OnOwnerChange(Player newOwner, Player previousOwner)
    {
        ownershipTransferPending = false;
        Debug.Log($"Ownership Changed {previousOwner}->{newOwner}");
    }
    public void OnOwnershipTransfered(PhotonView targetView, Player previousOwner)
    {
        ownershipTransferPending = false;
        Debug.Log($"Ownership Transfered {previousOwner}->{targetView.Owner}");
    }
    void IPunOwnershipCallbacks.OnOwnershipRequest(PhotonView targetView, Player requestingPlayer)
    {
        Debug.Assert(false);
        Debug.Log("OnOwnershipRequest was called");
        targetView.TransferOwnership(requestingPlayer);
    }

    void UpdateOwnership()
    {
        if (!ownershipTransferPending && PhotonNetwork.IsConnectedAndReady)
        {
            if(isPickedUpByMeLocally && !photonView.IsMine)
            {
                ownershipTransferPending = true;
                photonView.RequestOwnership();
            }else if(!isPickedUpByMeLocally && photonView.IsMine)
            {
                ownershipTransferPending = true;
                photonView.TransferOwnership(PhotonNetwork.MasterClient);
            }
        }
    }
    [PunRPC]
    void RPCOnOtherPlayerPickupOrDrop(bool pickup)
    {
        if (pickup)
        {
            isPickedUpByMeLocally = false;
            onOtherPlayerTake.Invoke();
            isPickedUpByOther = true;
            body.isKinematic = true;
            DRV.FlagReset();
            DRQ.FlagReset();
        }
        else
        {
            isPickedUpByOther = false;
            body.isKinematic = false;
        }
    }
    [PunRPC]
    void RPCSetState(Vector3 newPosition, Vector3 newVelocity, Quaternion newRotation, Vector3 newAngularVelocity, double sentTime, PhotonMessageInfo info)
    {

        float lagUnclamped = Mathf.Clamp((float)(PhotonNetwork.Time - info.SentServerTime), 0, 10);

        Debug.Log($"SetState was called, lag {lagUnclamped * 1000}ms, position {newPosition} velocity {newVelocity} delta {newPosition + lagUnclamped * newVelocity - body.position}. Currently isMine={photonView.IsMine} IsKinematic={body.isKinematic}");

        //if (!photonView.IsMine && !isPickedUpByOther)
        //    FlagIgnoreOwnership();//???

        var extrapolated = Extrapolate(newPosition, newVelocity, newRotation, newAngularVelocity, lagUnclamped);

        AdjustBody(extrapolated.Item1, extrapolated.Item2, extrapolated.Item3, extrapolated.Item4);


    }


    public void OnLocalPlayerPickup()
    {
        isPickedUpByMeLocally = true;
        isPickedUpByOther = false;
        body.isKinematic = false;
        if (PhotonNetwork.IsConnectedAndReady)
        {
            photonView.RPC("RPCOnOtherPlayerPickupOrDrop", RpcTarget.OthersBuffered, true);//TODO deal with buffers
            UpdateOwnership();
            FlagIgnoreOwnership();
        }
    }
    public void OnLocalPlayerDrop()
    {
        isPickedUpByMeLocally = false;
        if (PhotonNetwork.IsConnectedAndReady)
        {

            Debug.Log($"Call SetState {body.position} {body.velocity}");
            photonView.RPC("RPCOnOtherPlayerPickupOrDrop", RpcTarget.OthersBuffered, false);
            photonView.RPC("RPCSetState", PhotonNetwork.MasterClient, body.position, body.velocity, body.rotation, body.angularVelocity, PhotonNetwork.Time);
            UpdateOwnership();
            FlagIgnoreOwnership();
        }
    }
    
    public void OnHitByLocalPlayer()
    {
        if (PhotonNetwork.IsConnectedAndReady)
        {
            if (!photonView.IsMine && !isPickedUpByOther && !isPickedUpByMeLocally)
            {
                Debug.Log($"Call SetState {body.position} {body.velocity}");
                photonView.RPC("RPCSetState", photonView.Owner, body.position, body.velocity, body.rotation, body.angularVelocity, PhotonNetwork.Time);
                FlagIgnoreOwnership();
            }
        }
    }
    protected virtual (Vector3,Vector3,Quaternion,Vector3) Extrapolate(Vector3 newPosition, Vector3 newVelocity, Quaternion newRotation, Vector3 newAngularVelocity,float lagUnclamped)
    {
        float lag = Mathf.Clamp(lagUnclamped, 0, maxExtrapolationTime);

        Vector3 extrapolatedPosition = newPosition + lag * newVelocity;
        Vector3 extrapolatedVelocity = newVelocity;

        if (body.useGravity && !isColliding)
        {
            extrapolatedPosition += .5f * lag * lag * Physics.gravity;
            extrapolatedVelocity += lag * Physics.gravity;
        }//should not use gravity. penetrate floor at even 100ms

        if (isColliding)
        {
            extrapolatedPosition = newPosition;
        }
        else
        {
            Vector3 delta = extrapolatedPosition - body.position;
            if (delta.magnitude > 0)//Prevent Penetration
            {
                if (body.SweepTest(delta.normalized, out RaycastHit hit, delta.magnitude))
                    delta = Vector3.ClampMagnitude(delta, hit.distance);
            }
            extrapolatedPosition = body.position + delta;
        }

        Quaternion extrapolatedRotation = GetRotationQuaternion(lag * body.angularVelocity) * newRotation;
        return (extrapolatedPosition, extrapolatedVelocity, extrapolatedRotation, newAngularVelocity);
    }
    void AdjustBody(Vector3 newPosition, Vector3 newVelocity, Quaternion newRotation, Vector3 newAngularVelocity)
    {
        Vector3 delta = newPosition - body.position;
        if (delta.magnitude > teleportDist || body.isKinematic)
        {
            body.position = newPosition;
            body.velocity = newVelocity;
            body.rotation = newRotation;
            body.angularVelocity = newAngularVelocity;
        }
        else
        {
            body.velocity = (newPosition - body.position) / adjustBodyTime + newVelocity;
            body.angularVelocity = GetRotationVector(body.rotation, newRotation) / adjustBodyTime + newAngularVelocity;
        }
    }
    void OnCollisionStay(Collision c)
    {
        _isCollidingLastFrame = true;
    }
    /*
    //Not working well
    void OnCollisionExit(Collision other)
    {
        var view = other.gameObject.GetComponentInParent<GrabableRigidbodyView>();
        if (view && view != this && isPickedUpByMeLocally)
        {
            view.OnHitByLocalPlayer();
        }
        if (!isPickedUpByMeLocally && other.collider.GetComponent<LocalPlayerColliderMarker>())
        {
            this.OnHitByLocalPlayer();
        }
    }
    */
    void OnCollisionEnter(Collision other)
    {
        var view = other.gameObject.GetComponentInParent<GrabableRigidbodyView>();
        if (view && view != this && isPickedUpByMeLocally)
        {
            view.OnHitByLocalPlayer();
        }
        if (!isPickedUpByMeLocally && other.collider.GetComponent<LocalPlayerColliderMarker>())
        {
            this.OnHitByLocalPlayer();
        }
    }


    void FixedUpdate()
    {
        isColliding = _isCollidingLastFrame || body.IsSleeping();
        _isCollidingLastFrame = false;
        if (PhotonNetwork.IsConnectedAndReady)
        {
            if (isPickedUpByOther)
            {
                if(!body.isKinematic)body.isKinematic = true;
                if (DRV.isReady)
                {
                    body.MovePosition(DRV.Update(Time.fixedDeltaTime));
                    body.velocity = DRV.lastVelocity;
                }
                if (DRQ.isReady)
                {
                    body.MoveRotation(DRQ.Update(Time.fixedDeltaTime));
                    body.angularVelocity = DRQ.lastAngularVelocity;
                }
            }
            else
            {
                if(body.isKinematic)body.isKinematic = false;
            }

            UpdateOwnership();
        }


        if (debug_color)
        {
            if (photonView.IsMine)
                GetComponent<MeshRenderer>().material.color = Color.green;
            else if (IsIgnoreOwnership())
                GetComponent<MeshRenderer>().material.color = Color.blue;
            else
                GetComponent<MeshRenderer>().material.color = Color.red;
            if (isColliding)
                GetComponent<MeshRenderer>().material.color = GetComponent<MeshRenderer>().material.color / 2;
        }
    }


    void IPunObservable.OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        float sendInterval = 1.0f / PhotonNetwork.SerializationRate;
        float lagUnclamped = Mathf.Clamp((float)(PhotonNetwork.Time - info.SentServerTime), 0, 10);

        if (stream.IsWriting)
        {
            debug_sendReceive_blinker += 1;
            debug_sentReceived_Position = body.position;

            stream.SendNext(body.position);
            stream.SendNext(body.velocity);
            stream.SendNext(body.rotation);
            stream.SendNext(body.angularVelocity);

            ResetIgnoreOwnership();
        }
        else
        {
            debug_sendReceive_blinker -= 1;
            averageLag = Mathf.Lerp(averageLag, lagUnclamped, sendInterval / 5f);

            Vector3 newPosition = (Vector3)stream.ReceiveNext();
            Vector3 newVelocity = (Vector3)stream.ReceiveNext();
            Quaternion newRotation = (Quaternion)stream.ReceiveNext();
            Vector3 newAngularVelocity = (Vector3)stream.ReceiveNext();

            debug_sentReceived_Position = newPosition;


            if (isPickedUpByOther)
            {
                float lag = Mathf.Clamp(lagUnclamped, 0, maxExtrapolationTime);
                DRV.NetworkUpdate(newPosition, newVelocity, lag, firstTake);
                DRQ.NetworkUpdate(newRotation, newAngularVelocity, lag, firstTake);
            }
            else
            {
                if (!IsIgnoreOwnership() && !isPickedUpByMeLocally)
                {
                    var extrapolated = Extrapolate(newPosition, newVelocity, newRotation, newAngularVelocity, lagUnclamped);

                    AdjustBody(extrapolated.Item1, extrapolated.Item2, extrapolated.Item3, extrapolated.Item4);
                }

            }
            ignoreOwnshipCountdown = Mathf.Max(ignoreOwnshipCountdown - sendInterval, 0);
        }
        firstTake = false;
    }

    void Awake()
    {
        body = GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
        firstTake = true;
        photonView.AddCallbackTarget(this);
    }
    void OnDisable()
    {
        photonView.RemoveCallbackTarget(this);
    }
    void OnValidate()
    {
        Debug.Assert(photonView.OwnershipTransfer == OwnershipOption.Takeover);
    }
    #region Utils
    static Vector3 GetRotationVector(Quaternion from, Quaternion to)
    {
        (to * Quaternion.Inverse(from)).ToAngleAxis(out float angle, out Vector3 axis);
        if (angle >= 360) return Vector3.zero;
        if (angle > 180) angle -= 360;
        return angle * Mathf.Deg2Rad * axis;
        //Only (-1,0,0,0) will return NaN
    }
    static Quaternion GetRotationQuaternion(Vector3 axis)=> Quaternion.AngleAxis(axis.magnitude * Mathf.Rad2Deg, axis.normalized);
    #endregion
}
