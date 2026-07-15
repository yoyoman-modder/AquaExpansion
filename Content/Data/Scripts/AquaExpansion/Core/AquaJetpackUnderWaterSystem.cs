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
using VRage.Utils;
using VRageMath;



namespace AquaExpansion.Core
{
    public struct DivingGearData
    {
        public int Level;
        public float GearmaxSpeed;
        public float GearBoost;
        public float GearO2MaxRefillDepth;
        public float GearO2RefillRate;
        public float GearSaltFilterLevel;
        public double SurfaceFloatingMin;    // min surface
        public double SurfaceFloatingMax;      // max surface
        public double MaxSinkSpeed; 
        public float SinkBias;
        public double HoverMinDepth;
        public double HoverMaxDepth;
        public double BuoyancyFactor;
        public double SwimForce;
        public double MaxseabedSpeed;

        public DivingGearData(
        int level,
        float maxSpeed,
        float boost,
        float maxRefillDepth,
        float refillRate,
        float saltFilter,
        double surfaceFloatingMin,
        double surfaceFloatingMax,
        double maxSinkSpeed,
        double hoverMinDepth,
        double hoverMaxDepth,
        double buoyancyFactor,
        double swimForce,
        float sinkBias,
        double maxSeabedSpeed)

        {
            Level = level;
            GearmaxSpeed = maxSpeed;
            GearBoost = boost;
            GearO2MaxRefillDepth = maxRefillDepth;
            GearO2RefillRate = refillRate;
            GearSaltFilterLevel = saltFilter;
            SurfaceFloatingMin = surfaceFloatingMin;
            SurfaceFloatingMax = surfaceFloatingMax;
            MaxSinkSpeed = maxSinkSpeed;
            SinkBias = sinkBias;
            HoverMinDepth = hoverMinDepth;
            HoverMaxDepth = hoverMaxDepth;
            BuoyancyFactor = buoyancyFactor;
            SwimForce = swimForce;
            MaxseabedSpeed = maxSeabedSpeed;
        }
    }

