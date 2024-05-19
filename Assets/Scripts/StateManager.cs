using DG.Tweening;
using Obi;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

public class StateManager : MonoBehaviour
{
    public enum Type { Player, Mushroom, Object, Orbe, Creature }
    public enum State { Default, Sticky, Link }
    public enum Position { Default, Held, Equipped }

    [Header("StateManager References")]
    public Type type;
    public List<State> states;
    public Position position;
    [SerializeField] public Transform pivot;
    [SerializeField] protected Rigidbody RigidBody;
    [SerializeField] private Collider ObjectCollider;
    [SerializeField] private Renderer Renderer;
    [SerializeField] private Outline Outline;
    public StickedWallElements stickedWall;

    [SerializeField] protected Renderer stickyRenderer;
    [SerializeField] private Material dissolveMaterial;
    [SerializeField] private Material stickyMaterial;
    private Material initMat = null;
    private Material stickyMat = null;
    private Material dissolveMat = null;
    private Coroutine stickyChange = null;
    private Coroutine stickyEffect = null;
    private AnimationCurve inOutCurve = null;
    private PlayerManager previousHoldingPlayer = null;

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

    protected Transform initParent;

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
    private float jointBreakTreshold = 1000f;
    private PlayerManager stickHoldingPlayer = null;

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
        objectCollider = ObjectCollider;
        renderer = Renderer;
        outline = Outline;
        isHeldObject = false;
        isHeld = false;
        initParent = transform.parent;
        inOutCurve = Utilities.ConvertEaseToCurve(Ease.InOutSine);

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

                if (objectToStick != null)
                {
                    if (objectToStick.TryGetComponent<StateManager>(out StateManager stateManager))
                    {
                        stateManager.stickedObjects.Remove(gameObject);
                    }
                }

                objectToStick = null;
                isSticked = false;
                rigidBody.useGravity = true;

                if (stickyChange != null)
                {
                    StopCoroutine(stickyChange);
                    stickyChange = null;
                }

                stickyChange = StartCoroutine(SetSticky(1f));
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

