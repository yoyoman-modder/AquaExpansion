using AquaExpansion.Core;
using Jakaria.API;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace AquaExpansion.UnderwaterTurbine
{
    public abstract class UnderWaterTurbineBase : MyGameLogicComponent
    {
        protected IMyFunctionalBlock block;
        private IMyEntity e;
        private MyResourceSourceComponent source;
        private IMyCubeGrid grid;
        private float MaxPowerOutput;
        private float WaterDepth;
        private float WaterEfficiency = 0f;
        protected float PowerBoost = 0.2f; //in MW
        private string Tmaxpower = "Max Power Output:";
        private string Tcupower = "Current Power Output:";
        private string Tdepth = "Water Depth:";
        private string Ttitle = "Underwater Power";
        private string TError = "Turbine ERROR";
        private string Tsalt = "Salt Level:";
        private string Tsaltboost = "Boost:";
        private string TResourseGroup = "SolarPanels";
        protected string TSubpartname = "TurbineRotor";
        private string TTurbineRPM = "RPM";
        private MyTuple<float, float, float, int> currentwaterData;
        private MyEntitySubpart Rotor;
        private float currentRPM = 0f;
        private float targetRPM = 0f;
        private float MAX_RPM = 30f;
        private float ACCELERATION = 5f;   // how fast it speeds up
        private float DECELERATION = 2f;   // how fast it slows down
        private float angle = 0f;
        private Matrix rotorBaseMatrix;
        private float maxDepth = 100f;
        private float saltLevel = 0f;
        private float saltP = 0f;
        private float Boost = 0f;
        long originalGridId;
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            block = (IMyFunctionalBlock)Entity;
            grid = block.CubeGrid;
            originalGridId = grid.EntityId;
            if (block != null)
            {
                e = block as IMyEntity;
                if (e != null)
                {

                }
                SetPowerSource();
            }
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME | MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME;
        }

        private void AppendCustomInfo(IMyTerminalBlock block, StringBuilder info)
        {
            if (block != null && !block.Closed)
            {
                if (block.IsFunctional && WaterModAPI.IsUnderwater(block.GetPosition()) && (grid != null || !grid.Closed) && grid.IsStatic)
                {
                    info.AppendLine(Ttitle);
                    SetInfo(info);
                }
                else
                {
                    currentRPM = 0f;
                    info.AppendLine(Ttitle);
                    info.AppendLine(TError);
                }
            }
        }

        public override void UpdateOnceBeforeFrame()
        {
            if (block == null || block.Closed || block.MarkedForClose)
                return;
            if (grid == null || grid.Closed || grid.MarkedForClose)
                return;
            if (grid.Physics == null)
                return;
            if (!grid.IsStatic)
                return;
            block.AppendingCustomInfo += AppendCustomInfo;
            SetTurbineRotor();
            base.UpdateOnceBeforeFrame();
        }

        public override void UpdateBeforeSimulation()
        {
            if (block == null || block.Closed || block.MarkedForClose)
                return;
            if (grid == null || grid.Closed || grid.MarkedForClose)
                return;
            if (grid.Physics == null)
                return;
            if (!grid.IsStatic)
                return;
            if (!WaterModAPI.IsUnderwater(block.GetPosition()))
                block.Enabled = false;
            UpdateTurbineBlades();
            base.UpdateBeforeSimulation();
        }

        public override void UpdateAfterSimulation10()
        {
            if (block == null || block.Closed || block.MarkedForClose)
                return;
            if (grid == null || grid.Closed || grid.MarkedForClose)
                return;
            if (grid.Physics == null)
                return;
            if (!grid.IsStatic)
                return;
            GetCurrentWaterStats(out currentwaterData);
            UpdatePowerSource();
            AquaExpansionSession.Insance.UpdateTerminal(block);
            base.UpdateAfterSimulation10();
        }

        private void SetPowerSource()
        {
            source = new MyResourceSourceComponent();
            Entity.Components.Add(source);
            List<MyResourceSourceInfo> sourceResourceData = new List<MyResourceSourceInfo>
            {
                 new MyResourceSourceInfo()
                 {
                      ResourceTypeId = MyResourceDistributorComponent.ElectricityId, DefinedOutput = WaterEfficiency, IsInfiniteCapacity = true
                 }
            };
            source.Init(MyStringHash.GetOrCompute(TResourseGroup), sourceResourceData);
            source.SetProductionEnabledByType(MyResourceDistributorComponent.ElectricityId, true);
            source.Enabled = true;
        }

        private void GetCurrentWaterStats(out MyTuple<float, float,float,int> waterdata)
        {
            waterdata = new MyTuple<float, float, float, int>(0f, 0f, 0f, 0);
            if (block != null && block.Enabled && block.IsFunctional && WaterModAPI.IsUnderwater(block.GetPosition()))
            {
                WaterDepth = AquaExpansionSession.Insance.GetWaterDepth(block);
                waterdata = AquaExpansionSession.Insance.GetPlanetWaveData(block);
                saltLevel = AquaExpansionSession.Insance.GetSaltLevel(block, WaterDepth);
                saltP = AquaExpansionSession.Insance.SaltToPercent(saltLevel);
            }
        }

        private float CalculatePowerOutput()
        {
            if (block == null || !block.Enabled || !block.IsFunctional || block.Closed)
                return 0f;
            if (!WaterModAPI.IsUnderwater(block.GetPosition()))
                return 0f;
            float waveHeight = currentwaterData.Item1;
            float waveSpeed = currentwaterData.Item2;
            float depth = WaterDepth;
            float waveScale = (currentwaterData.Item3);
            // normalize depth
            float depthNorm = MathHelper.Clamp(depth/-maxDepth, 0, 1f);
            depthNorm = (float)Math.Pow(depthNorm, 0.5f);
            // --- POWER COMPONENTS ---
            float flowPower = waveSpeed; // flow speed
            float wavePower = waveHeight * waveScale; // waves
            float powerFactor = flowPower * 0.7f + wavePower * 0.3f;
            float salteffect = 1f + (saltLevel / 3f) * 2f;
            Boost = (depthNorm * salteffect);
            Boost = MathHelper.Clamp(Boost, 1f, 3f);
            // --- FINAL POWER ---
            MaxPowerOutput = (powerFactor * depthNorm) * (PowerBoost * Boost);
            //AquaExpansionSession.Insance.DebugPanel(Logging, DebugBlockType.Turbine, depth, depthNorm, saltLevel, saltBoost, wavePower, powerFactor, MaxPowerOutput,0);
            return MaxPowerOutput;
        }

        private void UpdatePowerSource()
        {
            if (block == null || !block.Enabled || !block.IsFunctional || source == null || block.Closed)
            {
                source.SetMaxOutputByType(MyResourceDistributorComponent.ElectricityId, 0f);
            }
            else
            {
                WaterEfficiency = CalculatePowerOutput();
                source.SetMaxOutputByType(MyResourceDistributorComponent.ElectricityId, WaterEfficiency);
            }
        }

        private void SetInfo(StringBuilder info)
        {
            StringBuilder max = new StringBuilder();
            StringBuilder cu = new StringBuilder();
            MyValueFormatter.AppendWorkInBestUnit(source.MaxOutput, max);
            MyValueFormatter.AppendWorkInBestUnit(source.CurrentOutput, cu);
            info.AppendLine($"{Tmaxpower} {max}");
            info.AppendLine($"{Tcupower} {cu}");
            info.AppendLine($"{Tdepth} {(float)Math.Round(WaterDepth)} m");
            if (block.Enabled)
            {
                info.AppendLine($"{TTurbineRPM} {(float)Math.Round(currentRPM)}");
            }
            else
            {
                info.AppendLine($"{TTurbineRPM} {(float)Math.Round(0f)}");
            }
            info.AppendLine($"{Tsaltboost} {(float)Math.Round(Boost, 2)}");
            info.AppendLine($"{Tsalt} {(float)Math.Round(saltP)}%");
        }

        private void UpdateTurbineBlades()
        {
            if (Rotor == null || block == null || block.Closed || !block.IsFunctional || !block.Enabled)
            return;
            float deltaTime = MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
            // --- SAFE POWER RATIO ---
            float maxOutput = Math.Max(source.MaxOutput, 0.0001f);
            float curretOutput = Math.Max(source.CurrentOutput, 0f);
            float powerRatio = MathHelper.Clamp(source.CurrentOutput / maxOutput, 0f, 1f);
            // --- SALT BOOST---
            float LBoost = (saltLevel - 1f) / 2f;
            // --- TARGET RPM ---
            float rpmBoost = 1f + LBoost * 0.2f;
            targetRPM = MAX_RPM * powerRatio * rpmBoost;
            if (powerRatio > 0.01f)
                targetRPM = Math.Max(targetRPM, 2f);
            // --- SMOOTH INERTIA ---
            float accelBoost = 1f + ((LBoost - 1f) * 0.4f);
            float lerpSpeed = (targetRPM > currentRPM) ? ACCELERATION *accelBoost : DECELERATION;
            currentRPM = MathHelper.Lerp(currentRPM, targetRPM, lerpSpeed * deltaTime);
            // prevent NaN
            if (float.IsNaN(currentRPM) || float.IsInfinity(currentRPM))
                currentRPM = 0f;
            // --- ROTATION ---
            float angularSpeed = currentRPM * MathHelper.TwoPi / 60f;
            angle += angularSpeed * deltaTime;
            // keep angle stable
            if (angle > MathHelper.TwoPi)
                angle -= MathHelper.TwoPi;
            float noiseStrength = 0.05f * (1f - (Boost - 1f) / 2f); // salt reduces noise
            float noise = 1f + noiseStrength *
            (float)Math.Sin(MyAPIGateway.Session.GameplayFrameCounter * 0.05f);
            targetRPM = currentRPM * noise;
            // --- APPLY ROTATION ---
            Matrix rotation = Matrix.CreateFromAxisAngle(rotorBaseMatrix.Up, angle);
            Matrix finalMatrix = rotation * rotorBaseMatrix;
            Rotor.PositionComp.SetLocalMatrix(ref finalMatrix);
        }

        private void SetTurbineRotor()
        {
            if (block == null || block.Closed)
                return;
            var ent = block as MyEntity;
            if (ent != null)
            {
                if (ent?.Subparts.TryGetValue(TSubpartname, out Rotor) == true)
                {
                    rotorBaseMatrix = Rotor.PositionComp.LocalMatrixRef;
                }
            }
        }

        private void Clear()
        {
            if (block?.CubeGrid?.Physics != null)
            {
                block.AppendingCustomInfo -= AppendCustomInfo;
            }
        }
        public override void Close()
        {
            Clear();
            Rotor = null;
            source = null;
            grid = null;
            block = null;
            base.Close();
        }
    }
}
