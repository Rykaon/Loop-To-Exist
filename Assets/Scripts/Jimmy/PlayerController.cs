using Cinemachine;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class PlayerController : MonoBehaviour
{
    [Header("Component References")]
    //[SerializeField] private PlayerManager playerManager;
    [SerializeField] private Rigidbody rigidBody;
    [SerializeField] private Transform head;
    [SerializeField] private Transform feet;
    [SerializeField] private Transform eye;

    [SerializeField] private Transform PlayerCamera;
    public Transform playerCamera { get; private set; }

    [SerializeField] private Transform VirtualCamera;
    public Transform virtualCamera { get; private set; }

    [SerializeField] private PlayerInteractionTrigger Trigger;
    public PlayerInteractionTrigger trigger { get; private set; }

    [SerializeField] private GameManager GameManager;
    public GameManager gameManager { get; private set; }

    public PlayerControls playerControls { get; private set; }
    public InputRecorder recorder { get; private set; }

    public bool buttonSouthIsPressed = false;
    public bool buttonWestIsPressed = false;
    public bool buttonEastIsPressed = false;
    public bool aimIsPressed = false;
    public bool shotIsPressed = false;

    [Header("Status")]
    public bool isMainPlayer;
    public bool hasBeenRecorded;
    public bool isActive;
    public bool isRecording = true;
    private bool isGrounded;
    private bool wasJumpingLastFrame = false;
    private bool isAiming = false;

    [Header("Move Properties")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float maxMoveSpeed;
    [SerializeField] private float jumpForce;
    [SerializeField] private float collisionDetectionDistance;
    private Vector3 forceDirection = Vector3.zero;
    private Vector2 jumpFrameMovementSave;

    [Header("Catch Properties")]
    [SerializeField] private GrabObject selectedObject = null;
    [HideInInspector] private GrabObject previousSelectedObject = null;

    [SerializeField] public Transform cameraAimLockPoint;
    [SerializeField] public Transform cameraAimCursorLockPoint;
    [SerializeField] public float cameraCursorDistance;
    [SerializeField] public float cameraCursorMoveSpeed;

    [SerializeField] public float cameraMinX;
    [SerializeField] public float cameraMinY;
    [SerializeField] public float cameraMaxX;
    [SerializeField] public float cameraMaxY;

    public Vector3 startPosition { get; private set; }
    public Quaternion startRotation { get; private set; }

    public InputAction move { get; private set; }
    public InputAction jump { get; private set; }
    public InputAction grab { get; private set; }
    public InputAction throooooooow { get; private set; }

    public InputAction shot { get; private set; }

    public void Initialize()
    {
        this.gameManager = gameManager;
        recorder = new InputRecorder(this);

        playerCamera = PlayerCamera;
        virtualCamera = VirtualCamera;
        trigger = Trigger;
        this.gameManager = GameManager;

        playerControls = gameManager.playerControls;
        //playerControls.World.Enable();

        move = playerControls.Player.Move;
        jump = playerControls.Player.Jump;
        grab = playerControls.Player.Grab;
        throooooooow = playerControls.Player.Throw;
        shot = playerControls.Player.Shot;

        startPosition = transform.position;
        startRotation = transform.rotation;

        if (isMainPlayer)
        {
            isRecording = true;
        }
    }

    public void SetIsMainPlayer(bool isMainPlayer)
    {
        if (isMainPlayer)
        {
            playerCamera = gameManager.transform;
            playerControls = gameManager.playerControls;
            this.isMainPlayer = true;
        }
        else
        {
            playerCamera = virtualCamera;
            playerControls = null;
            this.isMainPlayer = false;
        }
    }

    public void Restart()
    {
        recorder.ResetExecution();

        rigidBody.isKinematic = true;
        Move(Vector2.zero);
        rigidBody.isKinematic = false;

        selectedObject = null;
        cameraAimCursorLockPoint.localPosition = new Vector3(0, 0, cameraCursorDistance);
        previousSelectedObject = null;

        transform.position = startPosition;
        transform.rotation = startRotation;

        isActive = false;
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
            //Debug.Log("Jump at : " + elapsedTime);
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
            //Debug.Log("Catch at : " + elapsedTime);
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



    public void Throw()
    {
        if (isRecording)
        {
            //Debug.Log("Throw at : " + elapsedTime);
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
            gameManager.SetCameraTarget(cameraAimLockPoint, cameraAimCursorLockPoint);
            isAiming = true;
            cameraAimCursorLockPoint.localPosition = new Vector3(0, 0, cameraCursorDistance);
        }
        else
        {
            gameManager.SetCameraTarget(transform, transform);
            isAiming = false;
        }

        gameManager.SetCameraAim(value);
    }

    public void MoveCameraCursor(Vector2 value)
    {
        Vector3 movement = new Vector3(value.x, value.y, 0f);

        Vector3 cameraPos = cameraAimCursorLockPoint.localPosition;
        cameraPos += movement * cameraCursorMoveSpeed;
        cameraPos.x = Mathf.Clamp(cameraPos.x, cameraMinX, cameraMaxX);
        cameraPos.y = Mathf.Clamp(cameraPos.y, cameraMinY, cameraMaxY);
        cameraAimCursorLockPoint.localPosition = cameraPos;
    }

    public void Shot()
    {
        if (isRecording)
        {
            recorder.RecordInput(playerControls.Player.Shot);
        }

        RaycastHit hit;
        if (Physics.Raycast(eye.position, (cameraAimCursorLockPoint.position - eye.position), out hit, cameraCursorDistance))
        {
            if ((hit.collider.tag == "Wall" || hit.collider.tag == "Player"))
            {
                Debug.Log(hit.transform.name);
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

    public void ResetObject()
    {
        selectedObject = null;
        previousSelectedObject = null;
    }

    public void SetSelectedObject(GrabObject grab)
    {
        selectedObject = grab;
        //Lancer animation et faire durer le tween le temps de l'animation;
        Transform[] path = new Transform[2];
        path[0] = grab.transform; path[1] = head;
        Debug.Log(grab.gameObject);
        grab.SetSelectedObject(path, 0.25f);
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
            if (selectedObject != null)
            {
                selectedObject.transform.position = head.position;
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

                    if (playerControls.Player.MoveCamera.ReadValue<Vector2>() != Vector2.zero)
                    {
                        MoveCameraCursor(playerControls.Player.MoveCamera.ReadValue<Vector2>());
                    }

                    if (playerControls.Player.Shot.IsPressed() && !shotIsPressed)
                    {
                        shotIsPressed = true;
                        Shot();
                    }
                }
            }
            else
            {
                if (recorder.CheckLog(gameManager.elapsedTime, null, null, recorder.cameraPosLogs))
                {
                    recorder.ExecuteCameraLog(recorder.GetCameraInputLogs(gameManager.elapsedTime, recorder.cameraPosLogs), false);
                    recorder.ExecuteCameraLog(recorder.GetCameraInputLogs(gameManager.elapsedTime, recorder.cameraRotLogs), true);
                }

                if (recorder.GetInputActions(gameManager.elapsedTime) != null)
                {
                    for (int i = 0; i < recorder.GetInputActions(gameManager.elapsedTime).Count; ++i)
                    {
                        if (recorder.GetInputActions(gameManager.elapsedTime)[i] == move)
                        {
                            recorder.ExecuteVectorLog(recorder.GetVectorInputLogs(gameManager.elapsedTime, recorder.moveLogs));
                        }
                        else if (recorder.GetInputActions(gameManager.elapsedTime)[i] == jump)
                        {
                            recorder.ExecuteFloatLog(recorder.GetFloatInputLogs(gameManager.elapsedTime, recorder.jumpLogs));
                        }
                        else if (recorder.GetInputActions(gameManager.elapsedTime)[i] == grab)
                        {
                            recorder.ExecuteFloatLog(recorder.GetFloatInputLogs(gameManager.elapsedTime, recorder.catchLogs));
                        }
                        else if (recorder.GetInputActions(gameManager.elapsedTime)[i] == throooooooow)
                        {
                            recorder.ExecuteFloatLog(recorder.GetFloatInputLogs(gameManager.elapsedTime, recorder.throwLogs));
                        }
                        else if (recorder.GetInputActions(gameManager.elapsedTime)[i] == shot)
                        {
                            recorder.ExecuteFloatLog(recorder.GetFloatInputLogs(gameManager.elapsedTime, recorder.shotLogs));
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
