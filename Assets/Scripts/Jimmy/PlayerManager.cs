using Cinemachine;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
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
    [SerializeField] public Transform cameraTarget;

    public Transform playerCamera { get; private set; }

    [SerializeField] private PlayerInteractionTrigger Trigger;
    public PlayerInteractionTrigger trigger { get; private set; }

    public PlayerControls playerControls { get; private set; }
    public StateManager heldObject { get; private set; }
    public StateManager equippedObject { get; private set; }

    [Header("Status")]
    public bool isMainPlayer;
    public bool isActive;
    private bool isAiming = false;

    [HideInInspector] public bool buttonSouthIsPressed = false;
    [HideInInspector] public bool buttonWestIsPressed = false;
    [HideInInspector] public bool buttonEastIsPressed = false;
    [HideInInspector] public bool buttonNorthIsPressed = false;
    [HideInInspector] public bool leftTriggerIsPressed = false;
    [HideInInspector] public bool rightTriggerIsPressed = false;

    [Header("Move Properties")]
    [SerializeField] protected float moveSpeed;
    [SerializeField] protected float maxMoveSpeed;
    [SerializeField] protected float jumpForce;
    [SerializeField] protected float collisionDetectionDistance;
    [HideInInspector] protected Vector3 forceDirection = Vector3.zero;
    [HideInInspector] protected Vector2 jumpFrameMovementSave;

    [Header("Camera Properties")]
    [SerializeField] public float cameraRotationSpeed;

    public InputAction moveAction { get; private set; }
    public InputAction jumpAction { get; private set; }
    public InputAction grabAction { get; private set; }
    public InputAction throwAction { get; private set; }
    public InputAction shotAction { get; private set; }

    ///////////////////////////////////////////////////
    ///            FONCTIONS HÉRITÉES               ///
    ///////////////////////////////////////////////////

    public override void Initialize(GameManager instance)
    {
        base.Initialize(instance);

        trigger = Trigger;
        heldObject = null;
        equippedObject = null;

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

    public override void ThrowObject(float throwForceHorizontal, float throwForceVertical)
    {
        base.ThrowObject(throwForceHorizontal, throwForceVertical);
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

    public void Move(Vector2 value)
    {
        Vector3 movement = new Vector3(value.x, 0f, value.y);

        if (!RaycastCollision() && value != Vector2.zero)
        {
            forceDirection += movement.x * Utilities.GetCameraRight(gameManager.transform) * moveSpeed;
            forceDirection += movement.z * Utilities.GetCameraForward(gameManager.transform) * moveSpeed;
        }

        rigidBody.AddForce(forceDirection, ForceMode.Impulse);

        if (rigidBody.velocity.y < 0f)
        {
            rigidBody.velocity += Vector3.down * -Physics.gravity.y * Time.fixedDeltaTime;
        }

        Vector3 horizontalVelocity = rigidBody.velocity;
        horizontalVelocity.y = 0f;

        if (horizontalVelocity.sqrMagnitude > maxMoveSpeed * maxMoveSpeed)
        {
            rigidBody.velocity = horizontalVelocity.normalized * maxMoveSpeed + Vector3.up * rigidBody.velocity.y;
        }

        if (isAiming)
        {
            LookAt(Vector2.zero);
        }
        else
        {
            LookAt(value);
        }

        forceDirection = Vector3.zero;
    }

    public void Jump()
    {
        if (RaycastGrounded())
        {
            jumpFrameMovementSave = new Vector2(rigidBody.velocity.x, rigidBody.velocity.z);
            rigidBody.AddForce(new Vector3(rigidBody.velocity.x, jumpForce, rigidBody.velocity.z), ForceMode.Impulse);
        }
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
            heldObject.ThrowObject(startThrowForceHorizontal, startThrowForceVertical);
            heldObject = null;
        }
    }

    public void Aim(bool value)
    {
        isAiming = value;
        gameManager.cameraManager.SetCameraAim(value);
    }

    public void MoveCamera(Vector2 value)
    {
        Quaternion rotation = cameraTarget.localRotation;

        if (isAiming)
        {
            rotation *= Quaternion.AngleAxis(value.x * cameraRotationSpeed, transform.up);
            rotation *= Quaternion.AngleAxis(-value.y * cameraRotationSpeed, transform.right);
            rotation.x = Utilities.ClampAngle(rotation.x, -30, 30);
            rotation.y = Utilities.ClampAngle(rotation.y, -30, 30);
        }
        else
        {
            rotation *= Quaternion.AngleAxis(-value.x * cameraRotationSpeed, transform.up);
            rotation *= Quaternion.AngleAxis(-value.y * cameraRotationSpeed, transform.right);
        }

        cameraTarget.localRotation = rotation;
    }

    public void Shot(InputAction action)
    {
        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
        Vector3 hitPoint = Vector3.zero;
        Ray ray = gameManager._camera.ScreenPointToRay(screenCenter);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            if (action == playerControls.Player.B)
            {
                if (hit.collider.tag == "Player" && hit.collider.transform != transform)
                {
                    if (hit.collider.TryGetComponent<PlayerManager>(out PlayerManager playerManager))
                    {
                        Aim(false);
                        gameManager.cameraManager.aimCamera.m_Follow = playerManager.cameraTarget;
                        gameManager.cameraManager.aimCamera.m_LookAt = playerManager.cameraTarget;
                        gameManager.SetMainPlayer(playerManager, true);
                        return;
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
                    if ((hit.collider.tag == "Player" || hit.collider.tag == "Mushroom" || hit.collider.tag == "Object") && hit.collider.transform != transform)
                    {
                        if (hit.collider.TryGetComponent<StateManager>(out StateManager stateManager))
                        {
                            MushroomManager equippedMushroom = (MushroomManager)equippedObject;
                            stateManager.SetState(equippedMushroom.stateToApply);
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

        if (value.sqrMagnitude > 0.1f && direction.sqrMagnitude > 0.1f)
        {
            rigidBody.rotation = Quaternion.LookRotation(direction, Vector3.up);
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
        bool isCollisionDetected = false;

        RaycastHit hit;
        if (Physics.Raycast(feet.position, -Vector3.up, out hit, collisionDetectionDistance))
        {
            float dotProduct = Vector3.Dot(hit.normal, Vector3.up);

            if (dotProduct >= 0.95f && dotProduct <= 1.05f)
            {
                isCollisionDetected = true;
            }
        }

        return isCollisionDetected;
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

                if (playerControls.Player.A.IsPressed() && !buttonSouthIsPressed)
                {
                    buttonSouthIsPressed = true;
                    Jump();
                }

                if (playerControls.Player.RightStick.ReadValue<Vector2>() != Vector2.zero)
                {
                    MoveCamera(playerControls.Player.RightStick.ReadValue<Vector2>());
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
                            heldObject.SetState(equippedMushroom.stateToApply);
                        }
                    }
                }
                else
                {
                    if (!playerControls.Player.LT.IsPressed() && leftTriggerIsPressed)
                    {
                        leftTriggerIsPressed = false;
                        Aim(false);
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
    }
}
