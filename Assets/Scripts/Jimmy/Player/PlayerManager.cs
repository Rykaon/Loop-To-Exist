using Cinemachine;
using DG.Tweening;
using Obi;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

public class PlayerManager : StateManager
{
    [SerializeField] private PlayerCameraHandler CameraHandler;
    public PlayerCameraHandler cameraHandler { get; private set; }

    [SerializeField] private PlayerMovementHandler MovementHandler;
    public PlayerMovementHandler movementHandler { get; private set; }

    [SerializeField] private PlayerObjectHandler ObjectHandler;
    public PlayerObjectHandler objectHandler { get; private set; }

    [Header("Player References")]
    [SerializeField] private Transform head;
    [SerializeField] private Transform hand;
    [SerializeField] private Transform feet;
    [SerializeField] private Transform eye;
    [SerializeField] private Transform linkTarget;
    [SerializeField] public Transform cameraTarget;

    public Transform playerCamera { get; private set; }

    [SerializeField] private PlayerInteractionTrigger Trigger;
    public PlayerInteractionTrigger trigger { get; private set; }

    public PlayerControls playerControls { get; private set; }
    public StateManager heldObject { get; set; }
    public StateManager equippedObject { get; set; }

    [Header("Status")]
    public bool isMainPlayer;
    public bool isActive;
    public bool isAiming = false;

    [HideInInspector] public bool buttonSouthIsPressed = false;
    [HideInInspector] public bool buttonWestIsPressed = false;
    [HideInInspector] public bool buttonEastIsPressed = false;
    [HideInInspector] public bool buttonNorthIsPressed = false;
    [HideInInspector] public bool leftTriggerIsPressed = false;
    [HideInInspector] public bool rightTriggerIsPressed = false;

    [Header("Camera Properties")]
    [SerializeField] public float cameraRotationSpeed;

    [Header("Link Properties")]
    [SerializeField] private GameObject ropePrefab;
    [SerializeField] private GameObject sphere;
    [SerializeField] private float ropeParticlesDistance = 0.5f;
    [SerializeField] private float ropeParticleMoovingSpeed = 0.1f;
    [HideInInspector] public CustomRope rope = null;

    ///////////////////////////////////////////////////
    ///            FONCTIONS HÉRITÉES               ///
    ///////////////////////////////////////////////////

    public override void Initialize(GameManager instance)
    {
        base.Initialize(instance);

        trigger = Trigger;
        heldObject = null;
        equippedObject = null;
        

        movementHandler = MovementHandler;
        movementHandler.Initialize(this);
        cameraHandler = CameraHandler;
        cameraHandler.Initialize(this);
        objectHandler = ObjectHandler;
        objectHandler.Initialize(this);

        playerControls = gameManager.playerControls;
    }

    public override void SetState(State state)
    {
        base.SetState(state);
    }

    public override void ResetState()
    {
        base.ResetState();
    }

    public override void SetHoldObject(Transform endPosition, float time)
    {
        base.SetHoldObject(endPosition, time);
    }

    public override void InitializeHoldObject(Transform parent)
    {
        base.InitializeHoldObject(parent);
    }

    public override void ThrowObject(float throwForceHorizontal, float throwForceVertical, Vector3 hitpoint)
    {
        base.ThrowObject(throwForceHorizontal, throwForceVertical, hitpoint);
    }

    public override void SetEquipObject(Transform endPosition, float time)
    {
        base.SetEquipObject(endPosition, time);
    }

    public override void InitializeEquipObject(Transform parent)
    {
        base.InitializeEquipObject(parent);
    }

    protected override void OnCollisionEnter(Collision collision)
    {
        base.OnCollisionEnter(collision);
    }

    protected override void OnJointBreak(float breakForce)
    {
        base.OnJointBreak(breakForce);
    }

    ///////////////////////////////////////////////////
    ///          FONCTIONS DE GESTIONS              ///
    ///////////////////////////////////////////////////

    public void SetIsMainPlayer(bool value)
    {
        if (value)
        {
            playerCamera = gameManager.cameraManager.worldCamera.transform;
            playerControls = gameManager.playerControls;
            isMainPlayer = true;
        }
        else
        {
            playerControls = null;
            isMainPlayer = false;
        }
    }

    ///////////////////////////////////////////////////
    ///           FONCTIONS D'ACTIONS               ///
    ///////////////////////////////////////////////////  

    

    

    [Header("Slope Handling")]
    public float maxSlopeAngle;
    private RaycastHit slopeHit;
    private bool exitingSlope;
    

