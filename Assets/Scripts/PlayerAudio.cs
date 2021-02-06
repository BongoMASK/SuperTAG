using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
    //[SerializeField] AudioSource source;

    public Sound[] sounds;
    public Sound[] randomFootsteps;

    private void Awake() {
        foreach (var s in sounds) {
            //s.source = source;
            s.source = gameObject.AddComponent<AudioSource>();

            s.source.clip = s.clip;
            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
            s.source.loop = s.loop;
            s.source.spatialBlend = 1;
            s.source.minDistance = 10;
            s.source.maxDistance = 100;
        }
        foreach (var s in randomFootsteps) {
            // s.source = source;
            s.source = gameObject.AddComponent<AudioSource>();

            s.source.clip = s.clip;
            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
            s.source.loop = s.loop;
            s.source.spatialBlend = 1;
            s.source.minDistance = 10;
            s.source.maxDistance = 100;
            s.source.playOnAwake = false;
        }
    }

    public void PlayRandomFootstep() {
        int value = UnityEngine.Random.Range(0, randomFootsteps.Length);
        randomFootsteps[value].source.Play();
    }

    public void Play(string name) {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s == null) {
            Debug.LogWarning("Sound name: " + name + " not found in list!");
            return;
        }
        s.source.Play();
    }

    public void Pause(string name) {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s == null) {
            Debug.LogWarning("Sound name: " + name + " not found in list!");
            return;
        }
        s.source.Stop();
    }

    public AudioSource GetAudioSource(string name) {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s == null) {
            Debug.LogWarning("Sound name: " + name + " not found in list!");
            return null;
        }
        return s.source;
    }
}
