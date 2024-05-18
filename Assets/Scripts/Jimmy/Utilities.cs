using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utilities
{
    public static int FindIndexInList<T>(T item, List<T> list)
    {
        return list.IndexOf(item);
    }

    public static float ClampAngle(float current, float min, float max)
    {
        float dtAngle = Mathf.Abs(((min - max) + 180) % 360 - 180);
        float hdtAngle = dtAngle * 0.5f;
        float midAngle = min + hdtAngle;

        float offset = Mathf.Abs(Mathf.DeltaAngle(current, midAngle)) - hdtAngle;
        if (offset > 0)
        {
            if (current < min)
            {
                min -= 360;
                max -= 360;
            }
            else if (current > max)
            {
                min += 360;
                max += 360;
            }

            current = Mathf.MoveTowardsAngle(current, midAngle, offset);
        }

        return Mathf.Clamp(current, min, max);
    }

    public static Vector3 GetCameraForward(Transform camera)
    {
        Vector3 forward = camera.forward;
        forward.y = 0f;
        return forward.normalized;
    }

    public static Vector3 GetCameraRight(Transform camera)
    {
        Vector3 right = camera.right;
        right.y = 0f;
        return right.normalized;
    }

    public static AnimationCurve ConvertEaseToCurve(Ease ease, int samplePoints = 10)
    {
        AnimationCurve curve = new AnimationCurve();

        for (int i = 0; i <= samplePoints; i++)
        {
            float time = (float)i / samplePoints;
            float value = DOVirtual.EasedValue(0, 1, time, ease);
            curve.AddKey(time, value);
        }

        return curve;
    }
}