    public void Hold()
    {
        if (heldObject == null)
        {
            if (trigger.triggeredObjectsList.Count > 0 && heldObject == null)
            {
                float startDistance = float.MaxValue;
                int index = -1;
                for (int i = 0; i < trigger.triggeredObjectsList.Count; i++)
                {
                    float distance = Vector3.Distance(trigger.transform.position, trigger.triggeredObjectsList[i].transform.position);
                    if (distance < startDistance)
                    {
                        index = i;
                        startDistance = distance;
                    }
                }

                if (index >= 0)
                {
                    heldObject = trigger.triggeredObjectsList[index];
                    heldObject.SetHoldObject(hand, 0.25f);
                }
            }
        }
        else if (heldObject != null)
        {
            heldObject.DropObject();
            heldObject = null;
        }
    }

    public void Equip()
    {
        if (equippedObject == null && heldObject != null)
        {
            equippedObject = heldObject;
            equippedObject.SetEquipObject(head, 0.25f);
            heldObject = null;
        }
        else if (equippedObject != null && heldObject == null)
        {
            heldObject = equippedObject;
            equippedObject.SetHoldObject(hand, 0.25f);
            equippedObject = null;
        }
    }

    public void Throw()
    {
        if (heldObject != null)
        {
            if (heldObject.isHeld)
            {
                StartCoroutine(CalculateThrowForce());
            }
        }
    }

