using AquaExpansion.Core;
using AquaExpansion.SubmarineBallastTank;
using Jakaria.API;
using Sandbox.ModAPI;
using System.Collections.Generic;
using System.Text;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace AquaExpansion.SubmarineController
{
    public abstract class SubmarineControllerBase : MyGameLogicComponent
    {
        private IMyFunctionalBlock block;
        private IMyCubeGrid grid;
        private string Ttitle = "Submarine Controller";
        private string TError = "Controller ERROR";
        private string Tmaxpower = "Max Power Input:";
        private string Tcupower = "Current Power Input:";
        private string TTanks = "Ballast Tanks:";
        private List<SubmarineBallastTankBase> tanks = new List<SubmarineBallastTankBase>();
        public bool autoDepth = false;
        public float targetDepth = 50f;
        private long lastGridId = 0;
        

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            block = (IMyFunctionalBlock)Entity;
            grid = block.CubeGrid;
            if (block != null)
            {
                block.AppendingCustomInfo += AppendCustomInfo;
                AquaExpansionSession.Insance.Log(true, $"Controller init");
            }
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME | MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME;
        }

        private void AppendCustomInfo(IMyTerminalBlock block, StringBuilder info)
        {
            if (block != null && !block.Closed)
            {
                if (block.IsFunctional && WaterModAPI.IsUnderwater(block.GetPosition()) && (grid != null || !grid.Closed) && !grid.IsStatic)
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

        public override void UpdateOnceBeforeFrame()
        {
            RefreshTanks();
            lastGridId = block?.CubeGrid?.EntityId ?? 0;
            base.UpdateOnceBeforeFrame();
        }

        private void SetInfo(StringBuilder info)
        {
            info.AppendLine($"{TTanks} {tanks.Count} ");
        }

        public override void UpdateAfterSimulation10()
        {
            if (block == null || block.Closed)
                return;
            if (grid == null || grid.Closed || grid.Physics == null)
                return;
                RefreshTanks();
            UpdateGridChange();
            AquaExpansionSession.Insance.UpdateTerminal(block);
            //AquaExpansionSession.Insance.Log(true, $"Controller tanks: {tanks.Count}");
            base.UpdateAfterSimulation10();
        }



        private void RefreshTanks()
        {
            if (block == null || block.Closed || grid == null || grid.Closed)
                return;

            // Get all tanks for this grid
            var liveTanks = SubmarineBallastTankBase.GetTanksForGrid(grid);

            // Remove any old dead tanks
            tanks.RemoveAll(t => t == null || t.Entity == null || t.Entity.Closed);

            // Add new tanks not in list yet
            foreach (var t in liveTanks)
            {
                if (!tanks.Contains(t))
                    tanks.Add(t);
            }
        }

        private void UpdateGridChange()
        {
            if (block == null || block.Closed)
                return;
            long currentGridId = block.CubeGrid?.EntityId ?? 0;
            if (currentGridId != lastGridId && currentGridId != 0)
            {
                ClearTanks();      // clear old tanks
                lastGridId = currentGridId;
            }
        }

        public List<SubmarineBallastTankBase> GetTanks()
        {
            return tanks;
        }

        private void ClearTanks()
        {
            tanks.Clear();
        }

        public override void Close()
        {
            block.AppendingCustomInfo -= AppendCustomInfo;
            ClearTanks();
            grid = null;
            block = null;
            base.Close();
        }
    }
}
