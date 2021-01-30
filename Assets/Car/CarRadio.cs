using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[RequireComponent(typeof(AudioSource))]
public class CarRadio : MonoBehaviour
{
    AudioSource audioSource;
    public AudioClip[] album;
    private int current=0;
    [SerializeField] private bool isPlaying = false;
    [SerializeField] private bool spacialBlending = true;
    [SerializeField] private bool dooplerLevel = true;

    float lastButtonUp = -1;
    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.spatialBlend = spacialBlending ? 1 : 0;
        audioSource.dopplerLevel = dooplerLevel ? 1 : 0;
        current = 0;
        audioSource.clip = album[current];
        if (isPlaying)
            audioSource.Play();
    }
    void Update()
    {
        if (lastButtonUp >= 0)
            lastButtonUp += Time.unscaledDeltaTime;
        if (lastButtonUp > .3f)
        {
            lastButtonUp = -1;

            if (isPlaying)
            {
                audioSource.Pause();
                isPlaying = false;
            }
            else
            {
                audioSource.Play();
                isPlaying = true;
            }
        }
        if (Input.GetButtonUp("CarRadio"))
        {
            if (lastButtonUp>0 && lastButtonUp < .3f)
            {
                lastButtonUp = -1;
                current = (current + 1) % album.Length;
                audioSource.clip = album[current];
                audioSource.Play();
            }
            else
            {
                lastButtonUp = 0;
            }
        }
        if(isPlaying && !audioSource.isPlaying)
        {
            current = (current + 1) % album.Length;
            audioSource.clip = album[current];
            audioSource.Play();
        }
    }
}
