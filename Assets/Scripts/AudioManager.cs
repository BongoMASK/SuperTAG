using System.Collections;
using System.Collections.Generic;
using UnityEngine.Audio;
using System;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public Sound[] sounds;
    public Sound[] randomFootsteps;
    public static AudioManager instance;

    private void Awake() {

        if (instance == null) {
            instance = this;
        }
        else {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        foreach (var s in sounds) {
            s.source = gameObject.AddComponent<AudioSource>();

            s.source.clip = s.clip;
            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
            s.source.loop = s.loop;
            //s.source.spatialBlend = 1;
        }
        foreach (var s in randomFootsteps) {
            s.source = gameObject.AddComponent<AudioSource>();

            s.source.clip = s.clip;
            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
            s.source.loop = s.loop;
            //s.source.spatialBlend = 1;
        }
    }

    void Start()
    {
        Play("Theme");
    }

    public void Play(string name) {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s == null) {
            Debug.LogWarning("Sound name: " + name + " not found in list!");
            return;
        }
        s.source.Play();
    }

    public void PlayFromAudioSource(AudioSource source, string name) {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        s.source = source;
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

    public void PlayRandomFootstep() {
        int value = UnityEngine.Random.Range(0, randomFootsteps.Length);
        Sound s = randomFootsteps[value];
        s.source.Play();
    }

    public void PlayOthersFootsteps(AudioSource source) {
        int value = UnityEngine.Random.Range(0, randomFootsteps.Length);
        Sound s = randomFootsteps[value];
        s.source = source;
        s.source.spatialBlend = 1;
        s.source.clip = source.clip;
        s.source.volume = source.volume;
        s.source.pitch = source.pitch;
        s.source.loop = source.loop;
        Debug.Log(s.source.gameObject.transform.position + ", name:" + s.source.gameObject.name + " is playing: " + s.source.isPlaying);
        s.source.Play();
    }
}
