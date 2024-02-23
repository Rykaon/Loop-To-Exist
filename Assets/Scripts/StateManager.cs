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
    public State stateToApply;
    [SerializeField] protected Rigidbody RigidBody;
    
    public GameManager gameManager { get; private set; }
    public Rigidbody rigidBody { get; private set; }
    public Vector3 startPosition { get; private set; }
    public Quaternion startRotation { get; private set; }

    protected Transform parent;

    public virtual void Initialize(GameManager instance)
    {
        startPosition = transform.position;
        startRotation = transform.rotation;

        gameManager = instance;
        rigidBody = RigidBody;
        parent = transform.parent;
    }

    public virtual void Reset()
    {
        if (state != State.FreezeTime)
        {
            ResetState();

            stateToApply = State.Default;
            rigidBody.velocity = Vector3.zero;
            rigidBody.angularVelocity = Vector3.zero;
            rigidBody.angularDrag = 0f;

            transform.position = startPosition;
            transform.rotation = startRotation;
        }
    }

    public virtual void SetState(State state)
    {
        ResetState();

        switch (state)
        {
            case State.FreezePosition:
                rigidBody.isKinematic = true;
                rigidBody.useGravity = false;
                break;
        }

        this.state = state;
    }

    public virtual void ResetState()
    {
        switch (state)
        {
            case State.Sticky:
                if (rigidBody.TryGetComponent<Joint>(out Joint joint))
                {
                    Destroy(joint);
                }
                rigidBody.useGravity = true;
                break;

            case State.FreezePosition:
                rigidBody.useGravity = true;
                rigidBody.isKinematic = false;
                break;

            case State.FreezeTime:
                break;
        }

        state = State.Default;
    }

    protected virtual void SetParentWithConstantScale(Transform parent)
    {
        Vector3 scale = transform.localScale;
        transform.SetParent(parent, true);
        transform.localScale = new Vector3(1f / parent.localScale.x, 1f / parent.localScale.y, 1f / parent.localScale.z);
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {

    }
}
