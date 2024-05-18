using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalVolumeController : MonoBehaviour
{
    [SerializeField] private BoxCollider collider;
    [SerializeField] private List<string> soundNames;
    [SerializeField] private List<float> volumes;

    private void Awake()
    {
        
    }

    public bool IsTransformInsideBoundingBox(Transform target)
    {
        Vector3 globalCenter = collider.transform.TransformPoint(collider.center);

        Vector3 halfSize = collider.size / 2;

        Vector3 right = collider.transform.right * halfSize.x;
        Vector3 up = collider.transform.up * halfSize.y;
        Vector3 forward = collider.transform.forward * halfSize.z;

        Vector3[] corners = new Vector3[8];
        corners[0] = globalCenter + right + up + forward;
        corners[1] = globalCenter + right + up - forward;
        corners[2] = globalCenter + right - up + forward;
        corners[3] = globalCenter + right - up - forward;
        corners[4] = globalCenter - right + up + forward;
        corners[5] = globalCenter - right + up - forward;
        corners[6] = globalCenter - right - up + forward;
        corners[7] = globalCenter - right - up - forward;

        Vector3 targetPos = target.position;

        Vector3 min = corners[0];
        Vector3 max = corners[0];
        foreach (var corner in corners)
        {
            min = Vector3.Min(min, corner);
            max = Vector3.Max(max, corner);
        }

        if (targetPos.x >= min.x && targetPos.x <= max.x &&
            targetPos.y >= min.y && targetPos.y <= max.y &&
            targetPos.z >= min.z && targetPos.z <= max.z)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public void SetSoundVolume(bool isActive, float fadeDuration)
    {
        if (isActive)
        {
            Debug.Log("Play : " + soundNames);
            for (int i = 0; i < soundNames.Count; i++)
            {
                AudioManager.instance.FadeInAndPlay(soundNames[i], fadeDuration, volumes[i]);
            }
        }
        else
        {
            Debug.Log("Stop : " + soundNames);
            for (int i = 0; i < soundNames.Count; i++)
            {
                AudioManager.instance.FadeOutAndStop(soundNames[i], fadeDuration);
            }
        }
    }
}
