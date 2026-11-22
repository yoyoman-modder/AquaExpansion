using AquaExpansion.Core.Animals;
using Jakaria.API;
using Sandbox.Game;
using Sandbox.Game.Entities.Character.Components;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Input;
using VRageMath;

namespace AquaExpansion.Core
{
    public sealed class AquaJetpackUnderWaterSystem
    {
        private float MaxWorkingDepth = 100f;
        private float minJetpackstartDepth = 2f;
        private readonly Dictionary<long, PlayerDiverState> playerStates = new Dictionary<long, PlayerDiverState>();
        public bool TryGetPlayerState(long identityId,out int gearLevel,out bool oxygenRefillActive)
        {
            gearLevel = 0;
            oxygenRefillActive = false;
            PlayerDiverState state;
            if (!playerStates.TryGetValue(identityId,out state))
            {
                return false;
            }
            gearLevel = state.GearLevel;
            oxygenRefillActive = state.OxygenRefillActive;
            return true;
        }
        private AquaDiverGearDefinition GetPlayerGear(IMyCharacter character)
        {
            if (character == null || character.IsDead || character.Closed)
                return AquaDiverGearDatabase.Get(0);
            IMyInventory charInv;
            MyInventory charInvE;
            AquaExpansionSession.Insance.GetCharacterInventory(
                character,
                out charInv,
                out charInvE);
            if (charInvE == null)
                return AquaDiverGearDatabase.Get(0);
            AquaDiverGearDefinition bestGear = AquaDiverGearDatabase.Get(0);
            var items = charInvE.GetItems();
            foreach (var item in items)
            {
                string subtype = item.Content.GetId().SubtypeId.ToString();
                AquaDiverGearDefinition gear = AquaDiverGearDatabase.Get(subtype);
                if (gear != null && gear.Level > bestGear.Level)
                    bestGear = gear;
            }
            return bestGear;
        }
        public void SetDiverMode(IMyCharacter character, long ID, int tick)
        {
            if (character == null ||
                character.Closed ||
                character.IsDead)
            {
                RemovePID(ID);
                return;
            }
            AddPID(ID);
            PlayerDiverState state;
            if (!playerStates.TryGetValue(ID, out state))
                return;
            float delta = MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
            bool underwater = WaterModAPI.IsUnderwater(character.GetPosition());
            if (!underwater)
            {
                state.GearLevel = 0;
                state.OxygenRefillActive = false;
                state.WasNearSeabed = false;
                state.LoadGraceTicks = 0;
                if (state.PID != null)
                    state.PID.Reset();
                return;
            }
            float depth = AquaExpansionSession.Insance.GetWaterDepthbyCharacter(character);
            float salt = AquaExpansionSession.Insance.GetSaltlevelbyPlayer(character, depth);
            //int gearlevel;
            SeaCreatureBase animal = character.GameLogic.GetAs<SeaCreatureBase>();
            AquaDiverGearDefinition gear;
            if (animal != null)
                gear = AquaDiverGearDatabase.Get(0);
            else
                gear = GetPlayerGear(character);
            if (gear == null)
                return;
            state.GearLevel = gear.Level;
            if (state.GearLevel > 0)
            {
                UpdateUnderwaterJetpack(character,delta,tick,ID);
            }
            else
            {
                UpdateSeabedMovement(character,ID,depth);
                if (tick % 7 != 0)
                    return;
                state.OxygenRefillActive = false;
                UpdateUnderwaterMovement(character, delta, depth, salt, ID);
            }
        }
        /// <summary>
        /// Call this every tick for each character to handle underwater propulsion and oxygen refill.
        /// </summary>
        private void UpdateUnderwaterJetpack(IMyCharacter character, float deltaTime, int tick, long ID)
        {
            if (character == null ||
                character.IsDead ||
                character.Closed)
            {
                RemovePID(ID);
                return;
            }
            PlayerDiverState state;
            if (!playerStates.TryGetValue(ID, out state))
            {
                AddPID(ID);
                if (!playerStates.TryGetValue(ID, out state))
                    return;
            }
            if (!AquaExpansionSession.Insance.IsCharacterUnderwater(character))
            {
                state.GearLevel = 0;
                state.OxygenRefillActive = false;
                state.WasNearSeabed = false;
                state.LoadGraceTicks = 0;
                if (state.PID != null)
                    state.PID.Reset();
                return;
            }
            float depth = AquaExpansionSession.Insance.GetWaterDepthbyCharacter(character);
            float salt = AquaExpansionSession.Insance.GetSaltlevelbyPlayer(character, depth);
            UpdateSeabedMovement(character, ID, depth);
            if (tick % 7 != 0)
                return;
            UpdateUnderwaterMovement(character, deltaTime, depth, salt, ID);
            RefillOxygen(character, deltaTime, depth, ID, salt);
        }
        private void UpdateUnderwaterMovement(IMyCharacter character, float deltaTime, float depth, float saltLevel,long ID)
        {
            if (character == null ||
                character.IsDead ||
                character.Closed)
            {
                RemovePID(ID);
                return;
            }
            PlayerDiverState state;
            if (!playerStates.TryGetValue(ID, out state))
            {
                AddPID(ID);
                if (!playerStates.TryGetValue(ID, out state))
                    return;
            }
            if (!AquaExpansionSession.Insance.IsCharacterUnderwater(character))
            {
                if (state.PID != null)
                    state.PID.Reset();
                return;
            }
            var oxygenProvider = MyAPIGateway.Session.OxygenProviderSystem;
            if (oxygenProvider == null)
                return;
            float eox = oxygenProvider.GetOxygenInPoint(character.GetPosition());
            float ingridox;
            AquaExpansionSession.Insance.GetInAirtightGrid(character, out ingridox);
            if (eox > AquaExpansionSession.Insance.MIN_ENVOXYGENLEVEL ||
                ingridox > AquaExpansionSession.Insance.MIN_ENVOXYGENLEVEL)
            {
                if (state.PID != null)
                    state.PID.Reset();
                return;
            }
            SeaCreatureBase animal = character.GameLogic.GetAs<SeaCreatureBase>();
            if (animal == null)
            {
                var j = character.Components.Get<MyCharacterJetpackComponent>();
                if (j != null &&
                    depth < -minJetpackstartDepth &&
                    j.TurnedOn)
                {
                    j.TurnOnJetpack(false);
                }
            }
            var physics = character.Physics;
            if (physics == null)
                return;
            AquaDiverGearDefinition gear = AquaDiverGearDatabase.Get(state.GearLevel);
            if (gear == null)
                return;
            float saltNormalized = MathHelper.Clamp(saltLevel / 3f, 0f, 1f);
            float effectiveSalt = saltNormalized * (1f - gear.GearSaltFilterLevel);
            float saltPenalty = MathHelper.Clamp(1f - effectiveSalt, 0.4f, 1f);
            float depthFactor = MathHelper.Clamp(depth / -MaxWorkingDepth, 0f, 1f);
            float depthBoost = 1f + depthFactor * (gear.GearBoost - 1f);
            float maxSpeed = gear.GearmaxSpeed * depthBoost * saltPenalty;
            Vector3D vel = physics.LinearVelocity;
            Vector3D gravity = physics.Gravity;
            if (gravity.LengthSquared() < 0.01)
                return;
            Vector3D upDir = -Vector3D.Normalize(gravity);
            double verticalVel = Vector3D.Dot(vel, upDir);
            Vector3D horizontalVel = vel - upDir * verticalVel;
            Vector3D input = animal == null ? GetInputDirection() : Vector3D.Zero;
            MatrixD matrix = character.WorldMatrix;
            Vector3D move = matrix.Forward * input.Z + matrix.Right * input.X;
            if (move.LengthSquared() > 0.001)
            {
                move.Normalize();
                Vector3D targetHorizontal = move * maxSpeed;
                double t = 1.0 - Math.Exp(-4.0 * deltaTime);
                horizontalVel = Vector3D.Lerp(horizontalVel, targetHorizontal, t);
            }
            else
            {
                float baseDrag = MathHelper.Lerp(0.95f, 0.88f, depthFactor);
                float drag = baseDrag * MathHelper.Lerp(1f, 0.9f, 1f - saltPenalty);
                horizontalVel *= drag;
            }
            double hLenSq = horizontalVel.LengthSquared();
            if (hLenSq > maxSpeed * maxSpeed && hLenSq > 0.0001)
            {
                horizontalVel = horizontalVel / Math.Sqrt(hLenSq) * maxSpeed;
            }
            double targetVertical = gear.SinkBias * MathHelper.Lerp(1f, 0.75f, 1f - saltPenalty);
            double currentVertical = Vector3D.Dot(vel, upDir);
            double vt = 1.0 - Math.Exp(-8.0 * deltaTime);
            verticalVel = MathHelper.Lerp(currentVertical, targetVertical, vt);
            Vector3D finalVel = horizontalVel + upDir * verticalVel;
            double hardCap = maxSpeed + 5.0;
            double fLenSq = finalVel.LengthSquared();
            if (fLenSq > hardCap * hardCap)
            {
                finalVel = finalVel / Math.Sqrt(fLenSq) * hardCap;
            }
            physics.LinearVelocity = finalVel;
            double targetSinkSpeed = 0.5f;
            if (state.PID != null)
            {
                state.PID.Update(character,deltaTime,depth,targetSinkSpeed,verticalVel,gear);
            }
        }
        private void RefillOxygen(IMyCharacter character, float deltaTime, float depth, long ID, float salt)
        {
            if (MyAPIGateway.Multiplayer != null &&
                !MyAPIGateway.Multiplayer.IsServer)
            {
                return;
            }
            if (character == null ||
                character.Closed ||
                character.IsDead)
                return;
            PlayerDiverState state;
            if (!playerStates.TryGetValue(ID, out state))
            {
                AddPID(ID);
                if (!playerStates.TryGetValue(ID, out state))
                    return;
            }
            SeaCreatureBase animal = character.GameLogic.GetAs<SeaCreatureBase>();
            if (animal != null)
                return;
            var helmet = MyVisualScriptLogicProvider.GetPlayersHelmetStatus(ID);
            var energy = MyVisualScriptLogicProvider.GetPlayersEnergyLevel(ID);
            var oxygenProvider = MyAPIGateway.Session.OxygenProviderSystem;
            if (oxygenProvider == null)
                return;
            float eox = oxygenProvider.GetOxygenInPoint(character.GetPosition());
            float ingridox;
            AquaExpansionSession.Insance.GetInAirtightGrid(character, out ingridox);
            if (!helmet ||
                energy <= 0f ||
                eox > AquaExpansionSession.Insance.MIN_ENVOXYGENLEVEL ||
                ingridox > AquaExpansionSession.Insance.MIN_ENVOXYGENLEVEL)
            {
                state.OxygenRefillActive = false;
                return;
            }
            float currentO2 = MyVisualScriptLogicProvider.GetPlayersOxygenLevel(ID);
            if (currentO2 >= 99.5f)
            {
                state.OxygenRefillActive = false;
                MyVisualScriptLogicProvider.SetPlayersOxygenLevel(ID, 100f);
                return;
            }
            AquaDiverGearDefinition gear = AquaDiverGearDatabase.Get(state.GearLevel);
            if (gear == null)
            {
                state.OxygenRefillActive = false;
                return;
            }
            float underwaterFactor = AquaExpansionSession.Insance.GetUnderWaterPercent(character);
            if (depth < -gear.GearO2MaxRefillDepth || underwaterFactor < 1f)
            {
                state.OxygenRefillActive = false;
                return;
            }
            state.OxygenRefillActive = true;
            float saltNormalized = MathHelper.Clamp(salt / 3f, 0f, 1f);
            float effectiveSalt = saltNormalized * (1f - gear.GearSaltFilterLevel);
            float saltPenalty = MathHelper.Clamp(1f - effectiveSalt, 0.2f, 1f);
            float depthFactor = MathHelper.Clamp(depth / -gear.GearO2MaxRefillDepth, 0f, 1f);
            float depthBoost = 1f + depthFactor * (gear.GearO2RefillRate - 1f);
            depthBoost = MathHelper.Clamp(depthBoost,1f,gear.GearO2RefillRate);
            float refill = gear.GearO2RefillRate * depthBoost * saltPenalty * deltaTime;
            float targetO2 = currentO2 + refill;
            if (targetO2 > 100f)
                targetO2 = 100f;
            MyVisualScriptLogicProvider.SetPlayersOxygenLevel(ID, targetO2);
        }
        private void UpdateSeabedMovement(IMyCharacter character, long ID, float depth)
        {
            if (character == null ||
                character.IsDead ||
                character.Closed)
                return;
            PlayerDiverState state;
            if (!playerStates.TryGetValue(ID,out state))
            {
                AddPID(ID);
                if (!playerStates.TryGetValue(ID,out state))
                {
                    return;
                }
            }
            var physics = character.Physics;
            if (physics == null)
                return;
            Vector3D position = character.GetPosition();
            bool underwater = AquaExpansionSession.Insance.IsCharacterUnderwater(character);
            float underwaterPercent = AquaExpansionSession.Insance.GetUnderWaterPercent(character);
            var oxygenProvider = MyAPIGateway.Session.OxygenProviderSystem;
            if (oxygenProvider == null)
                return;
            float oxygen = oxygenProvider.GetOxygenInPoint(position);
            float gridOxygen;
            AquaExpansionSession.Insance.GetInAirtightGrid(character,out gridOxygen);
            if (!underwater ||
                underwaterPercent < 1f ||
                oxygen > AquaExpansionSession.Insance.MIN_ENVOXYGENLEVEL ||
                gridOxygen > AquaExpansionSession.Insance.MIN_ENVOXYGENLEVEL)
            {
                character.CanSprint = true;
                state.WasNearSeabed = false;
                state.LoadGraceTicks = 0;
                return;
            }
            if (state.LoadGraceTicks < 10)
            {
                state.LoadGraceTicks++;
                return;
            }
            Vector3D from = position;
            Vector3D to = position + character.WorldMatrix.Down * 1.5;
            IHitInfo hit;
            bool grounded = MyAPIGateway.Physics.CastRay(from,to,out hit);
            Vector3D velocity = physics.LinearVelocity;
            Vector3D up = character.WorldMatrix.Up;
            Vector3D horizontalVelocity = Vector3D.Reject(velocity,up);
            double horizontalSpeed = horizontalVelocity.Length();
            if (grounded)
            {
                character.CanSprint = false;
                if (!state.WasNearSeabed)
                {
                    state.PID.Reset();
                }
                state.WasNearSeabed = true;
                double maxSpeed = 2.5;
                if (horizontalSpeed > maxSpeed &&
                    horizontalSpeed > 0.0001)
                {
                    horizontalVelocity = Vector3D.Normalize(horizontalVelocity) * maxSpeed;
                    Vector3D verticalVelocity = Vector3D.ProjectOnVector(ref velocity,ref up);
                    verticalVelocity *= 0.98;
                    physics.LinearVelocity = horizontalVelocity + verticalVelocity;
                }
            }
            else
            {
                character.CanSprint = true;
                state.WasNearSeabed = false;
            }
        }
        private Vector3D GetInputDirection()
        {
            var ctrl = MyAPIGateway.Input;
            Vector3D dir = Vector3D.Zero;
            if (ctrl.IsKeyPress(MyKeys.W)) dir += Vector3D.Forward;
            if (ctrl.IsKeyPress(MyKeys.S)) dir += Vector3D.Backward;
            if (ctrl.IsKeyPress(MyKeys.A)) dir += Vector3D.Left;
            if (ctrl.IsKeyPress(MyKeys.D)) dir += Vector3D.Right;
            return dir;
        }
        public void AddPID(long id)
        {
            if (id == 0)
                return;
            PlayerDiverState state;
            if (!playerStates.TryGetValue(id, out state))
            {
                state = new PlayerDiverState();
                state.PID = new UnderwaterBuoyancyPID();
                state.LoadGraceTicks = 0;
                state.WasNearSeabed = false;
                state.GearLevel = 0;
                state.OxygenRefillActive = false;
                playerStates[id] = state;
            }
        }
        private void RemovePID(long id)
        {
            PlayerDiverState state;
            if (playerStates.TryGetValue(id, out state))
            {
                if (state.PID != null)
                    state.PID.Reset();
                playerStates.Remove(id);
            }
        }
        public void Clear()
        {
            foreach (KeyValuePair<long, PlayerDiverState> pair in playerStates)
            {
                if (pair.Value != null &&
                    pair.Value.PID != null)
                {
                    pair.Value.PID.Reset();
                }
            }
            playerStates.Clear();
        }
        public sealed  class UnderwaterBuoyancyPID
        {
            private Vector3D position;
            private Vector3D gravity;
            private float interference;
            private double mass;
            private Vector3D gravityDir;
            private Vector3D velocity;
            private double verticalSpeed;
            private Vector3D counterForce;
            private Vector3D buoyancyForce;
            private bool initializedVelocity;
            public  void Reset()
            {
                buoyancyForce = Vector3D.Zero;
                counterForce = Vector3D.Zero;
            }
            /*private void Stabilize(IMyCharacter character)
            {
                if (!initializedVelocity)
                {
                    Reset();
                    var physics = character.Physics;
                    if (physics == null)
                        return;
                    Vector3D vel = physics.LinearVelocity;
                    physics.LinearVelocity = Vector3D.Zero;
                    physics.AngularVelocity = Vector3D.Zero;
                    double vertical = Vector3D.Dot(vel, gravityDir);
                    if (Math.Abs(vertical) < 0.15)
                    {
                        vel -= gravityDir * vertical;
                        physics.LinearVelocity = vel;
                    }
                    initializedVelocity = true;
                }
            }*/
            public void Update(IMyCharacter character,float deltaTime,double depth,double targetSinkSpeed,double VertSpeeed,AquaDiverGearDefinition gear)
            {

                if (character == null ||
                    character.Closed ||
                    character.IsDead ||
                    gear == null)
                {
                    Reset();
                    return;
                }
                if (!AquaExpansionSession.Insance.IsCharacterUnderwater(character))
                {
                    Reset();
                    return;
                }
                if (deltaTime <= 0f)
                    return;
                var physics = character.Physics;
                if (physics == null)
                    return;
                position = character.GetPosition();
                gravity = MyAPIGateway.Physics.CalculateNaturalGravityAt(position,out interference);
                if (gravity.LengthSquared() < 0.000001)
                    return;
                gravityDir = Vector3D.Normalize(gravity);
                velocity = physics.LinearVelocity;
                mass = physics.Mass;
                verticalSpeed = Vector3D.Dot(velocity, gravityDir);
                double buoyancyFactor = gear.BuoyancyFactor;
                double maxSinkSpeed = gear.MaxSinkSpeed;
                double swimForce = gear.SwimForce;
                double minHoverDepth = gear.HoverMinDepth;
                double maxHoverDepth = gear.HoverMaxDepth;
                double depthAbs = Math.Abs(depth);
                bool insideHoverRange =
                    depthAbs >= minHoverDepth &&
                    depthAbs <= maxHoverDepth;
                if (!insideHoverRange)
                {
                    maxSinkSpeed *= 2.0;
                    Reset();
                }
                buoyancyForce = -gravity * mass * buoyancyFactor;
                physics.AddForce(
                    MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE,
                    buoyancyForce,
                    position,
                    null);
                if (verticalSpeed > maxSinkSpeed)
                {
                    double excess = verticalSpeed - maxSinkSpeed;
                    counterForce =
                        -gravityDir *
                        excess *
                        mass *
                        1.5;
                    physics.AddForce(
                        MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE,
                        counterForce,
                        position,
                        null);
                }
                SeaCreatureBase animal = character.GameLogic.GetAs<SeaCreatureBase>();
                bool jumpPressed = false;
                bool crouchPressed = false;
                if (animal == null)
                {
                    jumpPressed = MyAPIGateway.Input.IsGameControlPressed(MyControlsSpace.JUMP);
                    crouchPressed = MyAPIGateway.Input.IsGameControlPressed(MyControlsSpace.CROUCH);
                }
                if (jumpPressed)
                {
                    Vector3D upwardForce =
                        -gravityDir *
                        mass *
                        swimForce;
                    physics.AddForce(
                        MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE,
                        upwardForce,
                        position,
                        null);
                }
                if (crouchPressed)
                {
                    Vector3D downwardForce =
                        gravityDir *
                        mass *
                        swimForce *
                        0.3f;
                    physics.AddForce(
                        MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE,
                        downwardForce,
                        position,
                        null);
                }
                //debug
                /*AquaExpansionSession.Insance.Log(true,
                   $"Depth {depthAbs:0.0} HoverRange {minHoverDepth}-{maxHoverDepth} InsideRange {insideHoverRange}" +
                   $"\nVertSpeed {verticalSpeed:0.00} MaxSink {maxSinkSpeed:0.00}" +
                   $"\nBuoyancy {buoyancyFactor:0.00}  counterForce {counterForce}");*/
            }
        }
        private sealed class PlayerDiverState
        {
            public UnderwaterBuoyancyPID PID;
            public int LoadGraceTicks;
            public bool WasNearSeabed;
            public int GearLevel;
            public bool OxygenRefillActive;
        }
    }
}
