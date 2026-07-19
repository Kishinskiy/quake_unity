using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class ZombieAI : MonoBehaviour
{
    [Header("Параметры атаки")]
    [SerializeField] private float attackRange = 2f;       // Дистанция удара
    [SerializeField] private float attackCooldown = 1.5f;   // Перезарядка удара
    [SerializeField] private float attackDamage = 15f;      // Урон

    private NavMeshAgent agent;
    private Animator animator;
    private Transform playerTransform;
    private PlayerHealth playerHealth;
    private float nextAttackTime;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        
        // Гарантируем, что физика не будет конфликтовать с навигацией
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    private void Start()
    {
        // Ищем игрока на сцене по тегу
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerHealth = player.GetComponent<PlayerHealth>();
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] КРИТИЧЕСКАЯ ОШИБКА: Игрок с тегом 'Player' не найден на сцене!");
        }

        // Принудительно привязываем капсулу к NavMesh сетке пола при старте
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.Warp(transform.position);
            
            // Задаем базовые олдскульные скорости, если в инспекторе сбросилось в 0
            if (agent.speed == 0) agent.speed = 4f;
            if (agent.acceleration == 0) agent.acceleration = 12f;
            agent.stoppingDistance = 0f;
        }
    }

    private void Update()
    {
        // 1. БЛОКИРОВКА ПАУЗЫ: Если игра на паузе, полностью замираем
        if (PauseManager.isPaused)
        {
            if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
            if (animator != null) animator.SetFloat("Speed", 0f);
            return;
        }

        if (playerTransform == null) return;

        // 2. БЕЗОПАСНОСТЬ НАВИГАЦИИ: Если потеряли пол, прерываем выполнение
        if (agent == null || !agent.isOnNavMesh || !agent.isActiveAndEnabled) return;

        // 3. ПРОВЕРКА СМЕРТИ ИГРОКА: Если игрок труп — останавливаемся
        if (playerHealth != null && playerHealth.IsDead())
        {
            agent.isStopped = true;
            if (animator != null) animator.SetFloat("Speed", 0f);
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // РЕЖИМ АТАКИ
        if (distanceToPlayer <= attackRange)
        {
            agent.isStopped = true;
            if (animator != null) animator.SetFloat("Speed", 0f); // Переводим в Idle при ударе

            // Поворот к игроку лицом по оси Y
            Vector3 direction = (playerTransform.position - transform.position).normalized;
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 12f * Time.deltaTime);
            }

            // Проверка Кулдауна атаки
            if (Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + attackCooldown;
                ExecuteAttack();
            }
        }
        // РЕЖИМ ПРЕСЛЕДОВАНИЯ
        else
        {
            agent.isStopped = false;
            agent.SetDestination(playerTransform.position);

            // Передаем реальную скорость перемещения агента в параметр аниматора
            if (animator != null)
            {
                animator.SetFloat("Speed", agent.velocity.magnitude);
            }
        }
    }

    private void ExecuteAttack()
    {
        // Вызываем триггер атаки в аниматоре
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        // Наносим урон игроку через наш исправленный скрипт PlayerHealth
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(attackDamage, PlayerHealth.DamageType.Bullet);
        }
    }
}
