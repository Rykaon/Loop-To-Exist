using DG.Tweening.Core.Easing;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class PlayerMovementHandler : PlayerHandler
{
    private CharacterController characterController;

    [Header ("Componenents References")]
    [SerializeField] private PlayerControls playerControls;
    [SerializeField] private Transform feet;
    [SerializeField] private Transform eye;

    [Header ("Move Properties")]
    //[SerializeField] protected float moveSpeed = 20f;
    [SerializeField] private float maxMoveSpeed;
    [SerializeField] private float acceleration = 7f;
    [SerializeField] private float deceleration = 7f;
    [SerializeField] private float velPower = 0.9f; //inférieur à 1
    [HideInInspector] public float moveMassMultiplier;
    [HideInInspector] private Vector3 direction = Vector3.zero;
    [HideInInspector] private float linkMoveMultiplier;
    [HideInInspector] private float linkJumpMultiplier;

    [Header ("Jump Properties")]
    [SerializeField] private float jumpForce;
    [SerializeField][Range(0f, 1f)] private float jumpCutMultiplier;
    [SerializeField] private Vector3 customGravity;
    [SerializeField] private float fallGravityMultiplier;
    [HideInInspector] private Vector2 jumpFrameMovementSave;

    [Header ("Collision Properties")]
    [SerializeField] private float collisionDetectionDistance;
    [SerializeField] private LayerMask groundLayer;



    [SerializeField] private float jumpBufferTime; // Temps de buffer pour le saut
    public float jumpBufferTimer;

    [SerializeField] private float coyoteTime; // Temps de coyote time
    public float coyoteTimer;

    public override void Initialize(PlayerManager manager)
    {
        playerManager = manager;
        playerControls = manager.playerControls;

        moveMassMultiplier = 1; //Ne pas toucher.
        linkMoveMultiplier = 1.75f; //Le multiplieur associé à la fonction Move() si le joueur est link. Vérifier le cas où le joueur tient un objet qui est link. Fonction Move(), ligne 173. J'ai fait une division mais peut-être que ça mérite une valeur dissociée
        linkJumpMultiplier = 5.25f; //Le multiplieur associé à la fonction Jump() si le joueur est link
    }

    public override void UpdateComponent()
    {
        Move(playerManager.playerControls.Player.LeftStick.ReadValue<Vector2>());
        //FallGravity();

        if ((RaycastGrounded() && jumpBufferTimer > 0))
        {
            //buttonSouthIsPressed = true;
            jumpBufferTimer = -5;
            coyoteTimer = -5;
            Jump();
        }
        else if (playerControls.Player.A.IsPressed() && coyoteTimer > 0 && !RaycastGrounded())
        {
            coyoteTimer = -5;
            jumpBufferTimer = -5;
            Jump();
        }

        if (!playerControls.Player.A.IsPressed() && !playerManager.buttonSouthIsPressed /*&& rigidBody.velocity.y > 0*/)
        {
            //Debug.Log("JumpCut!");
            OnJumpUp();
        }
    }

    public void Move(Vector2 inputValue)
    {
        Vector3 inputDirection = new Vector3(inputValue.x, 0f, inputValue.y);
        if (!RaycastCollision() && inputValue != Vector2.zero)
        {
            //On y multiplie la direction du forward et du right de la caméra pour avoir la direction globale du joueur.
            direction += inputDirection.x * Utilities.GetCameraRight(playerManager.gameManager.transform);
            direction += inputDirection.z * Utilities.GetCameraForward(playerManager.gameManager.transform);

            direction *= moveMassMultiplier;

            if (playerManager.link != null)
            {
                direction *= linkMoveMultiplier;
            }
            else if (playerManager.heldObject != null)
            {
                if (playerManager.heldObject.link != null)
                {
                    direction *= (linkMoveMultiplier / 2);
                }
            }

            //On calcule le vecteur de déplacement désiré.
            Vector3 TargetSpeed = new Vector3(direction.x * maxMoveSpeed, 0f, direction.z * maxMoveSpeed);
            //On prends la différence en le vecteur désiré et le vecteur actuel.
            Vector3 SpeedDiff = TargetSpeed - new Vector3(characterController.velocity.x, 0f, characterController.velocity.z);

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

            //On applique l'acceleration à la SpeedDiff, La puissance permet d'augmenter l'acceleration si la vitesse est plus élevée.
            //Enfin on multiplie par le signe de SpeedDiff pour avoir la bonne direction.
            //Vector3 movement = new Vector3(Mathf.Pow(Mathf.Abs(SpeedDiff.x) * AccelRate, velPower) * Mathf.Sign(SpeedDiff.x), 0f, Mathf.Pow(Mathf.Abs(SpeedDiff.z) * AccelRate, velPower) * Mathf.Sign(SpeedDiff.z));

            //On applique la force au GO
            //rigidBody.AddForce(movement, ForceMode.Force);

            //characterController.Move(direction);
        }
        else
        {
            //Debug.Log("yo");
        }











        //On récupère la direction donnée par le joystick
        /*Vector3 inputDirection = new Vector3(inputValue.x, 0f, inputValue.y);

        if (!RaycastCollision() && inputValue != Vector2.zero)
        {
            //On y multiplie la direction du forward et du right de la caméra pour avoir la direction globale du joueur.
            direction += inputDirection.x * Utilities.GetCameraRight(gameManager.transform);
            direction += inputDirection.z * Utilities.GetCameraForward(gameManager.transform);

            direction *= moveMassMultiplier;

            if (playerManager.link != null)
            {
                direction = direction * linkMoveMultiplier;
            }
            else if (playerManager.heldObject != null)
            {
                if (playerManager.heldObject.link != null)
                {
                    direction = direction * (linkMoveMultiplier / 2);
                }
            }

            //rigidBody.AddForce(direction, ForceMode.Impulse);
        }
        else
        {
            //Debug.Log("yo");
        }
        //On calcule le vecteur de déplacement désiré.
        Vector3 TargetSpeed = new Vector3(direction.x * maxMoveSpeed, 0f, direction.z * maxMoveSpeed);
        //On prends la différence en le vecteur désiré et le vecteur actuel.
        //Vector3 SpeedDiff = TargetSpeed - new Vector3(rigidBody.velocity.x, 0f, rigidBody.velocity.z);

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
        //On applique l'acceleration à la SpeedDiff, La puissance permet d'augmenter l'acceleration si la vitesse est plus élevée.
        //Enfin on multiplie par le signe de SpeedDiff pour avoir la bonne direction.
        //Vector3 movement = new Vector3(Mathf.Pow(Mathf.Abs(SpeedDiff.x) * AccelRate, velPower) * Mathf.Sign(SpeedDiff.x), 0f, Mathf.Pow(Mathf.Abs(SpeedDiff.z) * AccelRate, velPower) * Mathf.Sign(SpeedDiff.z));

        //On applique la force au GO
        //rigidBody.AddForce(movement, ForceMode.Force);


        //Limit la Speed du joueur à la speed Max (Pas necessaire)
        Vector3 horizontalVelocity = rigidBody.velocity;
        horizontalVelocity.y = 0f;
        if (horizontalVelocity.sqrMagnitude > maxMoveSpeed * maxMoveSpeed)
        {
            rigidBody.velocity = horizontalVelocity.normalized * maxMoveSpeed + Vector3.up * rigidBody.velocity.y;
        }

        LookAt(inputValue);

        direction = Vector3.zero;*/
    }

    public void LookAt(Vector2 value)
    {
        /*Vector3 direction = rigidBody.velocity;
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
        }*/
    }

    public void Jump()
    {

        //Reset de la velocité en Y
        /*rigidBody.velocity = new Vector3(rigidBody.velocity.x, 0f, rigidBody.velocity.z);

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

        rigidBody.AddForce(jumpForce, ForceMode.Impulse);*/

    }

    public void OnJumpUp()
    {
        //JumpCut
        /*if (rigidBody.velocity.y > 0)
        {
            rigidBody.AddForce(Vector3.down * rigidBody.velocity.y * (1 - jumpCutMultiplier), ForceMode.Impulse);
        }*/
    }

    public void FallGravity()//Ajoute une gravité fictive/ Lorsque le personnage retombe, donne un feeling avec plus de répondant.
    {
        //On applique la gravité custom
        /*if (rigidBody.velocity.y < 0f)
        {
            rigidBody.AddForce(customGravity * fallGravityMultiplier, ForceMode.Acceleration);
        }
        else
        {
            rigidBody.AddForce(customGravity, ForceMode.Acceleration);
        }*/
    }

    private bool RaycastGrounded()
    {
        //bool isCollisionDetected = Physics.Raycast(feet.position, Vector3.down, collisionDetectionDistance, groundLayer);

        bool isCollisionDetected = Physics.BoxCast(feet.position, feet.transform.lossyScale / 2, Vector3.down, feet.transform.rotation, collisionDetectionDistance, groundLayer);
        //bool isCollisionDetected = false;

        RaycastHit hit;
        if (Physics.Raycast(feet.position, Vector3.down, out hit, collisionDetectionDistance))
        {
            float dotProduct = Vector3.Dot(hit.normal, Vector3.up);

            if (dotProduct >= 0.95f && dotProduct <= 1.05f)
            {
                isCollisionDetected = true;
            }
        }
        return isCollisionDetected;
    }

    private bool RaycastCollision()
    {
        bool isCollisionDetected = false;

        RaycastHit hit;
        if (Physics.Raycast(eye.position, eye.forward, out hit, collisionDetectionDistance))
        {
            if ((hit.collider.tag == "Wall" || hit.collider.tag == "Ground")/* && rigidBody.velocity.sqrMagnitude > maxMoveSpeed*/)
            {
                isCollisionDetected = true;
            }
        }

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
}
