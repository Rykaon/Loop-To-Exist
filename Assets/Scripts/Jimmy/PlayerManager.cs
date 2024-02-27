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
using static UnityEditor.FilePathAttribute;

public class PlayerManager : StateManager
{
    [Header("Component References")]
    [SerializeField] private Transform head;
    [SerializeField] private Transform feet;
    [SerializeField] private Transform eye;
    [SerializeField] public Transform cameraTarget;

    public Transform playerCamera { get; private set; }

    [SerializeField] private Transform VirtualCamera;
    public Transform virtualCamera { get; private set; }

    [SerializeField] private PlayerInteractionTrigger Trigger;
    public PlayerInteractionTrigger trigger { get; private set; }

    public PlayerControls playerControls { get; private set; }
    public InputRecorder recorder { get; private set; }

    [HideInInspector] public bool buttonSouthIsPressed = false;
    [HideInInspector] public bool buttonWestIsPressed = false;
    [HideInInspector] public bool buttonEastIsPressed = false;
    [HideInInspector] public bool aimIsPressed = false;
    [HideInInspector] public bool shotIsPressed = false;

    [Header("Status")]
    public bool isMainPlayer;
    public bool hasBeenRecorded;
    public bool isActive;
    public bool isRecording = true;
    private bool isAiming = false;

    [Header("Move Properties")]
    [SerializeField] protected float moveSpeed;
    [SerializeField] protected float maxMoveSpeed;
    [SerializeField] protected float jumpForce;
    [SerializeField] protected float collisionDetectionDistance;
    [HideInInspector] protected Vector3 forceDirection = Vector3.zero;
    [HideInInspector] protected Vector2 jumpFrameMovementSave;

    [HideInInspector] protected ItemManager selectedObject = null;
    [HideInInspector] protected ItemManager previousSelectedObject = null;

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

        recorder = new InputRecorder(this);

        virtualCamera = VirtualCamera;
        trigger = Trigger;

        playerControls = gameManager.playerControls;

