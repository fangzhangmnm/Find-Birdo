using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WheelEffects : MonoBehaviour
{
    [Header("Setup")]
    public WheelCollider[] allWheels;
    public AudioClip skidAudio;
    public ParticleSystem skidParticleEmitter;
    public GameObject skidTrailPrefab;
    AudioSource[] allWheelAudioSources;
    TrailRenderer[] allWheelTrails;
    Transform[] allWheelMeshes;

    [field: Header("Values")]
    [field: SerializeField] public bool[] isSkid { get; set; }
    [field: SerializeField]  public float[] suspension { get; set; }
    [field: SerializeField]  public float[] steering { get; set; }
    [field: SerializeField]  public float[] rpm { get; set; }
    public bool doFetchValues { get; set; } = true;

    float[] rotation { get; set; }


    void Awake()
    {
        int n = allWheels.Length;
        allWheelMeshes = new Transform[n];
        allWheelAudioSources = new AudioSource[n];
        allWheelTrails = new TrailRenderer[n];
        isSkid = new bool[n];
        suspension = new float[n];
        steering = new float[n];
        rpm = new float[n];
        rotation = new float[n];

        for (int i = 0; i < allWheels.Length; ++i)
        {
            allWheelMeshes[i] = allWheels[i].GetComponentInChildren<Renderer>().transform;
            allWheelAudioSources[i] = allWheels[i].GetComponentInChildren<AudioSource>();
            if (allWheelAudioSources[i] == null) allWheelAudioSources[i] = allWheelMeshes[i].gameObject.AddComponent<AudioSource>();
            allWheelAudioSources[i].spatialBlend = 1;
            allWheelAudioSources[i].loop = false;
        }
    }
    private void Update()
    {
        if(doFetchValues)
            FetchValues();
        UpdateEffects(Time.fixedDeltaTime);
    }
    void FetchValues()
    {
        for (int i = 0; i < allWheels.Length; ++i)
        {
            WheelCollider wheel = allWheels[i];
            wheel.GetWorldPose(out Vector3 pos, out Quaternion rot);
            bool isGrounded= wheel.GetGroundHit(out WheelHit hit);

            isSkid[i]= isGrounded &&
                (Mathf.Abs(hit.forwardSlip) >= wheel.forwardFriction.extremumSlip * .9f || Mathf.Abs(hit.sidewaysSlip) >= wheel.sidewaysFriction.extremumSlip * .9f);
            rpm[i] = wheel.rpm;
            steering[i] = wheel.steerAngle;
            suspension[i] = wheel.transform.InverseTransformPoint(pos).y;
        }
    }
    void UpdateEffects(float dt)
    {
        for (int i = 0; i < allWheels.Length; ++i)
        {
            WheelCollider wheel = allWheels[i];

            allWheelMeshes[i].position = wheel.transform.TransformPoint(new Vector3(0,suspension[i],0));
            rotation[i] += rpm[i] * dt * 360 / 60f;
            allWheelMeshes[i].rotation = wheel.transform.rotation * Quaternion.Euler(0,steering[i], 0)*Quaternion.Euler(rotation[i], 0, 0);
            //if (wheelGrounded[i] && (Mathf.Abs(forwardSlip[i]) >= allWheels[i].forwardFriction.extremumSlip * .9f || Mathf.Abs(sidewaysSlip[i]) >= allWheels[i].sidewaysFriction.extremumSlip * .9f))
            if (isSkid[i])
            {
                bool skidAudioPlaying = false;
                //foreach (var a in allWheelAudioSources)
                //    if (a.isPlaying)
                //        skidAudioPlaying = true;//repetitive checking
                skidAudioPlaying = allWheelAudioSources[i].isPlaying;
                if (!skidAudioPlaying)
                {
                    allWheelAudioSources[i].time = 0;
                    allWheelAudioSources[i].volume = 0;
                    allWheelAudioSources[i].clip = skidAudio;
                    allWheelAudioSources[i].Play();
                }
                if (allWheelAudioSources[i].isPlaying)
                    allWheelAudioSources[i].volume = Mathf.Clamp01(allWheelAudioSources[i].time / .2f);
                if (allWheelTrails[i] == null)
                {
                    if (skidTrailPrefab != null)
                    {
                        allWheelTrails[i] = Instantiate(skidTrailPrefab).GetComponent<TrailRenderer>();
                        allWheelTrails[i].alignment = LineAlignment.TransformZ;
                        allWheelTrails[i].textureMode = LineTextureMode.Tile;
                        allWheelTrails[i].transform.rotation = Quaternion.LookRotation(transform.up, transform.forward);
                    }
                }
                if (allWheelTrails[i] != null)
                {
                    allWheelTrails[i].transform.position = allWheelMeshes[i].position + transform.up * allWheels[i].radius * allWheels[i].transform.lossyScale.x * -.9f;
                }
                if (skidParticleEmitter != null)
                {
                    skidParticleEmitter.Emit(1);
                }
            }
            else
            {
                //if(allWheelAudioSources[i].time>1f)
                //if(allWheelAudioSources[i].isPlaying)
                allWheelAudioSources[i].Stop();
                if (allWheelTrails[i] != null)
                {
                    Destroy(allWheelTrails[i].gameObject, 1f);
                    allWheelTrails[i] = null;
                }
            }
        }

    }
}
