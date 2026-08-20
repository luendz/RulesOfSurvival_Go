using ROS.Game.Combat;

namespace ROS.Game.BattleRoyale
{
    public readonly struct EliminationInfo
    {
        public EliminationInfo(
            Health victim,
            Health killer,
            DamageInfo damage,
            int placement
        )
        {
            Victim = victim;
            Killer = killer;
            Damage = damage;
            Placement = placement;
        }

        public Health Victim { get; }
        public Health Killer { get; }
        public DamageInfo Damage { get; }
        public int Placement { get; }
        public bool HasKiller =>
            Killer != null && Killer != Victim;
    }
}
