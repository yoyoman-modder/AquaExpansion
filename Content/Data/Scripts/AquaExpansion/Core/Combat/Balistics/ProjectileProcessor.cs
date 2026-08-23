using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRageMath;

namespace AquaExpansion.Core.Combat.Balistics
{
    public static class ProjectileProcessor
    {
        private static readonly double maxdistance = 2000.0;
        public static void Process(ref MyProjectileInfo projectile, ref MyProjectileHitInfo hit)
        {
            MyPlanet planet = CombatUtils.WaterPlanet(projectile.Position);
            if (planet == null)
                return;
            Vector3D origin = projectile.Position;
            Vector3D hitPosition = hit.HitPosition;
            Vector3D direction = Vector3D.Normalize(projectile.Velocity);
            WaterTrajectoryResult trajectory = WaterTrajectory.Calculate(planet,projectile.Position,direction,maxdistance);
            string hitInfo = CombatUtils.GetHitInfo(hit);
            // Visual effects
            CreateWaterEffects(ref projectile, trajectory);
            //aplly
            // Get ammo profile
            HydroAmmoProfile profile = HydroAmmoDatabase.Get(projectile.ProjectileAmmoDefinition.Id.SubtypeId.String)
               ?? HydroAmmoDatabase.DefaultProjectile();
            // Debug
            if (AquaExpansionSession.Insance.isModdingEnabled && AquaExpansionSession.Insance.isHydroModdingEnabled && AquaExpansionSession.Insance.LogsEnabled)
            {
                CombatUtils.LogProjectile(origin, hitPosition, projectile.Velocity.Length(), hit.Damage, hitInfo, trajectory);
            }
        }
        private static void CreateWaterEffects(ref MyProjectileInfo projectile, WaterTrajectoryResult trajectory)
        {
            if (trajectory.Type != WaterTrajectoryType.EnteredWater)
                return;
            //CombatUtils.CreateBulletSplash(trajectory.EntryPoint, projectile.Velocity);
        }
        public static void OnProjectileAdded(ref MyProjectileInfo projectile, int index)
        {
            MyPlanet planet = CombatUtils.WaterPlanet(projectile.Position);
            if (planet == null)
                return;
            // Find underwater profile for this ammo
            HydroAmmoProfile profile = HydroAmmoDatabase.Get(projectile.ProjectileAmmoDefinition.Id.SubtypeId.String)
               ?? HydroAmmoDatabase.DefaultProjectile();
            double speed = projectile.Velocity.Length();
            if (speed < 0.01)
                return;
            Vector3D origin = projectile.Position;
            Vector3D direction = Vector3D.Normalize(projectile.Velocity);
            WaterTrajectoryResult trajectory = WaterTrajectory.Calculate(planet,projectile.Position,direction,maxdistance);
            switch (trajectory.Type)
            {
                case WaterTrajectoryType.Air:
                    /*AquaExpansionSession.Insance.Log(true,
                    $"Projectile {index} ({trajectory.Type})");*/
                    return;
                case WaterTrajectoryType.EnteredWater:
                case WaterTrajectoryType.Underwater:
                case WaterTrajectoryType.ExitedWater:
                    /*AquaExpansionSession.Insance.Log(true,
                    $"Projectile {index} ({trajectory.Type})");*/
                    MyAPIGateway.Projectiles.MarkProjectileForDestroy(index);
                    AquaProjectileManager.Spawn(
                        index,
                        projectile.Position,
                        projectile.Velocity,
                        profile,
                        trajectory);
                    /*AquaExpansionSession.Insance.Log(true,
                    $"Projectile {index} replaced ({trajectory.Type})");*/
                    break;
            }
        }
        public static void Clear()
        {
            AquaProjectileManager.Clear();
        }
    }
}
    

