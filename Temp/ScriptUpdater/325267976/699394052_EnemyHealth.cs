using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Параметры здоровья")]
    [SerializeField] private float maxHealth = 50f; // Зомби умирает с 2-3 выстрелов дробовика
    [SerializeField] private float currentHealth;

    [Header("Озвучка боли")]
    [Tooltip("Повесьте сюда звук рычания при получении урона")]
    [SerializeField] private AudioClip painSound;
    [Tooltip("Повесьте сюда крик смерти зомби")]
    [SerializeField] private AudioClip deathSound;

    private Animation legacyAnimation;
    private AudioSource audioSource;
    private bool isDead = false;

    void Awake()
    {
        currentHealth = maxHealth;
        
        // Ищем компоненты на этом объекте или внутри него
        legacyAnimation = GetComponentInChildren<Animation>();
        audioSource = GetComponentInChildren<AudioSource>();
    }

    // Главный метод получения урона от оружия игрока
    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        Debug.Log($"<color=orange>[Зомби] Получил урон! Осталось здоровья: {currentHealth}</color>");

        if (currentHealth <= 0f)
        {
            Die();
        }
        else
        {
            // Воспроизводим звук боли
            if (audioSource != null && painSound != null)
            {
                audioSource.PlayOneShot(painSound);
            }
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        Debug.Log($"[{gameObject.name}] Убит!");

        // 1. Сначала полностью очищаем и выключаем навигацию, чтобы объект застыл
        UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            agent.ResetPath(); // Мгновенно стираем маршрут до игрока
            agent.isStopped = true;
            agent.enabled = false;
        }

        // 2. Выключаем ИИ патрулирования и погони
        ZombieAI zombieAI = GetComponent<ZombieAI>();
        if (zombieAI != null)
        {
            zombieAI.enabled = false;
        }

        // 3. Отключаем коллайдер
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // 4. Запускаем падение на бок
        StartCoroutine(DeathAnimationRoutine());
    }
    public bool IsDead() => isDead;
}