    private IEnumerator CalculateThrowForce()
    {
        float startThrowForceHorizontal = heldObject.startThrowForceHorizontal;
        float startThrowForceVertical = heldObject.startThrowForceVertical;
        float maxThrowForceHorizontal = 10;
        float maxThrowForceVertical = 7;

        while (isAiming && playerControls.Player.X.IsPressed())
        {
            startThrowForceHorizontal += (1 + Time.fixedDeltaTime);
            startThrowForceVertical += (1 + Time.fixedDeltaTime);

            if (startThrowForceHorizontal > maxThrowForceHorizontal)
            {
                startThrowForceHorizontal = maxThrowForceHorizontal;
            }

            if (startThrowForceVertical > maxThrowForceVertical)
            {
                startThrowForceVertical = maxThrowForceVertical;
            }

            yield return new WaitForFixedUpdate();
        }

        bool isStillAiming = true;

        if (!isAiming)
        {
            isStillAiming = false;
        }

        if (isStillAiming)
        {
            Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
            Vector3 hitPoint = Vector3.zero;
            Ray ray = Camera.main.ScreenPointToRay(screenCenter);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                hitPoint = hit.point;
            }

            heldObject.ThrowObject(startThrowForceHorizontal, startThrowForceVertical, hitPoint);
            heldObject = null;
        }
    }

    public void Aim(bool value)
    {
        isAiming = value;

        Vector3 cameraTargetPos = Vector3.zero;
        if (isAiming)
        {
            cameraTargetPos = new Vector3(0, 2, 0);
        }

        gameManager.cameraManager.SetCameraAim(value, cameraTargetPos);
    }

    public void Shot(InputAction action)
    {
        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
        Vector3 hitPoint = Vector3.zero;
        Ray ray = Camera.main.ScreenPointToRay(screenCenter);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            if (action == playerControls.Player.B)
            {
                if (hit.collider.tag == "Player" && hit.collider.transform != transform)
                {
                    if (hit.collider.TryGetComponent<PlayerManager>(out PlayerManager playerManager))
                    {
                        if (playerManager.position == Position.Default)
                        {
                            Aim(false);
                            //gameManager.cameraManager.aimCamera.m_Follow = playerManager.cameraTarget;
                            //gameManager.cameraManager.aimCamera.m_LookAt = playerManager.cameraTarget;
                            gameManager.SetMainPlayer(playerManager, true);
                            return;
                        }
                    }
                }
            }

            hitPoint = hit.point;
        }

        Vector3 rayDirection;

        if (hitPoint == Vector3.zero)
        {
            rayDirection = eye.forward;
        }
        else
        {
            rayDirection = hitPoint - eye.position;
        }

        ray.origin = eye.position;
        ray.direction = rayDirection;

        if (Physics.Raycast(ray, out hit))
        {
            if (action == playerControls.Player.RT)
            {
                if (equippedObject.type == Type.Mushroom)
                {
                    MushroomManager equippedMushroom = (MushroomManager)equippedObject;
                    if (equippedMushroom.stateToApply == State.Sticky)
                    {
                        if ((hit.collider.tag == "Player" || hit.collider.tag == "Mushroom" || hit.collider.tag == "Object") && hit.collider.transform != transform)
                        {
                            if (hit.collider.TryGetComponent<StateManager>(out StateManager stateManager))
                            {
                                stateManager.SetState(equippedMushroom.stateToApply);
                            }
                        }
                    }
                    else if (equippedMushroom.stateToApply == State.Link)
                    {
                        if ((hit.collider.tag == "Player" || hit.collider.tag == "Mushroom" || hit.collider.tag == "Object"))
                        {
                            if (hit.collider.TryGetComponent<StateManager>(out StateManager stateManager))
                            {
                                stateManager.SetState(equippedMushroom.stateToApply);

                                if (stateManager.states.Contains(State.Link))
                                {
                                    StartCoroutine(Link(stateManager, hitPoint));
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private IEnumerator Link(StateManager linkedObject, Vector3 hitPoint)
    {
        Vector3 direction = hitPoint - hand.position;

        if (!isLinked)
        {
            if (this.linkedObject != null)
            {
                this.linkedObject.isLinked = false;
                this.linkedObject.linkedObject = null;
                this.linkedObject.link = null;
            }

            if (link != null)
            {
                Destroy(link.GetComponent<CustomRope>().end.gameObject);
                Destroy(link.GetComponent<CustomRope>().start.gameObject);
                Destroy(link.gameObject);
            }

            if (states.Contains(State.Link))
            {
                states.Remove(State.Link);
            }

            this.linkedObject = null;
            link = null;

            linkAttachment = Instantiate(sphere, linkTarget.position + (Vector3.up * 0.1f), Quaternion.identity).GetComponent<Rigidbody>();
            ObiCollider startCollider = linkAttachment.GetComponent<ObiCollider>();
            linkJoint = transform.AddComponent<FixedJoint>();
            linkJoint.connectedBody = linkAttachment;

            linkedObject.linkAttachment = Instantiate(sphere, hitPoint - (direction.normalized * 0.1f), Quaternion.identity).GetComponent<Rigidbody>();
            ObiCollider endCollider = linkedObject.linkAttachment.GetComponent<ObiCollider>();
            linkedObject.linkJoint = linkedObject.AddComponent<FixedJoint>();
            linkedObject.linkJoint.connectedBody = linkedObject.linkAttachment;

            link = Instantiate(ropePrefab, hand.position, Quaternion.identity, gameManager.obiSolver.transform);
            rope = link.GetComponent<CustomRope>();
            rope.solver = gameManager.obiSolver;

            rope.Initialize(startCollider, null, endCollider);

            isLinked = true;

            this.linkedObject = linkedObject;
            this.linkedObject.link = link;
            this.linkedObject.linkedObject = this;

            SetState(State.Link);

        }
        else
        {
            Destroy(linkAttachment.gameObject);
            linkAttachment = null;

            ObiCollider endCollider = this.linkedObject.linkAttachment.GetComponent<ObiCollider>();

            linkedObject.linkAttachment = Instantiate(sphere, hitPoint - (direction.normalized * 0.1f), Quaternion.identity).GetComponent<Rigidbody>();
            ObiCollider startCollider = linkedObject.linkAttachment.GetComponent<ObiCollider>();
            linkedObject.linkJoint = linkedObject.AddComponent<FixedJoint>();
            linkedObject.linkJoint.connectedBody = linkedObject.linkAttachment;

            Destroy(rope.gameObject);
            link = Instantiate(ropePrefab, hand.position, Quaternion.identity, gameManager.obiSolver.transform);
            rope = link.GetComponent<CustomRope>();
            rope.solver = gameManager.obiSolver;
            rope.Initialize(startCollider, hand, endCollider);

            isLinked = false;
            this.linkedObject.link = link;
            this.linkedObject.linkedObject = linkedObject;
            this.linkedObject.linkedObject.linkedObject = this.linkedObject;
            this.linkedObject.linkedObject.link = link;
            this.linkedObject = null;
            link = null;

            if (states.Contains(State.Link))
            {
                states.Remove(State.Link);
            }
        }


        /*for (int i = 0; i < nbrOfMoves; i++)
        {
            if (!isLinked)
            {
                linkEnd.position = Vector3.Lerp(hand.position, hitPoint, i / nbrOfMoves);
            }
            else
            {
                linkStart.position = Vector3.Lerp(hand.position, hitPoint, i / nbrOfMoves);
            }

            yield return new WaitForEndOfFrame();
        }*/

        Destroy(linkedObject.obiRigidBody);
        yield return new WaitForEndOfFrame();
        linkedObject.obiRigidBody = linkedObject.AddComponent<ObiRigidbody>();
        linkedObject.obiRigidBody.kinematicForParticles = false;
    }

    ///////////////////////////////////////////////////
    ///          FONCTIONS UTILITAIRES              ///
    ///////////////////////////////////////////////////

    

    //private void CollisionManagement()
    //{

    //    RaycastHit hit = Physics.CapsuleCast(transform.position - new Vector3(0,/*Player Heigh*/0, 0), )
    //}


    private void ResetInputState()
    {
        if (buttonSouthIsPressed && !playerControls.Player.A.IsPressed())
        {
            buttonSouthIsPressed = false;
        }

        if (buttonWestIsPressed && !playerControls.Player.X.IsPressed())
        {
            buttonWestIsPressed = false;
        }

        if (buttonEastIsPressed && !playerControls.Player.B.IsPressed())
        {
            buttonEastIsPressed = false;
        }

        if (buttonNorthIsPressed && !playerControls.Player.Y.IsPressed())
        {
            buttonNorthIsPressed = false;
        }

        if (rightTriggerIsPressed && !playerControls.Player.RT.IsPressed())
        {
            rightTriggerIsPressed = false;
        }

        if (leftTriggerIsPressed && !playerControls.Player.LT.IsPressed())
        {
            leftTriggerIsPressed = false;

            if (isAiming)
            {
                Aim(false);
            }
        }
    }

    private void Update()//Gère les temps pour le coyoteTime et JumpBuffering
    {
        /*jumpBufferTimer -= Time.fixedDeltaTime;
        if (playerControls.Player.A.IsPressed() && !buttonSouthIsPressed)
        {
            buttonSouthIsPressed = true;
            jumpBufferTimer = jumpBufferTime;
        }
        //Debug.Log(jumpBufferTimer);

        if (RaycastGrounded())
        {
            coyoteTimer = coyoteTime;
        }
        else
        {
            coyoteTimer -= Time.fixedDeltaTime;
        }
        Debug.Log(RaycastGrounded());*/

    }

    private void FixedUpdate()
    {
        if (playerControls != null)
        {
            ResetInputState();
        }

        if (isActive)
        {
            if (isMainPlayer)
            {
                movementHandler.UpdateComponent();

                if (!isAiming)
                {
                    if (playerControls.Player.Y.IsPressed() && !buttonNorthIsPressed)
                    {
                        buttonNorthIsPressed = true;
                        Equip();
                    }

                    if (playerControls.Player.X.IsPressed() && !buttonWestIsPressed)
                    {
                        buttonWestIsPressed = true;
                        Hold();
                    }



                    if (playerControls.Player.LT.IsPressed() && !leftTriggerIsPressed)
                    {
                        leftTriggerIsPressed = true;
                        Aim(true);
                    }

                    if (playerControls.Player.RT.IsPressed() && !rightTriggerIsPressed)
                    {
                        rightTriggerIsPressed = true;

                        if (equippedObject != null && heldObject != null)
                        {
                            MushroomManager equippedMushroom = (MushroomManager)equippedObject;
                            if (!heldObject.states.Contains(equippedMushroom.stateToApply))
                            {
                                heldObject.SetState(equippedMushroom.stateToApply);
                            }
                        }
                    }
                }
                else
                {
                    /*if (!playerControls.Player.LT.IsPressed() && leftTriggerIsPressed)
                    {
                        leftTriggerIsPressed = false;
                        Aim(false);
                    }*/

                    if (playerControls.Player.X.IsPressed() && !buttonWestIsPressed)
                    {
                        buttonWestIsPressed = true;
                        Throw();
                    }

                    if (playerControls.Player.RT.IsPressed() && !rightTriggerIsPressed)
                    {
                        rightTriggerIsPressed = true;

                        if (equippedObject != null && heldObject == null)
                        {
                            Shot(playerControls.Player.RT);
                        }
                    }

                    if (playerControls.Player.B.IsPressed() && !buttonEastIsPressed)
                    {
                        buttonEastIsPressed = true;
                        Shot(playerControls.Player.B);
                    }
                }
            }
        }
        else
        {
            /*if (transform.name == "Player_08")
            {
                Debug.Log(obiRigidBody.linearVelocity);
                Debug.Log(obiRigidBody.angularVelocity);
            }
            obiRigidBody.UpdateVelocities(-obiRigidBody.linearVelocity, -obiRigidBody.angularVelocity);*/
        }
    }
}
