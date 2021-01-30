using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleWindEffects : MonoBehaviour
{
    public AudioSource windAudioSource;
    public AudioClip windAudio;
    public float minSpeed = 10f;
    public float maxSpeed = 120f;
    public float maxSpeedVolume = 1f;
    public float minSpeedPitch = .2f;
    public float maxSpeedPitch = .5f;
    [field: Header("Inputs")]
    [field: SerializeField]
    public float speed{get;set;}
    [ReadOnly]
    public float currentPitch, currentVolume;
    Rigidbody body;
    private void Awake()
    {
        body = GetComponent<Rigidbody>();
    }
    private void OnValidate()
    {
        if (windAudioSource)
        {
            windAudioSource.spatialBlend = 1;
            windAudioSource.loop = true;
        }
    }
    void Update()
    {
        if (body)
            speed = body.velocity.magnitude;
        if (windAudioSource)
        {
            currentPitch = windAudioSource.pitch = Mathf.Lerp(currentPitch, Mathf.Lerp(minSpeedPitch, maxSpeedPitch, (speed - minSpeed) / (maxSpeed - minSpeed)), Time.deltaTime / .5f);
            currentVolume = windAudioSource.volume = Mathf.Lerp(currentVolume, Mathf.Lerp(0, maxSpeedVolume, (speed - minSpeed) / (maxSpeed - minSpeed)), Time.deltaTime / .5f);

            if (!windAudioSource.isPlaying)
            {
                windAudioSource.clip = windAudio;
                windAudioSource.Play();
            }
        }
    }
}
