using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


public class AirFoil : MonoBehaviour
{
    public BoxCollider shapeReferenceCollider = null;
    public AirFoilSettings airFoilSettings;
    public float chordLength = 1;
    public float sectionLength = 1;
    public int segmentation = 4;
    public bool isFlap = false;

    [ReadOnly, SerializeField] public float wingArea;

    [ReadOnly, SerializeField] public float angleOfAttack;
    [ReadOnly, SerializeField] public float planarFlowSpeed;
    [ReadOnly, SerializeField] public float lift;
    [ReadOnly, SerializeField] public float drag;
    [ReadOnly, SerializeField] public Vector3 force;

    [HideInInspector] public float[] segAngleOfAttack;
    [HideInInspector] public float[] segPlanarFlowSpeed;
    [HideInInspector] public float[] segLift;
    [HideInInspector] public float[] segDrag;
    [HideInInspector] public Vector3[] segForce;
    [ReadOnly] public Vector3 foilBodyCOMPositionNoScale;
    [ReadOnly] public Quaternion foilBodyRotation;

    private void Start()
    {
        OnValidate();
        segAngleOfAttack = new float[segmentation];
        segPlanarFlowSpeed = new float[segmentation];
        segLift = new float[segmentation];
        segDrag = new float[segmentation];
        segForce = new Vector3[segmentation];
    }
    private void OnDrawGizmosSelected()
    {
        OnValidate();
    }
    private void OnValidate()
    {
        if (shapeReferenceCollider)
        {
            chordLength = shapeReferenceCollider.size.z * shapeReferenceCollider.transform.localScale.z;
            sectionLength = shapeReferenceCollider.size.x * shapeReferenceCollider.transform.localScale.x;
            if (shapeReferenceCollider.transform.parent)
            {
                float lossy = shapeReferenceCollider.transform.parent.lossyScale.magnitude / Mathf.Sqrt(3);
                chordLength *= lossy;
                sectionLength *= lossy;
            }
            wingArea = chordLength * sectionLength;
        }
    }
}