    public class AquaJetpackUnderWaterSystem
    {
        private Dictionary<string, MyObjectBuilder_ThrustDefinition> OriginalThrusterData = new Dictionary<string, MyObjectBuilder_ThrustDefinition>();
        // Underwater jetpack settings
        private float MaxWorkingDepth = 100f;
        private float UnderwaterMaxSpeed = 10f;
        private float OxygenRefillRate = 0.1f;           // Oxygen base refill
        private float MaxDepthForRefill = 50f;
        private float minJetpackstartDepth = 2f;
        public static readonly Dictionary<MyStringHash, DivingGearData> GearData =
            new Dictionary<MyStringHash, DivingGearData>
        {
                {
                 MyStringHash.GetOrCompute("NoGear"),
                 new DivingGearData{ Level = 0,
                     GearmaxSpeed = 4f,
                     GearBoost = 1f,
                     GearO2MaxRefillDepth = 0f,
                     GearO2RefillRate = 0.5f,
                     GearSaltFilterLevel = 0f,
                     SurfaceFloatingMin = 0.0,
                     SurfaceFloatingMax = 2.0,
                     MaxSinkSpeed = 4.0,
                     SinkBias = 6.0f,
                     HoverMinDepth = 0.0,
                     HoverMaxDepth = 2.0,
                     BuoyancyFactor = 0.02,
                     SwimForce = -0.9,
                     MaxseabedSpeed = 2.0
                 }
                },
                {
                MyStringHash.GetOrCompute("AquaDiveGearT1"),
                new DivingGearData { Level = 1,
                    GearmaxSpeed = 6f,
                    GearBoost = 1.1f,
                    GearO2MaxRefillDepth = 30f,
                    GearO2RefillRate = 0.5f,
                    GearSaltFilterLevel = 0.3f,
                    SurfaceFloatingMin = 0.0,
                    SurfaceFloatingMax = 2.0,
                    MaxSinkSpeed = 3.0,
                    SinkBias = 3.5f,
                    HoverMinDepth = 2.0,
                    HoverMaxDepth = 15.0,
                    BuoyancyFactor = 0.05,
                    SwimForce = -0.5,
                    MaxseabedSpeed = 3.0
                }
                },
                {
                MyStringHash.GetOrCompute("AquaDiveGearT2"),
                new DivingGearData { Level = 2,
                    GearmaxSpeed = 8f,
                    GearBoost = 1.25f,
                    GearO2MaxRefillDepth = 60f,
                    GearO2RefillRate = 0.9f,
                    GearSaltFilterLevel = 0.5f,
                    SurfaceFloatingMin = 0.0,
                    SurfaceFloatingMax = 2.0,
                    MaxSinkSpeed = 2.5,
                    SinkBias = 1.5f,
                    HoverMinDepth = 15.0,
                    HoverMaxDepth = 40.0,
                    BuoyancyFactor = 0.08,
                    SwimForce = -0.25,
                    MaxseabedSpeed = 4.0
                }
                },
                {
                MyStringHash.GetOrCompute("AquaDiveGearT3"),
                new DivingGearData { Level = 3,
                    GearmaxSpeed = 11f,
                    GearBoost = 1.50f,
                    GearO2MaxRefillDepth = 100f,
                    GearO2RefillRate = 1.2f,
                    GearSaltFilterLevel = 0.7f,
                    SurfaceFloatingMin = 0.0,
                    SurfaceFloatingMax = 2.0,
                    MaxSinkSpeed = 1.5,
                    SinkBias = -0.5f,
                    HoverMinDepth = 20.0,
                    HoverMaxDepth = 300.0,
                    BuoyancyFactor = 0.1,
                    SwimForce = -0.1,
                    MaxseabedSpeed = 5.0
                }
                }
        };
        public int PlayerGearlevelIndx = 0;
        public bool PlayerOxygenRefillActive = false;
        private UnderwaterBuoyancyPID PID;
        private Dictionary<long, UnderwaterBuoyancyPID> playerPID = new Dictionary<long, UnderwaterBuoyancyPID>();
        private int loadGraceTicks = 0;
        private bool wasNearSeabed = false;
        public void GetDiverGearLevel(IMyCharacter character, out int gearlevel)
        {
            gearlevel = 0;
            if (character == null || character.IsDead || character.Closed)
                return;
            if (!WaterModAPI.IsUnderwater(character.WorldMatrix.Translation))
                return;
            IMyInventory charInv;
            MyInventory charInvE;
            AquaExpansionSession.Insance.GetCharacterInventory(character, out charInv, out charInvE);
            if (charInv == null)
                return;
            int bestlevel = 0;
            var items = charInvE.GetItems();
            foreach (var item in items)
            {
                DivingGearData gear;
                var subtype = item.Content.GetId().SubtypeId;
                if (GearData.TryGetValue(subtype, out gear))
                {
                    if (gear.Level > bestlevel)
                        bestlevel = gear.Level;
                }
            }
            gearlevel = bestlevel;
        }

        public void SetDiverMode(IMyCharacter character, long ID, int tick)
        {
            if (character == null && character.Closed && character.IsDead)
                return;
            float delta = MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS; // changed
            bool underwater = WaterModAPI.IsUnderwater(character.GetPosition());

            if (!underwater)
                return;
            var depth = AquaExpansionSession.Insance.GetWaterDepthbyCharacter(character);
            var salt = AquaExpansionSession.Insance.GetSaltlevelbyPlayer(character, depth);
            int gearlevel;
            GetDiverGearLevel(character, out gearlevel);
            if (gearlevel > 0)
            {
                UpdateUnderwaterJetpack(character, delta, tick, ID, gearlevel);
            }
            else
            {
                UpdateSeabedMovement(character, ID, depth);
                if (tick % 10 != 0) // every ~0.3 sec
                    return;
                PlayerOxygenRefillActive = false;
                UpdateUnderwaterMovement(character, delta, depth, salt, 0, ID);
            }
            PlayerGearlevelIndx = gearlevel;
        }

