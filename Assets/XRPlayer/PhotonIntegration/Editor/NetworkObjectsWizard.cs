using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Photon.Pun;
using UnityEngine.Events;
using UnityEditor.Events;

public class NetworkObjectsWizard : MonoBehaviour
{
    [MenuItem("GameObject/Convert To Network Grabable",false,0)]
    static void ConvertGrabable()
    {
        Debug.Assert(Selection.activeGameObject);
        var go = Selection.activeGameObject;
        Undo.RegisterFullObjectHierarchyUndo(go, "Convert To Network Grabable");

        Debug.Log("Converting " + go.name + " to Network Grabable...");
        SetLayerRecursively(go, LayerMask.NameToLayer("Interactable"));

        var grabable = go.GetComponent<XRGrabable>()? go.GetComponent<XRGrabable>():Undo.AddComponent<XRGrabable>(go);
        var body = go.GetComponent<Rigidbody>();
        var view= go.GetComponent<PhotonView>() ? go.GetComponent<PhotonView>() : Undo.AddComponent<PhotonView>(go);
        view.OwnershipTransfer = OwnershipOption.Takeover;
        var grabableView = go.GetComponent<GrabableView>() ? go.GetComponent<GrabableView>() : Undo.AddComponent<GrabableView>(go);
        var outline = go.GetComponent<Outline>() ? go.GetComponent<Outline>() : go.AddComponent<Outline>();
        outline.precomputeOutline = true;

        grabable.outline = outline;

        grabableView.onOtherPlayerTake = new UnityEvent();
        grabable.onDrop = new UnityEvent();
        grabable.onPickUp = new UnityEvent();
        grabable.updateAttach = new XRGrabable.UpdateTransform();

        UnityEventTools.AddPersistentListener(grabableView.onOtherPlayerTake, grabable.DetachIfAttached);
        UnityEventTools.AddPersistentListener(grabable.onDrop, grabableView.OnLocalPlayerDrop);
        UnityEventTools.AddPersistentListener(grabable.onPickUp, grabableView.OnLocalPlayerPickup);
        UnityEventTools.AddPersistentListener(grabable.updateAttach, grabableView.SetAttach);
        string prompt=  "Completed! You should Configure:\n" +
             "1. rigidbody.collisionDetectionMode, \n" +
             "2. rigidbody.mass, \n" +
             "3. collider.material, \n" +
             "4. XRGrabable.attachMode, \n" +
             "5. XRGrabable.breakWhenLostTrack, ";
        EditorUtility.DisplayDialog("Convert To Network Grabable",prompt, "ok","ok");
        Debug.Log(prompt);
        if (!go.GetComponentInChildren<Collider>())
            Debug.LogWarning("Did you forget to add the colliders?");
        if (go.GetComponent<MeshFilter>() && !go.GetComponent<MeshFilter>().sharedMesh.isReadable)
            Debug.LogError("Please enable Reading in the Mesh Import Settings");
    }
    static void SetLayerRecursively(GameObject go, int layer)
    {
        if (go != null)
        {
            go.layer = layer;
            for (int i = 0; i < go.transform.childCount; ++i)
                SetLayerRecursively(go.transform.GetChild(i).gameObject, layer);
        }
    }
}
