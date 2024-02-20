using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GrabObject : MonoBehaviour
{
    private PlayerController player;

    [SerializeField] private Collider ObjectCollider;
    public Collider objectCollider { get; private set; }


    [SerializeField] private Rigidbody RigidBody;
    public Rigidbody rigidBody { get; private set; }
    private float mass;
    private float drag;
    private float angularDrag;

    private float throwForceHorizontal = 8;
    private float throwForceVertical = 5;

    public bool isSelectedObject = false;
    public bool isSet = false;

    private void Awake()
    {
        objectCollider = ObjectCollider;
        rigidBody = RigidBody;
        mass = rigidBody.mass;
        drag = rigidBody.drag;
        angularDrag = rigidBody.angularDrag;
    }

    public void ResetObject()
    {
        isSelectedObject = false;
        isSet = false;
        Debug.Log("yeaaaaaaaaaaaaaah");
        if (player != null)
        {
            player.ResetObject();
        }

        objectCollider.isTrigger = false;
        rigidBody.useGravity = true;
        rigidBody.isKinematic = false;
        rigidBody.velocity = Vector3.zero;
    }

    public void SetSelectedObject(Transform[] path, float time)
    {
        isSelectedObject = true;
        objectCollider.isTrigger = true;
        rigidBody.useGravity = false;

        StartCoroutine(SetObject(path, time));

        /*iTween.MoveTo(gameObject, iTween.Hash(
            "path", path,
            "time", time,
            "easetype", iTween.EaseType.easeOutSine,
            "oncomplete", "InitializeObjectSet",
            "oncompleteparams", path[1].parent
        ));*/
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
        transform.parent = parent;
        isSet = true;
        player = parent.GetComponent<PlayerController>();

        objectCollider.isTrigger = false;
        rigidBody.isKinematic = false;
        Debug.Log("yo");
    }

    public void ThrowObject()
    {
        if (isSelectedObject && isSet)
        {
            isSet = false;
            isSelectedObject = false;
            transform.parent = null;

            rigidBody.useGravity = true;

            rigidBody.AddForce(CalculateThrowForce(), ForceMode.Impulse);
            objectCollider.isTrigger = false;
            player = null;
        }
    }

    private Vector3 CalculateThrowForce()
    {
        Vector3 throwDirection = Vector3.zero;
        throwDirection += Vector3.zero.x * player.transform.right.normalized * throwForceHorizontal;
        throwDirection += Vector3.one.y * player.transform.up.normalized * throwForceVertical;
        throwDirection += Vector3.one.z * player.transform.forward.normalized * throwForceHorizontal;

        return throwDirection;
    }
}
