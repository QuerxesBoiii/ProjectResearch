using UnityEngine;

public class CreatureCombat : MonoBehaviour
{
    private CreatureBehavior behavior;
    private AudioSource attackAudioSource;

    [Header("Combat Settings")]
    [SerializeField] private float attackDamage = 1f;
    [SerializeField] private float attackRange = 1f;
    [SerializeField] private AudioClip attackSound;

    void Start()
    {
        behavior = GetComponent<CreatureBehavior>();
        if (!behavior) { Debug.LogError($"{name}: No CreatureBehavior component found!"); }

        attackAudioSource = GetComponentInChildren<AudioSource>();
        if (!attackAudioSource) { Debug.LogError($"{name}: No AudioSource found in children!"); }
        else if (attackSound) { attackAudioSource.clip = attackSound; }
    }

    public void Attack(CreatureBehavior target)
    {
        if (target == null || target.currentState == CreatureBehavior.State.Dead) return;

        float distanceToTarget = Vector3.Distance(transform.position, target.transform.position);
        if (distanceToTarget <= attackRange)
        {
            target.creatureCombat.TakeDamage(attackDamage, behavior);
            PlayAttackSound();
            Debug.Log($"{name} attacked {target.name} for {attackDamage} damage at range {distanceToTarget}");
        }
    }

    public void TakeDamage(float amount, CreatureBehavior attacker)
    {
        if (behavior.currentState == CreatureBehavior.State.Dead) return;

        behavior.Health -= amount;
        Debug.Log($"{name} took {amount} damage from {attacker.name}, health now {behavior.Health}");

        if (behavior.Health <= 0)
        {
            StartCoroutine(behavior.DieWithRotation());
        }
        else if (attacker != null)
        {
            if (behavior.combatBehavior == CreatureBehavior.CombatBehavior.Neutral)
            {
                behavior.combatTarget = attacker.transform;
                behavior.currentState = CreatureBehavior.State.Attacking;
                Debug.Log($"{name} (Neutral) is now attacking {attacker.name}");
            }
            else if (behavior.combatBehavior == CreatureBehavior.CombatBehavior.Friendly)
            {
                behavior.combatTarget = attacker.transform;
                behavior.currentState = CreatureBehavior.State.Fleeing;
                Debug.Log($"{name} (Friendly) is now fleeing from {attacker.name}");
            }
        }
    }

    private void PlayAttackSound()
    {
        if (attackAudioSource != null && attackSound != null)
        {
            attackAudioSource.PlayOneShot(attackSound);
        }
    }

    public float AttackRange => attackRange;
}