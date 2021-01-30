using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class GameObjectView : MonoBehaviourPunCallbacks
{
    static void Instantiate(string path, Vector3 pos, Quaternion rot)
    {
        PhotonNetwork.Instantiate(path, pos, rot);
    }
    void RPC()
    {

    }
}
