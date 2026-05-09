using AquaExpansion.Core;
using Jakaria.API;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using System;
using System.Text;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace AquaExpansion.SaltBattery
{
    public abstract class SaltBatteryBase : MyGameLogicComponent
    {
        protected bool Log = false;
        private IMyBatteryBlock block;
        private MyBatteryBlock batteryblock;
        private IMyCubeGrid grid;
        private MyResourceSinkComponent sink;
        private MyResourceSourceComponent source;
        private float WaterDepth;
        private float saltLevel = 0f;
        private float saltP = 0f;
        private float salteffect = 0f;
        private string Tdepth = "Water Depth:";
        private string Ttitle = "Salt Battery";
        private string TError = "No Water Data";
        private string Tsalt = "Salt level";
        private string Tboost = "Boost:";
        private string TPressure = "Pressure:";
        protected float maxDepth = 100f;
        private float Boost = 0f;
        private double WaterPressure = 0;
        protected float depthBoost = 0.15f;
        protected float saltBoost = 0.15f;
        private float baseChargeRate = -1f;
        private float baseOutput = -1f;
        private IMyEntity e;
        
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            Log = true;
            block = (IMyBatteryBlock)Entity;
            grid = block.CubeGrid;
            if (block != null)
            {
                GetBatteryComponents();
                e = block as IMyEntity;
                if (e != null)
                {

                }
                batteryblock = block as MyBatteryBlock;
                if (batteryblock != null)
                {
                    
                }
            }
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME | MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME;
        }

        private void AppendCustomInfo(IMyTerminalBlock block, StringBuilder info)
        {
            if (block != null && !block.Closed)
            {
                if (block.IsFunctional && WaterModAPI.IsUnderwater(block.GetPosition()))
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

        private void SetInfo(StringBuilder info)
        {
            if (grid == null || grid.Closed)
                return;

            if (grid.IsStatic)
            {
                info.AppendLine($"{Tboost} {(float)Math.Round(Boost, 2)}");
                info.AppendLine($"{Tdepth} {(float)Math.Round(WaterDepth)} m");
                info.AppendLine($"{Tsalt} {(float)Math.Round(saltP)}%");
            }
            else
            {
                info.AppendLine($"{TPressure} {(float)Math.Round(WaterPressure)} Kpa");
                info.AppendLine($"{Tboost} {(float)Math.Round(Boost, 2)}");
                info.AppendLine($"{Tdepth} {(float)Math.Round(WaterDepth)} m");
                info.AppendLine($"{Tsalt} {(float)Math.Round(saltP)}%");
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
            base.UpdateOnceBeforeFrame();
        }

        private void GetBatteryComponents()
        {
            if (block == null || block.Closed)
                return;

            sink = block.Components.Get<MyResourceSinkComponent>();
            source = block.Components.Get<MyResourceSourceComponent>();
            if (sink == null && source == null)
            {
                //AquaExpansionSession.Insance.Log(Log, "Components ERROR");
            }
        }

        public override void UpdateAfterSimulation10()
        {
            base.UpdateAfterSimulation10();
            if (block == null || block.Closed || block.MarkedForClose)
                return;
            if (grid == null || grid.Closed || grid.MarkedForClose)
                return;
            if (grid.Physics == null)
                return;
            GetCurrentWaterData();
            UpdateBatteryOutput();
            AquaExpansionSession.Insance.UpdateTerminal(block);
        }

        private void GetCurrentWaterData()
        {
            if (block != null && block.Enabled && block.IsFunctional && WaterModAPI.IsUnderwater(block.GetPosition()))
            {
                WaterDepth = AquaExpansionSession.Insance.GetWaterDepth(block);
                saltLevel = AquaExpansionSession.Insance.GetSaltLevel(block, WaterDepth);
                saltP = AquaExpansionSession.Insance.SaltToPercent(saltLevel);
                if (!grid.IsStatic)
                {
                    WaterPressure = AquaExpansionSession.Insance.GetPressurebyGrid(grid);
                }
            }
        }

        private float CalculateBoostEffect()
        {
            var boost = 0f;
            if (block == null || !block.Enabled || !block.IsFunctional || block.Closed)
                return 0f;
            if (!WaterModAPI.IsUnderwater(block.GetPosition()))
                return 1f;
            float depth = WaterDepth;
            float depthFactor = MathHelper.Clamp(depth / -maxDepth, 0, 1f);
            float depthScale = depthFactor * depthBoost;
            salteffect = 1f + (saltLevel / 3f) * 2f;
            float saltScale = (salteffect - 1f) * saltBoost;
            // --- FINAL BOOST ---
            boost = 1f + depthScale + saltScale;
            boost = MathHelper.Clamp(boost, 1f, (1f+(depthBoost + saltBoost)));
            return boost;
        }

        private void UpdateBatteryOutput()
        {
            if (block == null || !block.Enabled || !block.IsFunctional || source == null || sink == null || block.Closed)
                return;
            // --- SAFE INIT ---
            if (baseOutput <= 0f || baseChargeRate <= 0f)
            {
                InitBaseoutput();
                return;
            }
            Boost = MathHelper.Lerp(Boost, CalculateBoostEffect(), 0.1f);
            float finalCharge = baseChargeRate * Boost;
            float finalOutput = baseOutput * Boost;
            // Set resource sink/source
            sink.SetMaxRequiredInputByType(MyResourceDistributorComponent.ElectricityId, finalCharge);
            source.SetMaxOutputByType(MyResourceDistributorComponent.ElectricityId, finalOutput);
        }

        private void InitBaseoutput()
        {
            if (block == null || !block.Enabled || !block.IsFunctional || source == null || sink == null || block.Closed)
                return;
            if (grid == null || grid.Closed)
                return;
            baseChargeRate = sink.MaxRequiredInputByType(MyResourceDistributorComponent.ElectricityId); ;
            baseOutput = source.MaxOutputByType(MyResourceDistributorComponent.ElectricityId);
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
            grid = null;
            block = null;
            base.Close();
        }
    }
}
