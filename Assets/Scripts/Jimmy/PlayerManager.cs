using Cinemachine;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

public class PlayerManager : StateManager
{
    [Header("Player References")]
    [SerializeField] private PhysicMaterial physicMaterial;
    [SerializeField] private Transform head;
    [SerializeField] private Transform hand;
    [SerializeField] private Transform feet;
    [SerializeField] private Transform eye;
    [SerializeField] private Transform linkTarget;
    [SerializeField] public Transform cameraTarget;
    [SerializeField] public Transform animationRoot;

    [SerializeField] private Animator Animator;
    [SerializeField] private Animator stickyAnimator;
    [SerializeField] private PlayerInteractionTrigger Trigger;
    public Animator animator { get; private set; }
    public PlayerInteractionTrigger trigger { get; private set; }

    public PlayerControls playerControls { get; private set; }
    public StateManager heldObject;
    public StateManager equippedObject;

    [Header("Status")]
    public bool isMainPlayer;
    public bool isActive;
    public bool isAiming = false;
    public bool isLadder = false;
    public bool isLadderTrigger = false;
    public bool isLookingEntity = false;
    public bool isJumping = false;
    public bool isJumpingDown = false;
    private float idleTime = 0f;
    private Coroutine mainPlayerRoutine = null;
    private bool canToggleEquip = true;

    [HideInInspector] public bool buttonSouthIsPressed = false;
    [HideInInspector] public bool buttonWestIsPressed = false;
    [HideInInspector] public bool buttonEastIsPressed = false;
    [HideInInspector] public bool buttonNorthIsPressed = false;
    [HideInInspector] public bool leftTriggerIsPressed = false;
    [HideInInspector] public bool rightTriggerIsPressed = false;

