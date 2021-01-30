using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using ExitGames.Client.Photon;
using System.Reflection;
using System;


public class SyncVarView : MonoBehaviour, IPunObservable
{
    public Component target;
    public string trueIfIsMine;
    public string[] propertyNames;
    PropertyInfo trueIfIsMineInfo;
    List<PropertyInfo> propertyInfos=new List<PropertyInfo>();

    
    void Awake()
    {
        if (target)
        {
            Type targetType = target.GetType();
            propertyInfos.Clear();
            foreach(var pn in propertyNames)
            {
                PropertyInfo info = targetType.GetProperty(pn);
                if (info != null)
                    propertyInfos.Add(info);
                else
                    Debug.LogError("Cannot find property " + pn);

            }
            if (trueIfIsMine != "")
            {
                trueIfIsMineInfo = targetType.GetProperty(trueIfIsMine);
                if(trueIfIsMineInfo==null)
                    Debug.LogError("Cannot find property " + trueIfIsMine);
            }
            else
                trueIfIsMineInfo = null;
        }
    }
    void OnValidate()
    {
        Awake();
    }
    void IPunObservable.OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsReading)
        {
            if (trueIfIsMineInfo != null)
                trueIfIsMineInfo.SetValue(target, false);
            foreach (var p in propertyInfos)
                p.SetValue(target,stream.ReceiveNext());
        }
        else
        {
            if (trueIfIsMineInfo != null)
                trueIfIsMineInfo.SetValue(target, true);
            foreach (var p in propertyInfos)
                stream.SendNext(p.GetValue(target));
            
        }
    }
    //Not used
    //https://www.c-sharpcorner.com/article/boosting-up-the-reflection-performance-in-c-sharp/
}
