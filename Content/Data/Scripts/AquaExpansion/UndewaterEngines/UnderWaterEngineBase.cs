using AquaExpansion.Core;
using Jakaria.API;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Text;
using VRage;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace AquaExpansion.UndewaterEngines
{
    public abstract class UnderWaterEngineBase : MyGameLogicComponent
    {
        private IMyThrust block;
        private IMyEntity e;
        private IMyCubeGrid grid;
        private long originalGridId;
        private MyThrust thrust;
        private float WaterDepth;//current depth
        private float saltLevel = 0f;
        private float saltP = 0f;
        private string Tdepth = "Water Depth:";
        private string Tpressure = "Water Pressure:";
        private string TError = "No Water Data";
        private string Tsalt = "Salt Level";
        protected float maxDepth = 100f;//optimal max depth
        protected float MaxDeepDepth = 300f;
        protected float CavitationDepth = 2f;// cavitation danger depth
        protected float Cavitationpenalty = 0.30f; //+consumtion%
        protected float DeepDepthpenalty = 0.10f; //-consumtion%
        protected float ConsFactorMin = 0.8f;
        protected float ConsFactorMax = 1.5f;
        private string Ttitle = "UnderWater Engine";
        protected string TSubpartname = "Propeller";
        private string TCavwarning = "Cavitation Danger! Low  pressure!";
        private string TDeepwarning = "Warning! High pressure!";
        private MyEntitySubpart Blades;
        private Matrix bladesBaseMatrix;
        private MyTuple<float, float, float, int> currentwaterData;
        private double WaterPressure;
        private List<IMyModelDummy> flameDummies;
        public IMyModelDummy FlameDummy;
        public MatrixD FlameWorld;
        public Vector3D Flamepos;
        public Vector3D Flameforward;
        private float MinSafetyDepth = 5f;
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            block = (IMyThrust)Entity;
            grid = block.CubeGrid;
            originalGridId = grid.EntityId;
            if (block != null)
            {
                
                e = block as IMyEntity;
                if (e != null)
                {

                }
                thrust = (MyThrust)(block as MyEntity);
                if (thrust != null)
                {

                }
            }
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME | MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME;
        }

        private void AppendCustomInfo(IMyTerminalBlock block, StringBuilder info)
        {
            if (block != null && !block.Closed)
            {
                if (block.IsFunctional && WaterModAPI.IsUnderwater(block.GetPosition()) && block.IsWorking)
                {
                    info.AppendLine(Ttitle);
                    SetInfo(info);
                }
                else
                {
                    info.AppendLine(Ttitle);
                    info.AppendLine(TError);
                }
            }
        }

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();
            if (block == null || block.MarkedForClose)
                return;
                var model = block.Model;
            if (model == null)
                return;
            var temp = new Dictionary<string, IMyModelDummy>();
            model.GetDummies(temp);
            flameDummies = new List<IMyModelDummy>();
            foreach (var pair in temp)
            {
                if (pair.Key.IndexOf("flame", StringComparison.OrdinalIgnoreCase) >= 0)
                    flameDummies.Add(pair.Value);
            }
            if (flameDummies.Count == 0)
                flameDummies = null;
        }

        private void SetInfo(StringBuilder info)
        {
            info.AppendLine($"{Tdepth} {(float)Math.Round(WaterDepth)} m");
            info.AppendLine($"{Tpressure} {(double)Math.Round(WaterPressure)} Kpa");
            info.AppendLine($"{Tsalt} {(float)Math.Round(saltP)}%");
            if (WaterDepth >= -CavitationDepth)
            {
                info.AppendLine($"{TCavwarning}");
            }
            if (WaterDepth <= -maxDepth)
            {
                info.AppendLine($"{TDeepwarning}");
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
            block.AppendingCustomInfo += AppendCustomInfo;
            SetEngineBlades();
            base.UpdateOnceBeforeFrame();
        }

        public override void UpdateAfterSimulation10()
        {
            if (block == null || block.Closed || block.MarkedForClose)
                return;
            if (grid == null || grid.Closed || grid.MarkedForClose)
                return;
            if (grid.Physics == null)
                return;
            GetCurrentWaterStats(out currentwaterData);
            UpdatePowerConsumption();
            AquaExpansionSession.Insance.UpdateTerminal(block);
            base.UpdateAfterSimulation10();
        }

        public override void UpdateBeforeSimulation()
        {
            if (block == null || block.Closed || block.MarkedForClose)
                return;
            //if (grid == null || grid.Closed || grid.MarkedForClose)
                //return;
            if (grid.Physics == null)
                return;
            if (!WaterModAPI.IsUnderwater(block.GetPosition()) || !block.IsWorking || grid == null || grid.Closed || grid.MarkedForClose)
            {
                if(block.Enabled)
                block.Enabled = false;
            }
            else
            {
                if(!block.Enabled && WaterDepth >= -MinSafetyDepth)
                block.Enabled = true;
                UpdateCahedDummy();
            }
            //AquaExpansionSession.Insance.Log(true, $"DummyFlames {flameDummies.Count}");
            base.UpdateBeforeSimulation();
        }

        private void UpdateCahedDummy()
        {
            if (flameDummies == null)
                return;
            foreach (var dummy in flameDummies)
            {
                FlameDummy = dummy;
                FlameWorld = FlameDummy.Matrix * block.WorldMatrix;
                Flamepos = FlameWorld.Translation;
                Flameforward = FlameWorld.Forward;
            }
        }

        private float CalculatePowerConsumption()
        {
            if (block == null || !block.IsFunctional || !block.Enabled || block.Closed || !block.IsWorking)
                return 0f;
            if (grid == null || grid.Closed)
                return 0f;
            if (!WaterModAPI.IsUnderwater(block.GetPosition()))
            {
                block.PowerConsumptionMultiplier = 1f;
                return 0f;
            }
            float PowerConsumption = 0f;
            float depth = Math.Abs(WaterDepth);
            float salt = MathHelper.Clamp(saltLevel, 0f, 1f);
            // --- Cavitation
            float cavitationEffect = 1f - MathHelper.Clamp(depth / CavitationDepth, 0f, 1f);
            float cavitationMult = 1f + cavitationEffect * Cavitationpenalty;
            // --- Deep penalty
            float deepEffect = MathHelper.Clamp((depth - maxDepth) / MaxDeepDepth, 0f, 1f);
            float deepMult = 1f + deepEffect * DeepDepthpenalty;
            // --- Salt efficiency
            float saltEffect = 1f - (float)Math.Pow(salt, 1.5f) * 0.2f;
            // --- Final
            float targetMultiplier = cavitationMult * deepMult * saltEffect;
            // Stability
            targetMultiplier = MathHelper.Clamp(targetMultiplier, ConsFactorMin, ConsFactorMax);
            PowerConsumption = targetMultiplier;
            return PowerConsumption;
        }

        private void UpdatePowerConsumption()
        {
            if (block == null || !block.IsFunctional || !block.Enabled || block.Closed || !block.IsWorking)
                return;
            if (grid == null || grid.Closed)
                return;
            if (!WaterModAPI.IsUnderwater(block.GetPosition()))
            {
                block.PowerConsumptionMultiplier = 1f;
                return;
            }
            block.PowerConsumptionMultiplier = MathHelper.Lerp(block.PowerConsumptionMultiplier, CalculatePowerConsumption(), 0.1f);
        }


        private void GetCurrentWaterStats(out MyTuple<float, float, float, int> waterdata)
        {
            waterdata = new MyTuple<float, float, float, int>(0f, 0f, 0f, 0);
            if (grid == null || grid.Closed)
                return;
            if (block != null && block.Enabled && block.IsFunctional && !block.Closed && WaterModAPI.IsUnderwater(block.GetPosition()) && block.IsWorking)
            {
                WaterDepth = AquaExpansionSession.Insance.GetWaterDepth(block);
                waterdata = AquaExpansionSession.Insance.GetPlanetWaveDatabyGrid(grid);
                saltLevel = AquaExpansionSession.Insance.GetSaltLevelbyGrid(grid, WaterDepth);
                saltP = AquaExpansionSession.Insance.SaltToPercent(saltLevel);
                WaterPressure = AquaExpansionSession.Insance.GetPressurebyGrid(grid);
            }
        }

        private void SetEngineBlades()
        {
            if (block == null || block.Closed)
                return;
            var ent = block as MyEntity;
            if (ent != null)
            {
                if (ent?.Subparts.TryGetValue(TSubpartname, out Blades) == true)
                {
                    bladesBaseMatrix = Blades.PositionComp.LocalMatrixRef;
                }
            }
        }

        private void Clear()
        {
            if (block?.CubeGrid?.Physics != null)
                block.AppendingCustomInfo -= AppendCustomInfo;
        }

        public override void Close()
        {
            Clear();
            FlameDummy = null;
            flameDummies = null;
            Blades = null;
            grid = null;
            block = null;
            base.Close();
        }
    }
}
