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
    [Header("Player References")]
    [SerializeField] private Transform head;
    [SerializeField] private Transform hand;
    [SerializeField] private Transform feet;
    [SerializeField] private Transform eye;
    [SerializeField] private Transform linkTarget;
    [SerializeField] public Transform cameraTarget;

    [SerializeField] private PlayerInteractionTrigger Trigger;
    public PlayerInteractionTrigger trigger { get; private set; }

    public PlayerControls playerControls { get; private set; }
    public StateManager heldObject { get; set; }
    public StateManager equippedObject { get; set; }

    [Header("Status")]
    public bool isMainPlayer;
    public bool isActive;
    public bool isAiming = false;
    public bool isLadder = false;
    public bool isLadderTrigger = false;
    public bool isJumping = false;
    public bool isJumpingDown = false;

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

    ///////////////////////////////////////////////////
    ///            FONCTIONS H�RIT�ES               ///
    ///////////////////////////////////////////////////

    public override void Initialize(GameManager instance)
    {
        base.Initialize(instance);

        throwDirection = new Vector2(startThrowForceHorizontal, startThrowForceVertical);

        trigger = Trigger;
        heldObject = null;
        equippedObject = null;
        moveMassMultiplier = 1; //Ne pas toucher.
        linkMoveMultiplier = 1.75f; //Le multiplieur associ� � la fonction Move() si le joueur est link. V�rifier le cas o� le joueur tient un objet qui est link. Fonction Move(), ligne 173. J'ai fait une division mais peut-�tre que �a m�rite une valeur dissoci�e
        linkJumpMultiplier = 5.25f; //Le multiplieur associ� � la fonction Jump() si le joueur est link

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

    protected override Vector3 GetThrowForce(float throwForceHorizontal, float throwForceVertical, Vector3 hitpoint)
    {
        return base.GetThrowForce(throwForceHorizontal, throwForceVertical, hitpoint);
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
            isMainPlayer = true;
        }
        else
        {
            isMainPlayer = false;
            isActive = false;
        }
    }

    ///////////////////////////////////////////////////
    ///           FONCTIONS D'ACTIONS               ///
    ///////////////////////////////////////////////////
    [Header("Move Properties")]
    //[SerializeField] protected float moveSpeed = 20f;
    [SerializeField] protected float maxMoveSpeed;
    [SerializeField] protected float acceleration = 7f;
    [SerializeField] protected float deceleration = 7f;
    [SerializeField] protected float velPower = 0.9f; //inf�rieur � 1

    [SerializeField] protected float jumpForce;
    [Range(0f, 5f)] [SerializeField] protected float jumpCutMultiplier;
    [SerializeField] private float jumpBufferTime; // Temps de buffer pour le saut
    public float jumpBufferTimer;
    [SerializeField] private float coyoteTime; // Temps de coyote time
    public float coyoteTimer;

    [SerializeField] protected Vector3 customGravity;
    [SerializeField] protected float fallGravityMultiplier;

    [SerializeField] protected float collisionDetectionDistance;
    [SerializeField] protected LayerMask GroundLayer;
    [HideInInspector] protected Vector3 direction = Vector3.zero;
    [HideInInspector] protected Vector2 jumpFrameMovementSave;
    [HideInInspector] protected float linkMoveMultiplier;
    [HideInInspector] protected float linkJumpMultiplier;
    public float moveMassMultiplier;

    [Header("Slope Handling")]
    public float maxSlopeAngle;
    private RaycastHit slopeHit;
    private bool exitingSlope;

    public void Move(Vector2 inputValue)
    {
        //On r�cup�re la direction donn� par le joystick
        Vector3 inputDirection = new Vector3(inputValue.x, 0f, inputValue.y);

        if (!RaycastCollision() && inputValue != Vector2.zero)
        {
            //On y multiplie la direction du forward et du right de la cam�ra pour avoir la direction globale du joueur.
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
        }
        else
        {
            //Debug.Log("yo");
        }
        //On calcule le vecteur de d�placement d�sir�.
        Vector3 TargetSpeed = new Vector3(direction.x * maxMoveSpeed, 0f, direction.z * maxMoveSpeed);
        //On prends la diff�rence en le vecteur d�sir� et le vecteur actuel.
        Vector3 SpeedDiff = TargetSpeed - new Vector3(rigidBody.velocity.x, 0f, rigidBody.velocity.z);

        //On calcule check si il faut accelerer ou decelerer.
        float AccelRate;
        if (Mathf.Abs(TargetSpeed.x) > 0.01f || Mathf.Abs(TargetSpeed.z) > 0.01f)

        {
            AccelRate = acceleration;
        }
        else
        {
            AccelRate = deceleration;
        }
        //On applique l'acceleration � la SpeedDiff, La puissance permet d'augmenter l'acceleration si la vitesse est plus �lev�e.
        //Enfin on multiplie par le signe de SpeedDiff pour avoir la bonne direction.
        Vector3 movement = new Vector3(Mathf.Pow(Mathf.Abs(SpeedDiff.x) * AccelRate, velPower) * Mathf.Sign(SpeedDiff.x), 0f, Mathf.Pow(Mathf.Abs(SpeedDiff.z) * AccelRate, velPower) * Mathf.Sign(SpeedDiff.z));

        //On applique la force au GO
        rigidBody.AddForce(movement, ForceMode.Force);


        //Limit la Speed du joueur � la speed Max (Pas necessaire)
        Vector3 horizontalVelocity = rigidBody.velocity;
        horizontalVelocity.y = 0f;
        if (horizontalVelocity.sqrMagnitude > maxMoveSpeed * maxMoveSpeed)
        {
            rigidBody.velocity = horizontalVelocity.normalized * maxMoveSpeed + Vector3.up * rigidBody.velocity.y;
        }

        LookAt(inputValue);

        direction = Vector3.zero;
    }

    public void Jump()
    {

        //Reset de la celocité en Y
        rigidBody.velocity = new Vector3(rigidBody.velocity.x, 0f, rigidBody.velocity.z);

        jumpFrameMovementSave = new Vector2(rigidBody.velocity.x, rigidBody.velocity.z);
        Vector3 jumpForce = new Vector3(rigidBody.velocity.x, this.jumpForce, rigidBody.velocity.z);

        jumpForce = jumpForce * moveMassMultiplier;
        if (isLinked)
        {
            jumpForce = jumpForce * linkMoveMultiplier;
        }
        else if (heldObject != null)
        {
            if (heldObject.link != null)
            {
                jumpForce = jumpForce * linkMoveMultiplier;
            }
        }
        else if (equippedObject != null)
        {
            if (equippedObject.link != null)
            {
                jumpForce = jumpForce * linkMoveMultiplier;
            }
        }

        rigidBody.AddForce(jumpForce, ForceMode.Impulse);
        isJumping = true;
        isJumpingDown = false;
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
        if (heldObject == null)
        {
            if (trigger.triggeredObjectsList.Count > 0 && trigger.current != null)
            {
                heldObject = trigger.current;
                heldObject.SetHoldObject(hand, 0.25f);
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
        float maxThrowForceHorizontal = 15;
        float maxThrowForceVertical = 7;
        throwDirection = new Vector2(startThrowForceHorizontal, startThrowForceVertical);
        isCalculatingThrowForce = true;

        while (isAiming && playerControls.Player.X.IsPressed())
        {
            startThrowForceHorizontal += (0.1f + Time.fixedDeltaTime);
            startThrowForceVertical += (0.1f + Time.fixedDeltaTime);

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

            heldObject.ThrowObject(startThrowForceHorizontal, startThrowForceVertical, hitPoint);
            heldObject = null;
        }
        isCalculatingThrowForce = false;
        throwDirection = new Vector2(this.startThrowForceHorizontal, this.startThrowForceVertical);
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

                            if (lineRenderer.enabled)
                            {
                                lineRenderer.enabled = false;
                            }

                            if (target != null)
                            {
                                target.outline.enabled = false;
                                target = null;
                            }

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
                                }
                                else
                                {
                                    stateManager.SetState(equippedMushroom.stateToApply);
                                }
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
            else
            {
                rigidBody.MoveRotation(Quaternion.RotateTowards(rigidBody.rotation, Quaternion.LookRotation(direction , Vector3.up), 100 * Time.fixedDeltaTime));
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
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
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

    //private void CollisionManagement()
    //{

    //    RaycastHit hit = Physics.CapsuleCast(transform.position - new Vector3(0,/*Player Heigh*/0, 0), )
    //}

    private bool RaycastGrounded()
    {
        //bool isCollisionDetected = Physics.Raycast(feet.position, Vector3.down, collisionDetectionDistance, GroundLayer);

        bool isCollisionDetected = Physics.BoxCast(feet.position, feet.transform.lossyScale / 2, Vector3.down, feet.transform.rotation, collisionDetectionDistance, GroundLayer);
        //bool isCollisionDetected = false;

        if (isCollisionDetected)
        {
            RaycastHit hit;
            if (Physics.Raycast(feet.position, Vector3.down, out hit, collisionDetectionDistance, GroundLayer))
            {
                float dotProduct = Vector3.Dot(hit.normal, Vector3.up);
                if (dotProduct >= 0.75 && dotProduct <= 1.25f)
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
        float mass = 1;

        List<GameObject> stickedList = GetStickedObjects(GetFirstStickedObject(heldObject.gameObject));

        foreach (GameObject stickedObject in stickedList)
        {
            if (stickedObject.TryGetComponent<StateManager>(out StateManager stateManager))
            {
                mass += 0.1f;
            }
        }

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

    public void FallGravity()//Ajoute une gravit� fictive/ Lorsque le personnage retombe, donne un feeling avec plus de r�pondant.
    {
        //On applique la gravit� custom
        if (rigidBody.velocity.y < 0f)
        {
            rigidBody.AddForce(customGravity * fallGravityMultiplier, ForceMode.Acceleration);
        }
        else
        {
            rigidBody.AddForce(customGravity, ForceMode.Acceleration);
        }
    }

    private void Update()//Gère les temps pour le coyoteTime et JumpBuffering
    {
        jumpBufferTimer -= Time.fixedDeltaTime;
        if (playerControls == null)
        {
            Debug.Log(transform.name);
        }
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
        //Debug.Log(RaycastGrounded());

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
                    if (!RaycastGrounded() && rigidBody.velocity.y < 0 && !isJumpingDown)
                    {
                        isJumpingDown = true;
                        Debug.Log("MonGrosBool");
                    }
                    else if (RaycastGrounded() && isJumpingDown)
                    {
                        isJumping = false;
                        isJumpingDown = false;
                        Debug.Log("OuaisOuaisOuais");
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
            }
        }
        else
        {
            
        }
    }
}
