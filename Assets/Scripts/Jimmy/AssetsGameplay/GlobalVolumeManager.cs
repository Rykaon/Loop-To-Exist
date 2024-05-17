using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalVolumeManager : MonoBehaviour
{
    [SerializeField] private List<GlobalVolumeController> volumes;
    private GlobalVolumeController previousVolume;
    private GlobalVolumeController currentVolume;

    [SerializeField] private float fadeDuration;

    private void Awake()
    {
        
    }

    private void FixedUpdate()
    {
        for (int i = 0; i < volumes.Count; i++)
        {
            bool isInVolume = volumes[i].IsTransformInsideBoundingBox(Camera.main.transform);

            if (isInVolume)
            {
                if (currentVolume == null)
                {
                    currentVolume = new GlobalVolumeController();
                    currentVolume.SetSoundVolume(isInVolume, fadeDuration);
                }
                else
                {
                    if (previousVolume != volumes[i] && currentVolume != volumes[i])
                    {
                        previousVolume = currentVolume;
                        currentVolume = volumes[i];

                        previousVolume.SetSoundVolume(false, fadeDuration);
                        currentVolume.SetSoundVolume(true, fadeDuration);
                        return;
                    }
                }
            }
        }
    }
}