        moveAction = playerControls.Player.Move;
        jumpAction = playerControls.Player.Jump;
        grabAction = playerControls.Player.Grab;
        throwAction = playerControls.Player.Throw;
        shotAction = playerControls.Player.Shot;
    }
    
    public override void Reset()
    {
        recorder.ResetExecution();

        base.Reset();

        isAiming = false;
        selectedObject = null;
        previousSelectedObject = null;
    }

    public override void SetState(State state)
    {
        base.SetState(state);
    }

    public override void ResetState()
    {
        base.ResetState();
    }

    ///////////////////////////////////////////////////
    ///          FONCTIONS DE GESTIONS              ///
    ///////////////////////////////////////////////////

    public void SetIsMainPlayer(bool value)
    {
        if (value)
        {
            playerCamera = gameManager.cameraManager.currentCamera.transform;
            playerControls = gameManager.playerControls;
            isMainPlayer = true;
        }
        else
        {
            playerCamera = virtualCamera;
            playerControls = null;
            isMainPlayer = false;
        }
    }

    ///////////////////////////////////////////////////
    ///           FONCTIONS D'ACTIONS               ///
    ///////////////////////////////////////////////////

    public void Move(Vector2 value, Vector3 position, Vector3 rotation)
    {
        Vector3 movement = new Vector3(value.x, 0f, value.y);

        if (isRecording)
        {
            if (!RaycastCollision() && value != Vector2.zero)
            {
                forceDirection += movement.x * Utilities.GetCameraRight(playerCamera) * moveSpeed;
                forceDirection += movement.z * Utilities.GetCameraForward(playerCamera) * moveSpeed;
            }
        }
        else
        {
            forceDirection = position;
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

        /*if (isRecording)
        {
            
        }
        else
        {
            if (position !=  Vector3.zero && rotation != Vector3.zero)
            {
                rigidBody.MoveRotation(Quaternion.Euler(rotation));
                rigidBody.MovePosition(position);
            }
        }*/

        if (isAiming)
        {
            LookAt(Vector2.zero);

            if (isRecording)
            {
                recorder.RecordInput(playerControls.Player.Move, forceDirection, Vector3.zero);
            }
        }
        else
        {
            if (isRecording)
            {
                LookAt(value);
                recorder.RecordInput(playerControls.Player.Move, forceDirection, new Vector3(value.x, value.y, 0f));
            }
            else
            {
                LookAt(new Vector2(rotation.x, rotation.y));
            }
        }

        forceDirection = Vector3.zero;
    }

    public void Jump()
    {
        if (isRecording)
        {
            recorder.RecordInput(playerControls.Player.Jump);
        }

        if (RaycastGrounded())
        {
            jumpFrameMovementSave = new Vector2(rigidBody.velocity.x, rigidBody.velocity.z);
            rigidBody.AddForce(new Vector3(rigidBody.velocity.x, jumpForce, rigidBody.velocity.z), ForceMode.Impulse);
        }
    }

    public void Grab()
    {
        if (isRecording)
        {
            recorder.RecordInput(playerControls.Player.Grab);
        }

        if (trigger.triggeredObjectsList.Count > 0 && selectedObject == null)
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
                SetSelectedObject(trigger.triggeredObjectsList[index]);
            }
        }
    }

    public void SetSelectedObject(ItemManager grab)
    {
        selectedObject = grab;
        Transform[] path = new Transform[2];
        path[0] = grab.transform; path[1] = head;
        grab.SetSelectedObject(path, 0.25f);
    }

    public void Throw()
    {
        if (isRecording)
        {
            recorder.RecordInput(playerControls.Player.Throw);
        }

        if (selectedObject != null)
        {
            if (selectedObject.isSet)
            {
                selectedObject.ThrowObject();
                previousSelectedObject = selectedObject;
                selectedObject = null;
            }
        }
    }

    public void Aim(bool value)
    {
        if (value)
        {
            isAiming = true;
        }
        else
        {
            isAiming = false;
        }
        
        gameManager.cameraManager.SetCameraAim(value);
        //gameManager.SetCameraAim(value);
    }

    public void MoveCamera(Vector2 value)
    {
        Quaternion rotation = cameraTarget.localRotation;
        rotation *= Quaternion.AngleAxis(-value.x * cameraRotationSpeed, Vector3.up);
        rotation *= Quaternion.AngleAxis(-value.y * cameraRotationSpeed, Vector3.right);

        Vector3 euler = rotation.eulerAngles;

        if (isAiming)
        {
            euler.x = Utilities.ClampAngle(euler.x, -30, 30);
            euler.y = Utilities.ClampAngle(euler.y, -30, 30);
        }

        rotation = Quaternion.Euler(euler);

        cameraTarget.localRotation = rotation;
    }

    public void Shot(Vector3 direction)
    {
        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
        Vector3 hitPoint = Vector3.zero;
        Ray ray = gameManager._camera.ScreenPointToRay(screenCenter);
        RaycastHit hit;

        if (isRecording)
        {
            if (Physics.Raycast(ray, out hit))
            {
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
            recorder.RecordInput(playerControls.Player.Shot, ray.direction);
            Debug.Log("Real // Position : " + eye.position + ", Direction : " + ray.direction);
        }
        else
        {
            ray.origin = eye.position;
            ray.direction = direction;
            Debug.Log("Virtual // Position : " + eye.position + ", Direction : " + ray.direction);
        }

        if (Physics.Raycast(ray, out hit))
        {
            if ((hit.collider.tag == "Wall" || hit.collider.tag == "Player"))
            {
                if (hit.collider.TryGetComponent<StateManager>(out StateManager stateManager))
                {
                    if (stateManager.type == Type.Player || stateManager.type == Type.Item)
                    {
                        stateManager.SetState(stateToApply);
                        Debug.Log(hit.transform.name);
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

        if (value.sqrMagnitude > 0.1f && direction.sqrMagnitude > 0.01f)
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

    protected override void OnCollisionEnter(Collision collision)
    {

    }

    private void FixedUpdate()
    {        
        if (playerControls != null)
        {
            if (buttonSouthIsPressed && !playerControls.Player.Jump.IsPressed())
            {
                buttonSouthIsPressed = false;
            }

            if (buttonWestIsPressed && !playerControls.Player.Grab.IsPressed())
            {
                buttonWestIsPressed = false;
            }

            if (buttonEastIsPressed && !playerControls.Player.Throw.IsPressed())
            {
                buttonEastIsPressed = false;
            }

            if (shotIsPressed && !playerControls.Player.Shot.IsPressed())
            {
                shotIsPressed = false;
            }
        }

        if (isActive)
        {
            /*if (selectedObject != null)
            {
                selectedObject.transform.position = head.position;
            }*/

            if (transform.name == "Player_01")
            {
                /*if (isRecording)
                {
                    Debug.Log(transform.position);
                }
                else
                {
                    Debug.Log(transform.position);
                }*/
            }

            if (isMainPlayer)
            {
                if (isRecording)
                {
                    recorder.RecordInput(null);
                }

                Move(playerControls.Player.Move.ReadValue<Vector2>(), Vector3.zero, Vector3.zero);

                if (playerControls.Player.Jump.IsPressed() && !buttonSouthIsPressed)
                {
                    buttonSouthIsPressed = true;
                    Jump();
                }

                if (playerControls.Player.MoveCamera.ReadValue<Vector2>() != Vector2.zero)
                {
                    MoveCamera(playerControls.Player.MoveCamera.ReadValue<Vector2>());
                }

                if (!isAiming)
                {
                    if (playerControls.Player.Grab.IsPressed() && !buttonWestIsPressed)
                    {
                        buttonWestIsPressed = true;
                        Grab();
                    }

                    if (playerControls.Player.Throw.IsPressed() && !buttonEastIsPressed)
                    {
                        buttonEastIsPressed = true;
                        Throw();
                    }

                    if (playerControls.Player.Aim.IsPressed() && !aimIsPressed)
                    {
                        aimIsPressed = true;
                        Aim(true);
                    }
                }
                else
                {
                    if (!playerControls.Player.Aim.IsPressed() && aimIsPressed)
                    {
                        aimIsPressed = false;
                        Aim(false);
                    }

                    if (playerControls.Player.Shot.IsPressed() && !shotIsPressed)
                    {
                        shotIsPressed = true;
                        Shot(Vector3.zero);
                    }
                }
            }
            else
            {
                if (recorder.CheckLog(gameManager.elapsedTime, null, recorder.cameraPosLogs))
                {
                    //recorder.ExecuteVectorLog(recorder.GetVectorInputLogs(gameManager.elapsedTime, recorder.cameraPosLogs), recorder.GetVectorInputLogs(gameManager.elapsedTime, recorder.cameraRotLogs));
                }

                List<InputAction> actions = recorder.GetInputActions(gameManager.elapsedTime);

                if (actions != null)
                {
                    for (int i = 0; i < actions.Count; ++i)
                    {
                        if (actions[i] == moveAction)
                        {
                            recorder.ExecuteVectorLog(recorder.GetVectorInputLogs(gameManager.elapsedTime, recorder.movePosLogs), recorder.GetVectorInputLogs(gameManager.elapsedTime, recorder.moveRotLogs));
                        }
                        
                        if (actions[i] == jumpAction)
                        {
                            recorder.ExecuteFloatLog(recorder.GetFloatInputLogs(gameManager.elapsedTime, recorder.jumpLogs));
                        }
                        
                        if (!isAiming)
                        {
                            
                        }
                        else
                        {
                            
                        }

                        if (actions[i] == grabAction)
                        {
                            recorder.ExecuteFloatLog(recorder.GetFloatInputLogs(gameManager.elapsedTime, recorder.catchLogs));
                        }

                        if (actions[i] == throwAction)
                        {
                            recorder.ExecuteFloatLog(recorder.GetFloatInputLogs(gameManager.elapsedTime, recorder.throwLogs));
                        }

                        if (actions[i] == shotAction)
                        {
                            recorder.ExecuteVectorLog(recorder.GetVectorInputLogs(gameManager.elapsedTime, recorder.shotLogs), null);
                        }
                    }
                }

                if (!recorder.CheckLog(gameManager.elapsedTime, null, recorder.movePosLogs))
                {
                    Move(Vector2.zero, Vector3.zero, Vector3.zero);
                }
            }
        }
    }
}
