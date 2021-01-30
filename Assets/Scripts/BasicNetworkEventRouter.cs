using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Photon.Pun;

[RequireComponent(typeof(PhotonView))]
public class BasicNetworkEventRouter : MonoBehaviourPun
{
    public enum SendTarget { All, Others, Master};

    [System.Serializable]
    public struct RouterInfo
    {
        public string name;
        public RpcTarget target;
        public UnityEvent callback;
    }
    public RouterInfo[] routers;

    [PunRPC]
    void RPCTriggerEvent(string name)
    {
        foreach (var r in routers)
            if (r.name == name)
                r.callback.Invoke();
    }
    public void TriggerEvent(string name)
    {
        foreach (var r in routers)
            if (r.name == name)
            {
                photonView.RPC("RPCTriggerEvent", r.target, name);
            }

    }
}