            if (state == State.Sticky)
            {
                if (isHeld)
                {
                    stickHoldingPlayer = holdingPlayer;
                }

                if (stickyChange != null)
                {
                    StopCoroutine(stickyChange);
                    stickyChange = null;
                }

                stickyChange = StartCoroutine(SetSticky(0f));
            }
        }
    }

    private IEnumerator SetSticky(float value)
    {
        float dissolveTime = 1.5f;

        float dissolve = stickyRenderer.material.GetFloat("_Dissolve");
        float dissolveToSet = value;

        float elapsedTime = 0f;
        while (elapsedTime < dissolveTime)
        {
            float time = elapsedTime / dissolveTime;
            dissolve = Mathf.Lerp(dissolve, dissolveToSet, time);

            stickyRenderer.material.SetFloat("_Dissolve", dissolve);
            elapsedTime += Time.deltaTime;
            yield return new WaitForFixedUpdate();
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

    public virtual void SetHoldObject(PlayerManager player, Transform endPosition, float time)
    {
        bool wasEquipped = false;

        if (isEquipped)
        {
            isEquipped = false;
            isEquippedObject = false;
            equippingPlayer = null;
            wasEquipped = true;
        }

        if (stickHoldingPlayer == null && states.Contains(State.Sticky))
        {
            stickHoldingPlayer = player;
        }

        position = Position.Held;
        if (states.Contains(State.Sticky))
        {
            transform.SetParent(initParent, true);
            
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
        isHeld = true;
        objectCollider.isTrigger = true;
        rigidBody.useGravity = false;

        rigidBody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rigidBody.mass = 0.1f;
        holdingPlayer = player;

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
            Vector3 endRot = endPosition.localEulerAngles;
            endRot.x = 0f;
            endRot.z = 0f;

            Vector3 pos = Vector3.zero;

            if (pivot != null)
            {
                Vector3 diff = transform.position - pivot.position;
                pos = endPosition.position + diff;
            }
            else
            {
                pos = endPosition.position;
            }

            transform.position = Vector3.Lerp(transform.position, pos, time);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(endRot), time);

            elapsedTime += Time.deltaTime;
            yield return new WaitForFixedUpdate();
        }

        InitializeHoldObject(endPosition);
    }

    public virtual void InitializeHoldObject(Transform parent)
    {
        objectCollider.isTrigger = false;
        transform.SetParent(parent, true);
        rigidBody.useGravity = false;
        rigidBody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePosition;
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

            rigidBody.constraints = RigidbodyConstraints.None;
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
            previousHoldingPlayer = holdingPlayer;
            holdingPlayer = null;
            transform.SetParent(initParent, true);
            objectCollider.isTrigger = false;
            rigidBody.useGravity = true;
            rigidBody.isKinematic = false;
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
            Vector3 playerToHitPoint = hitpoint - transform.position;
            throwDirection = Vector3.ProjectOnPlane(playerToHitPoint, Vector3.up).normalized * throwForceHorizontal;
        }
        else
        {
            throwDirection = Camera.main.transform.forward.normalized * throwForceHorizontal;
        }

       throwDirection += Vector3.up * throwForceVertical;

        objectCollider.isTrigger = false;
        rigidBody.isKinematic = false;
        rigidBody.useGravity = true;
        rigidBody.velocity = Vector3.zero;
        rigidBody.AddForce(throwDirection, ForceMode.Impulse);
        holdingPlayer = null;
    }

    protected virtual Vector3 GetThrowForce(float throwForceHorizontal, float throwForceVertical, Vector3 hitpoint)
    {
        Vector3 throwDirection = Vector3.zero;

        if (hitpoint != Vector3.zero)
        {
            Vector3 playerToHitPoint = hitpoint - transform.position;
            throwDirection = Vector3.ProjectOnPlane(playerToHitPoint, Vector3.up).normalized * throwForceHorizontal;
        }
        else
        {
            throwDirection = Camera.main.transform.forward.normalized * throwForceHorizontal;
        }

        throwDirection += Vector3.up * throwForceVertical;
        return throwDirection;
    }

    ///////////////////////////////////////////////////
    ///                EQUIP METHODS                ///
    ///////////////////////////////////////////////////

    public virtual void SetEquipObject(PlayerManager player, Transform endPosition, float time)
    {
        position = Position.Equipped;
        isHeld = false;
        isEquipped = true;
        holdingPlayer = null;
        equippingPlayer = player;

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
            Vector3 endRot = endPosition.localEulerAngles;
            endRot.x = 0f;
            endRot.z = 0f;

            Vector3 pos = Vector3.zero;

            if (pivot != null)
            {
                Vector3 diff = transform.position - pivot.position;
                pos = endPosition.position + diff;
            }
            else
            {
                pos = endPosition.position;
            }

            transform.position = Vector3.Lerp(transform.position, pos, time);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(endRot), time);

            elapsedTime += Time.deltaTime;
            yield return new WaitForFixedUpdate();
        }

        InitializeEquipObject(endPosition);
    }

    public virtual void InitializeEquipObject(Transform parent)
    {
        objectCollider.isTrigger = false;
        transform.SetParent(parent, true);
        rigidBody.useGravity = false;
        rigidBody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePosition;
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
            /*if (stickHoldingPlayer != null)
            {
                if (collision.gameObject == holdingPlayer.gameObject)
                {
                    return;
                }
                else
                {
                    stickHoldingPlayer = null;
                }
            }

            rigidBody.velocity = Vector3.zero;
            rigidBody.angularVelocity = Vector3.zero;
            rigidBody.useGravity = false;
            rigidBody.constraints = RigidbodyConstraints.FreezePosition;

            objectToStick = collision.gameObject;
            isSticked = true;

            Transform root = collision.transform;
            bool isAnimated = false;

            if (collision.transform.TryGetComponent<StateManager>(out StateManager stateManager))
            {
                stateManager.stickedObjects.Add(this.gameObject);

                if (stateManager.tag == "Player")
                {
                    PlayerManager playerManager = (PlayerManager)stateManager;
                    root = playerManager.animationRoot;
                    isAnimated = true;
                }
                else if (stateManager.tag == "Creature")
                {
                    CreatureManager creatureManager = (CreatureManager)stateManager;
                    root = creatureManager.animationRoot;
                    isAnimated = true;
                }
            }

            Vector3 contactPoint = Vector3.zero;

            foreach (ContactPoint contact in collision.contacts)
            {
                if (contact.thisCollider == collision.collider || contact.otherCollider == collision.collider)
                {
                    contactPoint = contact.point;
                    break;
                }
            }

            if (isAnimated)
            {
                float contactDistance = Vector3.Distance(transform.position, contactPoint);
                Transform nearestTransform = GetNearestTransform(contactPoint, contactDistance, root);
                
                if (nearestTransform != null)
                {
                    root = nearestTransform;
                }
            }

            transform.SetParent(root, true);*/

            if (previousHoldingPlayer != null)
            {
                if (collision.gameObject == previousHoldingPlayer.gameObject)
                {
                    return;
                }
            }

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

    private Transform GetNearestTransform(Vector3 contactPoint, float distance, Transform transform)
    {
        float thisDistance = Vector3.Distance(contactPoint, transform.position);

        if (thisDistance < distance)
        {
            if (transform.childCount > 0)
            {
                Transform nearerChild = null;

                for (int i = 0; i < transform.childCount; ++i)
                {
                    Transform child = GetNearestTransform(contactPoint, thisDistance, transform.GetChild(i));
                   
                    if (child != null)
                    {
                        nearerChild = child;
                    }

                    if (i == transform.childCount - 1 && nearerChild != null)
                    {
                        return nearerChild;
                    }
                    else
                    {
                        return transform;
                    }
                }
            }

            return transform;
        }
        else
        { 
            return null;
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
                transform.SetParent(initParent, true);
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
                transform.SetParent(initParent, true);
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