        /// <summary>
        /// Call this every tick for each character to handle underwater propulsion and oxygen refill.
        /// </summary>
        private void UpdateUnderwaterJetpack(IMyCharacter character, float deltaTime, int tick, long ID, int glevel)
        {
            if (character == null || character.IsDead || character.Closed)
                return;
            // Only active underwater
            if (!WaterModAPI.IsUnderwater(character.GetPosition()))
                return;
            float depth = AquaExpansionSession.Insance.GetWaterDepthbyCharacter(character);
            float salt = AquaExpansionSession.Insance.GetSaltlevelbyPlayer(character, depth);
            UpdateSeabedMovement(character, ID, depth);
            if (tick % 10 != 0) // every ~0.3 sec
                return;
            UpdateUnderwaterMovement(character, deltaTime, depth, salt, glevel, ID);
            RefillOxygen(character, deltaTime, depth, ID, salt, glevel);
        }

        private void UpdateUnderwaterMovement(IMyCharacter character, float deltaTime, float depth, float saltLevel, int glevel, long ID)
        {
            if (character == null || character.IsDead || character.Closed)
            {
                RemovePID(ID);
                return;
            }
            if (!WaterModAPI.IsUnderwater(character.GetPosition()))
            {
                RemovePID(ID);
                return;
            }
            var eox = MyAPIGateway.Session.OxygenProviderSystem.GetOxygenInPoint(character.GetPosition());
            float ingridox;
            AquaExpansionSession.Insance.GetInAirtightGrid(character, out ingridox);
            if (eox > AquaExpansionSession.Insance.MIN_ENVOXYGENLEVEL || ingridox > AquaExpansionSession.Insance.MIN_ENVOXYGENLEVEL 
                || AquaExpansionSession.Insance.IsPlayerProtected(MyAPIGateway.Session?.Player))
            {
                RemovePID(ID);
                return;
            }
            // Get live jetpack component
            var j = character.Components.Get<MyCharacterJetpackComponent>();
            if (j != null)
            {
                // Turn off default hydrogen jetpack underwater
                if (depth < -minJetpackstartDepth && j.TurnedOn)
                {
                    j.TurnOnJetpack(false);
                }
            }
            var physics = character.Physics;
            if (physics == null)
                return;
            var gearsubtype = "";
            switch (glevel)
            {
                case 0:
                    gearsubtype = "NoGear";
                    break;
                case 1:
                    gearsubtype = "AquaDiveGearT1";
                    break;
                case 2:
                    gearsubtype = "AquaDiveGearT2";
                    break;
                case 3:
                    gearsubtype = "AquaDiveGearT3";
                    break;
            }
            var subtype = MyStringHash.GetOrCompute(gearsubtype);
            DivingGearData gear;
            if (!GearData.TryGetValue(subtype, out gear))
            {
                //def
                gear = new DivingGearData
                {
                    GearBoost = 1f,
                    GearmaxSpeed = UnderwaterMaxSpeed,
                    GearSaltFilterLevel = 0f,
                    GearO2RefillRate = OxygenRefillRate,
                    GearO2MaxRefillDepth = MaxDepthForRefill,
                    SinkBias = 2.0f,
                };
            }
            //salt penalty
            float saltNormalized = MathHelper.Clamp(saltLevel / 3f, 0f, 1f);
            float effectiveSalt = saltNormalized * (1f - gear.GearSaltFilterLevel);
            float saltPenalty = 1f - effectiveSalt;
            saltPenalty = MathHelper.Clamp(saltPenalty, 0.4f, 1f);
            // --- Depth-based boost ---
            float depthFactor = MathHelper.Clamp(depth / -MaxWorkingDepth, 0f, 1f);
            float depthBoost = 1f + depthFactor * (gear.GearBoost - 1f);
            //gear boost
            float maxSpeed = gear.GearmaxSpeed * depthBoost * saltPenalty;
            // --- Movement calculations ---
            Vector3D vel = physics.LinearVelocity;
            Vector3D gravity = physics.Gravity;
            if (gravity.LengthSquared() < 0.01)
                return;
            Vector3D upDir = -Vector3D.Normalize(gravity);
            double verticalVel = Vector3D.Dot(vel, upDir);
            Vector3D horizontalVel = vel - upDir * verticalVel;
            // input
            var matrix = character.WorldMatrix;
            Vector3D input = GetInputDirection();
            Vector3D move =
                matrix.Forward * input.Z +
                matrix.Right * input.X;
            // horizontal
            if (move.LengthSquared() > 0.001)
            {
                move.Normalize();
                Vector3D targetHorizontal = move * maxSpeed;
                double t = 1.0 - Math.Exp(-5.0 * deltaTime);
                horizontalVel = Vector3D.Lerp(horizontalVel, targetHorizontal, t);
            }
            else
            {
                // stable custom drag (salt affects thickness)
                float baseDrag = MathHelper.Lerp(0.95f, 0.88f, depthFactor);
                float drag = baseDrag * MathHelper.Lerp(1f, 0.9f, 1f - saltPenalty);
                horizontalVel *= drag;
            }
            // --- clamp horizontal ---
            double hLenSq = horizontalVel.LengthSquared();
            if (hLenSq > maxSpeed * maxSpeed && hLenSq > 0.0001)
            {
                horizontalVel = horizontalVel / Math.Sqrt(hLenSq) * maxSpeed;
            }
            double targetVertical;
            targetVertical = gear.SinkBias *
                MathHelper.Lerp(1f, 0.75f, 1f - saltPenalty);
            double currentVertical = Vector3D.Dot(vel, upDir);
            // smooth response
            double vt = 1.0 - Math.Exp(-8.0 * deltaTime);
            verticalVel = MathHelper.Lerp(currentVertical, targetVertical, vt);
            // final
            Vector3D finalVel = horizontalVel + upDir * verticalVel;
            // safety clamp
            double hardCap = maxSpeed + 5.0;
            double fLenSq = finalVel.LengthSquared();
            if (fLenSq > hardCap * hardCap)
            {
               finalVel = finalVel / Math.Sqrt(fLenSq) * hardCap;
            }
            physics.LinearVelocity = finalVel;
            double targetSinkSpeed = 0.5f;
            PID.Update(character, deltaTime, depth, targetSinkSpeed, glevel, verticalVel, gear);
        }

