namespace AquaExpansion.API
{
    public class HydroAmmoProfileData
    {
        public string SubtypeId;
        public float Mass;
        public float DragCoefficient;
        public AquaSplashType SplashType;
        public float MinimumSpeed;
        public float MaxRange;
        public float WaterStability;
        public float EnergyLossCoefficient;
        public float ProjectileMassDamage;
        public float ProjectileHealthDamage;
        public float ExitVelocityMultiplier;
        public float ExitDamageMultiplier;
        public string TrailEffect;
        public float UnderwaterTurnMultiplier;
        public float EngineAcceleration;
        public float UnderwaterEngineMultiplier;
        public int Torpedo;
    }
    public enum AquaSplashType
    {
        Bullet = 0,
        Missile = 1,
        Exit = 2
    }
}