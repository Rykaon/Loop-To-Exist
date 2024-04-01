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
    ///            FONCTIONS H�RIT�ES               ///
    ///////////////////////////////////////////////////

    public override void Initialize(GameManager instance)
    {
        base.Initialize(instance);

        trigger = Trigger;
        heldObject = null;
        equippedObject = null;
        moveMassMultiplier = 1; //Ne pas toucher.
        linkMoveMultiplier = 1.75f; //Le multiplieur associ� � la fonction Move() si le joueur est link. V�rifier le cas o� le joueur tient un objet qui est link. Fonction Move(), ligne 173. J'ai fait une division mais peut-�tre que �a m�rite une valeur dissoci�e
        linkJumpMultiplier = 5.25f; //Le multiplieur associ� � la fonction Jump() si le joueur est link

        customGravity = new Vector3(0f, -9.81f, 0f);

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
    [Header("Move Properties")]
    [SerializeField] protected float moveSpeed = 20f;
    [SerializeField] protected float maxMoveSpeed;
    [SerializeField] protected float acceleration = 7f;
    [SerializeField] protected float deceleration = 7f;
    [SerializeField] protected float velPower = 0.9f; //inf�rieur � 1

    [SerializeField] protected float jumpForce;
    [Range(0f, 1f)] [SerializeField] protected float jumpCutMultiplier;

    [SerializeField] protected Vector3 customGravity;
    [SerializeField] protected float fallGravityMultiplier;


    [SerializeField] protected float collisionDetectionDistance;
    [SerializeField] protected LayerMask GroundLayer;
    [HideInInspector] protected Vector3 direction = Vector3.zero;
    [HideInInspector] protected Vector2 jumpFrameMovementSave;
    [HideInInspector] protected float linkMoveMultiplier;
    [HideInInspector] protected float linkJumpMultiplier;
    public float moveMassMultiplier;
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
        Vector3 TargetSpeed = new Vector3(direction.x * moveSpeed, 0f, direction.z * moveSpeed);
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
        if (RaycastGrounded())
        {
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
        }
    }

    public void OnJumpUp()
    {
        //JumpCut
        rigidBody.AddForce(Vector3.down * rigidBody.velocity.y * (1 - jumpCutMultiplier), ForceMode.Impulse);
    }

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

    public void LookAt(Vector2 value)
    {
        Vector3 direction = rigidBody.velocity;
        direction.y = 0f;

        if (value.sqrMagnitude > 0.1f && direction.sqrMagnitude > 0.1f)
        {
            if (!isAiming)
            {
                rigidBody.MoveRotation(Quaternion.RotateTowards(rigidBody.rotation, Quaternion.LookRotation(direction, Vector3.up), 800 * Time.fixedDeltaTime));
            }
            else
            {
                rigidBody.MoveRotation(Quaternion.RotateTowards(rigidBody.rotation, Quaternion.LookRotation(direction, Vector3.up), 300 * Time.fixedDeltaTime));
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

    private bool RaycastGrounded()
    {
        //bool isCollisionDetected = Physics.Raycast(feet.position, Vector3.down, collisionDetectionDistance, GroundLayer);

        bool isCollisionDetected = Physics.BoxCast(feet.position, feet.transform.lossyScale / 2, Vector3.down, feet.transform.rotation, collisionDetectionDistance, GroundLayer);
        //bool isCollisionDetected = false;

        //RaycastHit hit;
        //if (Physics.Raycast(feet.position, -Vector3.up, out hit, collisionDetectionDistance))
        //{
        //    float dotProduct = Vector3.Dot(hit.normal, Vector3.up);

        //    if (dotProduct >= 0.95f && dotProduct <= 1.05f)
        //    {
        //        isCollisionDetected = true;
        //    }
        //}

        return isCollisionDetected;
    }

    private void OnDrawGizmos()//Permet de visualiser le boxCast pour la détection du ground
    {
        RaycastHit hit;

        bool isHit = Physics.BoxCast(feet.position, feet.transform.lossyScale / 2, Vector3.down,out hit, feet.transform.rotation, collisionDetectionDistance);

        if (isHit)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(feet.transform.position + Vector3.down * hit.distance, feet.lossyScale);
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
                Move(playerControls.Player.LeftStick.ReadValue<Vector2>());
                FallGravity();

                if (playerControls.Player.A.IsPressed() && !buttonSouthIsPressed)
                {
                    buttonSouthIsPressed = true;
                    Jump();
                }
                if (!playerControls.Player.A.IsPressed() && !buttonSouthIsPressed && rigidBody.velocity.y > 0)
                {
                    //Debug.Log("JumpCut!");
                    OnJumpUp();
                }



                if (playerControls.Player.RightStick.ReadValue<Vector2>() != Vector2.zero)
                {
                    //MoveCamera(playerControls.Player.RightStick.ReadValue<Vector2>());
                }

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