        private void RefillOxygen(IMyCharacter character, float deltaTime, float depth, long ID, float salt, int glevel)
        {
            if (character == null || character.Closed || character.IsDead)
                return;
            var helmet = MyVisualScriptLogicProvider.GetPlayersHelmetStatus(ID);
            var energy = MyVisualScriptLogicProvider.GetPlayersEnergyLevel(ID); //changes
            var eox = MyAPIGateway.Session.OxygenProviderSystem.GetOxygenInPoint(character.GetPosition());
            float ingridox;
            AquaExpansionSession.Insance.GetInAirtightGrid(character, out ingridox);
            if (!helmet || energy <= 0f || eox > AquaExpansionSession.Insance.MIN_ENVOXYGENLEVEL || ingridox > AquaExpansionSession.Insance.MIN_ENVOXYGENLEVEL ||
                AquaExpansionSession.Insance.IsPlayerProtected(MyAPIGateway.Session?.Player))
            {
                PlayerOxygenRefillActive = false;
                return;
            }
            float currentO2 = MyVisualScriptLogicProvider.GetPlayersOxygenLevel(ID);
            if (currentO2 >= 99.5f)
            {
                MyVisualScriptLogicProvider.SetPlayersOxygenLevel(ID, 100f);
                return;
            }
            //gear data
            var gearsubtype = "";
            switch (glevel)
            {
                case 0:
                    gearsubtype = "NoGear";
                    break;
                case 1:
                    gearsubtype = "AquaDiveGearT1";
                    break;
                case 2:
                    gearsubtype = "AquaDiveGearT2";
                    break;
                case 3:
                    gearsubtype = "AquaDiveGearT3";
                    break;
            }
            var subtype = MyStringHash.GetOrCompute(gearsubtype);
            DivingGearData gear;
            if (!GearData.TryGetValue(subtype, out gear))
            {
                //def
                gear = new DivingGearData
                {
                    GearBoost = 1f,
                    GearmaxSpeed = UnderwaterMaxSpeed,
                    GearSaltFilterLevel = 0f,
                    GearO2RefillRate = 0.5f,
                    GearO2MaxRefillDepth = 50f
                };
            }
            // Only refill if within depth limit and 100% undewater
            float underwaterFactor = AquaExpansionSession.Insance.GetUnderWaterPercent(character);
            if (depth < -gear.GearO2MaxRefillDepth || underwaterFactor < 1f)
            {
                PlayerOxygenRefillActive = false;
                return;
            }
            PlayerOxygenRefillActive = true;
            //salt penalty
            float saltNormalized = MathHelper.Clamp(salt / 3f, 0f, 1f);
            float effectiveSalt = saltNormalized * (1f - gear.GearSaltFilterLevel);
            float saltPenalty = 1f - effectiveSalt;
            saltPenalty = MathHelper.Clamp(saltPenalty, 0.2f, 1f);
            //depth
            float depthFactor = MathHelper.Clamp(depth / -gear.GearO2MaxRefillDepth, 0f, 1f);
            float depthBoost = 1f + depthFactor * (gear.GearO2RefillRate - 1f);
            depthBoost = MathHelper.Clamp(depthBoost, 1f, gear.GearO2RefillRate);
            float refill = gear.GearO2RefillRate * depthBoost * saltPenalty * deltaTime;
            float targetO2 = currentO2 + refill;
            if (targetO2 > 100f)
                targetO2 = 100f;

            MyVisualScriptLogicProvider.SetPlayersOxygenLevel(ID, targetO2);
        }

