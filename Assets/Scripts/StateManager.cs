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
    [SerializeField] public Transform handPivot;
    [SerializeField] public Transform headPivot;
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

    public Transform initParent;

    public GameObject objectToStick = null;
    public List<GameObject> stickedObjects = new List<GameObject>();

    [HideInInspector] public GameObject link = null;
    [HideInInspector] public StateManager linkedObject = null;
    [HideInInspector] public Rigidbody linkAttachment = null;
    [HideInInspector] public FixedJoint linkJoint = null;
    public bool isSticked;
    public bool isLinked;

    public PlayerManager holdingPlayer = null;
    public PlayerManager equippingPlayer = null;
    protected Joint joint = null;
    private float jointBreakTreshold = 1000f;
    private PlayerManager stickHoldingPlayer = null;
    private Transform stickedTransform = null;
    private Vector3 stickedOffsetPosition;
    private Quaternion stickedOffsetRotation;
    private List<GameObject> stickedCollisions = new List<GameObject>();

    public bool isHeldObject;
    public bool isHeld;
    public bool isEquippedObject;
    public bool isEquipped;

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

        if (initParent == null)
        {
            initParent = transform.parent;
        }
        
        inOutCurve = Utilities.ConvertEaseToCurve(Ease.InOutSine);

        lastGroundedPosition = rigidBody.position;
        lastGroundedRotation = rigidBody.rotation;

        if (stickedWall != null )
        {
            if (stickyRenderer != null)
            {
                stickyRenderer.material.SetFloat("_Dissolve", 0f);
            }
        }

        startThrowForceHorizontal = 80;
        startThrowForceVertical = 50;

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
                rigidBody.isKinematic = true;

                if (transform.tag == "Player")
                {
                    rigidBody.constraints = RigidbodyConstraints.FreezeRotation;
                }
                else
                {
                    rigidBody.constraints = RigidbodyConstraints.None;
                }

                if (stickyChange != null)
                {
                    StopCoroutine(stickyChange);
                    stickyChange = null;
                }

                if (stickyRenderer != null)
                {
                    stickyChange = StartCoroutine(SetSticky(1f));
                }
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

                if (stickyRenderer != null)
                {
                    stickyChange = StartCoroutine(SetSticky(0f));
                }
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
            equippingPlayer.equippedObject = null;
            equippingPlayer.moveMassMultiplier -= playerMoveMassMultiplier;
            equippingPlayer = null;
            wasEquipped = true;
        }

        if (isHeld)
        {
            isHeld = false;
            isHeldObject = false;
            holdingPlayer.heldObject = null;
            holdingPlayer.moveMassMultiplier -= playerMoveMassMultiplier;
            holdingPlayer = null;
        }

        if (isSticked)
        {
            isSticked = false;
            rigidBody.constraints = RigidbodyConstraints.None;
        }

        if (stickHoldingPlayer == null && states.Contains(State.Sticky))
        {
            stickHoldingPlayer = player;
        }

        position = Position.Held;

        isHeldObject = true;
        isHeld = true;
        objectCollider.isTrigger = true;
        rigidBody.useGravity = false;

        rigidBody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rigidBody.mass = 0.1f;
        holdingPlayer = player;

        holdingPlayer.moveMassMultiplier += playerMoveMassMultiplier;

        StartCoroutine(HoldObject(endPosition, time));
    }

    private IEnumerator HoldObject(Transform endTransform, float transitionDuration)
    {
        float elapsedTime = 0;

        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.fixedDeltaTime;

            float time = elapsedTime / transitionDuration;

            Vector3 pos = Vector3.zero;
            Quaternion rot = Quaternion.identity;

            if (handPivot != null)
            {
                Vector3 posOffset = transform.position - handPivot.position;
                Quaternion rotOffset = Quaternion.Inverse(handPivot.rotation) * endTransform.rotation;

                pos = endTransform.position + posOffset;
                rot = endTransform.rotation * Quaternion.Inverse(handPivot.rotation) * transform.rotation;
            }
            else
            {
                pos = endTransform.position;
                rot = endTransform.rotation;
            }

            transform.position = Vector3.Lerp(transform.position, pos, time);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, time);

            elapsedTime += Time.deltaTime;
            yield return new WaitForFixedUpdate();
        }

        InitializeHoldObject(endTransform);
    }

    public virtual void InitializeHoldObject(Transform parent)
    {
        objectCollider.isTrigger = false;
        transform.SetParentWithGlobalScale(parent, true);
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

            if (transform.tag == "Player")
            {
                rigidBody.constraints = RigidbodyConstraints.FreezeRotation;
            }
            else
            {
                rigidBody.constraints = RigidbodyConstraints.None;
            }

            rigidBody.mass = 10f;
            holdingPlayer.moveMassMultiplier -= playerMoveMassMultiplier;

            position = Position.Default;
            isHeld = false;
            isHeldObject = false;
            previousHoldingPlayer = holdingPlayer;
            holdingPlayer = null;
            transform.SetParentWithGlobalScale(initParent, true);
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

        isEquippedObject = true;
        objectCollider.isTrigger = true;
        rigidBody.useGravity = false;

        StartCoroutine(EquipObject(endPosition, time));
    }

    private IEnumerator EquipObject(Transform endTransform, float transitionDuration)
    {
        float elapsedTime = 0;

        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.fixedDeltaTime;

            float time = elapsedTime / transitionDuration;

            Vector3 pos = Vector3.zero;
            Quaternion rot = Quaternion.identity;

            if (handPivot != null)
            {
                Vector3 posOffset = transform.position - headPivot.position;
                Quaternion rotOffset = Quaternion.Inverse(headPivot.rotation) * endTransform.rotation;

                pos = endTransform.position + posOffset;
                rot = endTransform.rotation * Quaternion.Inverse(headPivot.rotation) * transform.rotation;
            }
            else
            {
                pos = endTransform.position;
                rot = endTransform.rotation;
            }

            transform.position = Vector3.Lerp(transform.position, pos, time);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, time);

            elapsedTime += Time.deltaTime;
            yield return new WaitForFixedUpdate();
        }

        InitializeEquipObject(endTransform);
    }

    public virtual void InitializeEquipObject(Transform parent)
    {
        objectCollider.isTrigger = false;
        transform.SetParentWithGlobalScale(parent, true);
        rigidBody.useGravity = false;
        rigidBody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePosition;
    }

    ///////////////////////////////////////////////////
    ///            COLLISION MANAGEMENT             ///
    ///////////////////////////////////////////////////

    protected virtual void OnCollisionEnter(Collision collision)
    {
        if (isSticked && !isHeldObject && !isEquippedObject)
        {
            bool hadCollision = false;

            if (stickedCollisions.Count > 0)
            {
                hadCollision = true;
            }

            stickedCollisions.Add(collision.gameObject);

            if (hadCollision )
            {
                rigidBody.constraints = RigidbodyConstraints.None;
                rigidBody.isKinematic = true;
            }
        }

        if (states.Contains(State.Sticky) && !isHeldObject && !isEquippedObject && !isSticked)
        {
            if (stickHoldingPlayer != null)
            {
                if (collision.gameObject == stickHoldingPlayer.objectCollider.gameObject)
                {
                    return;
                }
                else
                {
                    if (stickHoldingPlayer.equippedObject != null)
                    {
                        if (collision.gameObject == stickHoldingPlayer.equippedObject.objectCollider.gameObject)
                        {
                            return;
                        }
                    }

                    stickHoldingPlayer = null;
                }
            }

            rigidBody.velocity = Vector3.zero;
            rigidBody.angularVelocity = Vector3.zero;
            rigidBody.useGravity = false;
            rigidBody.constraints = RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotation;

            objectToStick = collision.gameObject;
            isSticked = true;

            stickedTransform = collision.transform;
            bool isAnimated = false;

            if (collision.transform.TryGetComponent<StateManager>(out StateManager stateManager))
            {
                stateManager.stickedObjects.Add(this.gameObject);

                if (stateManager.tag == "Player")
                {
                    PlayerManager playerManager = (PlayerManager)stateManager;
                    stickedTransform = playerManager.animationRoot;
                    isAnimated = true;
                }
                else if (stateManager.tag == "Creature")
                {
                    CreatureManager creatureManager = (CreatureManager)stateManager;
                    stickedTransform = creatureManager.animationRoot;
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
                Transform nearestTransform = GetNearestTransform(contactPoint, contactDistance, stickedTransform);

                if (nearestTransform != null)
                {
                    stickedTransform = nearestTransform;
                }
            }

            transform.SetParentWithGlobalScale(stickedTransform, true);
        }
    }

    protected virtual void OnCollisionExit(Collision collision)
    {
        if (isSticked && !isHeldObject && !isEquippedObject)
        {
            if (stickedCollisions.Contains(collision.gameObject))
            {
                if (stickedCollisions.Count == 0)
                {
                    rigidBody.isKinematic = false;
                    rigidBody.constraints = RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotation;
                }
            }
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
}
