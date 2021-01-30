using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarEffects : MonoBehaviour
{
    [Header("Effects")]
    public AudioClip skidAudio;
    public ParticleSystem skidParticles;
    public GameObject skidTrailPrefab;
    public AudioSource engineSoundSource;
    public AudioClip engineAudio;
    AudioSource[] allWheelAudioSources;
    TrailRenderer[] allWheelTrails;
    public float engineAudioRPM = 8000;
    public float engineAudioPitchSlope = 1f;
    public float engineAudioMaxPitch = 1.5f;
    public float engineAudioMinPitch = .2f;
    public float engineAudioMinVolume = .25f;
    public float engineAudioMaxVolume = 1f;
    [ReadOnly] public float currentEngineAudioPitch;
    public GameObject[] headLampOn,headLampOff,rearLampOn,rearLampOff;

    [Header("Input")]
    public bool[] isSkid;
    public float currentEngineRPM;
    public float currentThrottle;
    public bool headLamp;
    public bool rearLamp;


    WheelCollider[] allWheels;
    Transform[] allWheelMeshes;
    private void Awake()
    {
        allWheels = GetComponentsInChildren<WheelCollider>();
        allWheelMeshes = new Transform[allWheels.Length];
        allWheelAudioSources = new AudioSource[allWheels.Length];
        allWheelTrails = new TrailRenderer[allWheels.Length];
        isSkid = new bool[allWheels.Length];
        for (int i = 0; i < allWheels.Length; ++i)
        {
            allWheelMeshes[i] = allWheels[i].GetComponentInChildren<Renderer>().transform;
            allWheelAudioSources[i] = allWheels[i].GetComponentInChildren<AudioSource>();
            if (allWheelAudioSources[i] == null) allWheelAudioSources[i] = allWheelMeshes[i].gameObject.AddComponent<AudioSource>();
            allWheelAudioSources[i].spatialBlend = 1;
            allWheelAudioSources[i].loop = false;
        }
        if (engineSoundSource != null)
        {
            engineSoundSource.spatialBlend = 1;
            engineSoundSource.loop = true;
        }
    }
    private void Update()
    {
        HandleEffects();
    }

    void HandleEffects()
    {
        for (int i = 0; i < allWheels.Length; ++i)
        {
            Vector3 pos; Quaternion rot;
            allWheels[i].GetWorldPose(out pos, out rot);
            allWheelMeshes[i].position = pos;
            allWheelMeshes[i].rotation = rot;
            //if (wheelGrounded[i] && (Mathf.Abs(forwardSlip[i]) >= allWheels[i].forwardFriction.extremumSlip * .9f || Mathf.Abs(sidewaysSlip[i]) >= allWheels[i].sidewaysFriction.extremumSlip * .9f))
            if(isSkid[i])
            {
                bool skidAudioPlaying = false;
                foreach (var a in allWheelAudioSources)
                    if (a.isPlaying)
                        skidAudioPlaying = true;//repetitive checking
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
                    allWheelTrails[i].transform.position = pos + transform.up * allWheels[i].radius * allWheels[i].transform.lossyScale.x * -.9f;
                }
                if (skidParticles != null)
                {
                    skidParticles.Emit(1);
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
        if (engineSoundSource)
        {
            engineSoundSource.pitch = currentEngineAudioPitch = Mathf.Clamp(currentEngineRPM / engineAudioRPM, engineAudioMinPitch, engineAudioMaxPitch);
            engineSoundSource.volume = Mathf.Lerp(engineSoundSource.volume, Mathf.Lerp(engineAudioMinVolume, engineAudioMaxVolume, currentThrottle), Time.deltaTime / .5f);

            if (!engineSoundSource.isPlaying)
            {
                engineSoundSource.clip = engineAudio;
                engineSoundSource.Play();
            }
        }
        foreach (var g in headLampOn)
            g.SetActive(headLamp);
        foreach (var g in headLampOff)
            g.SetActive(!headLamp);
        foreach (var g in rearLampOn)
            g.SetActive(rearLamp);
        foreach (var g in rearLampOff)
            g.SetActive(!rearLamp);
    }
}