        private void UpdateSeabedMovement(IMyCharacter character, long ID, float depth)
        {
            if (character == null || character.IsDead || character.Closed)
                return;
            var physics = character.Physics;
            if (physics == null)
                return;
            Vector3D pos = character.GetPosition();
            bool underwater = WaterModAPI.IsUnderwater(pos);
            float fullyUnderwater = AquaExpansionSession.Insance.GetUnderWaterPercent(character);
            var eox = MyAPIGateway.Session.OxygenProviderSystem.GetOxygenInPoint(character.GetPosition());
            float ingridox;
            AquaExpansionSession.Insance.GetInAirtightGrid(character, out ingridox);
            if (!underwater || fullyUnderwater < 1f || eox > AquaExpansionSession.Insance.MIN_ENVOXYGENLEVEL || 
                ingridox > AquaExpansionSession.Insance.MIN_ENVOXYGENLEVEL ||
                AquaExpansionSession.Insance.IsPlayerProtected(MyAPIGateway.Session?.Player))
            {
                character.CanSprint = true;
                wasNearSeabed = false;
                return;
            }
            if (loadGraceTicks < 10)
            {
                loadGraceTicks++;
                return;
            }
            Vector3D from = pos;
            Vector3D to = pos + character.WorldMatrix.Down * 1.5;
            IHitInfo hit;
            bool grounded = MyAPIGateway.Physics.CastRay(from, to, out hit);
            Vector3D vel = physics.LinearVelocity;
            Vector3D horizontalVel = Vector3D.Reject(vel, character.WorldMatrix.Up);
            double horizontalSpeed = horizontalVel.Length();
            bool nearSeabed = grounded;
            if (nearSeabed)
            {
                character.CanSprint = false;
                if (!wasNearSeabed)
                    PID.Reset();
                wasNearSeabed = true;
                double maxSpeed = 2.5;

                if (horizontalSpeed > maxSpeed)
                {
                    horizontalVel = Vector3D.Normalize(horizontalVel) * maxSpeed;
                    var v = character.WorldMatrix.Up;
                    vel *= 0.98;
                    physics.LinearVelocity = horizontalVel + Vector3D.ProjectOnVector(ref vel,ref v);
                }
               
                //AquaExpansionSession.Insance.Log(true, $"depth {depth:F1} maxSpeed {maxSpeed:F2} vel {vel.Length()}");
            }
            else
            {
                character.CanSprint = true;
                wasNearSeabed = false;
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
            if (!playerPID.TryGetValue(id, out PID))
            {
                PID = new UnderwaterBuoyancyPID();
                playerPID[id] = PID;
            }
        }

        private void RemovePID(long id)
        {
            if (playerPID.TryGetValue(id, out PID))
            {
                PID.Reset();
                playerPID.Remove(id);
            }
        }

        public  class UnderwaterBuoyancyPID
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

            private void Stabilize(IMyCharacter character)
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
            }

