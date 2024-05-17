using UnityEngine.Audio;
using System;
using UnityEngine;
using System.Collections;
using TMPro;

public class LocalAudioManager : MonoBehaviour
{
    public Sound[] sounds;

    void Awake()
    {
        foreach (Sound s in sounds)
        {

            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;

            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
            s.source.loop = s.loop;
        }
    }

    public void Play(string name, float radius)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s == null)
        {
            Debug.LogWarning("Sound : " + name + " not found !");
            return;
        }

        AudioSource.PlayClipAtPoint(s.clip, transform.position, radius);
    }

    public void Play(string name, float radius, Transform transform)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s == null)
        {
            Debug.LogWarning("Sound : " + name + " not found !");
            return;
        }

        AudioSource.PlayClipAtPoint(s.clip, transform.position, radius);
    }

    public void PlayVariation(string name, float diffPitch, float diffVolume, float radius)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s == null)
        {
            Debug.LogWarning("Sound : " + name + " not found !");
            return;
        }

        s.source.pitch = s.pitch + UnityEngine.Random.Range(-diffPitch, diffPitch);
        s.source.volume = s.volume + UnityEngine.Random.Range(-diffVolume, diffVolume);

        AudioSource.PlayClipAtPoint(s.clip, transform.position, radius);
    }

    public void PlayVariation(string name, float diffPitch, float diffVolume, float radius, Transform transform)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s == null)
        {
            Debug.LogWarning("Sound : " + name + " not found !");
            return;
        }

        s.source.pitch = s.pitch + UnityEngine.Random.Range(-diffPitch, diffPitch);
        s.source.volume = s.volume + UnityEngine.Random.Range(-diffVolume, diffVolume);

        AudioSource.PlayClipAtPoint(s.clip, transform.position, radius);
    }

    public void PlayFixedVariation(string name, float diffPitch, float diffVolume, float radius)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s == null)
        {
            Debug.LogWarning("Sound : " + name + " not found !");
            return;
        }

        s.source.pitch = s.pitch + diffPitch;
        s.source.volume = s.volume + diffVolume;

        AudioSource.PlayClipAtPoint(s.clip, transform.position, radius);
    }

    public void PlayFixedVariation(string name, float diffPitch, float diffVolume, float radius, Transform transform)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s == null)
        {
            Debug.LogWarning("Sound : " + name + " not found !");
            return;
        }

        s.source.pitch = s.pitch + diffPitch;
        s.source.volume = s.volume + diffVolume;

        AudioSource.PlayClipAtPoint(s.clip, transform.position, radius);
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