using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EngineEffects : MonoBehaviour
{
    [Header("Setup")]
    public AudioSource engineAudioSource;
    public AudioClip engineAudio;
    public float engineAudioRPM = 8000;
    public float engineAudioMaxPitch = 1.5f;
    public float engineAudioMinPitch = .2f;
    public float engineAudioMinVolume = .25f;
    public float engineAudioMaxVolume = 1f;

    [field:Header("Inputs")]
    [field:SerializeField]
    public float currentEngineRPM { get; set; }
    [field: SerializeField]
    public float currentThrottle { get; set; }
    [ReadOnly]
    public float currentPitch,currentVolume;

    private void OnValidate()
    {
        if (engineAudioSource)
        {
            engineAudioSource.spatialBlend = 1;
            engineAudioSource.loop = true;
        }
    }
    void Update()
    {
        if (engineAudioSource)
        {
            currentPitch=engineAudioSource.pitch = Mathf.Lerp(currentPitch, Mathf.Clamp(currentEngineRPM / engineAudioRPM, engineAudioMinPitch, engineAudioMaxPitch), Time.deltaTime / .5f);
            currentVolume =engineAudioSource.volume = Mathf.Lerp(currentVolume, Mathf.Lerp(engineAudioMinVolume, engineAudioMaxVolume, currentThrottle), Time.deltaTime / .5f);

            if (!engineAudioSource.isPlaying)
            {
                engineAudioSource.clip = engineAudio;
                engineAudioSource.Play();
            }
        }
    }
}
