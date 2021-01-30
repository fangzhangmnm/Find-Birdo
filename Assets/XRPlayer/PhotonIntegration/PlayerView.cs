using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

[DefaultExecutionOrder(-9)]
[RequireComponent(typeof(PhotonView))]
public class PlayerView : MonoBehaviourPun,IPunObservable
{
    public Behaviour[] localPlayerControlScripts;
    public Rigidbody[] localPlayerRigidbodies;
    public Collider[] localPlayerColliders;
    public GameObject[] localPlayerHidden;
    public int layerToUnhide;

    public Transform playerRoot;
    public Transform[] riggingTransforms;
    [System.Serializable]
    public struct AnimInfo
    {
        public Animator animator;
        public string[] bools;
        public string[] floats;
        public string[] integers;
    }
    public AnimInfo[] animators;
    bool firstTake;

    public void MarkTeleport() { firstTake = true; }
    [Header("Debug Exposed")]
    public int attachedPhotonViewID=0;
    public Transform attachedTransform;
    public void SetAttach(Transform attachedChildTransform)
    {
        if (photonView.IsMine)
        {
            var attachedPhotonView = attachedChildTransform?attachedChildTransform.GetComponentInParent<PhotonView>():null;
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
    }

    DeadReckoningVector DRPlayerRootV = new DeadReckoningVector();
    DeadReckoningQuaternion DRPlayerRootQ = new DeadReckoningQuaternion();
    DeadReckoningVector[] DRRiggingV;
    DeadReckoningQuaternion[] DRRiggingQ;
    void InitDR()
    {
        DRRiggingV = new DeadReckoningVector[riggingTransforms.Length];
        DRRiggingQ = new DeadReckoningQuaternion[riggingTransforms.Length];
        for(int i = 0; i < riggingTransforms.Length; ++i)
        {
            DRRiggingV[i]= new DeadReckoningVector();
            DRRiggingQ[i] = new DeadReckoningQuaternion();
        }
    }

    void OnEnable()
    {
        MarkTeleport();
    }

    void IPunObservable.OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        float sendInterval = 1.0f / PhotonNetwork.SerializationRate;
        float lag = Mathf.Max(0,(float)(PhotonNetwork.Time - info.SentServerTime));

        if (stream.IsWriting)
        {
            Vector3 playerPosition = attachedTransform ? attachedTransform.InverseTransformPoint(playerRoot.position) : playerRoot.position;
            Quaternion playerRotation = attachedTransform ? Quaternion.Inverse(attachedTransform.rotation) * playerRoot.rotation : playerRoot.rotation;

            stream.SendNext(attachedPhotonViewID);
            stream.SendNext(playerPosition);
            stream.SendNext(DRPlayerRootV.EstimateVelocity(playerPosition, sendInterval, firstTake));
            stream.SendNext(playerRotation);
            stream.SendNext(DRPlayerRootQ.EstimateVelocity(playerRotation, sendInterval, firstTake));
            for (int i = 0; i < riggingTransforms.Length; ++i)
            {
                stream.SendNext(toLocal(riggingTransforms[i].position));
                stream.SendNext(DRRiggingV[i].EstimateVelocity(toLocal(riggingTransforms[i].position), sendInterval, firstTake));
                stream.SendNext(toLocal(riggingTransforms[i].rotation));
                stream.SendNext(DRRiggingQ[i].EstimateVelocity(toLocal(riggingTransforms[i].rotation), sendInterval, firstTake));
            }
            for(int i = 0; i < animators.Length; ++i)
            {
                var anim = animators[i];
                foreach (var s in anim.floats)
                    stream.SendNext(anim.animator.GetFloat(s));
                foreach (var s in anim.bools)
                    stream.SendNext(anim.animator.GetBool(s));
                foreach (var s in anim.integers)
                    stream.SendNext(anim.animator.GetInteger(s));
            }
        }
        else
        {
            Transform oldAttachedTransform = attachedTransform;

            attachedPhotonViewID = (int)stream.ReceiveNext();

            if (attachedPhotonViewID == 0)
                attachedTransform = null;
            else
                attachedTransform = PhotonView.Find(attachedPhotonViewID).transform;
            if (oldAttachedTransform != attachedTransform)
                MarkTeleport();


            DRPlayerRootV.NetworkUpdate((Vector3)stream.ReceiveNext(), (Vector3)stream.ReceiveNext(), lag, firstTake);
            DRPlayerRootQ.NetworkUpdate((Quaternion)stream.ReceiveNext(), (Vector3)stream.ReceiveNext(), lag, firstTake);
            for (int i = 0; i < riggingTransforms.Length; ++i)
            {
                DRRiggingV[i].NetworkUpdate((Vector3)stream.ReceiveNext(), (Vector3)stream.ReceiveNext(), lag, firstTake);
                DRRiggingQ[i].NetworkUpdate((Quaternion)stream.ReceiveNext(), (Vector3)stream.ReceiveNext(), lag, firstTake);
            }
            for (int i = 0; i < animators.Length; ++i)
            {
                var anim = animators[i];
                foreach (var s in anim.floats)
                    anim.animator.SetFloat(s,(float)stream.ReceiveNext());
                foreach (var s in anim.bools)
                    anim.animator.SetBool(s, (bool)stream.ReceiveNext());
                foreach (var s in anim.integers)
                    anim.animator.SetInteger(s, (int)stream.ReceiveNext());
            }
        }
        firstTake = false;
    }
    void FixedUpdate()
    {
        if (!firstTake && !photonView.IsMine)
        {
            if (DRPlayerRootV.isReady)
            {
                Vector3 playerPosition = DRPlayerRootV.Update(Time.fixedDeltaTime);
                playerRoot.position = attachedTransform ? attachedTransform.TransformPoint(playerPosition) : playerPosition;
            }
            if (DRPlayerRootQ.isReady)
            {
                Quaternion playerRotation = DRPlayerRootQ.Update(Time.fixedDeltaTime);
                playerRoot.rotation = attachedTransform ? attachedTransform.rotation * playerRotation : playerRotation;
            }
            for (int i = 0; i < riggingTransforms.Length; ++i)
            {
                if (DRRiggingV[i].isReady)
                    riggingTransforms[i].position = fromLocal(DRRiggingV[i].Update(Time.fixedDeltaTime));
                if (DRRiggingQ[i].isReady)
                    riggingTransforms[i].rotation = fromLocal(DRRiggingQ[i].Update(Time.fixedDeltaTime));
            }
        }
    }

