using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
public class XRSeat : XRInteractable
{
    public Transform seat;
    [Header("Debug")]
    public bool isSitting = false;
    public bool isOccupied = false;

    public void OnOtherOccupy()
    {
        if(isSitting)
            XRPlayerLocomotion.instance.LeaveSeat();
        isOccupied = true;
    }
    public void OnOtherFree()
    {
        isOccupied = false;
    }

    public Behaviour[] enableWhenSit;
    public UnityEvent onSit;
    public UnityEvent onLeave;

    public override bool CanInteract(XRHand hand, out int priority)
    {
        priority = -2;return isActiveAndEnabled && !isOccupied;
    }
    public override void OnInteract()
    {
        if (isOccupied) return;

        Debug.Log("EnterSeat " + gameObject.name);
        XRPlayerLocomotion.instance.Sit(seat);

        XRPlayerLocomotion.instance.onLeaveSeat.AddListener(OnLeaveSeat);//Be careful with the order
        foreach (var b in enableWhenSit)if(b) b.enabled = true;
        isSitting = isOccupied=true;
        onSit.Invoke();
    }
    void OnLeaveSeat()
    {
        Debug.Log("LeaveSeat " + gameObject.name);
        foreach (var b in enableWhenSit) b.enabled = false;
        XRPlayerLocomotion.instance.onLeaveSeat.RemoveListener(OnLeaveSeat);
        isSitting = isOccupied= false;
        onLeave.Invoke();
    }
    private void OnDisable()
    {
        if (isSitting)
            XRPlayerLocomotion.instance.LeaveSeat();
        if (outline) outline.enabled = false;
    }
    private void Awake()
    {
        if (outline) outline.enabled = false;
    }
    public override void SetHovering(bool isHovering)
    {
        if (outline) outline.enabled = isHovering;
    }
    public Behaviour outline;
}
