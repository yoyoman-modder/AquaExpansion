using AquaExpansion.Core;
using Jakaria.API;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using System;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace AquaExpansion.SoilGenerator
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Refinery), false, "SoilExtractor")]
    public class SoilGeneratorBase :MyGameLogicComponent
    {
        private IMyRefinery block;
        private IMyEntity e;
        private IMyCubeGrid grid;
        private long originalGridId;
        private float WaterDepth;
        private float saltLevel = 0f;
        private float saltP = 0f;
        private float salteffect = 0f;
        private string Tdepth = "Water Depth:";
        private string Tsalt = "Salt level:";
        private string Ttitle = "Soil Extractor";
        private string TError = "Extractor ERROR";
        protected float maxDepth = 100f;
        private string Tboost = "Boost:";
        private MyFixedPoint lastSoilAmount = 0;
        private double WaterPressure;
        private string TPressure = "Pressure:";

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            block = (IMyRefinery)Entity;
            grid = block.CubeGrid;
            originalGridId = grid.EntityId;
            if (block != null)
            {
                e = block as IMyEntity;
                if (e != null)
                {

                }
                if (grid != null)
                {

                }
            }
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME | MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME;
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

        private void SetInfo(StringBuilder info)
        {
            if (grid == null || grid.Closed)
                return;
            if (grid.IsStatic)
            {
                info.AppendLine($"{Tboost} {(float)Math.Round(salteffect, 2)}");
                info.AppendLine($"{Tdepth} {(float)Math.Round(WaterDepth)} m");
                info.AppendLine($"{Tsalt} {(float)Math.Round(saltP)}%");
            }
            else
            {
                info.AppendLine($"{TPressure} {(float)Math.Round(WaterPressure)} Kpa");
                info.AppendLine($"{Tboost} {(float)Math.Round(salteffect, 2)}");
                info.AppendLine($"{Tdepth} {(float)Math.Round(WaterDepth)} m");
                info.AppendLine($"{Tsalt} {(float)Math.Round(saltP)}%");
            }
        }

        private void GetCurrentWaterData()
        {
            if (block != null && !block.Closed && block.Enabled && block.IsFunctional && WaterModAPI.IsUnderwater(block.GetPosition()) && block.IsWorking)
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

        private void CalculateSaltEffect()
        {
            if (block == null || !block.Enabled || !block.IsFunctional || block.Closed || !block.IsWorking)
                return;
            if (!WaterModAPI.IsUnderwater(block.GetPosition()))
                return;
            // Depth + salt
            float depth = WaterDepth;
            float depthFactor = MathHelper.Clamp(-depth / maxDepth, 0f, 1f);
            depthFactor = (float)Math.Pow(depthFactor, 0.5f);
            float saltBoost = 1f + (saltLevel / 3f) * 2f;
            salteffect = 1f + depthFactor * (saltBoost - 1f);
            salteffect = MathHelper.Clamp(salteffect, 1f, 3f);
        }

        public override void UpdateBeforeSimulation()
        {
            if (block == null || block.Closed || block.MarkedForClose)
                return;
            if (grid == null || grid.Closed || grid.MarkedForClose)
                return;
            if (grid.Physics == null)
                return;
            if (!WaterModAPI.IsUnderwater(block.GetPosition()))
                block.Enabled = false;
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
            GetCurrentWaterData();
            CalculateSaltEffect();
            UpdateGeneratorOutput();
            AquaExpansionSession.Insance.UpdateTerminal(block);
            base.UpdateAfterSimulation10();
        }

        private void UpdateGeneratorOutput()
        {
            if (block == null || !block.Enabled || !block.IsFunctional || block.Closed || !block.IsWorking)
                return;
            if (!WaterModAPI.IsUnderwater(block.GetPosition()))
                return;
            if (!block.IsProducing)
                return;
            var outputInv = block.OutputInventory;
            if (outputInv == null)
                return;
            MyFixedPoint currentSoil = 0;
            for (int i = 0; i < outputInv.ItemCount; i++)
            {
                var item = outputInv.GetItemAt(i);
                if (item.HasValue && item.Value.Type.SubtypeId == "SeaSoil")
                {
                    currentSoil += item.Value.Amount;
                }
            }
            // Reset
            if (currentSoil < lastSoilAmount)
            {
                lastSoilAmount = currentSoil;
                return;
            }
            if (lastSoilAmount == 0)
            {
                lastSoilAmount = currentSoil;
                return;
            }
            var produced = currentSoil - lastSoilAmount;
            if (produced <= 0)
                return;
            if (outputInv.VolumeFillFactor > 0.95f)
                return;
            float bonus = (float)produced * (salteffect - 1f);
            if (bonus < 0.01f)
                return;
            bonus = MathHelper.Clamp(bonus, 0f, 5f);
            var soil = new MyObjectBuilder_Ore()
            {
                SubtypeName = "SeaSoil"
            };
            outputInv.AddItems((MyFixedPoint)bonus, soil);
            lastSoilAmount = currentSoil;
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
