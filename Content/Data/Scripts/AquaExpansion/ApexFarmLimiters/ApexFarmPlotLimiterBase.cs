using AquaExpansion.Core;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace AquaExpansion.ApexFarmLimiters
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_FunctionalBlock), false, "LargeBlockFarmPlot")]
    public class ApexFarmPlotLimiterBase :MyGameLogicComponent
    {
        private IMyFunctionalBlock block;
        private IMyCubeGrid grid;
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            block = (IMyFunctionalBlock)Entity;
            grid = block.CubeGrid;
            if (block != null)
            {
               
               
            }
            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME| MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            if (block?.CubeGrid?.Physics == null) // ignore projected and other non-physical grids
                return;
            //AquaExpansionSession.Insance.Log(true, $"FarmPlot limiter initialized for block: {block.EntityId}");
            base.UpdateOnceBeforeFrame();
        }

        public override void UpdateBeforeSimulation()
        {
            if (block == null || block.Closed || block.MarkedForClose || grid == null || grid.Closed || grid.Physics == null)
                return;
            AquaExpansionSession.Insance.CheckUnderwaterBlockRules(block, true);
            base.UpdateBeforeSimulation();
        }

        public override void Close()
        {
            grid = null;
            block = null;
            base.Close();
        }
    }
}
