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

    /*public static float ClampAngle(float current, float min, float max)
    {
        float dtAngle = Mathf.Abs(((min - max) + 180) % 360 - 180);
        float hdtAngle = dtAngle * 0.5f;
        float midAngle = min + hdtAngle;

        float offset = Mathf.Abs(Mathf.DeltaAngle(current, midAngle)) - hdtAngle;
        if (offset > 0)
            current = Mathf.MoveTowardsAngle(current, midAngle, offset);
        return current;
    }*/

    public static float ClampAngle(float current, float min, float max)
    {
        float dtAngle = Mathf.Abs(((min - max) + 180) % 360 - 180);
        float hdtAngle = dtAngle * 0.5f;
        float midAngle = min + hdtAngle;

        float offset = Mathf.Abs(Mathf.DeltaAngle(current, midAngle)) - hdtAngle;
        if (offset > 0)
        {
            // Si l'angle est en dehors de la plage spécifiée, ajustez les limites
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
}
