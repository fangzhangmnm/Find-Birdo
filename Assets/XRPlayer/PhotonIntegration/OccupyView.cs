using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.Events;

[RequireComponent(typeof(PhotonView))]
public class OccupyView : MonoBehaviourPun
{
    public float occupancyTTL = 5f;
    public float claimOccupancyInterval = 1f;
    [System.Serializable]
    public class SubOccupyView
    {
        public UnityEvent onOtherOccupied;
        public UnityEvent onOtherFree;
        public float occupancyCD = 0;
        public float refreshOccupancyCD;
        public OccupancyState occupancyState = OccupancyState.Free;
    }
    public enum OccupancyState { Free, OccupiedByOthers, OccupiedByLocalPlayer }
    public SubOccupyView[] occupyViews;

    public void Occupy(int subID)
    {
        SubOccupyView occupyView = occupyViews[subID];

        occupyView.occupancyState = OccupancyState.OccupiedByLocalPlayer;
        if(PhotonNetwork.IsConnectedAndReady)
            photonView.RPC("ClaimOccupancy", RpcTarget.Others,subID);
        occupyView.refreshOccupancyCD = claimOccupancyInterval;
    }
    [PunRPC]
    void ClaimOccupancy(int subID)
    {
        SubOccupyView occupyView = occupyViews[subID];

        occupyView.occupancyState = OccupancyState.OccupiedByOthers;
        occupyView.onOtherOccupied.Invoke();
        occupyView.occupancyCD = occupancyTTL;
    }

    public void Free(int subID)
    {
        SubOccupyView occupyView = occupyViews[subID];

        occupyView.occupancyState = OccupancyState.Free;
        if (PhotonNetwork.IsConnectedAndReady)
            photonView.RPC("FreeOccupancy", RpcTarget.Others,subID);
    }
    [PunRPC]
    void FreeOccupancy(int subID)
    {
        SubOccupyView occupyView = occupyViews[subID];

        occupyView.occupancyState = OccupancyState.Free;
        occupyView.onOtherFree.Invoke();
    }

    private void Update()
    {
        for(int subID = 0; subID < occupyViews.Length;++subID)
        {
            SubOccupyView occupyView = occupyViews[subID];

            if (occupyView.occupancyState == OccupancyState.OccupiedByOthers)
            {
                occupyView.occupancyCD -= Time.deltaTime;
                if (occupyView.occupancyCD < 0)
                {
                    occupyView.occupancyState = OccupancyState.Free;
                    occupyView.onOtherFree.Invoke();
                }
            }
            else if (occupyView.occupancyState == OccupancyState.OccupiedByLocalPlayer)
            {

                occupyView.refreshOccupancyCD -= Time.deltaTime;
                if (occupyView.refreshOccupancyCD < 0)
                {
                    if (PhotonNetwork.IsConnectedAndReady)
                        photonView.RPC("ClaimOccupancy", RpcTarget.Others, subID);
                    occupyView.refreshOccupancyCD = claimOccupancyInterval;
                }
            }
        }
    }
}
