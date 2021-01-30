using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicPlayerSettingUI : MonoBehaviour
{
    public void DoToggleTeleportMode(bool b)
    {
        XRPlayerLocomotion.instance.teleportMode = b;
    }
}
