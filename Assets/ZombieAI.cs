using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class ZombieAI : MonoBehaviour
{
    public enum EnemyState { Patrolling, Chasing, Attacking }

    [Header("Настройки ИИ и Состояния")]
    [SerializeField] private EnemyState currentState = EnemyState.Patrolling;
    [SerializeField] private float detectionRange = 10f;    
    [SerializeField] private float attackRange = 2f;        
    [SerializeField] private float attackCooldown = 1.5f;   
    [SerializeField] private float attackDamage = 15f;      

    [Header("Параметры патрулирования")]
    [SerializeField] private float patrolRadius = 8f;       
    [SerializeField] private float patrolWaitTime = 2.5f;    

    private NavMeshAgent agent;
    private Animator animator;
    private Transform playerTransform;
    private PlayerHealth playerHealth;
    private EnemyHealth enemyHealth; // Ссылка на собственное здоровье
    
    private Vector3 spawnPosition;
    private float patrolTimer;
    private float nextAttackTime;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        enemyHealth = GetComponent<EnemyHealth>(); // Находим здоровье при старте
        
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    private void Start()
    {
        spawnPosition = transform.position;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerHealth = player.GetComponent<PlayerHealth>();
        }

        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.Warp(transform.position);
            if (agent.speed == 0) agent.speed = 3f; 
            if (agent.acceleration == 0) agent.acceleration = 12f;
            // agent.stoppingDistance = 0f;
        }

        SetNewPatrolDestination();
    }

    private void Update()
    {
        if (PauseManager.isPaused)
        {
            if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
            if (animator != null) animator.SetFloat("Speed", 0f);
            return;
        }

        // БАГ-ФИКС: Если этот зомби уже мертв, прерываем выполнение ИИ мгновенно!
        if (enemyHealth != null && enemyHealth.IsDead())
        {
            if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
            return;
        }

        if (playerTransform == null) return;
        if (agent == null || !agent.isOnNavMesh || !agent.isActiveAndEnabled) return;

        if (playerHealth != null && playerHealth.IsDead())
        {
            currentState = EnemyState.Patrolling; 
            agent.isStopped = true;
            if (animator != null) animator.SetFloat("Speed", 0f);
            return;
        }

        if (animator != null)
        {
            animator.SetFloat("Speed", agent.velocity.magnitude);
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= attackRange)
        {
            currentState = EnemyState.Attacking;
        }
        else if (distanceToPlayer <= detectionRange)
        {
            currentState = EnemyState.Chasing;
        }
        else if (currentState == EnemyState.Attacking || currentState == EnemyState.Chasing)
        {
            currentState = EnemyState.Patrolling;
            SetNewPatrolDestination();
        }

        switch (currentState)
        {
            case EnemyState.Patrolling:
                ExecutePatrol();
                break;

            case EnemyState.Chasing:
                ExecuteChase();
                break;

            case EnemyState.Attacking:
                ExecuteAttackLogic(distanceToPlayer);
                break;
        }
    }

    private void ExecutePatrol()
    {
        agent.isStopped = false;
        agent.speed = 2.5f; 

        if (!agent.pathPending && agent.remainingDistance <= 0.5f)
        {
            patrolTimer += Time.deltaTime;

            if (patrolTimer >= patrolWaitTime)
            {
                patrolTimer = 0f;
                SetNewPatrolDestination();
            }
        }
    }

    private void SetNewPatrolDestination()
    {
        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
        randomDirection += spawnPosition;

        NavMeshHit navHit;
        if (NavMesh.SamplePosition(randomDirection, out navHit, patrolRadius, -1))
        {
            agent.SetDestination(navHit.position);
        }
    }

    private void ExecuteChase()
    {
        agent.isStopped = false;
        agent.speed = 4.5f; 
        agent.SetDestination(playerTransform.position);
    }

    private void ExecuteAttackLogic(float distanceToPlayer)
    {
        agent.isStopped = true;

        Vector3 direction = (playerTransform.position - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 12f * Time.deltaTime);
        }

        if (Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + attackCooldown;
            
            if (animator != null) animator.SetTrigger("Attack");
            if (playerHealth != null) playerHealth.TakeDamage(attackDamage, PlayerHealth.DamageType.Bullet);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange); 

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);    
    }
}
