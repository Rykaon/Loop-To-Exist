using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractibleObject : MonoBehaviour
{
    private Vector3 startPosition;
    private Vector3 startRotation;

    private Rigidbody rigidBody;

    private void Awake()
    {
        startPosition = transform.position;
        startRotation = transform.rotation.eulerAngles;
        rigidBody = GetComponent<Rigidbody>();
    }

    public void Replay()
    {
        if (transform.TryGetComponent<GrabObject>(out GrabObject grab))
        {
            grab.ResetObject();
        }

        rigidBody.isKinematic = true;
        transform.position = startPosition;
        transform.rotation = Quaternion.Euler(startRotation);
        rigidBody.isKinematic = false;
        rigidBody.velocity = Vector3.zero;
    }
}
