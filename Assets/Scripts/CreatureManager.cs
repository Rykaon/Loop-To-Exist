using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class CreatureManager : StateManager
{
    [Header("Creature Reference")]
    [SerializeField] private LocalAudioManager AudioManager;
    [SerializeField] private Transform feet;
    [SerializeField] private Animator Animator;
    [SerializeField] private NavMeshAgent Agent;

    [Header("Properties")]
    [SerializeField] private float roamingRadius;
    [SerializeField] private float collisionDetectionDistance;
    [SerializeField] private LayerMask GroundLayer;
    private float angularSpeed = 200f;
    private float speed = 0.75f;
    private float radius = 0.1f;

    [Header("Status")]
    public bool isActive = false;
    public bool isRoaming = true;
    private bool isHeldAnim = false;

    private Coroutine roamRoutine = null;
    private Coroutine walkRoutine = null;

    public LocalAudioManager audioManager { get; private set; }
    public Animator animator { get; private set; }
    public NavMeshAgent agent { get; private set; }

    ///////////////////////////////////////////////////
    ///            FONCTIONS HÉRITÉES               ///
    ///////////////////////////////////////////////////

    public override void Initialize(GameManager instance)
    {
        base.Initialize(instance);

        audioManager = AudioManager;
        animator = Animator;
        agent = Agent;
        isActive = true;
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
        rigidBody.velocity = Vector3.zero;
        isRoaming = false;

        if (roamRoutine != null)
        {
            StopCoroutine(roamRoutine);
        }

        if (walkRoutine != null)
        {
            StopCoroutine(walkRoutine);
            walkRoutine = null;
        }

        if (agent != null)
        {
            if (agent.enabled)
            {
                agent.ResetPath();
            }

            Destroy(agent);
        }

        animator.SetBool("isWalking", false);
        animator.SetBool("isGrab", true);
        audioManager.Play("Sfx_Creature_OnGrab", radius);
        base.SetHoldObject(endPosition, time);
    }

    public override void InitializeHoldObject(Transform parent)
    {
        base.InitializeHoldObject(parent);
        isHeldAnim = true;
        audioManager.Play("Sfx_Creature_WhileGrab", radius);
    }

    public override void ThrowObject(float throwForceHorizontal, float throwForceVertical, Vector3 hitpoint)
    {
        base.ThrowObject(throwForceHorizontal, throwForceVertical, hitpoint);
    }

    protected override Vector3 GetThrowForce(float throwForceHorizontal, float throwForceVertical, Vector3 hitpoint)
    {
        audioManager.Stop("Sfx_Creature_WhileGrab");
        audioManager.Play("Sfx_Creature_OnThrow", radius);
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

    public override void DropObject()
    {
        base.DropObject();
        audioManager.Stop("Sfx_Creature_WhileGrab");

    }

    protected override void OnCollisionEnter(Collision collision)
    {
        base.OnCollisionEnter(collision);
    }

    protected override void OnJointBreak(float breakForce)
    {
        isRoaming = false;

        if (roamRoutine != null)
        {
            StopCoroutine(roamRoutine);
        }

        if (agent != null)
        {
            if (agent.enabled)
            {
                agent.ResetPath();
            }

            Destroy(agent);
        }

        animator.SetBool("isWalking", false);
        animator.SetBool("isGrab", true);
        
        base.OnJointBreak(breakForce);
        isHeldAnim = true;
    }

    ///////////////////////////////////////////////////
    ///           FONCTIONS UTILITAIRES             ///
    ///////////////////////////////////////////////////

    private bool RaycastGrounded()
    {
        bool isCollisionDetected = Physics.BoxCast(feet.position, feet.transform.lossyScale / 2, Vector3.down, feet.transform.rotation, collisionDetectionDistance, GroundLayer);

        return isCollisionDetected;
    }

    private bool RandomPoint(Vector3 pos, float radius, out Vector3 result)
    {
        Vector3 randomPoint = Random.insideUnitSphere * radius;

        while(Vector3.Distance(pos, randomPoint) < 2f)
        {
            randomPoint = Random.insideUnitSphere * radius;
        }

        NavMeshHit hit;

        if (NavMesh.SamplePosition(randomPoint + pos, out hit, roamingRadius, NavMesh.AllAreas))
        {
            result = hit.position;
            return true;
        }

        result = Vector3.zero;
        return false;
    }

    private IEnumerator Roam()
    {
        yield return null;
        Vector3 point;

        if (RandomPoint(transform.position, roamingRadius, out point))
        {
            agent.SetDestination(point);
        }

        animator.SetBool("isWalking", true);
        roamRoutine = null;

        if (walkRoutine == null)
        {
            walkRoutine = StartCoroutine(Walk());
        }
    }

    private IEnumerator WaitAndRoam()
    {
        float rand = Random.Range(2f, 5f);
        agent.ResetPath();
        animator.SetBool("isWalking", false);
        
        if (walkRoutine != null)
        {
            StopCoroutine(walkRoutine);
            walkRoutine = null;
        }

        yield return new WaitForSecondsRealtime(rand);

        Vector3 point;

        if (RandomPoint(transform.position, roamingRadius, out point))
        {
            agent.SetDestination(point);
        }

        animator.SetBool("isWalking", true);
        roamRoutine = null;

        if (walkRoutine == null)
        {
            walkRoutine = StartCoroutine(Walk());
        }
    }

    private IEnumerator Walk()
    {
        audioManager.PlayVariation("Sfx_Creature_Walk", 0.15f, 0.1f, radius);

        yield return new WaitForSecondsRealtime(0.60f);

        walkRoutine = StartCoroutine(Walk());
    }

    private void FixedUpdate()
    {
        if (isActive)
        {
            if (!isHeld && !isEquipped && isHeldAnim)
            {
                if (RaycastGrounded())
                {
                    rigidBody.velocity = Vector3.zero;
                    isHeldAnim = false;
                    isRoaming = true;

                    if (agent == null)
                    {
                        Agent = this.AddComponent<NavMeshAgent>();
                        agent = Agent;
                        agent.speed = speed;
                        agent.angularSpeed = angularSpeed;
                    }
                    else
                    {
                        agent.enabled = true;
                    }

                    animator.SetBool("isGrab", false);
                    roamRoutine = StartCoroutine(WaitAndRoam());
                    audioManager.PlayFixedVariation("Sfx_Creature_Fall", 1.25f, 0f, radius);
                }
            }

            if (isRoaming)
            {
                rigidBody.velocity = Vector3.zero;

                if (agent.remainingDistance <= agent.stoppingDistance)
                {
                    if (roamRoutine == null)
                    {
                        float rand = Random.Range(0, 100);

                        if (rand < 30)
                        {
                            roamRoutine = StartCoroutine(WaitAndRoam());
                        }
                        else
                        {
                            roamRoutine = StartCoroutine(Roam());
                        }
                    }
                }
            }
        }
    }
}
