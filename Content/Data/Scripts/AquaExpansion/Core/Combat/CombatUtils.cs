using Jakaria.API;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Weapons;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRage.Voxels;
using VRageMath;
using VRageRender;

namespace AquaExpansion.Core.Combat
{
    public enum WaterTrajectoryType { Air, EnteredWater, ExitedWater, Underwater }
    public enum WaterIntersectionType { Entry, Exit }
    public enum SplashType { Bullet, Missile, Exit,Shell,Railgun }
    public enum AquaWeaponBlockType { SmallGatlingGun,SmallMissileLauncher, SmallMissileLauncherReload }
    public enum AquaSoundType { Bullet, Voxel,Character }
    /// <summary>
    /// WaterTrajectoryResult represents the result of a projecile's trajectory relative to water
    /// </summary>
    public struct WaterTrajectoryResult
    {
        /// <summary>
        /// Type of trajectory relative to water.
        /// </summary>
        public WaterTrajectoryType Type;
        /// <summary>
        /// Total projectile path length.
        /// </summary>
        public double TotalDistance;
        /// <summary>
        /// Distance traveled through air.
        /// </summary>
        public double AirDistance;
        /// <summary>
        /// Distance traveled through water.
        /// </summary>
        public double WaterDistance;
        /// <summary>
        /// Point where the projectile entered the water.
        /// Undefined if it never entered.
        /// </summary>
        public Vector3D EntryPoint;
        /// <summary>
        /// Point where the projectile exited the water.
        /// Undefined if it never exited.
        /// </summary>
        public Vector3D ExitPoint;
        /// <summary>
        /// Water Depth
        /// </summary>
        public float AverageDepth;
        /// <summary>
        /// Water Density
        /// </summary>
        public float WaterDensity;
        /// <summary>
        /// Impact velocity
        /// </summary>
        public double ImpactVelocity;
        public bool TouchesWater
        {
            get { return Type != WaterTrajectoryType.Air; }
        }
        public bool IsFullyUnderwater
        {
            get { return Type == WaterTrajectoryType.Underwater; }
        }
        public bool EnteredWater
        {
            get { return Type == WaterTrajectoryType.EnteredWater; }
        }
        public bool ExitedWater
        {
            get { return Type == WaterTrajectoryType.ExitedWater; }
        }
    }
    public static class CombatUtils
    {
        /// <summary>
        /// Log projectile hit info
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="hitPosition"></param>
        /// <param name="speed"></param>
        /// <param name="damage"></param>
        /// <param name="hitInfo"></param>
        /// <param name="trajectory"></param>
        public static void LogProjectile(Vector3D origin, Vector3D hitPosition, double speed, float damage, string hitInfo, WaterTrajectoryResult trajectory)
        {
            AquaExpansionSession.Insance.Log(
            true,
            string.Format(
                "Projectile Hit\n" +
                "Origin      : {0}\n" +
                "Hit         : {1}\n" +
                "Speed       : {2:0.00} m/s\n" +
                "Damage      : {3:0.00}\n" +
                "Target      : {4}\n" +
                "Water State : {5}\n" +
                "Air Dist    : {6:0.00} m\n" +
                "Water Dist  : {7:0.00} m\n" +
                "Total Dist  : {8:0.00} m",
                origin,
                hitPosition,
                speed,
                damage,
                hitInfo,
                trajectory.Type,
                trajectory.AirDistance,
                trajectory.WaterDistance,
                trajectory.TotalDistance));
        }
        /// <summary>
        /// Log AquaProjectile hit info
        /// </summary>
        /// <param name="aquaProjectile"></param>
        /// <param name="hitPosition"></param>
        /// <param name="hitInfo"></param>
        /// <param name="hit"></param>
        /// <param name="voxel"></param>
        public static void LogAquaProjectile(AquaProjectile aquaProjectile, Vector3D hitPosition, string hitInfo, IHitInfo hit)
        {
            AquaExpansionSession.Insance.Log(
            true,
            string.Format(
                "Aqua Projectile Hit\n" +
                "Origin      : {0}\n" +
                "Hit         : {1}\n" +
                "Speed       : {2:0.00} m/s\n" +
                "Drag        : {3:0.000}\n" +
                "EnergyLoss  : {4:0.00}\n"+
                "Damage      : {5:0.00}\n" +
                "Target      : {6}\n" +
                "Water State : {7}\n" +
                "Air Dist    : {8:0.00} m\n" +
                "Water Dist  : {9:0.00} m\n" +
                "Total Dist  : {10:0.00} m",
                aquaProjectile.Position,
                hitPosition,
                aquaProjectile.Velocity.Length(),
                aquaProjectile.Profile.DragCoefficient,
                aquaProjectile.Profile.EnergyLossCoefficient,
                LogDamage(aquaProjectile, hit),
                hitInfo,
                aquaProjectile.Trajectory.Type,
                aquaProjectile.Trajectory.AirDistance,
                aquaProjectile.Trajectory.WaterDistance,
                aquaProjectile.Trajectory.TotalDistance));
        }
        /// <summary>
        /// Log AquaProjectile underwater physics data
        /// </summary>
        /// <param name="type"></param>
        /// <param name="speed"></param>
        /// <param name="density"></param>
        /// <param name="drag"></param>
        /// <param name="dec"></param>
        /// <param name="damagemass"></param>
        /// <param name="damagehealth"></param>
        public static void LogAquaProjectileWaterPhysics(float mass,WaterTrajectoryType type, float water, float speed, float density, float drag, float dec, float damagemass, float damagehealth, float eloss)
        {
            AquaExpansionSession.Insance.Log(true,
           string.Format(
            "AquaProjectile Data\n" +
            "Mass          : {0:0.00}\n" +
            "Water State   : {1}\n" +
            "Water Dist    : {2:0.00} m\n" +
            "Speed         : {3:0.00} m/s\n" +
            "Density Mult  : {4:0.000}\n" +
            "Drag          : {5:0.000}\n" +
            "EnergyLoss    : {6:0.00}\n"+
            "Deceleration  : {7:0.000}\n" +
            "Damage Mass   : {8:0.00}\n" +
            "Damage Health : {9:0.00}",
            mass,
            type,
            water,
            speed,
            density,
            drag,
            eloss,
            dec,
            damagemass,
            damagehealth));
        }
        /// <summary>
        /// Log the damage of the AquaProjectile based on the hit entity type
        /// </summary>
        /// <param name="aquaProjectile"></param>
        /// <param name="hit"></param>
        /// <returns></returns>
        public static float LogDamage(AquaProjectile aquaProjectile, IHitInfo hit)
        {
            if (aquaProjectile == null || hit?.HitEntity == null)
                return 0f;
            if (hit.HitEntity is IMyCubeGrid)
                return aquaProjectile.CurrentMassDamage;
            if (hit.HitEntity is IMyCharacter)
                return aquaProjectile.CurrentHealthDamage;
            return 0f;
        }
        /// <summary>
        /// Log missile hit info
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="hitPosition"></param>
        /// <param name="speed"></param>
        /// <param name="damage"></param>
        /// <param name="hitInfo"></param>
        /// <param name="trajectory"></param>
        public static void LogMissile(Vector3D origin, Vector3D hitPosition, double speed, float damage, string hitInfo, WaterTrajectoryResult trajectory, float flytime)
        {
            AquaExpansionSession.Insance.Log(true,
            string.Format(
             "Missile Hit\n" +
             "Origin      : {0}\n" +
             "Hit         : {1}\n" +
             "Speed       : {2:0.00} m/s\n" +
             "Damage      : {3:0.00}\n" +
             "Target      : {4}\n" +
             "Water State : {5}\n" +
             "Air Dist    : {6:0.00} m\n" +
             "Water Dist  : {7:0.00} m\n" +
             "Total Dist  : {8:0.00} m\n" +
             "FlightTime  : {9:0.00} s",
             origin,
             hitPosition,
             speed,
             damage,
             hitInfo,
             trajectory.Type,
             trajectory.AirDistance,
             trajectory.WaterDistance,
             trajectory.TotalDistance,
             flytime));
        }
        /// <summary>
        /// Log destroyed missile
        /// </summary>
        /// <param name="type"></param>
        /// <param name="water"></param>
        /// <param name="speed"></param>
        /// <param name="flytime"></param>
        public static void LogKilledMissile(WaterTrajectoryType type, float water, float speed, float flytime)
        {
            AquaExpansionSession.Insance.Log(true,
            string.Format(
             "Missile Killed\n" +
             "Water State : {0}\n" +
             "Water Dist  : {1:0.00} m\n" +
             "Speed       : {2:0.00} m/s\n" +
             "FlightTime  : {3:0.00} s",
             type,
             water,
             speed,
             flytime));
        }
        /// <summary>
        /// Log Active missile
        /// </summary>
        /// <param name="type"></param>
        /// <param name="speed"></param>
        /// <param name="flytime"></param>
        public static void LogRunningMissile(WaterTrajectoryType type, float water, float speed, float flytime, float trailscale)
        {
            AquaExpansionSession.Insance.Log(true,
            string.Format(
             "Missile Run\n" +
             "Water State      : {0}\n" +
             "Water Dist       : {1:0.00} m\n" +
             "Speed            : {2:0.00} m/s\n" +
             "FlightTime       : {3:0.00} s\n" +
             "Main Trail scale : {4:0.000}\n",
             type,
             water,
             speed,
             flytime,
             trailscale));
        }
        /// <summary>
        /// Log Missile undewater physics data
        /// </summary>
        /// <param name="type"></param>
        /// <param name="speed"></param>
        /// <param name="waterdencity"></param>
        /// <param name="drag"></param>
        /// <param name="enginemult"></param>
        public static void LogMissileWaterPhysics(WaterTrajectoryType type, float speed, float waterdencity, float drag, float enginemult)
        {
            AquaExpansionSession.Insance.Log(true,
            string.Format(
             "Missile Physics\n" +
             "Water State  : {0}\n" +
             "Speed        : {1:0.00} m/s\n" +
             "Density Mult : {2:0.000}\n" +
             "Drag         : {3:0.000}\n" +
             "Engine Mult  : {4:0.000}",
             type,
             speed,
             waterdencity,
             drag,
             enginemult));
        }
        /// <summary>
        /// Debug line from origin to hit position
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="hitPosition"></param>
        public static void DebugLine(Vector3D origin, Vector3D hitPosition)
        {
            Vector4 color = Color.Red * 10f;
            Vector3D start = origin;
            Vector3D end = hitPosition;
            MySimpleObjectDraw.DrawLine(start, end, MyStringId.GetOrCompute("WeaponLaser"), ref color, 0.05f, MyBillboard.BlendTypeEnum.PostPP);
        }
        /// <summary>
        /// Debug line from start to end with specified color and thinkness
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="color"></param>
        /// <param name="thickness"></param>
        public static void DebugLine(Vector3D start, Vector3D end, Color color, float thickness = 0.15f)
        {
            Vector4 lineColor = color.ToVector4() * 10f;

            MySimpleObjectDraw.DrawLine(
                start,
                end,
                MyStringId.GetOrCompute("Square"),
                ref lineColor,
                thickness, MyBillboard.BlendTypeEnum.PostPP);
        }
        /// <summary>
        /// Debig Box at position with specified color and size
        /// </summary>
        /// <param name="position"></param>
        /// <param name="color"></param>
        /// <param name="size"></param>
        public static void DebugBox(Vector3D position, Color color, double size = 0.02)
        {
            Color c = color * 10f;
            MatrixD world = MatrixD.CreateTranslation(position);
            BoundingBoxD box = new BoundingBoxD(new Vector3D(-size), new Vector3D(size));
            MySimpleObjectDraw.DrawTransparentBox(
            ref world,
            ref box,
            ref c,
            MySimpleObjectRasterizer.Solid,
            1,
            0.02f,
            MyStringId.GetOrCompute("Square"),
            MyStringId.GetOrCompute("Square"),
            false,
            -1,
            MyBillboard.BlendTypeEnum.PostPP,
            1f);
        }
        /// <summary>
        /// Debug point at position with specified color and size
        /// </summary>
        /// <param name="position"></param>
        /// <param name="color"></param>
        /// <param name="size"></param>
        public static void DebugPoint(Vector3D position, Color color, double size)
        {
            MatrixD world = MatrixD.CreateTranslation(position);
            var c = color * 20f;
            BoundingBoxD box = new BoundingBoxD(new Vector3D(-size), new Vector3D(size));
            MySimpleObjectDraw.DrawTransparentBox(
            ref world,
            ref box,
            ref c,
            MySimpleObjectRasterizer.Solid,
            1,
            0.02f,
            MyStringId.GetOrCompute("Square"),
            MyStringId.GetOrCompute("Square"),
            false,
            -1,
            MyBillboard.BlendTypeEnum.PostPP,
            1f);
        }
        /// <summary>
        /// Debug Point for WaterTrajectoryResult, draws entry and exit point with different colors based on the trajectory type
        /// </summary>
        /// <param name="result"></param>
        public static void DebugPoint(WaterTrajectoryResult result)
        {
            switch (result.Type)
            {
                case WaterTrajectoryType.EnteredWater:
                    DebugBox(result.EntryPoint, Color.Blue);
                    break;
                case WaterTrajectoryType.ExitedWater:
                    DebugBox(result.ExitPoint, Color.Red);
                    break;
                case WaterTrajectoryType.Underwater:
                    DebugBox(result.EntryPoint, Color.Cyan);
                    DebugBox(result.ExitPoint, Color.Cyan);
                    break;
            }
        }
        /// <summary>
        /// Debug trajectory from origin to hit position, and draw entry and exit points based on the WaterTrajectoryResult type
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="hitPosition"></param>
        /// <param name="result"></param>
        public static void DebugTrajectory(Vector3D origin, Vector3D hitPosition, WaterTrajectoryResult result)
        {
            // Projectile trajectory
            DebugLine(origin, hitPosition);
            switch (result.Type)
            {
                case WaterTrajectoryType.EnteredWater:
                    DebugBox(result.EntryPoint, Color.Blue);
                    break;
                case WaterTrajectoryType.ExitedWater:
                    DebugBox(result.ExitPoint, Color.Red);
                    break;
                case WaterTrajectoryType.Underwater:
                    DebugBox(origin, Color.Cyan);
                    DebugBox(hitPosition, Color.Cyan);
                    break;
            }
        }
        /// <summary>
        /// Create a bullet splash effect at the given position and velocity, typically used when a bullet enters water
        /// </summary>
        /// <param name="position">The position where the splash should be created</param>
        /// <param name="velocity">The velocity of the entering bullet</param>
        public static void CreateBulletSplash(string subtype,Vector3D position, Vector3 velocity, SplashType type)
        {
            string impact = WaterImpactDatabase.Get(subtype, type, WaterIntersectionType.Entry);
            if (string.IsNullOrEmpty(subtype))
                impact = WaterImpactDatabase.DefaultWaterImpact(SplashType.Bullet, WaterIntersectionType.Entry);
            MyVisualScriptLogicProvider.CreateParticleEffectAtPosition(impact, position);
            MyVisualScriptLogicProvider.PlaySingleSoundAtPosition("JSplash", position);
        }
        /// <summary>
        /// Create a missile splash effect at the given position and velocity, typically used when a missile enters water   
        /// </summary>
        /// <param name="position">The position where the splash should be created</param>
        /// <param name="velocity">The velocity of the entering missile</param>
        public static void CreateMissileSplash(string subtype, Vector3D position, Vector3 velocity, SplashType type)
        {
            string impact = WaterImpactDatabase.Get(subtype, type, WaterIntersectionType.Entry);
            if (string.IsNullOrEmpty(subtype))
                impact = WaterImpactDatabase.DefaultWaterImpact(SplashType.Missile, WaterIntersectionType.Entry);
            MyVisualScriptLogicProvider.CreateParticleEffectAtPosition(impact, position);
            MyVisualScriptLogicProvider.PlaySingleSoundAtPosition("JSplash", position);
        }
        /// <summary>
        /// Create an exit splash effect at the given position and velocity, typically used when a projectile exits water
        /// </summary>
        /// <param name="position">The position where the splash should be created</param>
        /// <param name="velocity">The velocity of the exiting projectile</param>
        public static void CreateExitSplash(string subtype, Vector3D position, Vector3 velocity, SplashType type)
        {
            string impact = WaterImpactDatabase.Get(subtype, type, WaterIntersectionType.Exit);
            if (string.IsNullOrEmpty(subtype))
                impact = WaterImpactDatabase.DefaultWaterImpact(SplashType.Bullet, WaterIntersectionType.Exit);
            MyVisualScriptLogicProvider.CreateParticleEffectAtPosition(impact, position);
            MyVisualScriptLogicProvider.PlaySingleSoundAtPosition("JSplashUnderwater", position);
        }
        /// <summary>
        /// Create a missile exit splash effect at the given position and velocity, typically used when a missile exits water
        /// </summary>
        /// <param name="position">The position where the splash should be created</param>
        /// <param name="velocity">The velocity of the exiting missile</param>
        public static void CreateMissileExitSplash(string subtype, Vector3D position, Vector3 velocity, SplashType type)
        {
            string impact = WaterImpactDatabase.Get(subtype, type, WaterIntersectionType.Exit);
            if (string.IsNullOrEmpty(subtype))
                impact = WaterImpactDatabase.DefaultWaterImpact(SplashType.Missile, WaterIntersectionType.Exit);
            MyVisualScriptLogicProvider.CreateParticleEffectAtPosition(impact, position);
            MyVisualScriptLogicProvider.PlaySingleSoundAtPosition("JSplashUnderwater", position);
        }
        /// <summary>
        /// Create a bullet impact effect at the given position and velocity, typically used when a bullet hits a solid surface
        /// </summary>
        /// <param name="position">The position where the impact should be created</param>
        /// <param name="velocity">The velocity of the impacting bullet</param>
        public static void CreateBulletImpact(Vector3D position, Vector3 velocity, WaterTrajectoryType state)
        {
            MyVisualScriptLogicProvider.CreateParticleEffectAtPosition("MaterialHit_Metal", position);
            MyVisualScriptLogicProvider.PlaySingleSoundAtPosition(GetSoundFX(state, AquaSoundType.Bullet), position);
        }
        /// <summary>
        /// Create a character impact effect at the given position and velocity, typically used when a character is hit
        /// </summary>
        /// <param name="position">The position where the impact should be created</param>
        /// <param name="velocity">The velocity of the impacting character</param>
        public static void CreateCharacterImpact(Vector3D position, Vector3 velocity, string material,WaterTrajectoryType state)
        {
            MyVisualScriptLogicProvider.CreateParticleEffectAtPosition(material, position);
            MyVisualScriptLogicProvider.PlaySingleSoundAtPosition(GetSoundFX(state, AquaSoundType.Character), position);
        }
        /// <summary>
        /// Create a voxel impact effect at the given position and velocity, typically used when a vaoxel is hit
        /// </summary>
        /// <param name="position"></param>
        /// <param name="velocity"></param>
        /// <param name="material"></param>
        public static void CreateVoxelImpact(Vector3D position, Vector3 velocity, string material, WaterTrajectoryType state)
        {
            MyVisualScriptLogicProvider.CreateParticleEffectAtPosition(material, position);
            MyVisualScriptLogicProvider.PlaySingleSoundAtPosition(GetSoundFX(state, AquaSoundType.Voxel), position);
        }
        /// <summary>
        /// Get SoundFX
        /// </summary>
        /// <param name="waterstate"></param>
        /// <returns></returns>
        private static string GetSoundFX(WaterTrajectoryType waterstate, AquaSoundType soundType)
        {
            switch (soundType)
            {
                case AquaSoundType.Bullet:
                    switch (waterstate)
                    {
                        case WaterTrajectoryType.Air:
                            return "ArcWepShipGatlingImpMetal";
                        case WaterTrajectoryType.Underwater:
                            return "ArcWepShipGatlingImpMetal";
                    }
                    break;
                case AquaSoundType.Voxel:
                    switch (waterstate)
                    {
                        case WaterTrajectoryType.Air:
                            return "ArcWepShipGatlingImpRock";

                        case WaterTrajectoryType.Underwater:
                            return "ArcWepShipGatlingImpRock";
                    }
                    break;
                case AquaSoundType.Character:
                    switch (waterstate)
                    {
                        case WaterTrajectoryType.Air:
                            return "ArcWepShipGatlingImpGrass";
                        case WaterTrajectoryType.Underwater:
                            return "ArcWepShipGatlingImpGrass";
                    }
                    break;
            }
            return string.Empty;
        }
        /// <summary>
        /// Check if the given position is within a water planet and return the planet if so, otherwise return null
        /// </summary>
        /// <param name="origin">The position to check</param>
        /// <returns>The water planet if the position is within one, otherwise null</returns>
        public static MyPlanet WaterPlanet(Vector3D origin)
        {
            MyPlanet planet = MyGamePruningStructure.GetClosestPlanet(origin);
            if (planet == null || !WaterModAPI.HasWater(planet))
                return null;
            return planet;
        }
        /// <summary>
        /// Get missile target info
        /// </summary>
        /// <param name="missile"></param>
        /// <returns></returns>
        public static string GetTargetInfo(IMyMissile missile)
        {
            if (missile.CollidedEntity == null)
                return "None";
            IMyCubeGrid grid = missile.CollidedEntity as IMyCubeGrid;
            if (grid != null)
            {
                Vector3I cell = grid.WorldToGridInteger(missile.CollisionPoint ?? missile.PositionComp.GetPosition());
                IMySlimBlock slim = grid.GetCubeBlock(cell);
                if (slim != null)
                {
                    return string.Format(
                        "Grid: {0} | Block: {1}",
                        grid.DisplayName,
                        slim.BlockDefinition.Id.SubtypeName);
                }
                return string.Format(
                    "Grid: {0}",
                    grid.DisplayName);
            }
            return string.Format(
               "{0} ({1})",
               missile.CollidedEntity.DisplayName,
               missile.CollidedEntity.GetType().Name);
        }
        /// <summary>
        /// Get Projectile hit info
        /// </summary>
        /// <param name="hit"></param>
        /// <returns></returns>
        public static string GetHitInfo(MyProjectileHitInfo hit)
        {
            if (hit.HitEntity == null)
                return "None";
            IMyCubeGrid grid = hit.HitEntity as IMyCubeGrid;
            if (grid != null)
            {
                Vector3I cell = grid.WorldToGridInteger(hit.HitPosition);
                IMySlimBlock slim = grid.GetCubeBlock(cell);
                if (slim != null)
                {
                    return string.Format(
                        "Grid: {0} | Block: {1}",
                        grid.DisplayName,
                        slim.BlockDefinition.Id.SubtypeName);
                }
                return string.Format(
                    "Grid: {0}",
                    grid.DisplayName);
            }
            return string.Format(
                "{0} ({1})",
                hit.HitEntity.DisplayName,
                hit.HitEntity.GetType().Name);
        }
        /// <summary>
        /// Hit info returns a string
        /// </summary>
        /// <param name="hit"></param>
        /// <returns></returns>
        public static string GetHitInfo(IHitInfo hit)
        {
            if (hit.HitEntity == null)
                return "None";
            IMyCubeGrid grid = hit.HitEntity as IMyCubeGrid;
            if (grid != null)
            {
                Vector3I cell = grid.WorldToGridInteger(hit.Position);
                IMySlimBlock slim = grid.GetCubeBlock(cell);
                if (slim != null)
                {
                    return string.Format(
                        "Grid: {0} | Block: {1}",
                        grid.DisplayName,
                        slim.BlockDefinition.Id.SubtypeName);
                }
                return string.Format(
                    "Grid: {0}",
                    grid.DisplayName);
            }
            return string.Format(
                "{0} ({1})",
                hit.HitEntity.DisplayName,
                hit.HitEntity.GetType().Name);
        }
        /// <summary>
        /// Get voxel material at the given world position, returns 0 if no voxel is present or if the voxel is null
        /// </summary>
        /// <param name="voxel"></param>
        /// <param name="worldPosition"></param>
        /// <returns></returns>
        public static byte GetVoxelMaterialID(IMyVoxelBase voxel, Vector3D worldPosition)
        {
            if (voxel == null)
                return 0;
            // Convert world position to voxel coordinates
            Vector3D localPos = worldPosition - voxel.PositionLeftBottomCorner;
            Vector3I voxelCoord = Vector3I.Round(localPos);
            MyStorageData data = new MyStorageData();
            data.Resize(Vector3I.Zero, Vector3I.Zero);
            voxel.Storage.ReadRange(
                data,
                MyStorageDataTypeFlags.Material,
                0,
                voxelCoord,
                voxelCoord);
            return data.Material(0);
        }
        /// <summary>
        /// Get the voxel material subtype ID
        /// </summary>
        /// <param name="voxelBase"></param>
        /// <param name="hit"></param>
        /// <returns></returns>
        public static string GetVoxelMaterialSubtypeid(IMyVoxelBase voxelBase, IHitInfo hit)
        {
            if (voxelBase == null || hit?.HitEntity == null)
                return null;
            byte material = GetVoxelMaterialID(voxelBase, hit.Position);
            MyVoxelMaterialDefinition def = MyDefinitionManager.Static.GetVoxelMaterialDefinition(material);
            if (def != null)
                return def.MaterialTypeName;
            return null;
        }
        /// <summary>
        /// Create projectile Trail Effect
        /// </summary>
        /// <param name="projectile"></param>
        /// <param name="moved"></param>
        /// <param name="speed"></param>
        public static void CreateProjectileTrail(AquaProjectile projectile, float moved, float speed)
        {
            if (projectile == null ||
                !projectile.Alive)
                return;
            projectile.BubbleDistance += moved;
            float spacing = MathHelper.Lerp(0.5f,1.0f,MathHelper.Clamp(projectile.Profile.Mass / 20f,0f,1f));
            spacing *= MathHelper.Lerp(0.8f,1.2f,MathHelper.Clamp(speed / 800f,0f,1f));
            Vector3D segment =
                projectile.Position -
                projectile.PreviousPosition;
            double segmentLength = segment.Length();
            if (segmentLength < 1e-6)
                return;
            Vector3D direction =
                segment / segmentLength;
            while (projectile.BubbleDistance >= spacing)
            {
                projectile.BubbleDistance -= spacing;

                double distanceAlongSegment =
                    segmentLength -
                    projectile.BubbleDistance;

                Vector3D spawnPosition =
                    projectile.PreviousPosition; /*+
                    direction * distanceAlongSegment;*/
                MyVisualScriptLogicProvider.CreateParticleEffectAtPosition(projectile.Profile.TrailEffect, spawnPosition);
            }
        }
        /// <summary>
        /// Update missike trails
        /// </summary>
        /// <param name="missile"></param>
        /// <param name="state"></param>
        public static void UpdateMissileTrail(IMyMissile missile, MissileState state)
        {
            if (missile == null || missile.ParticleEffect == null ||
                state == null || state.Profile == null)
                return;
            float targetScale = GetTorpedoTrailScale(state.Profile.Torpedo, state.WaterState);
            // Normal missile gets distance-based fading underwater
            if (state.Profile.Torpedo == 0 &&
                state.WaterState == WaterTrajectoryType.Underwater)
            {
                targetScale = GetUnderwaterTrailScale(state.WaterDistance, state.Profile.MaxRange);
            }
            if (Math.Abs(missile.ParticleEffect.UserScale - targetScale) > 0.01f)
            {
                missile.ParticleEffect.UserScale = targetScale;
            }
        }
        /// <summary>
        /// Get Missile trail scale
        /// </summary>
        /// <param name="underwaterDistance"></param>
        /// <param name="fadeDistance"></param>
        /// <returns></returns>
        private static float GetUnderwaterTrailScale(float underwaterDistance, float fadeDistance)
        {
            if (underwaterDistance <= 0f)
                return 1f;
            if (underwaterDistance >= fadeDistance)
                return 0f;
            float t = underwaterDistance / fadeDistance;
            // Smooth fade
            t = t * t * (3f - 2f * t);
            return 1f - t;
        }
        /// <summary>
        /// Get torpedo mode air trail scale
        /// </summary>
        /// <param name="torpedo"></param>
        /// <returns></returns>
        private static float GetTorpedoTrailScale(int torpedo, WaterTrajectoryType waterState)
        {
            if (torpedo == 1)
            {
                // Torpedo engine trail only underwater
                return waterState == WaterTrajectoryType.Underwater
                    ? 1f
                    : 0f;
            }
            // Normal missile trail
            return waterState == WaterTrajectoryType.Underwater
                ? 0f
                : 1f;
        }
        //debug chat
        /// <summary>
        /// Get float from chat input
        /// </summary>
        /// <param name="args"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static bool TryGetFloat(string[] args, int index, out float value)
        {
            value = 0f;
            if (args.Length <= index)
                return false;
            return float.TryParse(args[index], out value);
        }
        /// <summary>
        /// Get string input from chat input
        /// </summary>
        /// <param name="args"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static bool TryGetString(string[] args, int index, out string value)
        {
            value = null;
            if (args == null || args.Length <= index)
                return false;
            value = args[index];
            return true;
        }
        /// <summary>
        /// Get int from chat input
        /// </summary>
        /// <param name="args"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static bool TryGetInt(string[] args, int index, out int value)
        {
            value = 0;
            if (args == null || args.Length <= index)
                return false;
            return int.TryParse(args[index], out value);
        }
        /// <summary>
        /// Save  runtime profile
        /// </summary>
        /// <param name="p"></param>
        private static void SaveAmmoProfile(HydroAmmoProfile p)
        {
            string text = string.Format(
            @"new HydroAmmoProfile(
            ""{0}"",
            {1}f,
            {2}f,
            SplashType.{3},
            {4}f,
            {5}f,
            {6}f,
            {7}f,
            {8}f,
            {9}f,
            {10}f,
            {11}f,
            ""{12}"",
            {13}f,
            {14}f,
            {15}f,
            {16});",
                p.SubtypeId,
                p.Mass,
                p.DragCoefficient,
                p.SplashType,
                p.MinimumSpeed,
                p.MaxRange,
                p.WaterStability,
                p.EnergyLossCoefficient,
                p.ProjectileMassDamage,
                p.ProjectileHealthDamage,
                p.ExitVelocityMultiplier,
                p.ExitDamageMultiplier,
                p.TrailEffect,
                p.UnderwaterTurnMultiplier,
                p.EngineAcceleration,
                p.UnderwaterEngineMultiplier,
                p.Torpedo);
            MyAPIGateway.Utilities.ShowMissionScreen(
                "HydroAmmoProfile",
                "",
                "",
                text,
                null,
                "Close");
        }
        /// <summary>
        /// Show  HydroAmmo profile by subtype
        /// </summary>
        /// <param name="p"></param>
        private static void ShowAmmoProfile(HydroAmmoProfile p)
        {
            MyAPIGateway.Utilities.ShowMessage(
                AquaExpansionSession.Insance.AquaAPI,
                string.Format(@"{0}
                Mass={1}
                Drag={2}
                MinSpeed={3}
                Range={4}
                Stability={5}
                EnergyLoss={6}
                Engine={7}
                EngineMult={8}
                Turn={9}
                MassDmg={10}
                HealthDmg={11}
                Trail={12}
                Torpedo{13}",
                p.SubtypeId,
                p.Mass,
                p.DragCoefficient,
                p.MinimumSpeed,
                p.MaxRange,
                p.WaterStability,
                p.EnergyLossCoefficient,
                p.EngineAcceleration,
                p.UnderwaterEngineMultiplier,
                p.UnderwaterTurnMultiplier,
                p.ProjectileMassDamage,
                p.ProjectileHealthDamage,
                p.TrailEffect,
                p.Torpedo));
        }
        /// <summary>
        /// Show chat commands
        /// </summary>
        public static void ShowAmmoHelp()
        {
            MyAPIGateway.Utilities.ShowMissionScreen(
                "Hydro Ammo Runtime Commands",
                "",
                "",
                @"GLOBAL
                /hammo help
                /hammo clear
                /hammo log
                /hammo list
                /hammo vis

                PROFILE
                /hammo <SubtypeId> show
                /hammo <SubtypeId> save
                /hammo <SubtypeId> reset

                PHYSICS
                /hammo <SubtypeId> mass <value>
                /hammo <SubtypeId> drag <value>
                /hammo <SubtypeId> minspeed <value>
                /hammo <SubtypeId> energy <value>
                /hammo <SubtypeId> stability <value>

                MISSILES
                /hammo <SubtypeId> engine <value>
                /hammo <SubtypeId> enginemult <value>
                /hammo <SubtypeId> turn <value>

                DAMAGE
                /hammo <SubtypeId> massdamage <value>
                /hammo <SubtypeId> healthdamage <value>
                /hammo <SubtypeId> exitvelocity <value>
                /hammo <SubtypeId> exitdamage <value>

                VISUAL
                /hammo <SubtypeId> range <value>
                /hammo <SubtypeId> trail <value>
                /hammo <SubtypeId> torpedo <value>

                EXAMPLES
                /hammo DefaultMissile show
                /hammo DefaultMissile drag 5
                /hammo DefaultMissile engine 25
                /hammo DefaultMissile save
                /hammo DefaultMissile reset
                /hammo clear",
                null,
                "Close");
        }
        /// <summary>
        /// Show all registered ammo profiles
        /// </summary>
        public static void ListAllAmmo()
        {
            StringBuilder text = new StringBuilder();
            foreach (var profile in HydroAmmoDatabase.GetAllProfiles())
            {
                if (profile == null ||
                    string.IsNullOrWhiteSpace(profile.SubtypeId))
                    continue;
                text.AppendLine(profile.SubtypeId);
            }
            if (text.Length == 0)
                text.AppendLine("No registered Hydro Ammo profiles.");
            MyAPIGateway.Utilities.ShowMissionScreen(
                "Registered Hydro Ammo profiles",
                "",
                "",
                text.ToString(),
                null,
                "Close");
        }
        /// <summary>
        /// Run  runtime Ammo command
        /// </summary>
        /// <param name="args"></param>
        public static void ExecuteAmmoCommand(string[] args)
        {
            string subtype = args[1];
            string command = args[2].ToLower();
            HydroAmmoProfile profile;
            float value;
            string strvalue;
            int intvalue;
            switch (command)
            {
                case "show":
                    profile = HydroAmmoDatabase.Get(subtype);
                    if (profile == null)
                    {
                        MyAPIGateway.Utilities.ShowMessage(
                            AquaExpansionSession.Insance.AquaAPI,
                            AquaModdingNamesDatabase.GetModCommandByID(3) + subtype);
                        return;
                    }
                    ShowAmmoProfile(profile);
                    return;
                case "reset":
                    HydroAmmoDatabase.RuntimeProfiles.Remove(subtype);

                    MyAPIGateway.Utilities.ShowMessage(
                        AquaExpansionSession.Insance.AquaAPI,
                        subtype + AquaModdingNamesDatabase.GetModCommandByID(4));

                    return;
                case "save":
                    profile = HydroAmmoDatabase.Get(subtype);
                    if (profile == null)
                    {
                        MyAPIGateway.Utilities.ShowMessage(
                            AquaExpansionSession.Insance.AquaAPI,
                            AquaModdingNamesDatabase.GetModCommandByID(3) + subtype);
                        return;
                    }
                    SaveAmmoProfile(profile);
                    return;
            }
            // Editing commands use runtime profile
            profile = HydroAmmoDatabase.GetRuntime(subtype);
            if (profile == null)
            {
                MyAPIGateway.Utilities.ShowMessage(
                    AquaExpansionSession.Insance.AquaAPI,
                    AquaModdingNamesDatabase.GetModCommandByID(3) + subtype);
                return;
            }
            switch (command)
            {
                case "mass":
                    if (TryGetFloat(args, 3, out value))
                        profile.Mass = value;
                    break;
                case "drag":
                    if (TryGetFloat(args, 3, out value))
                        profile.DragCoefficient = value;
                    break;
                case "engine":
                    if (TryGetFloat(args, 3, out value))
                        profile.EngineAcceleration = value;
                    break;
                case "enginemult":
                    if (TryGetFloat(args, 3, out value))
                        profile.UnderwaterEngineMultiplier = value;
                    break;
                case "turn":
                    if (TryGetFloat(args, 3, out value))
                        profile.UnderwaterTurnMultiplier = value;
                    break;
                case "minspeed":
                    if (TryGetFloat(args, 3, out value))
                        profile.MinimumSpeed = value;
                    break;
                case "range":
                    if (TryGetFloat(args, 3, out value))
                        profile.MaxRange = value;
                    break;
                case "stability":
                    if (TryGetFloat(args, 3, out value))
                        profile.WaterStability = value;
                    break;
                case "massdamage":
                    if (TryGetFloat(args, 3, out value))
                        profile.ProjectileMassDamage = value;
                    break;
                case "healthdamage":
                    if (TryGetFloat(args, 3, out value))
                        profile.ProjectileHealthDamage = value;
                    break;
                case "exitvelocity":
                    if (TryGetFloat(args, 3, out value))
                        profile.ExitVelocityMultiplier = value;
                    break;
                case "exitdamage":
                    if (TryGetFloat(args, 3, out value))
                        profile.ExitDamageMultiplier = value;
                    break;
                case "trail":

                    if (TryGetString(args, 3, out strvalue))
                        profile.TrailEffect = strvalue;
                    break;
                case "torpedo":
                    {
                        if (TryGetInt(args, 3, out intvalue))
                            profile.Torpedo = intvalue != 0 ? 1 : 0;
                        break;
                    }
                case "energy":
                    {
                        if (TryGetFloat(args, 3, out value))
                            profile.EnergyLossCoefficient = value;
                        break;
                    }
                default:
                    ShowAmmoHelp();
                    return;
            }
            MyAPIGateway.Utilities.ShowMessage(
                AquaExpansionSession.Insance.AquaAPI,
                string.Format("{0}: {1} = updated.", subtype, command));
        }
        /// <summary>
        /// Crate Missile trail effect
        /// </summary>
        /// <param name="missile"></param>
        /// <param name="state"></param>
        /// <param name="scale"></param>
        /// <param name="effects"></param>
        public static void CreateMissileEffect(IMyMissile missile, MissileState state, float scale, Dictionary<long, MyParticleEffect> effects)
        {
            if (missile == null || missile.Closed || missile.MarkedForClose)
                return;
            if (state == null || state.Profile == null)
                return;
            // Prevent duplicate effects
            if (effects.ContainsKey(missile.EntityId))
                return;
            string effectName = state.Profile.TrailEffect;
            if (string.IsNullOrWhiteSpace(effectName))
                return;
            MatrixD matrix = MatrixD.Identity;
            Vector3D position = missile.GetPosition();
            MyParticleEffect effect;
            int keepXFramesAhead = MyAPIGateway.Session.IsServer ? 0 : 1;
            if (MyParticlesManager.TryCreateParticleEffect(
                effectName,
                ref matrix,
                ref position,
                missile.Render.GetRenderObjectID(),
                out effect,
                keepXFramesAhead))
            {
                effect.Autodelete = false;
                effect.UserScale = scale;
                effects.Add(missile.EntityId, effect);
                //AquaExpansionSession.Insance.Log(true,"Added missile underwater effect: " + effectName);
            }
            else
            {
                //AquaExpansionSession.Insance.Log(true,"Failed to create missile underwater effect: " + effectName);
            }
        }
        /// <summary>
        /// Stop Missile Trail Effect
        /// </summary>
        /// <param name="missile"></param>
        /// <param name="effects"></param>
        public static void StopMissileEffect(IMyMissile missile, Dictionary<long, MyParticleEffect> effects)
        {
            if (missile == null || effects == null)
                return;
            MyParticleEffect effect;
            if (!effects.TryGetValue(missile.EntityId, out effect))
                return;
            // Remove from dictionary first
            effects.Remove(missile.EntityId);
            if (effect == null)
                return;
            effect.Stop(true);
            effect.Autodelete = true;
            MyParticlesManager.RemoveParticleEffect(effect);
            //AquaExpansionSession.Insance.Log(true,"Stopped and deleted missile underwater effect.");
        }
        /// <summary>
        /// Create a HandWeapon Effect
        /// </summary>
        /// <param name="weapon"></param>
        /// <param name="matrix"></param>
        /// <param name="effectName"></param>
        /// <param name="scale"></param>
        public static void CreateHandWeaponEffect(IMyAutomaticRifleGun weapon,MatrixD matrix,string effectName,float scale)
        {
            if (weapon == null ||
                 weapon.Closed ||
                 weapon.MarkedForClose)
                return;
            Vector3D position = matrix.Translation;
            MatrixD identity = weapon.GunBase.GetMuzzleLocalMatrix();
            MyParticleEffect effect;
            int keepXFramesAhead = MyAPIGateway.Session.IsServer ? 0 : 1;
            bool created =
                MyParticlesManager.TryCreateParticleEffect(
                    effectName,
                    ref identity,
                    ref position,
                    weapon.Render.GetRenderObjectID(),
                    out effect,
                    keepXFramesAhead);
            /*AquaExpansionSession.Insance.Log(
                true,
                $"Rifle particle created={created} " +
                $"position={position}");*/
            if (!created || effect == null)
                return;
            effect.Autodelete = true;
            effect.UserScale = scale;
        }
        /// <summary>
        /// Stop HandWeapon Effect
        /// </summary>
        /// <param name="weaponId"></param>
        /// <param name="effects"></param>
        public static void StopHandWeaponEffect(long weaponId, Dictionary<long, MyParticleEffect> effects)
        {
            if (effects == null)
                return;
            MyParticleEffect effect;
            if (!effects.TryGetValue(weaponId, out effect))
                return;
            // Remove first so the effect is no longer tracked.
            effects.Remove(weaponId);
            if (effect == null)
                return;
            effect.Stop(true);
            effect.Autodelete = true;
            MyParticlesManager.RemoveParticleEffect(effect);
            //AquaExpansionSession.Insance.Log(true, "Stopped and deleted hand weapon underwater effect.");
        }
        /// <summary>
        /// LoadWeapon Deffinition
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        public static MyWeaponDefinition LoadWeaponDefinition(IMyTerminalBlock block)
        {
            if (block == null ||
                block.Closed ||
                block.MarkedForClose)
                return null;
            var weapondef = block.SlimBlock.BlockDefinition as MyWeaponBlockDefinition;
            if (weapondef == null)
                return null;
            var weapon = MyDefinitionManager.Static.GetWeaponDefinition(weapondef.WeaponDefinitionId);
            if (weapon == null)
            {
                AquaExpansionSession.Insance.Log(
                    true,
                    $"Weapon definition not found: " +
                    weapondef.WeaponDefinitionId);

                return null;
            }
            return weapon;
        }
        /// <summary>
        /// Create Weapon Effect
        /// </summary>
        /// <param name="weapon"></param>
        /// <param name="matrix"></param>
        /// <param name="effectName"></param>
        /// <param name="scale"></param>
        public static void CreateWeaponEffect(IMySmallGatlingGun weapon, MatrixD matrix,Vector3D pos,string effectName, float scale)
        {
            if (weapon == null ||
                 weapon.Closed ||
                 weapon.MarkedForClose)
                return;
            Vector3D position = pos;
            MatrixD identity = matrix;
            MyParticleEffect effect;
            int keepXFramesAhead = MyAPIGateway.Session.IsServer ? 0 : 1;
            bool created =
                 MyParticlesManager.TryCreateParticleEffect(
                    effectName,
                    ref identity,
                    ref position,
                    weapon.Render.GetRenderObjectID(),
                    out effect,
                    keepXFramesAhead);
            /*AquaExpansionSession.Insance.Log(
                true,
                $"Rifle particle created={created} " +
                $"position={position}");*/
            if (!created || effect == null)
                return;
            effect.Autodelete = true;
            effect.UserScale = scale;
        }
        /// <summary>
        /// Stop Weapon Effect
        /// </summary>
        /// <param name="weaponId"></param>
        /// <param name="effects"></param>
        public static void StopWeaponEffect(long weaponId, Dictionary<long, MyParticleEffect> effects)
        {
            if (effects == null)
                return;
            MyParticleEffect effect;
            if (!effects.TryGetValue(weaponId, out effect))
                return;
            // Remove first so the effect is no longer tracked.
            effects.Remove(weaponId);
            if (effect == null)
                return;
            effect.Stop(true);
            effect.Autodelete = true;
            MyParticlesManager.RemoveParticleEffect(effect);
        }
        private static void DrawMissileMass(IMyMissile missile, MissileState state)
        {
            float speed = missile.Physics.LinearVelocity.Length();
            Vector4 color = new Vector4(0.95f, 0.98f, 1f, 0.9f);
            float radius = MathHelper.Clamp(
                state.Profile.Mass * 0.003f,
                0.015f,
                0.08f);
            MyTransparentGeometry.AddPointBillboard(
                MyStringId.GetOrCompute("WhiteDot"),
                color,
                missile.GetPosition(),
                radius,
                0);
        }
        private static void DrawMissileWake(IMyMissile missile, MissileState state)
        {
            if (missile == null ||
                missile.Physics == null)
                return;
            Vector3 velocity = missile.Physics.LinearVelocity;
            float speed = velocity.Length();
            if (speed < state.Profile.MinimumSpeed)
                return;
            Vector3D dir = Vector3D.Normalize((Vector3D)velocity);
            float length =
                MathHelper.Clamp(
                    speed * 0.008f,
                    0.25f,
                    4.0f);
            float thickness =
                MathHelper.Clamp(
                    state.Profile.Mass * 0.002f,
                    0.015f,
                    0.08f);
            Vector4 color =
                new Vector4(
                    0.85f,
                    0.95f,
                    1.0f,
                    0.25f);
            MyTransparentGeometry.AddLineBillboard(
                MyStringId.GetOrCompute("WeaponLaser"),
                color,
                missile.GetPosition(),
                -dir,
                length,
                thickness,
                MyBillboard.BlendTypeEnum.AdditiveTop);
        }
        public static void VisualizeMissile(IMyMissile missile, MissileState state)
        {
            DrawMissileMass(missile,state);
            DrawMissileWake(missile,state);
        }
    }
    /// <summary>
    /// MissileState tracks the state of a missile in the game world, including its last collision point, whether it is underwater, and its water trajectory type
    /// </summary>
    public class MissileState
    {
        public IMyMissile Missile;
        public WaterTrajectoryType WaterState;
        public Vector3D PreviousPosition;
        public float WaterDistance;
        public float BubbleDistance;
        public Vector3D? LastCollision;
        public HydroAmmoProfile Profile;
        public float FlyTime;
        public Vector3D LastVelocity;
        public float LastSpeed;
    }
    /// <summary>
    /// AquaProjectile represents a projectile in the game world, tracking its position, velocity, damage, and water interaction state
    /// </summary>
    public class AquaProjectile
    {
        public long Id;
        public long SourceProjectileId;
        public Vector3D Position;
        public Vector3D PreviousPosition;
        public Vector3 Velocity;
        public HydroAmmoProfile Profile;
        public WaterTrajectoryResult Trajectory;
        public SplashType SplashType;
        public bool SplashEnterCreated;
        public bool SplashExitCreated;
        public bool Alive;
        public double AirDistance;
        public float WaterDistance;
        public float TravelDistance;
        public float CurrentMassDamage;
        public float CurrentHealthDamage;
        public float LifeTime;
        public float BubbleDistance;
    }
    /// <summary>
    /// Hydrodynamic ammo profile defines the properies of a projectile when interacting with water, including mass, drag coefficient, and splash type
    /// </summary>
    public class HydroAmmoProfile
    {
        public string SubtypeId;
        public float Mass;
        public float DragCoefficient;
        public SplashType SplashType;
        public float MinimumSpeed;
        public float MaxRange;
        public float WaterStability;
        public float EnergyLossCoefficient;
        public float ProjectileMassDamage;
        public float ProjectileHealthDamage;
        public float ExitVelocityMultiplier;
        public float ExitDamageMultiplier;
        public string TrailEffect;
        public float UnderwaterTurnMultiplier = 1f;
        public float EngineAcceleration;
        public float UnderwaterEngineMultiplier;
        public int Torpedo;
        public HydroAmmoProfile(string subtypeId, float mass, float drag, SplashType splashType, float minimumSpeed, 
            float maxRange,float waterStability, float energyLoss, float massDamage, float healthDamage,
            float exivelocitymult, float exitdamagemult, string traileffect, float turnrate, float engineaccel, float enginemult, int torp)
        {
            SubtypeId = subtypeId;
            Mass = mass;
            DragCoefficient = drag;
            SplashType = splashType;
            MinimumSpeed = minimumSpeed;
            MaxRange = maxRange;
            WaterStability = waterStability;
            EnergyLossCoefficient = energyLoss;
            ProjectileMassDamage = massDamage;
            ProjectileHealthDamage = healthDamage;
            ExitVelocityMultiplier = exivelocitymult;
            ExitDamageMultiplier = exitdamagemult;
            TrailEffect = traileffect;
            UnderwaterTurnMultiplier = turnrate;
            EngineAcceleration = engineaccel;
            UnderwaterEngineMultiplier = enginemult;
            Torpedo = torp;
        }
        public HydroAmmoProfile Clone()
        {
            return new HydroAmmoProfile(
                SubtypeId,
                Mass,
                DragCoefficient,
                SplashType,
                MinimumSpeed,
                MaxRange,
                WaterStability,
                EnergyLossCoefficient,
                ProjectileMassDamage,
                ProjectileHealthDamage,
                ExitVelocityMultiplier,
                ExitDamageMultiplier,
                TrailEffect,
                UnderwaterTurnMultiplier,
                EngineAcceleration,
                UnderwaterEngineMultiplier,
                Torpedo);
        }
    }
    /// <summary>
    /// HydroAmmoDatabase manages a collection of hydrodynamic ammo profiles, allowing for registration and retrieval of profiles by subtype ID
    /// </summary>
    public static class HydroAmmoDatabase
    {
        private static readonly Dictionary<string, HydroAmmoProfile> Profiles = new Dictionary<string, HydroAmmoProfile>();
        public static readonly Dictionary<string, HydroAmmoProfile> RuntimeProfiles = new Dictionary<string, HydroAmmoProfile>();
        public static void Init()
        {
            // pistol
            Register(new HydroAmmoProfile(
                "PistolCaliber",         // subtype
                  1.0f,                  // Mass
                  0.050f,                 // DragCoefficient
                  SplashType.Bullet,     // Splash
                  35f,                   // MinimumSpeed (m/s)
                  5f,                   // MaxRange (m)
                  0.35f,                 // WaterStability
                  2.2f,                  // EnergyLossCoefficient
                  10f,                   // ProjectileMassDamage
                  21f,                   // ProjectileHealthDamage
                  0.88f,                 //ExitVelocityMultiplier
                  0.75f,                 //ExitDamageMultiplier
                  "AquaBulletTrailSmall",//Traileffect 
                  0f,                    //EngineTurn 
                  0f,                    //Engine Acceleration
                  0f,                    //Engine Multiplier
                  0));                   //Torpedo mode
            // small
            Register(new HydroAmmoProfile(
                "SmallCaliber", 
                2.2f, 
                0.022f, 
                SplashType.Bullet,
                45f,
                10f,
                0.55f,
                1.20f,
                18f,
                34f,
                0.92f,
                0.85f,
                "AquaBulletTrailSmall",
                0f,
                0f,
                0f,
                0));
            // large
            Register(new HydroAmmoProfile(
                "LargeCaliber",
                5f,
                0.008f,
                SplashType.Bullet,
                70f,
                50f,
                0.92f,
                0.75f,
                55f,
                28f,
                0.97f,
                0.96f,
                "AquaBulletTrailSmall",
                0f,
                0f,
                0f,
                0));
            // autocannon
            Register(new HydroAmmoProfile(
                "AutocannonShell",
                15f,
                0.005f,
                SplashType.Bullet,
                80f,
                70f,
                0.98f,
                0.55f,
                220f,
                65f,
                0.985f,
                0.98f,
                "AquaBulletTrailSmall",
                0f,
                0f,
                0f,
                0));
            //missile
            Register(new HydroAmmoProfile(
               "Missile",
                45f,
                0.05f,
                SplashType.Missile,
                20f,
                10f,
                0f,
                0f,
                0f,
                0f,
                0f,
                0f,
                "AquaMissileTrail",
                0.40f,
                0.1f, //engine acceleration
                0.10f,
                0));
            // flare
            Register(new HydroAmmoProfile(
                "Flare",
                1f,
                0.05f,
                SplashType.Missile,
                16f,
                50f,
                0f,
                0f,
                0f,
                0f,
                0f,
                0f,
                "AquaFlaregunTrail",
                0.40f,
                0.1f, 
                0.10f,
                0));
            // fireworks
            Register(new HydroAmmoProfile(
                "FireworkBlue",
                1f,
                0.05f,
                SplashType.Missile,
                18f,
                50f,
                0f,
                0f,
                0f,
                0f,
                0f,
                0f,
                "AquaMissileTrail",
                0.40f,
                0.1f,
                0.10f,
                0));
            Register(new HydroAmmoProfile(
                "FireworkGreen",
                1f,
                0.05f,
                SplashType.Missile,
                18f,
                50f,
                0f,
                0f,
                0f,
                0f,
                0f,
                0f,
                "AquaMissileTrail",
                0.40f,
                0.1f,
                0.10f,
                0));
            Register(new HydroAmmoProfile(
               "FireworkRed",
               1f,
               0.05f,
               SplashType.Missile,
               18f,
               50f,
               0f,
               0f,
               0f,
               0f,
               0f,
               0f,
               "AquaMissileTrail",
               0.40f,
               0.1f,
               0.10f,
               0));
            Register(new HydroAmmoProfile(
               "FireworkPink",
               1f,
               0.05f,
               SplashType.Missile,
               18f,
               50f,
               0f,
               0f,
               0f,
               0f,
               0f,
               0f,
               "AquaMissileTrail",
               0.40f,
               0.1f,
               0.10f,
               0));
            Register(new HydroAmmoProfile(
               "FireworkYellow",
               1f,
               0.05f,
               SplashType.Missile,
               18f,
               50f,
               0f,
               0f,
               0f,
               0f,
               0f,
               0f,
               "AquaMissileTrail",
               0.40f,
               0.1f,
               0.10f,
               0));
            Register(new HydroAmmoProfile(
               "FireworkRainbow",
               1f,
               0.05f,
               SplashType.Missile,
               18f,
               50f,
               0f,
               0f,
               0f,
               0f,
               0f,
               0f,
               "AquaMissileTrail",
               0.40f,
               0.1f,
               0.10f,
               0));
            //large calibre shell
            Register(new HydroAmmoProfile(
               "LargeCalibreShell",
                800f,
                0.012f,
                SplashType.Missile,
                20f,
                10f,
                0f,
                0f,
                0f,
                0f,
                0f,
                0f,
                "AquaMissileTrail",
                0f,
                0f, 
                0f,
                0));
            // med calibre shell
            Register(new HydroAmmoProfile(
               "MediumCalibreShell",
                300f,
                0.020f,
                SplashType.Missile,
                20f,
                10f,
                0f,
                0f,
                0f,
                0f,
                0f,
                0f,
                "AquaMissileTrail",
                0f,
                0f,
                0f,
                0));
            // large railgun
            Register(new HydroAmmoProfile(
               "LargeRailgunSlug",
                2000f,
                0.006f,
                SplashType.Missile,
                20f,
                20f,
                0f,
                0f,
                0f,
                0f,
                0f,
                0f,
                "AquaMissileTrail",
                0f,
                0f,
                0f,
                0));
            // small railgun
            Register(new HydroAmmoProfile(
               "SmallRailgunSlug",
                2000f,
                0.009f,
                SplashType.Missile,
                20f,
                20f,
                0f,
                0f,
                0f,
                0f,
                0f,
                0f,
                "AquaMissileTrail",
                0f,
                0f,
                0f,
                0));
        }
        private static void Register(HydroAmmoProfile profile)
        {
            Profiles[profile.SubtypeId] = profile;
        }
        public static HydroAmmoProfile Get(string subtype)
        {
            HydroAmmoProfile profile;
            if (RuntimeProfiles.TryGetValue(subtype, out profile))
                return profile;
            if (Profiles.TryGetValue(subtype, out profile))
                return profile;
            return null;
        }
        public static HydroAmmoProfile GetOriginal(string subtype)
        {
            if (string.IsNullOrWhiteSpace(subtype))
                return null;
            subtype = subtype.Trim();
            HydroAmmoProfile profile;
            if (Profiles.TryGetValue(subtype,out profile))
            {
                return profile;
            }
            return null;
        }
        public static HydroAmmoProfile GetRuntime(string subtype)
        {
            if (string.IsNullOrWhiteSpace(subtype))
                return null;
            subtype = subtype.Trim();
            // Already has a runtime override.
            HydroAmmoProfile profile;
            if (RuntimeProfiles.TryGetValue(subtype,out profile))
            {
                return profile;
            }
            // Find registered/original profile.
            HydroAmmoProfile original = GetOriginal(subtype);
            if (original == null)
                return null;
            // Create an independent runtime copy.
            profile = original.Clone();
            RuntimeProfiles[subtype] = profile;
            return profile;
        }
        public static HydroAmmoProfile DefaultProjectile()
        {
            return new HydroAmmoProfile(
                "DefaultProjectile",
                2.2f,
                0.022f,
                SplashType.Bullet,
                45f,
                10f,
                0.55f,
                1.20f,
                18f,
                34f,
                0.92f,
                0.85f,
                "AquaBulletTrailSmall",
                0f,
                0f,
                0f,
                0);
        }
        public static HydroAmmoProfile DefaultMissile()
        {
            return new HydroAmmoProfile(
                "DefaultMissile",
                45f,
                0.03f,
                SplashType.Missile,
                20f,
                20f,
                0f,
                0f,
                0f,
                0f,
                0f,
                0f,
                "AquaBulletTrailSmall",
                0.40f,
                0.1f, //engine acceleration
                0.10f,
                0); // engine multyplier
        }
        public static IEnumerable<HydroAmmoProfile> GetAllProfiles()
        {
            return Profiles.Values;
        }
        public static HydroAmmoProfile GetAmmoProfile(string subtypeId)
        {
            if (string.IsNullOrWhiteSpace(subtypeId))
                return null;
            subtypeId = subtypeId.Trim();
            HydroAmmoProfile profile;
            if (Profiles.TryGetValue(subtypeId, out profile))
                return profile;
            return null;
        }
        //API 
        public static bool RegisterAmmoProfile(HydroAmmoProfile profile)
        {
            if (profile == null)
                return false;
            if (string.IsNullOrWhiteSpace(profile.SubtypeId))
                return false;
            string subtype = profile.SubtypeId.Trim();
            // Never allow duplicate registered ammo.
            if (Profiles.ContainsKey(subtype))
            {
                MyLog.Default.WriteLine(
                    "[AquaExpansion] Duplicate ammo profile rejected: "
                    + subtype);
                //AquaExpansionSession.Insance.Log(true,"Duplicate ammo profile rejected: " + subtype);
                return false;
            }
            // RuntimeProfiles should not normally contain an entry
            // before the ammo is registered.
            if (RuntimeProfiles.ContainsKey(subtype))
            {
                MyLog.Default.WriteLine(
                    "[AquaExpansion] Runtime profile already exists: "
                    + subtype);
                //AquaExpansionSession.Insance.Log(true,"Runtime profile already exists: " + subtype);
                return false;
            }
            profile.SubtypeId = subtype;
            Profiles.Add(subtype, profile);
            MyLog.Default.WriteLine(
                "[AquaExpansion] External ammo profile registered: "
                + subtype);
            //AquaExpansionSession.Insance.Log(true,"External ammo profile registered: " + subtype);
            return true;
        }
        public static bool HasAmmoProfile(string subtype)
        {
            if (string.IsNullOrWhiteSpace(subtype))
                return false;
            return Profiles.ContainsKey(subtype) || RuntimeProfiles.ContainsKey(subtype);
        }
        public static bool UnregisterExternalAmmoProfile(string subtypeId)
        {
            if (string.IsNullOrWhiteSpace(subtypeId))
                return false;
            return RuntimeProfiles.Remove(subtypeId);
        }
        public static void ClearRuntime()
        {
            RuntimeProfiles.Clear();
        }
    }
    /// <summary>
    /// Database for voxelImpacts
    /// </summary>
    public static class VoxelImpactDatabase
    {
        private static readonly Dictionary<string, string> voxelmaterials = new Dictionary<string, string>();
        public static void Init()
        {
            Register("Soil", "MaterialHit_Soil");
            Register("Grass", "MaterialHit_GrassGreen");
            Register("Rock", "MaterialHit_Rock");
            Register("Snow", "MaterialHit_Snow");
            Register("Ice", "MaterialHit_Ice");
            Register("Sand", "MaterialHit_Sand");
            Register("Stone", "MaterialHit_Rock");
            Register("Salt", "MaterialHit_Ice");
            Register("SeaSoil", "MaterialHit_Ice");
        }
        private static void Register(string materialsubtype, string impactname)
        {
            if (string.IsNullOrWhiteSpace(materialsubtype) ||
                string.IsNullOrWhiteSpace(impactname))
                return;
            voxelmaterials[materialsubtype] = impactname;
        }
        public static string Get(string subtype)
        {
            if (string.IsNullOrWhiteSpace(subtype))
                return null;
            string mat;
            if (voxelmaterials.TryGetValue(subtype, out mat))
                return mat;
            return DefaultImpact();
        }
        private static string DefaultImpact()
        {
            return "MaterialHit_Rock";
        }
    }
    /// <summary>
    /// Database for characterImpacts
    /// </summary>
    public static class CharacterImpactDatabase
    {
        private static readonly Dictionary<string, string> charactermaterials = new Dictionary<string, string>();
        public static void Init()
        {
            Register("Character", "MaterialHit_Character");
            Register("Spider", "Blood_Spider");
            Register("Wolf", "MaterialHit_Character");
        }
        private static void Register(string material, string impact)
        {
            if (string.IsNullOrWhiteSpace(material) ||
                string.IsNullOrWhiteSpace(impact))
                return;
            charactermaterials[material] = impact;
        }
        public static string Get(string subtype)
        {
            if (string.IsNullOrWhiteSpace(subtype))
                return null;
            string mat;
            if (charactermaterials.TryGetValue(subtype, out mat))
                return mat;
            return DefaultImpact();
        }
        private static string DefaultImpact()
        {
            return "MaterialHit_Character";
        }
    }
    /// <summary>
    /// Database for waterImpacts
    /// </summary>
    public static class WaterImpactDatabase
    {
        private static readonly Dictionary<string, string> enterbulletbySubtype = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> exitbulletbySubtype = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> entermissilebySubtype = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> exitmissilebySubtype = new Dictionary<string, string>();
        public static void Init()
        {
            //enter bullet
            Register("PistolCaliber", "AquaBulletSplash", SplashType.Bullet, WaterIntersectionType.Entry);
            Register("SmallCaliber", "AquaBulletSplash", SplashType.Bullet, WaterIntersectionType.Entry);
            Register("LargeCaliber", "AquaBulletSplash", SplashType.Bullet, WaterIntersectionType.Entry);
            Register("AutocannonShell", "AquaBulletSplash", SplashType.Bullet, WaterIntersectionType.Entry);
            //exit bullet
            Register("PistolCaliber", "AquaBulletSplashExit", SplashType.Bullet, WaterIntersectionType.Exit);
            Register("SmallCaliber", "AquaBulletSplashExit", SplashType.Bullet, WaterIntersectionType.Exit);
            Register("LargeCaliber", "AquaBulletSplashExit", SplashType.Bullet, WaterIntersectionType.Exit);
            Register("AutocannonShell", "AquaBulletSplashExit", SplashType.Bullet, WaterIntersectionType.Exit);
            //enter missile
            Register("Missile", "AquaMissileSplash", SplashType.Missile, WaterIntersectionType.Entry);
            Register("Flare", "AquaMissileSplash", SplashType.Missile, WaterIntersectionType.Entry);
            Register("FireworkBlue", "AquaMissileSplash", SplashType.Missile, WaterIntersectionType.Entry);
            Register("FireworkGreen", "AquaMissileSplash", SplashType.Missile, WaterIntersectionType.Entry);
            Register("FireworkRed", "AquaMissileSplash", SplashType.Missile, WaterIntersectionType.Entry);
            Register("FireworkPink", "AquaMissileSplash", SplashType.Missile, WaterIntersectionType.Entry);
            Register("FireworkYellow", "AquaMissileSplash", SplashType.Missile, WaterIntersectionType.Entry);
            Register("FireworkRainbow", "AquaMissileSplash", SplashType.Missile, WaterIntersectionType.Entry);
            Register("LargeCalibreShell", "AquaMissileSplash", SplashType.Shell, WaterIntersectionType.Entry);
            Register("MediumCalibreShell", "AquaMissileSplash", SplashType.Shell, WaterIntersectionType.Entry);
            Register("LargeRailgunSlug", "AquaMissileSplash", SplashType.Railgun, WaterIntersectionType.Entry);
            Register("SmallRailgunSlug", "AquaMissileSplash", SplashType.Railgun, WaterIntersectionType.Entry);
            //exit missile
            Register("Missile", "AquaMissileSplashExit", SplashType.Missile, WaterIntersectionType.Exit);
            Register("Flare", "AquaMissileSplashExit", SplashType.Missile, WaterIntersectionType.Exit);
            Register("FireworkBlue", "AquaMissileSplashExit", SplashType.Missile, WaterIntersectionType.Exit);
            Register("FireworkGreen", "AquaMissileSplashExit", SplashType.Missile, WaterIntersectionType.Exit);
            Register("FireworkRed", "AquaMissileSplashExit", SplashType.Missile, WaterIntersectionType.Exit);
            Register("FireworkPink", "AquaMissileSplashExit", SplashType.Missile, WaterIntersectionType.Exit);
            Register("FireworkYellow", "AquaMissileSplashExit", SplashType.Missile, WaterIntersectionType.Exit);
            Register("FireworkRainbow", "AquaMissileSplashExit", SplashType.Missile, WaterIntersectionType.Exit);
            Register("LargeCalibreShell", "AquaMissileSplashExit", SplashType.Shell, WaterIntersectionType.Exit);
            Register("MediumCalibreShell", "AquaMissileSplashExit", SplashType.Shell, WaterIntersectionType.Exit);
            Register("LargeRailgunSlug", "AquaMissileSplashExit", SplashType.Railgun, WaterIntersectionType.Exit);
            Register("SmallRailgunSlug", "AquaMissileSplashExit", SplashType.Railgun, WaterIntersectionType.Exit);
        }
        private static void Register(string subtype, string impact, SplashType type, WaterIntersectionType water)
        {
            if (string.IsNullOrWhiteSpace(subtype) ||
                string.IsNullOrWhiteSpace(impact))
                return;
            switch (type)
            {
                case SplashType.Bullet:
                    switch (water)
                    {
                        case WaterIntersectionType.Entry:
                            enterbulletbySubtype[subtype] = impact;
                            break;
                        case WaterIntersectionType.Exit:
                            exitbulletbySubtype[subtype] = impact;
                            break;
                    }
                    break;
                case SplashType.Missile:

                    switch (water)
                    {
                        case WaterIntersectionType.Entry:
                            entermissilebySubtype[subtype] = impact;
                            break;
                        case WaterIntersectionType.Exit:
                            exitmissilebySubtype[subtype] = impact;
                            break;
                    }
                    break;
                case SplashType.Shell:
                    switch (water)
                    {
                        case WaterIntersectionType.Entry:
                            entermissilebySubtype[subtype] = impact;
                            break;
                        case WaterIntersectionType.Exit:
                            exitmissilebySubtype[subtype] = impact;
                            break;
                    }
                    break;
                case SplashType.Railgun:
                    switch (water)
                    {
                        case WaterIntersectionType.Entry:
                            entermissilebySubtype[subtype] = impact;
                            break;
                        case WaterIntersectionType.Exit:
                            exitmissilebySubtype[subtype] = impact;
                            break;
                    }
                    break;
            }
        }
        public static string Get(string subtype, SplashType type, WaterIntersectionType water)
        {
            if (string.IsNullOrWhiteSpace(subtype))
                return string.Empty;
            switch (type)
            {
                case SplashType.Bullet:
                    switch (water)
                    {
                        case WaterIntersectionType.Entry:
                            string bulletEntry;
                            if (enterbulletbySubtype.TryGetValue(subtype,out bulletEntry))
                            {
                                return bulletEntry;
                            }
                            break;
                        case WaterIntersectionType.Exit:
                            string bulletExit;
                            if (exitbulletbySubtype.TryGetValue(subtype,out bulletExit))
                            {
                                return bulletExit;
                            }
                            break;
                    }
                    break;
                case SplashType.Missile:

                    switch (water)
                    {
                        case WaterIntersectionType.Entry:
                            string missileEntry;
                            if (entermissilebySubtype.TryGetValue(subtype,out missileEntry))
                            {
                                return missileEntry;
                            }
                            break;
                        case WaterIntersectionType.Exit:
                            string missileExit;
                            if (exitmissilebySubtype.TryGetValue(subtype,out missileExit))
                            {
                                return missileExit;
                            }
                            break;
                    }
                    break;
                case SplashType.Shell:
                    switch (water)
                    {
                        case WaterIntersectionType.Entry:
                            string missileEntry;
                            if (entermissilebySubtype.TryGetValue(subtype, out missileEntry))
                            {
                                return missileEntry;
                            }
                            break;
                        case WaterIntersectionType.Exit:
                            string missileExit;
                            if (exitmissilebySubtype.TryGetValue(subtype, out missileExit))
                            {
                                return missileExit;
                            }
                            break;
                    }
                    break;
                case SplashType.Railgun:
                    switch (water)
                    {
                        case WaterIntersectionType.Entry:
                            string missileEntry;
                            if (entermissilebySubtype.TryGetValue(subtype, out missileEntry))
                            {
                                return missileEntry;
                            }
                            break;
                        case WaterIntersectionType.Exit:
                            string missileExit;
                            if (exitmissilebySubtype.TryGetValue(subtype, out missileExit))
                            {
                                return missileExit;
                            }
                            break;
                    }
                    break;
            }
            return string.Empty;
        }
        public static string DefaultWaterImpact(SplashType type, WaterIntersectionType water)
        {
            switch (type)
            {
                case SplashType.Bullet:

                    switch (water)
                    {
                        case WaterIntersectionType.Entry:
                            return "AquaBulletSplash";
                        case WaterIntersectionType.Exit:
                            return "AquaBulletSplashExit";
                    }
                    break;
                case SplashType.Missile:

                    switch (water)
                    {
                        case WaterIntersectionType.Entry:
                            return "AquaMissileSplash";
                        case WaterIntersectionType.Exit:
                            return "AquaMissileSplashExit";
                    }
                    break;
                case SplashType.Shell:
                    switch (water)
                    {
                        case WaterIntersectionType.Entry:
                            return "AquaMissileSplash";
                        case WaterIntersectionType.Exit:
                            return "AquaMissileSplashExit";
                    }
                    break;
                case SplashType.Railgun:
                    switch (water)
                    {
                        case WaterIntersectionType.Entry:
                            return "AquaMissileSplash";
                        case WaterIntersectionType.Exit:
                            return "AquaMissileSplashExit";
                    }
                    break;
            }
            return string.Empty;
        }
    }
    /// <summary>
    /// Database for underwater burst effects
    /// </summary>
    public static class UnderwaterWeaponBurstDatabase
    {
        private static readonly Dictionary<string, string> muzzleburstbySubtype = new Dictionary<string, string>();
        public static void Init()
        {
            //hand
            Register("AutomaticRifleGun", "AquaHandWeaponBubbles");
            Register("PreciseAutomaticRifleGun", "AquaHandWeaponBubbles");
            Register("RapidFireAutomaticRifleGun", "AquaHandWeaponBubbles");
            Register("UltimateAutomaticRifleGun", "AquaHandWeaponBubbles");
            Register("SemiAutoPistolGun", "AquaHandWeaponBubbles");
            Register("FullAutoPistolGun", "AquaHandWeaponBubbles");
            Register("ElitePistolGun", "AquaHandWeaponBubbles");
            Register("FlarePistolGun", "AquaHandWeaponBubbles_FlareGun");
            //blocks
            Register("GatlingGun", "AquaWeaponBubblesGatlingGun");
            Register("GatlingGunWarfare", "AquaWeaponBubblesGatlingGun");
            Register("Autocannon", "AquaWeaponBubblesAutoCannon");
        }
        private static void Register(string subtype, string effect)
        {
            if (string.IsNullOrWhiteSpace(subtype) ||
                string.IsNullOrWhiteSpace(effect))
                return;
            muzzleburstbySubtype[subtype] = effect;
        }
        public static bool RegisterExternal(string subtype, string effect)
        {
            if (string.IsNullOrWhiteSpace(subtype) ||
                string.IsNullOrWhiteSpace(effect))
                return false;
            string subtypeID = subtype.Trim();
            string effectName = effect.Trim();
            // Never allow duplicate registered subtype.
            if (muzzleburstbySubtype.ContainsKey(subtypeID))
            {
                MyLog.Default.WriteLine(
                    "[AquaExpansion] Duplicate Muzzle Burst rejected: "
                    + subtypeID);
                //AquaExpansionSession.Insance.Log(true,"Duplicate Muzzle Burst rejected: " + subtypeID);
                return false;
            }
            muzzleburstbySubtype.Add(subtypeID,effectName);
            MyLog.Default.WriteLine(
                "[AquaExpansion] External Muzzle Burst registered: "
                + subtypeID
                + " -> "
                + effectName);
            /*AquaExpansionSession.Insance.Log(
                true,
                "External Muzzle Burst registered: "
                + subtypeID
                + " -> "
                + effectName);*/
            return true;
        }
        public static string Get(string subtype)
        {
            if (string.IsNullOrWhiteSpace(subtype))
                return null;
            string e;
            if (muzzleburstbySubtype.TryGetValue(subtype, out e))
                return e;
            return Default();
        }
        public static bool HasMuzzleBurst(string subtype)
        {
            if (string.IsNullOrWhiteSpace(subtype))
                return false;
            return muzzleburstbySubtype.ContainsKey(
                subtype.Trim());
        }
        private static string Default()
        {
            return "AquaHandWeaponBubbles";
        }
    }
    /// <summary>
    /// WeaponDummies Database
    /// </summary>
    public static class WeaponDummiesDatabase
    {
        private static readonly Dictionary<int, string> weapondumiesbyID = new Dictionary<int, string>();
        public static void Init()
        {
            //main/offset
            Register(1, "muzzle_projectile");
            //alt
            Register(2, "subpart_Barrel");
            //missiles
            Register(3, "muzzle_missile_001");
            Register(4, "Muzzle_Missile");
            Register(5, "Barelle_Missile");
        }
        private static void Register(int index, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return;
            weapondumiesbyID[index] = name;
        }
        public static string Get(int id)
        {
            string line;
            if (weapondumiesbyID.TryGetValue(id, out line))
                return line;
            AquaExpansionSession.Insance.Log(true, $"Dummy NOT FOUND (id): {id}");
            return null;
        }
        public static void Validate()
        {
            var dummies = new HashSet<string>();
            foreach (var pair in weapondumiesbyID)
            {
                int id = pair.Key;
                string line = pair.Value;
                if (string.IsNullOrWhiteSpace(line))
                    throw new Exception($"Dummy ID {id} has null/empty line");
                if (!dummies.Add(line))
                    throw new Exception($"Dummy Duplicate line: {line}");
            }
        }
    }
    /// <summary>
    /// Aqua Projectile Manager is responsible for tracking and updating projectiles in the game world, applying physics and handling water transitions
    /// </summary>
    public static class AquaProjectileManager
    {
        private static readonly List<AquaProjectile> Projectiles = new List<AquaProjectile>();
        private static long NextId = 1;
        /// <summary>
        /// Spawn a new projectile to be tracked and updated by the system
        /// </summary>
        /// <param name="sourceId"></param>
        /// <param name="position"></param>
        /// <param name="velocity"></param>
        /// <param name="profile"></param>
        /// <param name="trajectory"></param>
        public static void Spawn(long sourceId,Vector3D position,Vector3 velocity,HydroAmmoProfile profile,WaterTrajectoryResult trajectory)
        {
            AquaProjectile projectile = new AquaProjectile();
            projectile.Id = NextId++;
            projectile.SourceProjectileId = sourceId;
            projectile.Position = position;
            projectile.PreviousPosition = position;
            projectile.Velocity = velocity;
            projectile.Profile = profile;
            projectile.Trajectory = trajectory;
            projectile.Alive = true;
            projectile.TravelDistance = 0f;
            projectile.CurrentMassDamage = profile.ProjectileMassDamage;
            projectile.CurrentHealthDamage = profile.ProjectileHealthDamage;
            projectile.WaterDistance = 0f;
            projectile.BubbleDistance = 0f;
            projectile.SplashType = profile.SplashType;
            Projectiles.Add(projectile);
        }
        /// <summary>
        /// Update all active projectiles, applying physics and handling water transitions
        /// </summary>
        public static void Update()
        {
            const float dt = 1f / 60f;
            const int MaxSubSteps = 10;
            for (int i = Projectiles.Count - 1; i >= 0; i--)
            {
                AquaProjectile projectile = Projectiles[i];
                float distancePerFrame = projectile.Velocity.Length() * dt;
                int subSteps = Math.Min(MaxSubSteps,Math.Max(1, (int)Math.Ceiling(distancePerFrame / 2f)));
                float stepDt = dt / subSteps;
                for (int s = 0; s < subSteps; s++)
                {
                    if (!projectile.Alive)
                        break;
                    projectile.LifeTime += stepDt;
                    projectile.PreviousPosition = projectile.Position;
                    projectile.Position += projectile.Velocity * stepDt;
                    float moved = (float)Vector3D.Distance(projectile.PreviousPosition,projectile.Position);
                    projectile.TravelDistance += moved;
                    // Water entry / exit
                    UpdateWaterTransition(projectile);
                    //trail update
                    // Collision
                    CheckHit(projectile);
                    if (!projectile.Alive)
                        break;
                    // Water physics
                    ApplyWaterPhysics(projectile, stepDt, moved);
                    // Lifetime / speed / damage
                    CheckKill(projectile);
                    if (!projectile.Alive)
                        break;
                }
                if (!projectile.Alive)
                {
                    Projectiles.RemoveAt(i);
                }
            }
        }
        /// <summary>
        /// Clear projectiles
        /// </summary>
        public static void Clear()
        {
            Projectiles.Clear();
        }
        /// <summary>
        /// Update the water transition state of  the projectile and create splash effects if necessary
        /// </summary>
        /// <param name="projectile"></param>
        private static void UpdateWaterTransition(AquaProjectile projectile)
        {
            switch (projectile.Trajectory.Type)
            {
                case WaterTrajectoryType.EnteredWater:

                    if (!projectile.SplashEnterCreated &&
                        CrossedPoint(projectile.PreviousPosition,projectile.Position,projectile.Trajectory.EntryPoint))
                    {
                        projectile.SplashEnterCreated = true;
                        CombatUtils.CreateBulletSplash(projectile.Profile.SubtypeId,projectile.Trajectory.EntryPoint,projectile.Velocity,projectile.SplashType);
                    }
                    break;
                case WaterTrajectoryType.ExitedWater:

                    if (!projectile.SplashExitCreated &&
                        CrossedPoint(projectile.PreviousPosition,projectile.Position,projectile.Trajectory.ExitPoint))
                    {
                        projectile.SplashExitCreated = true;
                        CombatUtils.CreateExitSplash(projectile.Profile.SubtypeId,projectile.Trajectory.ExitPoint,projectile.Velocity,projectile.SplashType);
                        // Energy lost breaking through the surface
                        projectile.Velocity *= projectile.Profile.ExitVelocityMultiplier;
                        projectile.CurrentMassDamage *= projectile.Profile.ExitDamageMultiplier;
                        projectile.CurrentHealthDamage *= projectile.Profile.ExitDamageMultiplier;
                    }
                    break;
            }
        }
        /// <summary>
        /// Cross point check to see if the projectile has crossed the water entry point between the last and current position
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        private static bool CrossedPoint(Vector3D from, Vector3D to, Vector3D point)
        {
            Vector3D segment = to - from;
            double lengthSq = segment.LengthSquared();
            if (lengthSq < 1e-6)
                return false;
            double t = Vector3D.Dot(point - from, segment) / lengthSq;
            if (t < 0.0 || t > 1.0)
                return false;
            Vector3D closest = from + segment * t;
            double tolerance = Math.Max(0.25, Math.Sqrt(lengthSq) * 0.02);
            return Vector3D.DistanceSquared(closest, point) <= tolerance * tolerance;
        }
        /// <summary>
        /// Calculate the depth multiplier for damage attenuation based on the depth of the projectile in water
        /// </summary>
        /// <param name="depth"></param>
        /// <returns></returns>
        private static float CalculateDepthMultiplier(float depth)
        {
            // 0 m = 1.00
            // 100 m = 0.98
            // 500 m = 0.90
            // 1000 m = 0.80
            return Math.Max(0.8f, 1f - depth * 0.0002f);
        }
        /// <summary>
        /// Calculate the drag multiplier for hydrodynamic drag based on the depth of the prjectile in water
        /// </summary>
        /// <param name="depth"></param>
        /// <returns></returns>
        private static float CalculateDragMultiplier(float depth)
        {
            // 0 m    = 1.00
            // 100 m  = 1.02
            // 500 m  = 1.10
            // 1000 m = 1.20

            return Math.Min(1.2f, 1f + depth * 0.0002f);
        }
        /// <summary>
        /// Calculate the damage multiplier for damage attenuation based on the depth of the projectile in water
        /// </summary>
        /// <param name="depth"></param>
        /// <returns></returns>
        private static float CalculateDamageMultiplier(float depth)
        {
            // Pressure slightly reduces effectiveness.

            // 0 m    = 1.00
            // 100 m  = 0.98
            // 500 m  = 0.90
            // 1000 m = 0.80

            return Math.Max(0.8f, 1f - depth * 0.0002f);
        }
        /// <summary>
        /// Calculate the final damage of a projectile based on its base damage, distance traveled underwater, depth and the hydrodynamic ammo profile
        /// </summary>
        /// <param name="baseDamage"></param>
        /// <param name="underwaterDistance"></param>
        /// <param name="depth"></param>
        /// <param name="profile"></param>
        /// <returns></returns>
        private static float CalculateDamage(float baseDamage,float underwaterDistance,float depth,HydroAmmoProfile profile)
        {
            float energyMultiplier = (float)Math.Exp(-underwaterDistance * (profile.EnergyLossCoefficient / profile.Mass));
            float depthMultiplier = CalculateDamageMultiplier(depth);
            float fdamage = baseDamage * energyMultiplier * depthMultiplier;
            return fdamage;
        }
        /// <summary>
        /// Calculate the final velocity of a projectile based on its initial velocity, distance traveled underwater, depth and the hydrodynnamic ammo profile
        /// </summary>
        /// <param name="projectile"></param>
        /// <param name="dt"></param>
        /// <param name="moved"></param>
        private static void ApplyWaterPhysics(AquaProjectile projectile, float dt, float moved)
        {
            if (!WaterModAPI.IsUnderwater(projectile.Position))
                return;
            // Underwater travel
            projectile.WaterDistance += moved;
            float depth = Math.Abs((float)WaterModAPI.GetDepth(projectile.Position));
            float speed = projectile.Velocity.Length();
            if (speed <= projectile.Profile.MinimumSpeed)
            {
                projectile.Alive = false;
                return;
            }
            CombatUtils.CreateProjectileTrail(projectile, moved, speed);
            // Water density (slight increase with depth)
            float densityMultiplier = 1f + Math.Min(depth, 1000f) * 0.0002f;
            // Hydrodynamic drag
            float drag = projectile.Profile.DragCoefficient * densityMultiplier;
            // Quadratic drag
            float deceleration = (drag * speed * speed) / projectile.Profile.Mass;
            // Prevent overshooting below zero
            deceleration = Math.Min(deceleration, speed / dt);
            speed -= deceleration * dt;
            if (speed <= projectile.Profile.MinimumSpeed)
            {
                projectile.Alive = false;
                return;
            }
            // Update velocity
            Vector3 direction = Vector3.Normalize(projectile.Velocity);
            projectile.Velocity = direction * speed;
            // Apply yaw / tumble
            ApplyWaterStability(projectile, dt);
            // Damage attenuation
            projectile.CurrentMassDamage = CalculateDamage(
                projectile.Profile.ProjectileMassDamage,
                projectile.WaterDistance,
                depth,
                projectile.Profile);
                projectile.CurrentHealthDamage = CalculateDamage(
                projectile.Profile.ProjectileHealthDamage,
                projectile.WaterDistance,
                depth,
                projectile.Profile);
            if (AquaExpansionSession.Insance.isModdingEnabled && AquaExpansionSession.Insance.isHydroModdingEnabled && AquaExpansionSession.Insance.LogsEnabled)
            {
                CombatUtils.LogAquaProjectileWaterPhysics(projectile.Profile.Mass,projectile.Trajectory.Type, projectile.WaterDistance ,projectile.Velocity.Length(), densityMultiplier, drag, deceleration,
                projectile.CurrentMassDamage, projectile.CurrentHealthDamage,projectile.Profile.EnergyLossCoefficient);
            }
            if (AquaExpansionSession.Insance.isModdingEnabled && AquaExpansionSession.Insance.isHydroModdingEnabled && AquaExpansionSession.Insance.RenderEnabled)
            {
                Visualize(projectile);
            }
        }
        /// <summary>
        /// Calculate the stability of the projectile underwater and apply random yaw and pitch deviation based on the water stability factor of the hydrodynamic ammo profile
        /// </summary>
        /// <param name="projectile"></param>
        /// <param name="dt"></param>
        private static void ApplyWaterStability(AquaProjectile projectile, float dt)
        {
            float stability = projectile.Profile.WaterStability;
            // 1 = perfectly stable
            if (stability >= 0.999f)
                return;
            float speed = projectile.Velocity.Length();
            if (speed < 1f)
                return;
            // Instability grows with distance traveled underwater
            float instability =
                (1f - stability) *
                Math.Min(1f, projectile.WaterDistance / 20f);
            // Maximum angular deviation (degrees/sec)
            float maxAngle = 20f * instability;
            float yaw = MyUtils.GetRandomFloat(-maxAngle, maxAngle);
            float pitch = MyUtils.GetRandomFloat(-maxAngle, maxAngle);
            yaw *= dt;
            pitch *= dt;
            Vector3 direction = Vector3.Normalize(projectile.Velocity);
            Matrix rotation = Matrix.CreateFromYawPitchRoll(MathHelper.ToRadians(yaw),MathHelper.ToRadians(pitch),0f);
            direction = Vector3.TransformNormal(direction, rotation);
            direction.Normalize();
            projectile.Velocity = direction * speed;
        }
        /// <summary>
        /// Check if the projectile should be killed due
        /// </summary>
        /// <param name="projectile"></param>
        private static void CheckKill(AquaProjectile projectile)
        {
            // Projectile has lost too much speed
            if (projectile.Velocity.Length() <= projectile.Profile.MinimumSpeed)
            {
                projectile.Alive = false;
                return;
            }
            // Projectile has no meaningful damage left
            const float MinDamage = 0.1f;
            if (projectile.CurrentMassDamage <= MinDamage &&
                projectile.CurrentHealthDamage <= MinDamage)
            {
                projectile.Alive = false;
                /*AquaExpansionSession.Insance.Log(true,
                $"Projectile killed by damage {projectile.CurrentMassDamage} / {projectile.CurrentHealthDamage}");*/
                return;
            }
            // Projectile has exceeded its lifetime
            const float MaxLifetime = 5f;
            if (projectile.LifeTime > MaxLifetime)
            {
                projectile.Alive = false;
                /*AquaExpansionSession.Insance.Log(true,
               $"Projectile killed by lifetime {projectile.LifeTime}");*/
                return;
            }
        }
        /// <summary>
        /// Check if the projectile has hit any any entity in the game world
        /// </summary>
        /// <param name="projectile"></param>
        private static void CheckHit(AquaProjectile projectile)
        {
            LineD line = new LineD(projectile.PreviousPosition,projectile.Position);
            IHitInfo hit;
            if (!MyAPIGateway.Physics.CastRay(line.From,line.To,out hit))
                return;
            if (hit.HitEntity == null)
                return;
            projectile.Position = hit.Position;
            OnHit(projectile, hit);
        }
        /// <summary>
        /// OnHit is called when a projectile hits a entity, applying damage and logging the hit info
        /// </summary>
        /// <param name="projectile"></param>
        /// <param name="hit"></param>
        private static void OnHit(AquaProjectile projectile, IHitInfo hit)
        {
            GetDamageTarget(projectile,hit);
            if (AquaExpansionSession.Insance.isModdingEnabled && AquaExpansionSession.Insance.isHydroModdingEnabled && AquaExpansionSession.Insance.LogsEnabled)
            {
                string hitInfo = CombatUtils.GetHitInfo(hit);
                CombatUtils.LogAquaProjectile(projectile, hit.Position, hitInfo, hit);
            }
        }
        /// <summary>
        /// Get Damage target applies damage to the hit entity based on the projectile's current damage values
        /// </summary>
        /// <param name="projectile"></param>
        /// <param name="hit"></param>
        private static void GetDamageTarget(AquaProjectile projectile,IHitInfo hit)
        {
            if (projectile == null || hit.HitEntity == null)
                return;
            // Cube grid
            IMyCubeGrid grid = hit.HitEntity as IMyCubeGrid;
            if (grid != null)
            {
                Vector3I cell = grid.WorldToGridInteger(hit.Position);
                IMySlimBlock slim = grid.GetCubeBlock(cell);
                if (slim != null)
                {
                    slim.DoDamage(
                        projectile.CurrentMassDamage,
                        MyStringHash.GetOrCompute("Bullet"),
                        true);
                    MyHitInfo decalHit = new MyHitInfo
                    {
                        Position = hit.Position,
                        Normal = hit.Normal
                    };
                    CombatUtils.CreateBulletImpact(hit.Position, projectile.Velocity,projectile.Trajectory.Type);
                    projectile.Alive = false;
                    return;
                }
            }
            // Character
            IMyCharacter character = hit.HitEntity as IMyCharacter;
            if (character != null)
            {
                character.DoDamage(
                    projectile.CurrentHealthDamage,
                    MyStringHash.GetOrCompute("Bullet"),
                    true);
                var mat = character.Physics.MaterialType;
                CombatUtils.CreateCharacterImpact(hit.Normal, projectile.Velocity, CharacterImpactDatabase.Get(mat.String), projectile.Trajectory.Type);
                projectile.Alive = false;
                return;
            }
            // Voxel
            IMyVoxelBase voxelBase = hit.HitEntity as IMyVoxelBase;
            if (voxelBase != null)
            {
                var mat = CombatUtils.GetVoxelMaterialSubtypeid(voxelBase, hit);
                CombatUtils.CreateVoxelImpact(hit.Position, projectile.Velocity, VoxelImpactDatabase.Get(mat),projectile.Trajectory.Type);
                projectile.Alive = false;
                return;
            }
            //Floating
            MyFloatingObject flo = hit.HitEntity as MyFloatingObject;
            if (flo != null)
            {
                flo.DoDamage(
                    projectile.CurrentHealthDamage,
                    MyStringHash.GetOrCompute("Bullet"),
                    true,
                    projectile.SourceProjectileId,
                    null);
            }
        }
        private static void DrawProjectile(AquaProjectile projectile)
        {
            float speed = projectile.Velocity.Length();
            Vector4 color = new Vector4(0.95f, 0.98f, 1f, 0.9f);
            float radius = MathHelper.Clamp(
                projectile.Profile.Mass * 0.003f,
                0.015f,
                0.08f);
            MyTransparentGeometry.AddPointBillboard(
                MyStringId.GetOrCompute("WhiteDot"),
                color,
                projectile.Position,
                radius,
                0);
        }
        private static void DrawWake(AquaProjectile projectile)
        {
            float speed = projectile.Velocity.Length();
            if (speed < projectile.Profile.MinimumSpeed)
                return;
            Vector3D dir = Vector3D.Normalize(projectile.Velocity);
            float length = MathHelper.Clamp(
                speed * 0.004f,
                0.15f,
                2.5f);
            float thickness = MathHelper.Clamp(
                projectile.Profile.Mass * 0.002f,
                0.01f,
                0.08f);
            Vector4 color = new Vector4(
                0.85f,
                0.95f,
                1.0f,
                0.35f);
            MyTransparentGeometry.AddLineBillboard(
                MyStringId.GetOrCompute("WeaponLaser"),
                color,
                projectile.Position,
                -dir,
                length,
                thickness);
        }
        private static void Visualize(AquaProjectile projectile)
        {
            DrawProjectile(projectile);
            DrawWake(projectile);
        }
    }
}
