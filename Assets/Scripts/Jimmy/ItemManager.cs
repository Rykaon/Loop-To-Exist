using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Profiling;

public class ItemManager : StateManager
{
    [Header("Items References")]
    [SerializeField] private Collider ObjectCollider;
    [SerializeField] protected float throwForceHorizontal = 8;
    [SerializeField] protected float throwForceVertical = 5;

    protected PlayerManager player;
    protected Joint joint = null;

    public Collider objectCollider { get; private set; }
    public bool isSelectedObject { get; private set; }
    public bool isSet { get; private set; }

    public override void Initialize(GameManager instance)
    {
        base.Initialize(instance);

        objectCollider = ObjectCollider;
        isSelectedObject = false;
        isSet = false;
    }

    public override void Reset()
    {
        isSelectedObject = false;
        isSet = false;

        if (joint != null)
        {
            Destroy(joint);
            joint = null;
        }

        base.Reset();

        objectCollider.isTrigger = false;
        rigidBody.useGravity = true;
    }

    public override void SetState(State state)
    {
        base.SetState(state);
    }

    public override void ResetState()
    {
        base.ResetState();

        switch (state)
        {
            case State.Sticky:
                joint = null;
                break;

            case State.FreezePosition:
                break;

            case State.FreezeTime:
                break;
        }
    }

    // Les fonctions propres aux objets interactifs

    public void SetSelectedObject(Transform[] path, float time)
    {
        if (state == State.Sticky)
        {
            transform.SetParent(parent, true);
            rigidBody.useGravity = true;
            //rigidBody.isKinematic = false;
        }

        if (joint != null)
        {
            Destroy(joint);
            joint = null;
        }

        isSelectedObject = true;
        objectCollider.isTrigger = true;
        rigidBody.useGravity = false;

        StartCoroutine(SetObject(path, time));
    }

    private IEnumerator SetObject(Transform[] path, float time)
    {
        float elapsedTime = 0;
        Tween tween = null;
        while (elapsedTime < time)
        {
            elapsedTime += Time.fixedDeltaTime;

            if (tween != null)
            {
                tween.Kill();
            }

            tween = transform.DOMove(path[1].position, time / 3);
            yield return new WaitForFixedUpdate();
        }

        tween.Kill();
        transform.position = path[1].position;

        InitializeObjectSet(path[1].parent);
    }

    private void InitializeObjectSet(Transform parent)
    {
        //transform.parent = parent;
        isSet = true;
        player = parent.GetComponent<PlayerManager>();

        objectCollider.isTrigger = false;
        if (joint == null)
        {
            joint = transform.AddComponent<FixedJoint>();
        }
        joint.connectedBody = player.transform.GetComponent<Rigidbody>();
        //rigidBody.isKinematic = false;
    }

    public void ThrowObject()
    {
        if (isSelectedObject && isSet)
        {
            isSet = false;
            isSelectedObject = false;
            transform.parent = null;

            if (joint != null)
            {
                Destroy(joint);
                joint = null;
            }
            rigidBody.useGravity = true;

            StartCoroutine(SetThrowForce());
        }
    }

    private IEnumerator SetThrowForce()
    {
        yield return new WaitForFixedUpdate();
        rigidBody.AddForce(CalculateThrowForce(), ForceMode.Impulse);
        objectCollider.isTrigger = false;
        player = null;
    }

    private Vector3 CalculateThrowForce()
    {
        Vector3 throwDirection = Vector3.zero;
        throwDirection += Vector3.zero.x * player.transform.right.normalized * throwForceHorizontal;
        throwDirection += Vector3.one.y * player.transform.up.normalized * throwForceVertical;
        throwDirection += Vector3.one.z * player.transform.forward.normalized * throwForceHorizontal;

        return throwDirection;
    }

    protected override void OnCollisionEnter(Collision collision)
    {
        if (state == State.Sticky && !isSelectedObject/* && collision.transform.tag != "Player"*/)
        {
            Utilities.StopRigidBody(rigidBody);

            if (joint == null)
            {
                joint = transform.AddComponent<FixedJoint>();
            }
            joint.connectedBody = collision.transform.GetComponent<Rigidbody>();

            Debug.Log(collision.transform.name);
            //SetParentWithConstantScale(collision.transform);
            //transform.SetParent(collision.transform, true);
        }
    }
}
