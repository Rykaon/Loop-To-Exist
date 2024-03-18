using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnapRigidbodyPosition : MonoBehaviour
{
    private Rigidbody rigidBody = null;
    private RigidbodyInterpolation interpolation;
    private CollisionDetectionMode collision;
    private Transform initParent;


    private Vector3 relativePosition;
    private Quaternion relativeRotation;
    private bool initialized = false;

    public void Initialize(Rigidbody rb, Transform parent)
    {
        rigidBody = rb;
        interpolation = rigidBody.interpolation;
        collision = rigidBody.collisionDetectionMode;
        initParent = transform.parent;

        rigidBody.interpolation = RigidbodyInterpolation.Interpolate;
        rigidBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        transform.SetParent(parent, true);
        relativePosition = parent.InverseTransformPoint(transform .position);
        relativeRotation = transform.localRotation;

        initialized = true;
    }

    public void OnDestroy()
    {
        transform.SetParent(initParent, true);
        rigidBody.collisionDetectionMode = collision;
        rigidBody.interpolation = interpolation;
    }

    private void FixedUpdate()
    {
        if (initialized)
        {
            rigidBody.MovePosition(transform.parent.TransformPoint(relativePosition));
            rigidBody.MoveRotation(relativeRotation);
        }
    }
}
