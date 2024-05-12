using UnityEngine.Audio;
using System;
using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;
    public Sound[] sounds;

    // Start is called before the first frame update
    void Awake()
    {
        if (instance == null)
            instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        foreach (Sound s in sounds)
        {

            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;

            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
            s.source.loop = s.loop;
        }
    }

    public void Play(string name)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s == null)
        {
            Debug.LogWarning("Sound : " + name + " not found !");
            return;
        }
        s.source.Play();
    }

    public void PlayVariation(string name, float diffPitch, float diffVolume)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s == null)
        {
            Debug.LogWarning("Sound : " + name + " not found !");
            return;
        }
        //var pitchDeBase = s.source.pitch;
        //var volumeDeBase = s.source.volume;
        s.source.pitch = s.pitch + UnityEngine.Random.Range(-diffPitch, diffPitch);
        s.source.volume = s.volume + UnityEngine.Random.Range(-diffVolume, diffVolume);

        //Debug.Log(s.source.pitch);

        s.source.Play();
        //s.source.pitch = pitchDeBase;
        //s.source.volume = volumeDeBase;
    }

    public void Stop(string name)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s == null)
        {
            Debug.LogWarning("Sound : " + name + " not found !");
            return;
        }
        s.source.Stop();
    }

    public void FadeOutAndStop(string name)
    {
        StartCoroutine(FadeOutAndStopCoroutine(name));
    }

    IEnumerator FadeOutAndStopCoroutine(string name)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);

        float startVolume = s.source.volume;

        while (s.source.volume > 0)
        {
            s.source.volume -= Time.deltaTime * 0.5f;
            yield return null;
        }

        s.source.Stop();
        s.source.volume = startVolume;
    }

    public void PitchShiftDown(string name)
    {
        StartCoroutine(PitchShiftDownCoroutine(name));
    }

    private IEnumerator PitchShiftDownCoroutine(string name)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);

        float startPitch = s.source.pitch;

        while (s.source.pitch > 0)
        {
            s.source.pitch -= Time.deltaTime * 0.06f;
            yield return null;
        }

        s.source.Stop();
        s.source.pitch = startPitch;
    }
}