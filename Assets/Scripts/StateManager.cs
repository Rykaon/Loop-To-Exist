using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class StateManager : MonoBehaviour
{
    public enum Type { Player, Mushroom, Object }
    public enum State { Default, Sticky, Link }
    public enum Position { Default, Hold, Equip }

    [Header("StateManager References")]
    public Type type;
    public State state;
    public Position position;
    [SerializeField] protected Rigidbody RigidBody;
    [SerializeField] private Collider ObjectCollider;

    [Header("Throw Properties")]
    [SerializeField] public float startThrowForceHorizontal = 5;
    [SerializeField] public float startThrowForceVertical = 5;

    public GameManager gameManager { get; private set; }
    public Rigidbody rigidBody { get; private set; }
    public Collider objectCollider { get; private set; }

    protected Transform parent;

    [HideInInspector] public GameObject objectToStick = null;
    [HideInInspector] public List<GameObject> stickedObjects = new List<GameObject>();

    public bool isSticked { get; private set; }
    public bool isLinked { get; private set; }

    protected PlayerManager holdingPlayer = null;
    protected PlayerManager equippingPlayer = null;
    protected Joint joint = null;

    public bool isHeldObject { get; private set; }
    public bool isHeld { get; private set; }
    public bool isEquippedObject { get; private set; }
    public bool isEquipped { get; private set; }

    ///////////////////////////////////////////////////
    ///               INITIALIZATION                ///
    ///////////////////////////////////////////////////

    public virtual void Initialize(GameManager instance)
    {
        gameManager = instance;
        rigidBody = RigidBody;
        parent = transform.parent;
        objectCollider = ObjectCollider;
        isHeldObject = false;
        isHeld = false;
    }

    ///////////////////////////////////////////////////
    ///              STATE MANAGEMENT               ///
    ///////////////////////////////////////////////////

    public virtual void SetState(State state)
    {
        switch (state)
        {
            case State.Sticky:
                if (this.state == State.Sticky)
                {
                    ResetState();
                }
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
                joint = null;
                isSticked = false;
                rigidBody.useGravity = true;
                break;
        }

        state = State.Default;
    }

    ///////////////////////////////////////////////////
    ///                HOLD METHODS                 ///
    ///////////////////////////////////////////////////

    public virtual void SetHoldObject(Transform endPosition, float time)
    {
        if (state == State.Sticky)
        {
            transform.SetParent(parent, true);
            
            if (joint != null)
            {
                Destroy(joint);
                joint = null;
                isSticked = false;

                if (objectToStick.TryGetComponent<StateManager>(out StateManager stateManager))
                {
                    if (stateManager.stickedObjects.Contains(this.gameObject))
                    {
                        stateManager.stickedObjects.Remove(this.gameObject);
                    }
                }

                objectToStick = null;
            }
        }

        if (joint != null)
        {
            Destroy(joint);
            joint = null;
        }

        isHeldObject = true;
        objectCollider.isTrigger = true;
        rigidBody.useGravity = false;
        
        rigidBody.mass = 0;
        foreach (GameObject stickedObject in GetStickedObjects(GetFirstStickedObject(this.gameObject)))
        {
            if (stickedObject.TryGetComponent<StateManager>(out StateManager stateManager)){
                stateManager.rigidBody.mass = 0f;
            }
        }

        StartCoroutine(HoldObject(endPosition, time));
    }

    private IEnumerator HoldObject(Transform endPosition, float transitionDuration)
    {
        float elapsedTime = 0;

        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.fixedDeltaTime;

            float time = elapsedTime / transitionDuration;

            transform.position = Vector3.Lerp(transform.position, endPosition.position, time);
            transform.rotation = Quaternion.Slerp(transform.rotation, endPosition.rotation, time);

            elapsedTime += Time.deltaTime;
            yield return new WaitForFixedUpdate();
        }

        InitializeHoldObject(endPosition.parent);
    }

    public virtual void InitializeHoldObject(Transform parent)
    {
        isHeld = true;
        holdingPlayer = parent.GetComponent<PlayerManager>();

        objectCollider.isTrigger = false;
        if (joint == null)
        {
            joint = transform.AddComponent<FixedJoint>();
        }
        joint.connectedBody = holdingPlayer.transform.GetComponent<Rigidbody>();
    }

    public virtual void DropObject()
    {
        if (isHeldObject && isHeld)
        {
            isHeld = false;
            isHeldObject = false;
            transform.parent = null;

            if (joint != null)
            {
                Destroy(joint);
                joint = null;
            }

            rigidBody.mass = 1f;
            foreach (GameObject stickedObject in GetStickedObjects(GetFirstStickedObject(this.gameObject)))
            {
                if (stickedObject.TryGetComponent<StateManager>(out StateManager stateManager)){
                    stateManager.rigidBody.mass = 1f;
                }
            }

            rigidBody.useGravity = true;
        }
    }

    public virtual void ThrowObject(float throwForceHorizontal, float throwForceVertical, Vector3 hitpoint)
    {
        DropObject();
        StartCoroutine(SetThrowForce(throwForceHorizontal, throwForceVertical, hitpoint));
    }

    private IEnumerator SetThrowForce(float throwForceHorizontal, float throwForceVertical, Vector3 hitpoint)
    {
        yield return new WaitForFixedUpdate();

        Vector3 throwDirection = Vector3.zero;

        if (hitpoint != Vector3.zero)
        {
            // Utiliser le hitpoint pour ajuster la direction du lancer
            Vector3 playerToHitPoint = hitpoint - transform.position;
            throwDirection = Vector3.ProjectOnPlane(playerToHitPoint, Vector3.up).normalized * throwForceHorizontal;
        }
        else
        {
            throwDirection = Camera.main.transform.forward.normalized * throwForceHorizontal;
        }

        // Ajuster la force verticale (hauteur de l'arc)
        throwDirection += Vector3.up * throwForceVertical;

        // Appliquer la force au rigidbody
        rigidBody.AddForce(throwDirection, ForceMode.Impulse);
        objectCollider.isTrigger = false;
        holdingPlayer = null;
    }

    ///////////////////////////////////////////////////
    ///                EQUIP METHODS                ///
    ///////////////////////////////////////////////////

    public virtual void SetEquipObject(Transform endPosition, float time)
    {
        if (joint != null)
        {
            Destroy(joint);
            joint = null;
        }

        isEquippedObject = true;
        objectCollider.isTrigger = true;
        rigidBody.useGravity = false;

        StartCoroutine(EquipObject(endPosition, time));
    }

    private IEnumerator EquipObject(Transform endPosition, float transitionDuration)
    {
        float elapsedTime = 0;

        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.fixedDeltaTime;

            float time = elapsedTime / transitionDuration;

            transform.position = Vector3.Lerp(transform.position, endPosition.position, time);
            transform.rotation = Quaternion.Slerp(transform.rotation, endPosition.rotation, time);

            elapsedTime += Time.deltaTime;
            yield return new WaitForFixedUpdate();
        }

        InitializeEquipObject(endPosition.parent);
    }

    public virtual void InitializeEquipObject(Transform parent)
    {
        isEquipped = true;
        equippingPlayer = parent.GetComponent<PlayerManager>();

        objectCollider.isTrigger = false;
        
        if (joint == null)
        {
            joint = transform.AddComponent<FixedJoint>();
        }
        
        joint.connectedBody = equippingPlayer.transform.GetComponent<Rigidbody>();
    }

    private GameObject GetFirstStickedObject(GameObject currentObject)
    {
        if (currentObject.TryGetComponent<StateManager>(out StateManager stateManager))
        {
            if (stateManager.objectToStick != null)
            {
                return GetFirstStickedObject(stateManager.objectToStick);
            }
            else
            {
                return currentObject;
            }
        }
        else if (currentObject.tag == "Wall" || currentObject.tag == "Floor")
        {
            return currentObject;
        }
        else
        {
            Debug.Log("FirstStickedObject Not Found");
            return null;
        }
    }

    private List<GameObject> GetStickedObjects(GameObject firstStickedObject)
    {
        List<GameObject> stickedObjects = new List<GameObject>();
        if (firstStickedObject.TryGetComponent<StateManager>(out StateManager stateManager))
        {
            if (stateManager.stickedObjects.Count > 0)
            {
                foreach (GameObject stickedObject in stateManager.stickedObjects)
                {
                    stickedObjects.Add(stickedObject);

                    foreach (GameObject stickedObjectinSticked in GetStickedObjects(stickedObject))
                    {
                        stickedObjects.Add(stickedObjectinSticked);
                    }
                }
            }
        }

        return stickedObjects;
    }

    ///////////////////////////////////////////////////
    ///            COLLISION MANAGEMENT             ///
    ///////////////////////////////////////////////////

    protected virtual void OnCollisionEnter(Collision collision)
    {
        if (state == State.Sticky && !isHeldObject  && !isEquipped && !isSticked)
        {
            Debug.Log("yeah");
            rigidBody.useGravity = false;
            rigidBody.velocity = Vector3.zero;
            rigidBody.angularVelocity = Vector3.zero;

            if (joint == null)
            {
                joint = transform.AddComponent<FixedJoint>();
            }
            joint.connectedBody = collision.transform.GetComponent<Rigidbody>();

            objectToStick = collision.gameObject;
            isSticked = true;

            if (collision.transform.TryGetComponent<StateManager>(out StateManager stateManager))
            {
                stateManager.stickedObjects.Add(this.gameObject);
            }

            Debug.Log(collision.transform.name);
        }
    }
}
