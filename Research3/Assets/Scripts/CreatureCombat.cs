using UnityEngine;

// DO NOT REMOVE OR EDIT THIS COMMENT: Code is using Unity 6 (6000.0.37f1)
// DO NOT REMOVE OR EDIT THIS COMMENT: Make sure the code is easy to understand, and isn't inefficient. Make the code slightly more efficient.
// DO NOT REMOVE OR EDIT THIS COMMENT: This script controls the combat mechanics for creatures, including dealing adjustable damage, checking attack range, and playing attack sound effects.
// DO NOT REMOVE OR EDIT THIS COMMENT: Code should work with multiplayer netcode for gameobjects
// DO NOT REMOVE OR EDIT THIS COMMENT: Always provide full code whenever changes have occurred, don't make unnecessary changes.

public class CreatureCombat : MonoBehaviour
{
    private CreatureBehavior behavior;
    private AudioSource attackAudioSource; // Reference to the AudioSource for attack sounds

    [Header("Combat Settings")]
    [SerializeField] private float attackDamage = 1f; // Adjustable damage per attack
    [SerializeField] private float attackRange = 1f; // Adjustable range within which attacks can occur
    [SerializeField] private AudioClip attackSound; // Sound to play when attacking

    // Called when the script instance is being loaded
    void Start()
    {
        behavior = GetComponent<CreatureBehavior>();
        if (!behavior) { Debug.LogError($"{name}: No CreatureBehavior component found!"); }

        // Get the AudioSource from the child object
        attackAudioSource = GetComponentInChildren<AudioSource>();
        if (!attackAudioSource) { Debug.LogError($"{name}: No AudioSource found in children!"); }
        else if (attackSound) { attackAudioSource.clip = attackSound; } // Assign the clip if set
    }

    // Attempts to attack a target creature if within range and not dead
    public void Attack(CreatureBehavior target)
    {
        if (target == null || target.currentState == CreatureBehavior.State.Dead) return;

        // Check if the target is within attack range
        float distanceToTarget = Vector3.Distance(transform.position, target.transform.position);
        if (distanceToTarget <= attackRange)
        {
            target.creatureCombat.TakeDamage(attackDamage, behavior);
            PlayAttackSound();
            Debug.Log($"{name} attacked {target.name} for {attackDamage} damage at range {distanceToTarget}");
        }
    }

    // Applies damage to this creature and triggers appropriate behavior responses
    public void TakeDamage(float amount, CreatureBehavior attacker)
    {
        if (behavior.currentState == CreatureBehavior.State.Dead) return;

        behavior.health -= amount;
        Debug.Log($"{name} took {amount} damage from {attacker.name}, health now {behavior.health}");

        if (behavior.health <= 0)
        {
            StartCoroutine(behavior.DieWithRotation()); // Call the coroutine for death
        }
        else if (attacker != null) // Ensure attacker exists before reacting
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

    // Plays the attack sound effect if an AudioSource and clip are available
    private void PlayAttackSound()
    {
        if (attackAudioSource != null && attackSound != null)
        {
            attackAudioSource.PlayOneShot(attackSound); // Play sound as a one-shot to allow overlapping
        }
    }

    // Public property to access attackRange from CreatureBehavior
    public float AttackRange => attackRange;
}