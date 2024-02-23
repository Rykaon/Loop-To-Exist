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
    private bool isGrounded;
    private bool wasJumpingLastFrame = false;
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
    [SerializeField] public Transform cameraAimLockPoint;
    [SerializeField] public Transform cameraAimCursorLockPoint;
    [SerializeField] public float cameraCursorDistance;
    [SerializeField] public float cameraCursorMoveSpeed;
    [SerializeField] public float cameraRotationSpeed;

    [SerializeField] public float cameraMinX;
    [SerializeField] public float cameraMinY;
    [SerializeField] public float cameraMaxX;
    [SerializeField] public float cameraMaxY;

    public InputAction moveAction { get; private set; }
    public InputAction jumpAction { get; private set; }
    public InputAction grabAction { get; private set; }
    public InputAction throwAction { get; private set; }
    public InputAction shotAction { get; private set; }

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
        cameraAimCursorLockPoint.localPosition = new Vector3(0, 0, cameraCursorDistance);
    }

    public override void SetState(State state)
    {
        base.SetState(state);
    }

    public override void ResetState()
    {
        base.ResetState();
    }

    // Les fonctions propres aux joueurs

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

    public void Move(Vector2 value)
    {
        Vector3 movement = new Vector3(value.x, 0f, value.y);

        if (isRecording && value != Vector2.zero)
        {
            recorder.RecordInput(playerControls.Player.Move);
        }

        if (!RaycastCollision() && value != Vector2.zero)
        {
            forceDirection += movement.x * GetCameraRight(playerCamera) * moveSpeed;
            forceDirection += movement.z * GetCameraForward(playerCamera) * moveSpeed;
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
        Quaternion rotation = cameraTarget.rotation;
        rotation *= Quaternion.AngleAxis(-value.x * cameraRotationSpeed, Vector3.up);
        rotation *= Quaternion.AngleAxis(-value.y * cameraRotationSpeed, Vector3.right);

        if (isAiming)
        {
            rotation = ClampCameraRotation(rotation);
        }

        cameraTarget.rotation = rotation;
    }

    private Quaternion ClampCameraRotation(Quaternion rotation)
    {
        Vector3 eulers = rotation.eulerAngles;
        eulers.x = ClampAngle(eulers.x, transform.rotation.eulerAngles.x - 30f, transform.rotation.eulerAngles.x + 30f);
        eulers.y = ClampAngle(eulers.y, transform.rotation.eulerAngles.y - 30f, transform.rotation.eulerAngles.y + 30f);

        return Quaternion.Euler(eulers);
    }

    private float ClampAngle(float current, float min, float max)
    {
        float dtAngle = Mathf.Abs(((min - max) + 180) % 360 - 180);
        float hdtAngle = dtAngle * 0.5f;
        float midAngle = min + hdtAngle;

        float offset = Mathf.Abs(Mathf.DeltaAngle(current, midAngle)) - hdtAngle;
        if (offset > 0)
            current = Mathf.MoveTowardsAngle(current, midAngle, offset);
        return current;
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
            Debug.Log("Real // Origin : " + eye.position + ", Direction : " + rayDirection);
            recorder.RecordInput(playerControls.Player.Shot, ray.direction);
        }
        else
        {
            ray.origin = eye.position;
            ray.direction = direction;
            Debug.Log("Record // Origin : " + eye.position + ", Direction : " + direction);
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

    public Vector3 GetCameraForward(Transform camera)
    {
        Vector3 forward = camera.forward;
        forward.y = 0f;
        return forward.normalized;
    }

    public Vector3 GetCameraRight(Transform camera)
    {
        Vector3 right = camera.right;
        right.y = 0f;
        return right.normalized;
    }

    public void LookAt(Vector2 value)
    {
        Vector3 direction = rigidBody.velocity;
        direction.y = 0f;

        if (value.sqrMagnitude > 0.1f && direction.sqrMagnitude > 0.1f)
        {
            this.rigidBody.rotation = Quaternion.LookRotation(direction, Vector3.up);
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
        //cameraTarget.position = rigidBody.position + (Vector3.up / 2);
        
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
            if (selectedObject != null)
            {
                selectedObject.transform.position = head.position;
            }
            
            if (transform.name == "Player_01")
            {
                Debug.Log(transform.position);
            }

            if (isMainPlayer)
            {
                if (isRecording)
                {
                    recorder.RecordInput(null);
                }

                Move(playerControls.Player.Move.ReadValue<Vector2>());

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
                if (recorder.CheckLog(gameManager.elapsedTime, null, null, recorder.cameraPosLogs))
                {
                    recorder.ExecuteCameraLog(recorder.GetVector3InputLogs(gameManager.elapsedTime, recorder.cameraPosLogs), false);
                    recorder.ExecuteCameraLog(recorder.GetVector3InputLogs(gameManager.elapsedTime, recorder.cameraRotLogs), true);
                }

                List<InputAction> actions = recorder.GetInputActions(gameManager.elapsedTime);

                if (actions != null)
                {
                    for (int i = 0; i < actions.Count; ++i)
                    {
                        if (actions[i] == moveAction)
                        {
                            recorder.ExecuteVectorLog(recorder.GetVector2InputLogs(gameManager.elapsedTime, recorder.moveLogs));
                        }
                        else if (actions[i] == jumpAction)
                        {
                            recorder.ExecuteFloatLog(recorder.GetFloatInputLogs(gameManager.elapsedTime, recorder.jumpLogs));
                        }
                        else if (actions[i] == grabAction)
                        {
                            recorder.ExecuteFloatLog(recorder.GetFloatInputLogs(gameManager.elapsedTime, recorder.catchLogs));
                        }
                        else if (actions[i] == throwAction)
                        {
                            recorder.ExecuteFloatLog(recorder.GetFloatInputLogs(gameManager.elapsedTime, recorder.throwLogs));
                        }
                        else if (actions[i] == shotAction)
                        {
                            recorder.ExecuteCameraLog(recorder.GetVector3InputLogs(gameManager.elapsedTime, recorder.shotLogs), false);
                        }
                    }
                }

                if (!recorder.CheckLog(gameManager.elapsedTime, recorder.moveLogs, null, null))
                {
                    Move(Vector2.zero);
                }
            }
        }
    }
}
