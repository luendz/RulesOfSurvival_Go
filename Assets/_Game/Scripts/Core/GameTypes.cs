namespace ROS.Game.Core
{
    public enum PlayerMovementState { Idle, Walking, Running, Sprinting, Crouching, Prone, Jumping, Falling, Parachuting, Driving, Dead }
    public enum PlayerCombatState { Unarmed, HipFire, Aiming, Reloading, Throwing, Healing }
    public enum MatchState { WaitingPlayers, Warmup, Plane, Playing, FinalCircle, Finished }
    public enum PlayerLifeState { Alive, Dead }
    public enum WeaponFireMode { Single, Burst, Auto }
    public enum ItemType { Weapon, Ammo, Healing, Armor, Helmet, Backpack, Throwable, Attachment, Misc }
    public enum LootRarity { Common, Uncommon, Rare, Epic }
    public enum LootPickupMode { Manual, Automatic, EquipOnPickup }
    public enum DataConfidence { Unknown, Prototype, Verified, Community, Estimated, Contradictory }
    public enum DamageType { Generic, Firearm, Explosion, Fall, SafeZone }
    public enum HitZone { None, Head, Torso, Arm, Leg }
    public enum ProtectionLevel { None, Level1, Level2, Level3 }
}
