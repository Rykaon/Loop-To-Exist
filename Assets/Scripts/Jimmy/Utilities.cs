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

    public static void StopRigidBody(Rigidbody rigidbody)
    {
        rigidbody.useGravity = false;
        rigidbody.velocity = Vector3.zero;
        rigidbody.drag = 0f;
        rigidbody.angularVelocity = Vector3.zero;
        rigidbody.angularDrag = 0.05f;
        rigidbody.useGravity = true;
    }

    public static float ClampAngle(float current, float min, float max)
    {
        float dtAngle = Mathf.Abs(((min - max) + 180) % 360 - 180);
        float hdtAngle = dtAngle * 0.5f;
        float midAngle = min + hdtAngle;

        float offset = Mathf.Abs(Mathf.DeltaAngle(current, midAngle)) - hdtAngle;
        if (offset > 0)
            current = Mathf.MoveTowardsAngle(current, midAngle, offset);
        return current;
    }

    public static ClipPlanePoints NearClipPlanePoints(this Camera camera, Vector3 pos, float clipPlaneMargin)
    {
        var clipPlanePoints = new ClipPlanePoints();

        var transform = camera.transform;
        var halfFOV = (camera.fieldOfView / 2) * Mathf.Deg2Rad;
        var aspect = camera.aspect;
        var distance = camera.nearClipPlane;
        var height = distance * Mathf.Tan(halfFOV);
        var width = height * aspect;
        height *= 1 + clipPlaneMargin;
        width *= 1 + clipPlaneMargin;
        clipPlanePoints.LowerRight = pos + transform.right * width;
        clipPlanePoints.LowerRight -= transform.up * height;
        clipPlanePoints.LowerRight += transform.forward * distance;

        clipPlanePoints.LowerLeft = pos - transform.right * width;
        clipPlanePoints.LowerLeft -= transform.up * height;
        clipPlanePoints.LowerLeft += transform.forward * distance;

        clipPlanePoints.UpperRight = pos + transform.right * width;
        clipPlanePoints.UpperRight += transform.up * height;
        clipPlanePoints.UpperRight += transform.forward * distance;

        clipPlanePoints.UpperLeft = pos - transform.right * width;
        clipPlanePoints.UpperLeft += transform.up * height;
        clipPlanePoints.UpperLeft += transform.forward * distance;

        return clipPlanePoints;
    }
}

public struct ClipPlanePoints
{
    public Vector3 UpperLeft;
    public Vector3 UpperRight;
    public Vector3 LowerLeft;
    public Vector3 LowerRight;
}
