using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateManager : MonoBehaviour
{
    public enum Type { Player, Item, Object }
    public enum State { Default, Sticky, FreezeTime, FreezePosition }
    
    [Header("StateManager References")]
    public Type type;
    public State state;
    [SerializeField] protected Rigidbody RigidBody;


    public GameManager gameManager { get; private set; }
    public Rigidbody rigidBody { get; private set; }
    public Vector3 startPosition { get; private set; }
    public Quaternion startRotation { get; private set; }

    public virtual void Initialize(GameManager instance)
    {
        startPosition = transform.position;
        startRotation = transform.rotation;

        gameManager = instance;
        rigidBody = RigidBody;
    }

    public virtual void Reset()
    {
        rigidBody.isKinematic = true;
        rigidBody.velocity = Vector3.zero;
        rigidBody.angularVelocity = Vector3.zero;
        rigidBody.angularDrag = 0f;
        rigidBody.isKinematic = false;

        transform.position = startPosition;
        transform.rotation = startRotation;
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {

    }
}