    void Awake()
    {
        InitDR();
        if (photonView.IsMine)
        {
            Debug.Log("Instantiate local Player");
            foreach (var b in localPlayerControlScripts)
            {
                b.enabled = true;
            }
            Debug.Log("SendRate: 1/" + PhotonNetwork.SerializationRate);
        }
        else
        {
            Debug.Log("Instantiate remote Player");
            foreach (var b in localPlayerControlScripts)
            {
                Destroy(b);
            }
            foreach (var r in localPlayerRigidbodies)
            {
                if (r.collisionDetectionMode == CollisionDetectionMode.ContinuousDynamic)
                    r.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                r.isKinematic = true;
            }
            foreach (var g in localPlayerHidden)
            {
                SetLayerRecursively(g, layerToUnhide);
            }
            foreach(var c in localPlayerColliders)
            {
                Destroy(c);
            }
        }
    }
    #region Utils
    static void SetLayerRecursively(GameObject go, int layer)
    {
        if (go != null)
        {
            go.layer = layer;
            for (int i = 0; i < go.transform.childCount; ++i)
                SetLayerRecursively(go.transform.GetChild(i).gameObject, layer);
        }
    }
    Vector3 toLocal(Vector3 p) => playerRoot.InverseTransformPoint(p);
    Vector3 fromLocal(Vector3 p) => playerRoot.TransformPoint(p);
    Quaternion toLocal(Quaternion q) => Quaternion.Inverse(playerRoot.rotation) * q;
    Quaternion fromLocal(Quaternion q) => playerRoot.rotation * q;
    #endregion
}
