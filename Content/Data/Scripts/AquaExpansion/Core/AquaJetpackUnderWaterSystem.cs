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
        public double HoverCenterDepth;    // min hover
        public double HoverStrength;      // max hover
        public double HoverRange; // hover range
        public float SinkBias;     // natural sinking
        public double HoverMinDepth;
        public double HoverMaxDepth;    
        public DivingGearData(
        int level,
        float maxSpeed,
        float boost,
        float maxRefillDepth,
        float refillRate,
        float saltFilter,
        double hoverCenterDepth,
        double hoverStrength,
        double hoverRange,
         double hoverMinDepth,
         double hoverMaxDepth,
        float sinkBias)

        {
            Level = level;
            GearmaxSpeed = maxSpeed;
            GearBoost = boost;
            GearO2MaxRefillDepth = maxRefillDepth;
            GearO2RefillRate = refillRate;
            GearSaltFilterLevel = saltFilter;
            HoverCenterDepth = hoverCenterDepth;
            HoverStrength = hoverStrength;
            HoverRange = hoverRange;
            SinkBias = sinkBias;
            HoverMinDepth = hoverMinDepth;
            HoverMaxDepth = hoverMaxDepth;
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
                     HoverCenterDepth = 0.0f,
                     HoverStrength = 0.0f   ,
                     HoverRange = 0.0f,
                     SinkBias = 6.0f,
                     HoverMinDepth = 0.0f,
                     HoverMaxDepth = 0.0f}
                },
                {
                MyStringHash.GetOrCompute("AquaDiveGearT1"),
                new DivingGearData { Level = 1,
                    GearmaxSpeed = 6f,
                    GearBoost = 1.1f,
                    GearO2MaxRefillDepth = 30f,
                    GearO2RefillRate = 0.5f,
                    GearSaltFilterLevel = 0.3f,
                    HoverCenterDepth = -15,
                    HoverStrength = 2,
                    HoverRange = 8,
                    SinkBias = 3.5f,
                    HoverMinDepth = -20,
                    HoverMaxDepth = -10}
                },
                {
                MyStringHash.GetOrCompute("AquaDiveGearT2"),
                new DivingGearData { Level = 2,
                    GearmaxSpeed = 8f,
                    GearBoost = 1.25f,
                    GearO2MaxRefillDepth = 60f,
                    GearO2RefillRate = 0.9f,
                    GearSaltFilterLevel = 0.5f,
                    HoverCenterDepth = -25,
                    HoverStrength = 3.5,
                    HoverRange = 15,
                    SinkBias = 1.5f,
                    HoverMinDepth = -30,
                    HoverMaxDepth = -20}
                },
                {
                MyStringHash.GetOrCompute("AquaDiveGearT3"),
                new DivingGearData { Level = 3,
                    GearmaxSpeed = 11f,
                    GearBoost = 1.50f,
                    GearO2MaxRefillDepth = 100f,
                    GearO2RefillRate = 1.2f,
                    GearSaltFilterLevel = 0.7f,
                    HoverCenterDepth = -50,
                    HoverStrength = 5,
                    HoverRange = 30,
                    SinkBias = -0.5f,
                    HoverMinDepth = -50,
                    HoverMaxDepth = -30}
                }
        };
        private double? TargetDepth = null; //autodepth
        public int PlayerGearlevelIndx = 0;
        public bool PlayerOxygenRefillActive = false;
        //private UnderwaterBuoyancyPID PID = new UnderwaterBuoyancyPID();
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
                UpdateSeabedMovement(character, ID);
                if (tick % 20 != 0) // every ~0.3 sec
                    return;
                PlayerOxygenRefillActive = false;
                UpdateUnderwaterMovement(character, delta, depth, salt, 0);
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
            UpdateSeabedMovement(character, ID);
            if (tick % 20 != 0) // every ~0.3 sec
                return;
            UpdateUnderwaterMovement(character, deltaTime, depth, salt, glevel);
            RefillOxygen(character, deltaTime, depth, ID, salt, glevel);
        }

        private void UpdateUnderwaterMovement(IMyCharacter character, float deltaTime, float depth, float saltLevel, int glevel)
        {
            if (character == null || character.IsDead || character.Closed)
                return;
            if (!WaterModAPI.IsUnderwater(character.GetPosition()))
                return;
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
                    HoverCenterDepth = -30,
                    HoverRange = 20,
                    SinkBias = 2.0f,
                    HoverMinDepth = -30,
                    HoverMaxDepth = -10
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
            //debug
            /*AquaExpansionSession.Insance.Log(true,
                $"vel {vel.Length():0.00}" +
                $"\nmax {maxSpeed:0.00}" +
                $"\nsaltPenalty {saltPenalty:0.00}" +
                $"\ndepthFactor {depthFactor:0.00}" +
                $"\nhVel {horizontalVel.Length():0.00}" +
                $"\nvVel {verticalVel:0.00}" +
                $"\nfinal {finalVel.Length():0.00}");*/
            //double targetSinkSpeed = 0.2;
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

        private void UpdateSeabedMovement(IMyCharacter character, long ID)
        {
            if (character == null || character.Closed || character.IsDead)
                return;
            Vector3D pos = character.GetPosition();
            var underwater = WaterModAPI.IsUnderwater(pos);
            var fullyUnderwater = AquaExpansionSession.Insance.GetUnderWaterPercent(character);
            if (!underwater || fullyUnderwater < 1f)
            {
                character.CanSprint = true;
                return;
            }
            var physics = character.Physics;
            if (physics == null)
                return;
            var state = character.CurrentMovementState;
            string stateName = state.ToString();
            // Check if player is walking/running on seabed (horizontal movement)

            bool nearSeabed = stateName.Contains("Walking") || stateName.Contains("Running") || stateName.Contains("Crouch") || stateName.Contains("Back");
            if (nearSeabed)
            {
                character.CanSprint = false;
            }
            else
            {
                character.CanSprint = true;
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

        public  class UnderwaterBuoyancyPID
        {
            private Vector3D position;
            private Vector3D gravity;
            private double mass;
            private Vector3D gravityDir;
            private Vector3D velocity;
            private double integral;
            private double lastError;
            private bool ready;
            public double MaxIntegral = 10.0;
            public double VerticalDamping = 0.8;
            private double verticalSpeed;
            private double overload;
            private double efficiency;
            private Vector3D baseBuoyancy;
            private double correction;
            private Vector3D correctionForce;
            private Vector3D dampingForce;
            private Vector3D finalForce;

            public  void Reset()
            {
                integral = 0;
                lastError = 0;
                correction = 0;
                correctionForce = Vector3D.Zero;
                dampingForce = Vector3D.Zero;
                finalForce = Vector3D.Zero;
                efficiency = 0;
                baseBuoyancy = Vector3D.Zero;
                ready = false;
            }

            public void Update(IMyCharacter character, float deltaTime, double depth, double targetSinkSpeed, int gearLevel, double VertSpeeed)
            {
                if (character == null || character.Closed || character.IsDead)
                    return;
                if (!WaterModAPI.IsUnderwater(character.GetPosition()))
                { Reset(); return; }
                if (deltaTime <= 0f)
                    return;
                var physics = character.Physics;
                if (physics == null)
                    return;
                position = character.GetPosition();
                float interference;
                gravity = MyAPIGateway.Physics.CalculateNaturalGravityAt(position, out interference);
                if (gravity.LengthSquared() < 0.000001)
                    return;
                mass = physics.Mass;
                gravityDir = Vector3D.Normalize(gravity);
                velocity = physics.LinearVelocity;
                // positive = sinking
                verticalSpeed = Vector3D.Dot(velocity, gravityDir);
                //double verticalSpeed = VertSpeeed;
                double buoyancyFactor = 0.0;
                double Kp = 0.0;
                double Ki = 0.0;
                double Kd = 0.0;
                double maxCorrection = 0.0;
                // Max depth where gear can hover efficiently
                // deeper = reduced buoyancy
                double maxHoverDepth = 0.0;
                switch (gearLevel)
                {
                    default:
                    case 0:
                        buoyancyFactor = 0.20;
                        Kp = 0.4;
                        Ki = 0.01;
                        Kd = 0.2;
                        maxCorrection = 0.25;
                        maxHoverDepth = 5.0;
                        break;
                    case 1:
                        buoyancyFactor = 0.65;
                        Kp = 1.1;
                        Ki = 0.03;
                        Kd = 0.6;
                        maxCorrection = 0.8;
                        maxHoverDepth = 40.0;
                        break;

                    case 2:
                        buoyancyFactor = 0.95;
                        Kp = 1.9;
                        Ki = 0.06;
                        Kd = 1.0;
                        maxCorrection = 1.2;
                        maxHoverDepth = 120.0;
                        break;

                    case 3:
                        buoyancyFactor = 0.4;
                        Kp = 0.5;
                        Ki = 0.01;
                        Kd = 0.2;
                        maxCorrection = 0.25;
                        maxHoverDepth = 40.0;
                        break;
                }
                // Beyond max hover depth
                // gear starts losing efficiency
                double depthAbs = Math.Abs(depth);
                if (depthAbs > maxHoverDepth)
                {
                    overload = (depthAbs - maxHoverDepth) /maxHoverDepth;
                    overload = MathHelper.Clamp((float)overload,0f,0.85f);
                    efficiency = 1.0 - overload;
                    buoyancyFactor *= efficiency;
                    maxCorrection *= efficiency;
                }
                baseBuoyancy = -gravity * mass * buoyancyFactor;
                // PID ERROR
                double error = targetSinkSpeed - verticalSpeed;
                // Integral
                integral += error * deltaTime;
                integral = MathHelper.Clamp((float)integral,(float)-MaxIntegral,(float)MaxIntegral);
                // Derivative
                double derivative = 0.0;
                if (ready)
                {
                    derivative =(error - lastError) / deltaTime;
                }
                lastError = error;
                ready = true;
                // PID output
                correction = (Kp * error) + (Ki * integral) + (Kd * derivative);
                correction = MathHelper.Clamp((float)correction,(float)-maxCorrection,(float)maxCorrection);
                // PID FORCE
                correctionForce = -gravityDir * correction * mass * 9.81;
                // VERTICAL DAMPING
                dampingForce = -gravityDir * verticalSpeed * mass * VerticalDamping;
                // FINAL FORCE
                finalForce = baseBuoyancy + correctionForce + dampingForce;
                physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE,finalForce, position,null);
                //debug
                AquaExpansionSession.Insance.Log(true,
                    $"vertSpeed {verticalSpeed}" +
                    $"\ndepthABS {depthAbs}" +
                    $"\noverload {overload}" +
                    $"\nefficiency {efficiency}" +
                    $"\nbuoynancyFactor {buoyancyFactor}" +
                    $"\nmaxCorrection {maxCorrection}" +
                    $"\nbasebuoynancy {baseBuoyancy}" +
                    $"\nI {integral}" +
                    $"\nError {error}" +
                    $"\nD {derivative}" +
                    $"\nCorrection {correction}" +
                    $"\nCorrectionForce {correctionForce}" +
                    $"\nDampingForce {dampingForce}" +
                    $"\nfinal {finalForce.Length():0.00}");
            }
        }
    }
}