    [Header("Throw Visualizer")]
    private Vector2 throwDirection;
    private bool isCalculatingThrowForce = false;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField][Range(10, 100)] private int linePoints = 25;
    [SerializeField][Range (0.01f, 0.25f)] private float timeBetweenPoints = 0.1f;

    private StateManager target = null;

    [Header("Move Properties")]
    //[SerializeField] protected float moveSpeed = 20f;
    [SerializeField] protected float maxMoveSpeed;
    [SerializeField] protected float acceleration = 7f;
    [SerializeField] protected float deceleration = 7f;
    [SerializeField] protected float velPower = 0.9f; //inf�rieur � 1

    [SerializeField] protected float jumpForce;
    [Range(0f, 5f)][SerializeField] protected float jumpCutMultiplier;
    [SerializeField] private float jumpBufferTime; // Temps de buffer pour le saut
    public float jumpBufferTimer;
    [SerializeField] private float coyoteTime; // Temps de coyote time
    public float coyoteTimer;

    [SerializeField] protected Vector3 customGravity;
    [SerializeField] protected float fallGravityMultiplier;

    [SerializeField] protected float collisionDetectionDistance;
    [SerializeField] protected LayerMask RaycastLayer;
    [HideInInspector] protected Vector3 direction = Vector3.zero;
    [HideInInspector] protected Vector2 jumpFrameMovementSave;
    [HideInInspector] protected float linkMoveMultiplier;
    [HideInInspector] protected float linkJumpMultiplier;
    public float moveMassMultiplier;
    public float walkRatio;
    public float walkRatioMultiplier;
    private Coroutine walkRoutine = null;

    [Header("Slope Handling")]
    public float maxSlopeAngle;
    private RaycastHit slopeHit;
    private bool exitingSlope;

    ///////////////////////////////////////////////////
    ///            FONCTIONS H�RIT�ES               ///
    ///////////////////////////////////////////////////

    public override void Initialize(GameManager instance)
    {
        base.Initialize(instance);

        throwDirection = new Vector2(startThrowForceHorizontal, startThrowForceVertical);

        animator = Animator;
        trigger = Trigger;
        moveMassMultiplier = 1;
        linkMoveMultiplier = 1.75f;
        linkJumpMultiplier = 5.25f;

        if (gameManager != null)
        {
            playerControls = gameManager.playerControls;
        }

        animator.SetBool("isActive", false);
        stickyAnimator.SetBool("isActive", false);
    }

    public override void SetState(State state)
    {
        base.SetState(state);
    }

    public override void ResetState()
    {
        base.ResetState();
    }

    public override void SetHoldObject(PlayerManager player, Transform endPosition, float time)
    {
        base.SetHoldObject(player, endPosition, time);
    }

    public override void InitializeHoldObject(Transform parent)
    {
        base.InitializeHoldObject(parent);
    }

    public override void DropObject()
    {
        base.DropObject();
        rigidBody.constraints = RigidbodyConstraints.FreezeRotation;
    }

    public override void ThrowObject(float throwForceHorizontal, float throwForceVertical, Vector3 hitpoint)
    {
        base.ThrowObject(throwForceHorizontal, throwForceVertical, hitpoint);
    }

    protected override Vector3 GetThrowForce(float throwForceHorizontal, float throwForceVertical, Vector3 hitpoint)
    {
        return base.GetThrowForce(throwForceHorizontal, throwForceVertical, hitpoint);
    }

    public override void SetEquipObject(PlayerManager player, Transform endPosition, float time)
    {
        base.SetEquipObject(player, endPosition, time);
    }

    public override void InitializeEquipObject(Transform parent)
    {
        base.InitializeEquipObject(parent);
    }

    protected override void OnCollisionEnter(Collision collision)
    {
        base.OnCollisionEnter(collision);
    }

    ///////////////////////////////////////////////////
    ///          FONCTIONS DE GESTIONS              ///
    ///////////////////////////////////////////////////

    public void SetIsMainPlayer(bool value)
    {
        if (value)
        {
            if (mainPlayerRoutine != null)
            {
                StopCoroutine(mainPlayerRoutine);
            }

            mainPlayerRoutine = StartCoroutine(IsMainPlayer());
            idleTime = 0;
            animator.SetBool("isActive", true);
            stickyAnimator.SetBool("isActive", true);
        }
        else
        {
            if (mainPlayerRoutine != null)
            {
                StopCoroutine(mainPlayerRoutine);
            }

            isMainPlayer = false;
            isActive = false;
            animator.SetBool("isActive", false);
            stickyAnimator.SetBool("isActive", false);
        }
    }

    private IEnumerator IsMainPlayer()
    {
        yield return new WaitForSecondsRealtime(2f);
        isMainPlayer = true;
    }

    ///////////////////////////////////////////////////
    ///           FONCTIONS D'ACTIONS               ///
    ///////////////////////////////////////////////////

    public void Move(Vector2 inputValue)
    {
        Vector3 inputDirection = new Vector3(inputValue.x, 0f, inputValue.y);

        if (!RaycastCollision() && inputValue != Vector2.zero)
        {
            direction += inputDirection.x * Utilities.GetCameraRight(gameManager.transform);
            direction += inputDirection.z * Utilities.GetCameraForward(gameManager.transform);

            direction *= moveMassMultiplier;

            if (link != null)
            {
                direction = direction * linkMoveMultiplier;
            }
            else if (heldObject != null)
            {
                if (heldObject.link != null)
                {
                    direction = direction * (linkMoveMultiplier / 2);
                }
            }

            rigidBody.AddForce(direction, ForceMode.Impulse);
            idleTime = 0;
        }

        Vector3 TargetSpeed = new Vector3(direction.x * maxMoveSpeed, 0f, direction.z * maxMoveSpeed);
        Vector3 SpeedDiff = TargetSpeed - new Vector3(rigidBody.velocity.x, 0f, rigidBody.velocity.z);

        float AccelRate;
        if (Mathf.Abs(TargetSpeed.x) > 0.01f || Mathf.Abs(TargetSpeed.z) > 0.01f)

        {
            AccelRate = acceleration;
        }
        else
        {
            AccelRate = deceleration;
        }
        Vector3 movement = new Vector3(Mathf.Pow(Mathf.Abs(SpeedDiff.x) * AccelRate, velPower) * Mathf.Sign(SpeedDiff.x), 0f, Mathf.Pow(Mathf.Abs(SpeedDiff.z) * AccelRate, velPower) * Mathf.Sign(SpeedDiff.z));

        rigidBody.AddForce(movement, ForceMode.Force);

        Vector3 horizontalVelocity = rigidBody.velocity;
        horizontalVelocity.y = 0f;

        if (horizontalVelocity.sqrMagnitude > maxMoveSpeed * maxMoveSpeed)
        {
            rigidBody.velocity = horizontalVelocity.normalized * maxMoveSpeed + Vector3.up * rigidBody.velocity.y;
        }

        LookAt(inputValue);

        Vector3 dir = rigidBody.velocity;
        dir.y = 0f;

        if (dir.magnitude >= 1f && dir.magnitude < 4f)
        {
            if (!isJumping && !isJumpingDown && objectCollider.material == null && isActive)
            {
                objectCollider.material = physicMaterial;
            }

            if (isActive && walkRoutine == null && RaycastFalling())
            {
                AudioManager.instance.PlayVariation("Sfx_Player_Steps", 0.25f, 0.18f);
                walkRoutine = StartCoroutine(Walk(walkRatio / dir.magnitude + (walkRatioMultiplier * dir.magnitude)));
            }

            animator.SetBool("isWalking", true);
            animator.SetBool("isRunning", false);
            stickyAnimator.SetBool("isWalking", true);
            stickyAnimator.SetBool("isRunning", false);
        }
        else if (dir.magnitude >= 4f)
        {
            if (!isJumping && !isJumpingDown && objectCollider.material == null && isActive)
            {
                objectCollider.material = physicMaterial;
            }

            if (isActive && walkRoutine == null && RaycastFalling())
            {
                AudioManager.instance.PlayVariation("Sfx_Player_Steps", 0.25f, 0.18f);
                walkRoutine = StartCoroutine(Walk(walkRatio / dir.magnitude + (walkRatioMultiplier * dir.magnitude)));
            }

            animator.SetBool("isRunning", true);
            animator.SetBool("isWalking", false);
            stickyAnimator.SetBool("isRunning", true);
            stickyAnimator.SetBool("isWalking", false);
        }
        else
        {
            if (walkRoutine != null)
            {
                StopCoroutine(walkRoutine);
                walkRoutine = null;
            }

            if (!isJumping && !isJumpingDown && objectCollider.material != physicMaterial)
            {
                objectCollider.material = null;
            }

            animator.SetBool("isWalking", false);
            animator.SetBool("isRunning", false);
            stickyAnimator.SetBool("isWalking", false);
            stickyAnimator.SetBool("isRunning", false);
        }

        direction = Vector3.zero;
    }

    public void Jump()
    {

        //Reset de la celocité en Y
        rigidBody.velocity = new Vector3(rigidBody.velocity.x, 0f, rigidBody.velocity.z);

        jumpFrameMovementSave = new Vector2(rigidBody.velocity.x, rigidBody.velocity.z);
        Vector3 jumpForce = new Vector3(rigidBody.velocity.x, this.jumpForce, rigidBody.velocity.z);

        if (equippedObject != null)
        {
            equippedObject.objectCollider.isTrigger = true;
        }

        objectCollider.material = physicMaterial;
        rigidBody.AddForce(jumpForce, ForceMode.Impulse);
        isJumping = true;
        isJumpingDown = false;
        animator.SetBool("isJumpingUp", true);
        stickyAnimator.SetBool("isJumpingUp", true);
        idleTime = 0;
        AudioManager.instance.PlayVariation("Sfx_Player_Jump", 0.15f, 0.1f);
    }

    public void OnJumpUp()
    {
        //JumpCut
        if (rigidBody.velocity.y > 0)
        {
            rigidBody.AddForce(Vector3.down * rigidBody.velocity.y * (1 - jumpCutMultiplier), ForceMode.Impulse);
        }
    }

    public void Hold()
    {
        if (heldObject == null && canToggleEquip)
        {
            if (trigger.triggeredObjectsList.Count > 0 && trigger.current != null)
            {
                heldObject = trigger.current;
                heldObject.SetHoldObject(this, hand, 0.75f);
                animator.SetTrigger("Grab");
                stickyAnimator.SetTrigger("Grab");
                idleTime = 0;
                AudioManager.instance.Play("Sfx_Player_Carry");
                StartCoroutine(ToggleEquip());
            }
        }
        else if (heldObject != null && canToggleEquip)
        {
            heldObject.DropObject();
            animator.SetTrigger("Drop");
            stickyAnimator.SetTrigger("Drop");
            heldObject = null;
            idleTime = 0;
            AudioManager.instance.Play("Sfx_Player_Drop");
            StartCoroutine(ToggleEquip());
        }
    }

    public void Equip()
    {
        if (equippedObject == null && heldObject != null && canToggleEquip)
        {
            equippedObject = heldObject;
            equippedObject.SetEquipObject(this, head, 0.75f);
            animator.SetTrigger("PutChapeau");
            stickyAnimator.SetTrigger("PutChapeau");
            heldObject = null;
            idleTime = 0;

            if (equippedObject.TryGetComponent<MushroomManager>(out MushroomManager mushroomManager))
            {
                AudioManager.instance.Play("Sfx_Player_GetPower");
            }
            else
            {
                AudioManager.instance.Play("Sfx_Player_OnHead");
            }
            StartCoroutine(ToggleEquip());
        }
        else if (equippedObject != null && heldObject == null && canToggleEquip)
        {
            heldObject = equippedObject;
            equippedObject.SetHoldObject(this, hand, 0.75f);
            animator.SetTrigger("RemoveChapeau");
            stickyAnimator.SetTrigger("RemoveChapeau");
            equippedObject = null;
            idleTime = 0;
            AudioManager.instance.Play("Sfx_Player_OffHead");
            StartCoroutine(ToggleEquip());
        }
    }

    public void Throw()
    {
        if (heldObject != null)
        {
            if (heldObject.isHeld)
            {
                StartCoroutine(CalculateThrowForce());
                idleTime = 0;
                AudioManager.instance.Play("Sfx_Player_Throw");
            }
        }
    }

    private IEnumerator CalculateThrowForce()
    {
        float startThrowForceHorizontal = heldObject.startThrowForceHorizontal;
        float startThrowForceVertical = heldObject.startThrowForceVertical;
        float maxThrowForceHorizontal = 100;
        float maxThrowForceVertical = 70;
        throwDirection = new Vector2(startThrowForceHorizontal, startThrowForceVertical);
        isCalculatingThrowForce = true;

        while (isAiming && playerControls.Player.X.IsPressed())
        {
            startThrowForceHorizontal += (1f + Time.fixedDeltaTime);
            startThrowForceVertical += (1f + Time.fixedDeltaTime);

            if (startThrowForceHorizontal > maxThrowForceHorizontal)
            {
                startThrowForceHorizontal = maxThrowForceHorizontal;
            }

            if (startThrowForceVertical > maxThrowForceVertical)
            {
                startThrowForceVertical = maxThrowForceVertical;
            }

            throwDirection = new Vector2(startThrowForceHorizontal, startThrowForceVertical);

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

            animator.SetTrigger("Throw");
            stickyAnimator.SetTrigger("Throw");
            heldObject.ThrowObject(startThrowForceHorizontal, startThrowForceVertical, hitPoint);
            heldObject = null;
        }
        isCalculatingThrowForce = false;
        throwDirection = new Vector2(this.startThrowForceHorizontal, this.startThrowForceVertical);
    }

    public void Aim(bool value)
    {
        isAiming = value;

        Vector3 cameraTargetPos = new Vector3(0, 3.5f, 0);
        if (isAiming)
        {
            gameObject.layer = 2;
            Vector3 dir = Camera.main.transform.forward;
            dir.y = 0f;
            rigidBody.rotation = Quaternion.LookRotation(dir, Vector3.up);
            gameManager.cameraManager.aimCamera.m_XAxis.Value = 0f;
            gameManager.cameraManager.aimCamera.m_YAxis.Value = 0.70f;
            cameraTargetPos = new Vector3(0, 6.25f, 0);
        }
        else
        {
            gameObject.layer = 3;
            gameManager.cameraManager.worldCamera.m_XAxis.Value = 0f;
            gameManager.cameraManager.worldCamera.m_YAxis.Value = 0.70f;
        }

        gameManager.cameraManager.SetCameraAim(value, cameraTargetPos);
    }

    public void Shot(InputAction action)
    {
        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
        Vector3 hitPoint = Vector3.zero;
        Ray ray = Camera.main.ScreenPointToRay(screenCenter);

        Vector3 rayDirection = ray.direction;

        if (Physics.Raycast(ray.origin, rayDirection, out RaycastHit hit, float.MaxValue, RaycastLayer))
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

                            if (lineRenderer.enabled)
                            {
                                lineRenderer.enabled = false;
                            }

                            if (target != null)
                            {
                                target.outline.enabled = false;
                                target = null;
                            }

                            trigger.Clear();
                            idleTime = 0;
                            AudioManager.instance.Play("Sfx_Player_Swap");
                            gameManager.SetMainPlayer(playerManager, true, true);
                            return;
                        }
                    }
                }
            }
            

            hitPoint = hit.point;
        }

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

        if (Physics.Raycast(ray, out hit, float.MaxValue, RaycastLayer))
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
                                if (stateManager.stickedWall != null)
                                {
                                    if (!stateManager.stickedWall.isWallDestroyed)
                                    {
                                        stateManager.stickedWall.manager.DestroyWall();
                                    }
                                    else
                                    {
                                        stateManager.SetState(equippedMushroom.stateToApply);
                                    }

                                    idleTime = 0;
                                }
                                else
                                {
                                    stateManager.SetState(equippedMushroom.stateToApply);
                                    idleTime = 0;
                                }

                                AudioManager.instance.Play("Sfx_Player_Sticky");
                            }
                        }
                    }
                }
            }
        }
    }

    ///////////////////////////////////////////////////
    ///          FONCTIONS UTILITAIRES              ///
    ///////////////////////////////////////////////////

    private IEnumerator ToggleEquip()
    {
        canToggleEquip = false;
        yield return new WaitForSecondsRealtime(0.5f);
        canToggleEquip = true;
    }

    private IEnumerator Walk(float value)
    {
        yield return new WaitForSecondsRealtime(value);

        Vector3 dir = rigidBody.velocity;
        dir.y = 0f;

        if (isActive && dir.magnitude > 0.25f && RaycastFalling())
        {
            AudioManager.instance.PlayVariation("Sfx_Player_Steps", 0.25f, 0.18f);

            walkRoutine = StartCoroutine(Walk(walkRatio / dir.magnitude + (walkRatioMultiplier * dir.magnitude)));
        }
        else
        {
            walkRoutine = null;
        }
    }

    public void LookAt(Vector2 value)
    {
        Vector3 direction = rigidBody.velocity;
        direction.y = 0f;
        Vector3 dirToLook = Vector3.zero;
        dirToLook = value.x * Utilities.GetCameraRight(gameManager.transform);
        dirToLook = value.y * Utilities.GetCameraForward(gameManager.transform);

        if (value.sqrMagnitude > 0.1f && direction.sqrMagnitude > 0.1f)
        {
            if (!isAiming)
            {
                rigidBody.MoveRotation(Quaternion.RotateTowards(rigidBody.rotation, Quaternion.LookRotation(direction, Vector3.up), 800 * Time.fixedDeltaTime));
            }
        }
        else
        {
            if (!rigidBody.isKinematic)
            {
                rigidBody.angularVelocity = Vector3.zero;
            }
        }
    }

    private void OutlineRaycast()
    {
        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
        Vector3 hitPoint = Vector3.zero;
        Ray ray = Camera.main.ScreenPointToRay(screenCenter);

        Vector3 rayDirection = ray.direction;

        if (Physics.Raycast(ray.origin, rayDirection, out RaycastHit hit, float.MaxValue, RaycastLayer))
        {
            if (hit.collider != null)
            {
                Debug.Log(hit.collider.name);
            }

            if (hit.collider.tag == "Player" && hit.collider.transform != transform)
            {
                if (hit.collider.TryGetComponent<PlayerManager>(out PlayerManager playerManager))
                {
                    if (target != null)
                    {
                        if (playerManager != target)
                        {
                            target.outline.enabled = false;
                        }
                    }
                    target = playerManager;
                    target.outline.enabled = true;
                    return;
                }
            }

            hitPoint = hit.point;
        }

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

        if (Physics.Raycast(ray, out hit, float.MaxValue, RaycastLayer))
        {
            if (equippedObject != null)
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
                                if (target != null)
                                {
                                    if (target != stateManager)
                                    {
                                        target.outline.enabled = false;
                                    }
                                }
                                target = stateManager;
                                target.outline.enabled = true;
                                return;
                            }
                        }
                    }
                }
            }
        }

        if (target != null)
        {
            target.outline.enabled = false;
            target = null;
        }
    }

    private bool RaycastCollision()
    {
        bool isCollisionDetected = false;

        RaycastHit hit;
        if (Physics.Raycast(eye.position, eye.forward, out hit, collisionDetectionDistance))
        {
            if ((hit.collider.tag == "Wall" || hit.collider.tag == "Ground") && rigidBody.velocity.sqrMagnitude > maxMoveSpeed)
            {
                isCollisionDetected = true;
            }
        }

        return isCollisionDetected;
    }

    public bool RaycastGrounded()
    {
        bool isCollisionDetected = Physics.BoxCast(feet.position, feet.transform.lossyScale / 2, Vector3.down, feet.transform.rotation, collisionDetectionDistance, RaycastLayer);

        if (isCollisionDetected)
        {
            RaycastHit hit;
            if (Physics.Raycast(feet.position, Vector3.down, out hit, collisionDetectionDistance, RaycastLayer))
            {
                float dotProduct = Vector3.Dot(hit.normal, Vector3.up);
                float minDot = 0f;
                float maxDot = 0f;

                if (renderer.transform.position.y > 28f)
                {
                    minDot = 0.6f;
                    maxDot = 1.4f;
                }
                else
                {
                    minDot = 0.75f;
                    maxDot = 1.25f;
                }

                if (dotProduct >= minDot && dotProduct <= maxDot)
                {
                    isCollisionDetected = true;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        else
        {
            return isCollisionDetected;
        }
    }

    private bool RaycastFalling()
    {
        bool isCollisionDetected = Physics.BoxCast(feet.position, feet.transform.lossyScale / 2, Vector3.down, feet.transform.rotation, collisionDetectionDistance * 2, RaycastLayer);

        return isCollisionDetected;
    }

    private void OnDrawGizmos()//Permet de visualiser le boxCast pour la détection du ground
    {
        RaycastHit hit;

        bool isHit = Physics.BoxCast(feet.position, feet.transform.lossyScale / 2, Vector3.down, out hit, feet.transform.rotation, collisionDetectionDistance);

        if (isHit)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(feet.transform.position + Vector3.down * hit.distance, feet.lossyScale);
        }
    }

    private void DrawThrowTrajectory()
    {
        if (!lineRenderer.enabled)
        {
            lineRenderer.enabled = true;
        }

        lineRenderer.positionCount = Mathf.CeilToInt(linePoints / timeBetweenPoints) + 1;
        Vector3 startPosition = hand.position;
        Vector3 startVelocity;
        float mass = 10;

        /*List<GameObject> stickedList = GetStickedObjects(GetFirstStickedObject(heldObject.gameObject));

        foreach (GameObject stickedObject in stickedList)
        {
            if (stickedObject.TryGetComponent<StateManager>(out StateManager stateManager))
            {
                mass += 0.1f;
            }
        }*/

        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
        Vector3 hitPoint = Vector3.zero;
        Ray ray = Camera.main.ScreenPointToRay(screenCenter);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            hitPoint = hit.point;
        }

        startVelocity = GetThrowForce(throwDirection.x, throwDirection.y, hitPoint) / mass;

        int i = 0;
        lineRenderer.SetPosition(i, startPosition);

        for (float time = 0; time < linePoints; time += timeBetweenPoints)
        {
            i++;
            Vector3 pos = startPosition + time * startVelocity;
            pos.y = startPosition.y + startVelocity.y * time + (Physics.gravity.y / 2f * time * time);
            lineRenderer.SetPosition(i, pos);
        }
    }

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

    private void FallGravity()
    {
        if (rigidBody.velocity.y < 0f)
        {
            rigidBody.AddForce(customGravity * fallGravityMultiplier, ForceMode.Acceleration);
        }
        else
        {
            rigidBody.AddForce(customGravity, ForceMode.Acceleration);
        }
    }

    private void HandleJoints()
    {
        if (heldObject != null)
        {
            if (!heldObject.rigidBody.isKinematic)
            {
                heldObject.rigidBody.constraints = RigidbodyConstraints.None;
                heldObject.rigidBody.isKinematic = true;
            }
        }

        if (equippedObject != null)
        {
            if (!equippedObject.rigidBody.isKinematic)
            {
                equippedObject.rigidBody.constraints = RigidbodyConstraints.None;
                equippedObject.rigidBody.isKinematic = true;
            }
        }
    }

    private void Update()
    {
        jumpBufferTimer -= Time.fixedDeltaTime;
        if (playerControls != null)
        {
            if (playerControls.Player.A.IsPressed() && !buttonSouthIsPressed)
            {
                buttonSouthIsPressed = true;
                jumpBufferTimer = jumpBufferTime;
            }

            if (RaycastGrounded())
            {
                coyoteTimer = coyoteTime;
            }
            else
            {
                coyoteTimer -= Time.fixedDeltaTime;
            }
        }
    }

    private void FixedUpdate()
    {
        if (playerControls != null)
        {
            ResetInputState();
            HandleJoints();
        }

        if (isActive)
        {
            if (isMainPlayer)
            {
                if (playerControls.Player.enabled && !playerControls.UI.enabled)
                {
                    gameManager.UIManager.SetUIInput(this);
                }
                else if (!playerControls.Player.enabled && playerControls.UI.enabled)
                {
                    gameManager.UIManager.SetUIInput(null);
                }
                
                Move(playerControls.Player.LeftStick.ReadValue<Vector2>());
                FallGravity();

                if ((RaycastGrounded() && jumpBufferTimer > 0))
                {
                    jumpBufferTimer = -5;
                    coyoteTimer = -5;
                    Jump();
                }
                else if(playerControls.Player.A.IsPressed() && coyoteTimer > 0 && !RaycastGrounded())
                {
                    buttonSouthIsPressed = true;
                    coyoteTimer = -5;
                    jumpBufferTimer = -5;
                    Jump();
                }

                if (!playerControls.Player.A.IsPressed() && rigidBody.velocity.y >= 0)
                {
                    OnJumpUp();
                }

                if (isJumping)
                {
                    if ((!RaycastFalling() || !RaycastGrounded()) && rigidBody.velocity.y < 0f && !isJumpingDown)
                    {
                        isJumpingDown = true;
                        animator.SetBool("isJumpingUp", false);
                        animator.SetBool("isJumpingDown", true);
                        stickyAnimator.SetBool("isJumpingUp", false);
                        stickyAnimator.SetBool("isJumpingDown", true);

                        if (equippedObject != null)
                        {
                            equippedObject.objectCollider.isTrigger = false;
                        }
                    }
                    else if ((RaycastFalling() || RaycastGrounded()) && isJumpingDown)
                    {
                        isJumping = false;
                        isJumpingDown = false;
                        animator.SetBool("isJumpingUp", false);
                        animator.SetBool("isJumpingDown", false);
                        stickyAnimator.SetBool("isJumpingUp", false);
                        stickyAnimator.SetBool("isJumpingDown", false);
                        AudioManager.instance.Play("Sfx_Player_Fall");
                    }
                }
                else
                {
                    if ((!RaycastFalling() || !RaycastGrounded()) && rigidBody.velocity.y < 0f && !isJumpingDown)
                    {
                        isJumpingDown = true;
                        objectCollider.material = physicMaterial;
                        animator.SetBool("isJumpingUp", false);
                        animator.SetBool("isJumpingDown", true);
                        stickyAnimator.SetBool("isJumpingUp", false);
                        stickyAnimator.SetBool("isJumpingDown", true);

                        if (equippedObject != null)
                        {
                            equippedObject.objectCollider.isTrigger = false;
                        }
                    }
                    else if ((RaycastFalling() || RaycastGrounded()) && isJumpingDown)
                    {
                        isJumpingDown = false;
                        animator.SetBool("isJumpingUp", false);
                        animator.SetBool("isJumpingDown", false);
                        stickyAnimator.SetBool("isJumpingUp", false);
                        stickyAnimator.SetBool("isJumpingDown", false);
                        AudioManager.instance.Play("Sfx_Player_Fall");
                    }
                }

                trigger.UpdateOutline();

                if (!isAiming)
                {
                    if (target != null)
                    {
                        target.outline.enabled = false;
                        target = null;
                    }

                    if (lineRenderer.enabled)
                    {
                        lineRenderer.enabled = false;
                    }

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

                    if (playerControls.Player.LT.IsPressed() && !leftTriggerIsPressed && !isLookingEntity)
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
                                AudioManager.instance.Play("Sfx_Player_Sticky");
                            }
                        }
                    }
                }
                else
                {
                    OutlineRaycast();

                    if (heldObject != null)
                    {
                        DrawThrowTrajectory();
                    }
                    else if (heldObject == null && lineRenderer.enabled)
                    {
                        lineRenderer.enabled = false;
                    }

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

                idleTime += Time.fixedDeltaTime;
                if (idleTime > 10f)
                {
                    animator.SetBool("isShaking", true);
                    stickyAnimator.SetBool("isShaking", true);
                    idleTime = -3f;

                    float random = UnityEngine.Random.Range(0f, 100f);

                    if (random < 50)
                    {
                        AudioManager.instance.Play("Sfx_Player_Idle");
                    }
                    else
                    {
                        AudioManager.instance.Play("Sfx_Player_IdleAlt");
                    }
                }
                else if (idleTime < 10f)
                {
                    animator.SetBool("isShaking", false);
                    stickyAnimator.SetBool("isShaking", false);
                }
            }
        }
    }
}
