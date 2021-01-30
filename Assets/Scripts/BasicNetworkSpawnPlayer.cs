using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class BasicNetworkSpawnPlayer: MonoBehaviourPunCallbacks
{
    public string playerPrefabName;
    public Transform playerSpawn;
    private void Start()
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            PhotonNetwork.LoadLevel("LoginScene");
            return;
        }
        var go=PhotonNetwork.Instantiate(playerPrefabName, playerSpawn.position, playerSpawn.rotation);
        go.name += "(local)";
    }
}
