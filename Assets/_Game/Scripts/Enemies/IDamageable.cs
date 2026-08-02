namespace DinnerRush
{
    /// <summary>Anything a projectile or contact can damage.</summary>
    public interface IDamageable
    {
        void TakeDamage(float amount);
    }
}