            public void Update(IMyCharacter character, float deltaTime, double depth, double targetSinkSpeed, int gearLevel, double VertSpeeed, DivingGearData gearData)
            {
                if (character == null || character.Closed || character.IsDead)
                {
                    Reset();
                    return;
                }
                if (!WaterModAPI.IsUnderwater(character.GetPosition()))
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
                gravity = MyAPIGateway.Physics.CalculateNaturalGravityAt(position, out interference);
                if (gravity.LengthSquared() < 0.000001)
                    return;
                gravityDir = Vector3D.Normalize(gravity);
                velocity = physics.LinearVelocity;
                mass = physics.Mass;
                verticalSpeed = Vector3D.Dot(velocity, gravityDir);
                double buoyancyFactor = 0.0;
                double maxSinkSpeed = 4.0;
                double swimForce = 0.0;
                double minHoverDepth = 0.0;
                double maxHoverDepth = 0.0;
                switch (gearLevel)
                {
                    default:
                    case 0:
                        buoyancyFactor = gearData.BuoyancyFactor;
                        maxSinkSpeed = gearData.MaxSinkSpeed;
                        swimForce = gearData.SwimForce;
                        minHoverDepth = gearData.HoverMinDepth;
                        maxHoverDepth = gearData.HoverMaxDepth;
                        break;

                    case 1:
                        buoyancyFactor = gearData.BuoyancyFactor;
                        maxSinkSpeed = gearData.MaxSinkSpeed;
                        swimForce = gearData.SwimForce;
                        minHoverDepth = gearData.HoverMinDepth;
                        maxHoverDepth = gearData.HoverMaxDepth;
                        break;

                    case 2:
                        buoyancyFactor = gearData.BuoyancyFactor;
                        maxSinkSpeed = gearData.MaxSinkSpeed;
                        swimForce = gearData.SwimForce;
                        minHoverDepth = gearData.HoverMinDepth;
                        maxHoverDepth = gearData.HoverMaxDepth;
                        break;

                    case 3:
                        buoyancyFactor = gearData.BuoyancyFactor;
                        maxSinkSpeed = gearData.MaxSinkSpeed;
                        swimForce = gearData.SwimForce;
                        minHoverDepth = gearData.HoverMinDepth;
                        maxHoverDepth = gearData.HoverMaxDepth;
                        break;
                }
                double depthAbs = Math.Abs(depth);
                bool insideHoverRange = depthAbs >= minHoverDepth && depthAbs <= maxHoverDepth;
                if (depthAbs < minHoverDepth)
                    insideHoverRange = false;
                // Outside supported depth
                // increase sink speed
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
                // UP
                if (MyAPIGateway.Input.IsGameControlPressed(MyControlsSpace.JUMP))
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

                // DOWN
                if (MyAPIGateway.Input.IsGameControlPressed(
                    MyControlsSpace.CROUCH))
                {
                    Vector3D downwardForce =
                        gravityDir *
                        mass *
                        swimForce *
                        0.6;

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
    }
}
