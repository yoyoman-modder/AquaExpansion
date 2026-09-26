using Jakaria.API;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRageMath;

namespace AquaExpansion.Core.Combat.Balistics
{
    public static class MissileProcessor
    {
        private static readonly Dictionary<long, MyParticleEffect> missileeffects = new Dictionary<long, MyParticleEffect>();
        private static readonly List<long> MissileUpdateIds = new List<long>();
        private static readonly List<long> PendingMissileAdds = new List<long>();
        private static readonly List<long> PendingMissileRemovals = new List<long>();
        private static readonly Dictionary<long, MissileState> PendingMissileStates =new Dictionary<long, MissileState>();
        private static bool UpdatingMissiles;
        public static void Process(IMyMissile missile, MissileState state)
        {
            if (missile == null || state == null)
                return;
            MyPlanet planet = CombatUtils.WaterPlanet(missile.Origin);
            if (planet == null)
                return;
            Vector3D origin = missile.Origin;
            Vector3D hitPosition = missile.CollisionPoint ?? missile.PositionComp.GetPosition();
            Vector3D direction;
            if (state.LastVelocity.LengthSquared() > 0.01)
                direction = Vector3D.Normalize(state.LastVelocity);
            else
                direction = Vector3D.Normalize(hitPosition - origin);
            float distance = (float)Vector3D.Distance(origin, hitPosition);
            WaterTrajectoryResult trajectory = WaterTrajectory.Calculate(planet,origin,direction,distance);
            if (AquaExpansionSession.Insance.isModdingEnabled && AquaExpansionSession.Insance.isHydroModdingEnabled && AquaExpansionSession.Insance.LogsEnabled)
            {
                string targetInfo = CombatUtils.GetTargetInfo(missile);
                CombatUtils.LogMissile(origin, hitPosition, state.LastSpeed, missile.ExplosionDamage, targetInfo, trajectory, state.FlyTime);
            }
        }
        public static void OnMissileAdded(IMyMissile missile, Dictionary<long, MissileState> tracked)
        {
            /*if (missile == null)
                return;
            Vector3D position = missile.GetPosition();
            MyPlanet planet = CombatUtils.WaterPlanet(position);
            if (planet == null)
                return;
            HydroAmmoProfile profile = HydroAmmoDatabase.Get(missile.AmmoDefinition.Id.SubtypeId.String)
                ?? HydroAmmoDatabase.DefaultMissile();
            MissileState state = new MissileState
            {
                Missile = missile,
                Profile = profile,
                PreviousPosition = position,
                WaterState = WaterModAPI.IsUnderwater(position)
                    ? WaterTrajectoryType.Underwater
                    : WaterTrajectoryType.Air,
                WaterDistance = 0f,
                BubbleDistance = 0f,
                
            };
            tracked[missile.EntityId] = state;*/
            if (missile == null || tracked == null)
                return;
            Vector3D position = missile.GetPosition();
            MyPlanet planet = CombatUtils.WaterPlanet(position);
            if (planet == null)
                return;
            HydroAmmoProfile profile = HydroAmmoDatabase.Get(missile.AmmoDefinition.Id.SubtypeId.String) ?? HydroAmmoDatabase.DefaultMissile();
            MissileState state =
                new MissileState
                {
                    Missile = missile,
                    Profile = profile,
                    PreviousPosition = position,
                    WaterState =
                        WaterModAPI.IsUnderwater(position)
                            ? WaterTrajectoryType.Underwater
                            : WaterTrajectoryType.Air,
                    WaterDistance = 0f,
                    BubbleDistance = 0f
                };
            if (UpdatingMissiles)
            {
                if (!PendingMissileStates.ContainsKey(missile.EntityId))
                {
                    PendingMissileStates.Add(missile.EntityId,state);
                    PendingMissileAdds.Add(missile.EntityId);
                }
                return;
            }
            tracked[missile.EntityId] = state;
            if (!MissileUpdateIds.Contains(missile.EntityId))
            {
                MissileUpdateIds.Add(missile.EntityId);
            }
        }
        public static void OnMissileKilled(IMyMissile missile, Dictionary<long,MissileState> tracked)
        {
            /*if (missile == null)
                return;
            MissileState state;
            if (!tracked.TryGetValue(missile.EntityId, out state))
                return;
            // Stop underwater particle effect
            CombatUtils.StopMissileEffect(missile,missileeffects);
            if (AquaExpansionSession.Insance.isModdingEnabled && AquaExpansionSession.Insance.isHydroModdingEnabled && AquaExpansionSession.Insance.LogsEnabled)
            { 
                CombatUtils.LogKilledMissile(state.WaterState, state.WaterDistance, state.LastSpeed, state.FlyTime); 
            }
            tracked.Remove(missile.EntityId);*/
            if (missile == null || tracked == null)
                return;
            MissileState state;
            if (!tracked.TryGetValue(missile.EntityId,out state))
            {
                return;
            }
            CombatUtils.StopMissileEffect(missile,missileeffects);
            AquaExpansionSession session = AquaExpansionSession.Insance;
            if (session != null &&
                session.isModdingEnabled &&
                session.isHydroModdingEnabled &&
                session.LogsEnabled)
            {
                CombatUtils.LogKilledMissile(state.WaterState,state.WaterDistance,state.LastSpeed,state.FlyTime);
            }
            if (UpdatingMissiles)
            {
                if (!PendingMissileRemovals.Contains(missile.EntityId))
                {
                    PendingMissileRemovals.Add(missile.EntityId);
                }
                return;
            }
            tracked.Remove(missile.EntityId);
            MissileUpdateIds.Remove(missile.EntityId);
        }
        public static void UpdateMissiles(Dictionary<long, MissileState> tracked)
        {
            /*float dt = (float)MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
            List<long> remove = null;
            foreach (var pair in tracked)
            {
                MissileState state = pair.Value;
                IMyMissile missile = state.Missile;
                // -------------------------------------------------
                // Validate missile
                // -------------------------------------------------
                if (missile == null || missile.Closed || missile.MarkedForClose)
                {
                    if (missile != null)
                    {
                        CombatUtils.StopMissileEffect(missile,missileeffects);
                    }
                    if (remove == null)
                        remove = new List<long>();
                    remove.Add(pair.Key);
                    continue;
                }
                // -------------------------------------------------
                // Flight time
                // -------------------------------------------------
                state.FlyTime += dt;
                // -------------------------------------------------
                // Current position / velocity
                // -------------------------------------------------
                Vector3D position = missile.GetPosition();
                Vector3 velocity = missile.LinearVelocity;
                // -------------------------------------------------
                // Current water state
                // -------------------------------------------------
                WaterTrajectoryType newState =
                    WaterModAPI.IsUnderwater(position)
                        ? WaterTrajectoryType.Underwater
                        : WaterTrajectoryType.Air;
                // -------------------------------------------------
                // Water transition
                // -------------------------------------------------
                if (newState != state.WaterState)
                {
                    if (newState == WaterTrajectoryType.Underwater)
                    {
                        state.WaterDistance = 0f;
                        // Actual air -> water entry
                        CombatUtils.CreateMissileSplash(state.Profile.SubtypeId,position,velocity,state.Profile.SplashType);
                    }
                    else
                    {
                        // Actual water -> air exit
                        CombatUtils.CreateMissileExitSplash(state.Profile.SubtypeId,position, velocity,state.Profile.SplashType);
                        CombatUtils.StopMissileEffect(missile,missileeffects);
                    }
                    state.WaterState = newState;
                }
                // -------------------------------------------------
                // Ensure underwater effect exists
                //
                // This also handles missiles spawned/fired
                // directly underwater.
                // -------------------------------------------------
                if (state.WaterState == WaterTrajectoryType.Underwater)
                {
                    CombatUtils.CreateMissileEffect(missile,state,1f,missileeffects);
                }
                // -------------------------------------------------
                // Distance travelled this update
                // -------------------------------------------------
                float moved = (float)Vector3D.Distance(state.PreviousPosition,position);
                state.PreviousPosition = position;
                // -------------------------------------------------
                // Underwater physics
                // -------------------------------------------------
                if (state.WaterState == WaterTrajectoryType.Underwater)
                {
                    state.WaterDistance += moved;
                    ApplyMissileWaterPhysics(state,moved,dt);
                }
                // -------------------------------------------------
                // Default SE missile trail
                // -------------------------------------------------
                if (missile.ParticleEffect != null)
                {
                    CombatUtils.UpdateMissileTrail(missile,state);
                }
                // -------------------------------------------------
                // Store FINAL velocity
                // -------------------------------------------------
                if (missile.Physics != null)
                {
                    state.LastVelocity = missile.Physics.LinearVelocity;
                    state.LastSpeed = (float)state.LastVelocity.Length();
                }
                else
                {
                    state.LastVelocity = velocity;
                    state.LastSpeed = velocity.Length();
                }
                // --------------------------------------------------
                // SELF DESTRUCT
                // --------------------------------------------------
                if (CombatUtils.MissileSelfDestruct(missile, state))
                {
                    if (remove == null)
                        remove = new List<long>();
                    remove.Add(pair.Key);
                    continue;
                }
                // -------------------------------------------------
                // Debug logging
                // -------------------------------------------------
                AquaExpansionSession session = AquaExpansionSession.Insance;
                if (session != null && session.isModdingEnabled && session.isHydroModdingEnabled && session.LogsEnabled)
                {
                    CombatUtils.LogRunningMissile(state.WaterState, state.WaterDistance ,state.LastSpeed, state.FlyTime, missile.ParticleEffect.UserScale);
                }
            }
            // -------------------------------------------------
            // Remove invalid missiles
            // -------------------------------------------------
            if (remove != null)
            {
                foreach (long id in remove)
                {
                    tracked.Remove(id);
                }
            }*/
            if (tracked == null)
                return;
            float dt = (float)MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
            UpdatingMissiles = true;
            int count = MissileUpdateIds.Count;
            for (int i = 0; i < count; i++)
            {
                long id = MissileUpdateIds[i];
                MissileState state;
                if (!tracked.TryGetValue(id,out state))
                {
                    continue;
                }
                IMyMissile missile = state.Missile;
                if (missile == null ||
                    missile.Closed ||
                    missile.MarkedForClose)
                {
                    if (missile != null)
                    {
                        CombatUtils.StopMissileEffect(missile,missileeffects);
                    }
                    if (!PendingMissileRemovals.Contains(id))
                    {
                        PendingMissileRemovals.Add(id);
                    }
                    continue;
                }
                state.FlyTime += dt;
                Vector3D position = missile.GetPosition();
                Vector3 velocity = missile.LinearVelocity;
                WaterTrajectoryType newState =
                    WaterModAPI.IsUnderwater(position)
                        ? WaterTrajectoryType.Underwater
                        : WaterTrajectoryType.Air;
                if (newState != state.WaterState)
                {
                    if (newState == WaterTrajectoryType.Underwater)
                    {
                        state.WaterDistance = 0f;
                        CombatUtils.CreateMissileSplash(state.Profile.SubtypeId,position,velocity,state.Profile.SplashType);
                    }
                    else
                    {
                        CombatUtils.CreateMissileExitSplash(state.Profile.SubtypeId,position,velocity,state.Profile.SplashType);
                        CombatUtils.StopMissileEffect(missile,missileeffects);
                    }
                    state.WaterState = newState;
                }
                if (state.WaterState == WaterTrajectoryType.Underwater)
                {
                    CombatUtils.CreateMissileEffect(missile,state,1f,missileeffects);
                }
                float moved = (float)Vector3D.Distance(state.PreviousPosition,position);
                state.PreviousPosition = position;
                if (state.WaterState == WaterTrajectoryType.Underwater)
                {
                    state.WaterDistance += moved;
                    ApplyMissileWaterPhysics(state,moved,dt);
                }
                if (missile.ParticleEffect != null)
                {
                    CombatUtils.UpdateMissileTrail(missile,state);
                }
                if (missile.Physics != null)
                {
                    state.LastVelocity = missile.Physics.LinearVelocity;
                    state.LastSpeed = (float)state.LastVelocity.Length();
                }
                else
                {
                    state.LastVelocity = velocity;
                    state.LastSpeed = velocity.Length();
                }
                if (CombatUtils.MissileSelfDestruct(missile,state))
                {
                    if (!PendingMissileRemovals.Contains(id))
                    {
                        PendingMissileRemovals.Add(id);
                    }
                    continue;
                }
                AquaExpansionSession session = AquaExpansionSession.Insance;
                if (session != null &&
                    session.isModdingEnabled &&
                    session.isHydroModdingEnabled &&
                    session.LogsEnabled)
                {
                    float scale = 0f;
                    if (missile.ParticleEffect != null)
                    {
                        scale = missile.ParticleEffect.UserScale;
                    }
                    CombatUtils.LogRunningMissile(state.WaterState,state.WaterDistance,state.LastSpeed,state.FlyTime,scale);
                }
            }
            for (int i = 0;i < PendingMissileRemovals.Count;i++)
            {
                long id = PendingMissileRemovals[i];
                tracked.Remove(id);
                MissileUpdateIds.Remove(id);
            }
            PendingMissileRemovals.Clear();
            for (int i = 0;i < PendingMissileAdds.Count;i++)
            {
                long id = PendingMissileAdds[i];
                MissileState state;
                if (PendingMissileStates.TryGetValue(id,out state))
                {
                    tracked[id] = state;
                    if (!MissileUpdateIds.Contains(id))
                    {
                        MissileUpdateIds.Add(id);
                    }
                }
            }
            PendingMissileAdds.Clear();
            PendingMissileStates.Clear();
            UpdatingMissiles = false;
        }
        public static void ApplyMissileWaterPhysics(MissileState state, float moved, float dt)
        {
            //Cubic drag model
            IMyMissile missile = state.Missile;
            if (missile == null || missile.Physics == null)
                return;
            HydroAmmoProfile profile = state.Profile;
            Vector3 velocity = missile.Physics.LinearVelocity;
            float speed = velocity.Length();
            if (speed <= 0.01f)
                return;
            float depth = Math.Abs((float)WaterModAPI.GetDepth(missile.GetPosition()));
            // Slight density increase with depth
            float densityMultiplier = 1f + Math.Min(depth, 1000f) * 0.0002f;
            // Stronger drag than v²
            float dragAcceleration =
                profile.DragCoefficient *
                densityMultiplier *
                speed * speed * speed /
                Math.Max(profile.Mass, 0.01f);
            float engineAcceleration =
                profile.EngineAcceleration *
                profile.UnderwaterEngineMultiplier;
            speed += (engineAcceleration - dragAcceleration) * dt;
            if (speed < profile.MinimumSpeed)
               speed = profile.MinimumSpeed;
            missile.Physics.LinearVelocity = Vector3.Normalize(velocity) * speed;
            if (profile.UnderwaterTurnMultiplier < 1f)
            {
                missile.Physics.AngularVelocity *= profile.UnderwaterTurnMultiplier;
            }
            if (AquaExpansionSession.Insance.isModdingEnabled && AquaExpansionSession.Insance.isHydroModdingEnabled && AquaExpansionSession.Insance.LogsEnabled)
            {
                CombatUtils.LogMissileWaterPhysics(state.WaterState, missile.Physics.LinearVelocity.Length(), densityMultiplier, dragAcceleration, engineAcceleration);
            }
            //Quadratic drag model
            /*IMyMissile missile = state.Missile;
            if (missile == null || missile.Physics == null)
                return;
            HydroAmmoProfile profile = state.Profile;
            Vector3 velocity = missile.Physics.LinearVelocity;
            float speed = velocity.Length();
            if (speed <= profile.MinimumSpeed)
                return;
            float depth = Math.Abs(
                (float)WaterModAPI.GetDepth(missile.GetPosition()));
            // Slight increase in water density with depth
            float densityMultiplier =
                1f + Math.Min(depth, 1000f) * 0.0002f;
            // Quadratic hydrodynamic drag
            float dragAcceleration =
                (profile.DragCoefficient *
                 densityMultiplier *
                 speed *
                 speed) /
                Math.Max(profile.Mass, 0.01f);
            // Underwater engine thrust
            float engineAcceleration =
                profile.EngineAcceleration *
                profile.UnderwaterEngineMultiplier;
            // Net acceleration
            speed += (engineAcceleration - dragAcceleration) * dt;
            // Don't allow the missile to stall
            if (speed < profile.MinimumSpeed)
                speed = profile.MinimumSpeed;
            missile.Physics.LinearVelocity =
                Vector3.Normalize(velocity) * speed;
            // Reduce maneuverability underwater
            if (profile.UnderwaterTurnMultiplier < 1f)
            {
                missile.Physics.AngularVelocity *=
                    profile.UnderwaterTurnMultiplier;
            }*/
        }
        private static void ClearEffects()
        {
            // Stop particle effects first
            foreach (MyParticleEffect effect in missileeffects.Values)
            {
                if (effect == null)
                    continue;
                effect.Stop(true);
                effect.Autodelete = true;
                MyParticlesManager.RemoveParticleEffect(effect);
            }
            missileeffects.Clear();
        }
        public static void ClearAll()
        {
            ClearEffects();
            MissileUpdateIds.Clear();
            PendingMissileAdds.Clear();
            PendingMissileRemovals.Clear();
            PendingMissileStates.Clear();
        }
    }
}
