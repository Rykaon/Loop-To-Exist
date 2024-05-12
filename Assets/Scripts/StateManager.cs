using DG.Tweening;
using Obi;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class StateManager : MonoBehaviour
{
    public enum Type { Player, Mushroom, Object, Creature }
    public enum State { Default, Sticky, Link }
    public enum Position { Default, Held, Equipped }

    [Header("StateManager References")]
    public Type type;
    public List<State> states;
    public Position position;
    [SerializeField] protected Rigidbody RigidBody;
    [SerializeField] private Collider ObjectCollider;
    [SerializeField] private Renderer Renderer;
    [SerializeField] private Outline Outline;
    public StickedWallElements stickedWall;

    [Header("Throw Properties")]
    [SerializeField] public float startThrowForceHorizontal = 5;
    [SerializeField] public float startThrowForceVertical = 5;
    private float linkThrowMultiplier; 
    private float playerMoveMassMultiplier;

    public GameManager gameManager { get; private set; }
    public Rigidbody rigidBody { get; private set; }
    public Collider objectCollider { get; private set; }
    public Renderer renderer { get; set; }
    public Outline outline { get; set; }

    protected Transform parent;

    public GameObject objectToStick = null;
    public List<GameObject> stickedObjects = new List<GameObject>();

    [HideInInspector] public GameObject link = null;
    [HideInInspector] public StateManager linkedObject = null;
    [HideInInspector] public Rigidbody linkAttachment = null;
    [HideInInspector] public FixedJoint linkJoint = null;
    public bool isSticked;
    public bool isLinked;

    protected PlayerManager holdingPlayer = null;
    protected PlayerManager equippingPlayer = null;
    protected Joint joint = null;
    private float jointBreakTreshold = 150f;

    public bool isHeldObject { get; private set; }
    public bool isHeld { get; private set; }
    public bool isEquippedObject { get; private set; }
    public bool isEquipped { get; private set; }

    public Vector3 lastGroundedPosition;
    public Quaternion lastGroundedRotation;

    ///////////////////////////////////////////////////
    ///               INITIALIZATION                ///
    ///////////////////////////////////////////////////

    public virtual void Initialize(GameManager instance)
    {
        gameManager = instance;
        rigidBody = RigidBody;
        parent = transform.parent;
        objectCollider = ObjectCollider;
        renderer = Renderer;
        outline = Outline;
        isHeldObject = false;
        isHeld = false;

        lastGroundedPosition = rigidBody.position;
        lastGroundedRotation = rigidBody.rotation;

        linkThrowMultiplier = 2.5f; // On multiplie la force de lancer par cette valeur si l'objet lancé est linké. 
        playerMoveMassMultiplier = 0.075f; // Le joueur qui porte l'objet a un multiplier de base de 1. Pour chaque objet porté, on lui ajoute cette valeur. On lui retire lorsqu'il drop l'item. Sert pour les fonctions Move() et Jump().
    }

    ///////////////////////////////////////////////////
    ///              STATE MANAGEMENT               ///
    ///////////////////////////////////////////////////

    public virtual void SetState(State state)
    {
        if (states.Contains(state))
        {
            if (state == State.Sticky)
            {
                if (rigidBody.TryGetComponent<Joint>(out Joint joint))
                {
                    if (joint.connectedBody == objectToStick.GetComponent<Rigidbody>())
                    {
                        Destroy(joint);
                        joint = null;
                    }
                }

                if (objectToStick.TryGetComponent<StateManager>(out StateManager stateManager))
                {
                    stateManager.stickedObjects.Remove(gameObject);
                }

                objectToStick = null;
                isSticked = false;
                rigidBody.useGravity = true;
            }
            else if (state == State.Link)
            {
                isLinked = false;
                if (linkedObject != null)
                {
                    linkedObject.isLinked = false;
                    linkedObject.linkedObject = null;
                    linkedObject.link = null;
                }
                
                if (link != null)
                {
                    Destroy(link.GetComponent<CustomRope>().end.gameObject);
                    Destroy(link.GetComponent<CustomRope>().start.gameObject);
                    Destroy(link.gameObject);
                }
                
                linkedObject = null;
                link = null;
            }

            states.Remove(state);
        }
        else
        {
            states.Add(state);
        }
    }

    public virtual void ResetState()
    {
        if (states.Contains(State.Sticky))
        {
            if (rigidBody.TryGetComponent<Joint>(out Joint joint))
            {
                if (joint.connectedBody == objectToStick.GetComponent<Rigidbody>())
                {
                    Destroy(joint);
                    joint = null;
                }
            }

            if (objectToStick.TryGetComponent<StateManager>(out StateManager stateManager))
            {
                stateManager.stickedObjects.Remove(gameObject);
            }

            objectToStick = null;
            isSticked = false;
            rigidBody.useGravity = true;
        }
        else if (states.Contains(State.Link))
        {
            isLinked = false;
            if (linkedObject != null)
            {
                linkedObject.isLinked = false;
                linkedObject.linkedObject = null;
                linkedObject.link = null;
            }

            if (link != null)
            {
                Destroy(link.GetComponent<CustomRope>().end.gameObject);
                Destroy(link.GetComponent<CustomRope>().start.gameObject);
                Destroy(link.gameObject);
            }

            linkedObject = null;
            link = null;
        }

        states = new List<State>();
    }

    ///////////////////////////////////////////////////
    ///                HOLD METHODS                 ///
    ///////////////////////////////////////////////////

    public virtual void SetHoldObject(Transform endPosition, float time)
    {
        bool wasEquipped = false;
        position = Position.Held;
        if (states.Contains(State.Sticky))
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
            isEquipped = false;
            isEquippedObject = false;
            equippingPlayer = null;
            Destroy(joint);
            joint = null;
            wasEquipped = true;
        }

        isHeldObject = true;
        objectCollider.isTrigger = true;
        rigidBody.useGravity = false;

        rigidBody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rigidBody.mass = 0.1f;
        holdingPlayer = endPosition.parent.GetComponent<PlayerManager>();

        if (!wasEquipped)
        {
            holdingPlayer.moveMassMultiplier += playerMoveMassMultiplier;
        }

        List<GameObject> stickedList = GetStickedObjects(GetFirstStickedObject(this.gameObject));

        foreach (GameObject stickedObject in stickedList)
        {
            if (stickedObject.TryGetComponent<StateManager>(out StateManager stateManager))
            {
                if (!wasEquipped)
                {
                    holdingPlayer.moveMassMultiplier += playerMoveMassMultiplier;
                }
                
                stateManager.rigidBody.mass = 0.1f;
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

        InitializeHoldObject(parent);
    }

    public virtual void InitializeHoldObject(Transform parent)
    {
        isHeld = true;

        objectCollider.isTrigger = false;
        if (joint == null)
        {
            joint = transform.AddComponent<FixedJoint>();
        }
        joint.connectedBody = holdingPlayer.transform.GetComponent<Rigidbody>();
        joint.enableCollision = true;

        if (states.Contains(State.Link))
        {
            joint.breakForce = float.PositiveInfinity;//jointBreakTreshold * 100;
            joint.breakTorque = float.PositiveInfinity;//jointBreakTreshold * 100;
        }
        else
        {
            joint.breakForce = jointBreakTreshold;
            joint.breakTorque = jointBreakTreshold;
        }
    }

    public virtual void DropObject()
    {
        if (isHeldObject && isHeld)
        {
            if (joint != null)
            { 
                Destroy(joint);
                joint = null;
            }

            rigidBody.constraints = RigidbodyConstraints.FreezeRotation;
            rigidBody.mass = 1f;
            holdingPlayer.moveMassMultiplier -= playerMoveMassMultiplier;

            foreach (GameObject stickedObject in GetStickedObjects(GetFirstStickedObject(this.gameObject)))
            {
                if (stickedObject.TryGetComponent<StateManager>(out StateManager stateManager)){
                    stateManager.rigidBody.mass = 1f;
                    holdingPlayer.moveMassMultiplier -= playerMoveMassMultiplier;
                }
            }

            position = Position.Default;
            isHeld = false;
            isHeldObject = false;
            holdingPlayer = null;
            transform.parent = parent;

            rigidBody.useGravity = true;
            rigidBody.angularVelocity = Vector3.zero;
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

        if (states.Contains(State.Link))
        {
            throwForceHorizontal = throwForceHorizontal * linkThrowMultiplier;
            throwForceVertical = throwForceVertical * linkThrowMultiplier;
        }

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
        rigidBody.velocity = Vector3.zero;
        rigidBody.AddForce(throwDirection, ForceMode.Impulse);
        objectCollider.isTrigger = false;
        holdingPlayer = null;
    }

    protected virtual Vector3 GetThrowForce(float throwForceHorizontal, float throwForceVertical, Vector3 hitpoint)
    {
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
        return throwDirection;
    }

    ///////////////////////////////////////////////////
    ///                EQUIP METHODS                ///
    ///////////////////////////////////////////////////

    public virtual void SetEquipObject(Transform endPosition, float time)
    {
        position = Position.Equipped;
        isHeld = false;
        holdingPlayer = null;

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
        joint.enableCollision = true;

        if (states.Contains(State.Link))
        {
            joint.breakForce = float.PositiveInfinity;//jointBreakTreshold * 100;
            joint.breakTorque = float.PositiveInfinity;//jointBreakTreshold * 100;
        }
        else
        {
            joint.breakForce = jointBreakTreshold;
            joint.breakTorque = jointBreakTreshold;
        }
    }

    protected GameObject GetFirstStickedObject(GameObject currentObject)
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

    protected List<GameObject> GetStickedObjects(GameObject firstStickedObject)
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
        if (states.Contains(State.Sticky) && !isHeldObject  && !isEquipped && !isSticked)
        {
            rigidBody.useGravity = false;
            rigidBody.velocity = Vector3.zero;
            rigidBody.angularVelocity = Vector3.zero;

            if (joint == null)
            {
                joint = transform.AddComponent<FixedJoint>();
            }
            joint.connectedBody = collision.transform.GetComponent<Rigidbody>();
            //joint.breakForce = jointBreakTreshold * 100000;
            //joint.breakTorque = jointBreakTreshold * 100000;

            /*if (stickSnap == null)
            {
                stickSnap = transform.AddComponent<SnapRigidbodyPosition>();
                stickSnap.Initialize(rigidBody, collision.transform);
            }*/

            objectToStick = collision.gameObject;
            isSticked = true;

            if (collision.transform.TryGetComponent<StateManager>(out StateManager stateManager))
            {
                stateManager.stickedObjects.Add(this.gameObject);
            }

            Debug.Log(collision.transform.name);
        }
    }

    protected virtual void OnJointBreak(float breakForce)
    {
        if (isHeld)
        {
            if (joint.connectedBody == holdingPlayer.rigidBody)
            {
                position = Position.Default;
                isHeld = false;
                isHeldObject = false;

                Destroy(joint);
                joint = null;

                rigidBody.constraints = RigidbodyConstraints.FreezeRotation;
                rigidBody.mass = 1f;
                holdingPlayer.moveMassMultiplier -= playerMoveMassMultiplier;

                foreach (GameObject stickedObject in GetStickedObjects(GetFirstStickedObject(this.gameObject)))
                {
                    if (stickedObject.TryGetComponent<StateManager>(out StateManager stateManager))
                    {
                        holdingPlayer.moveMassMultiplier -= playerMoveMassMultiplier;
                        stateManager.rigidBody.mass = 1f;
                    }
                }

                rigidBody.useGravity = true;

                holdingPlayer.animator.SetTrigger("Drop");
                holdingPlayer.heldObject = null;
                holdingPlayer = null;
                transform.parent = parent;
            }
        }
        else if (isEquipped)
        {
            if (joint.connectedBody == equippingPlayer.rigidBody)
            {
                position = Position.Default;
                isEquipped = false;
                isEquippedObject = false;

                Destroy(joint);
                joint = null;

                rigidBody.constraints = RigidbodyConstraints.FreezeRotation;
                rigidBody.mass = 1f;
                equippingPlayer.moveMassMultiplier -= playerMoveMassMultiplier;

                foreach (GameObject stickedObject in GetStickedObjects(GetFirstStickedObject(this.gameObject)))
                {
                    if (stickedObject.TryGetComponent<StateManager>(out StateManager stateManager))
                    {
                        equippingPlayer.moveMassMultiplier -= playerMoveMassMultiplier;
                        stateManager.rigidBody.mass = 1f;
                    }
                }

                rigidBody.useGravity = true;

                equippingPlayer.equippedObject = null;
                equippingPlayer = null;
                transform.parent = parent;
            }
        }
        else if (isSticked)
        {
            if (joint.connectedBody == objectToStick.GetComponent<Rigidbody>())
            {
                Destroy(joint);
                joint = null;
            }

            if (objectToStick.TryGetComponent<StateManager>(out StateManager stateManager))
            {
                stateManager.stickedObjects.Remove(gameObject);
            }

            objectToStick = null;
            isSticked = false;
            rigidBody.useGravity = true;
        }
    }
}
