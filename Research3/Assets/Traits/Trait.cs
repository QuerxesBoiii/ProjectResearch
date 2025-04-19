using UnityEngine;

public abstract class Trait : MonoBehaviour
{
    public abstract int Id { get; }
    public abstract void ApplyTrait(CreatureBehavior creature);
}