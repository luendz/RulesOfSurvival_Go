namespace ROS.Game.Combat
{
    public interface IDamageable
    {
        bool IsAlive { get; }
        void ApplyDamage(DamageInfo damage);
    }
}
