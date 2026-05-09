using System;
using VRage.Game;
using VRage.Game.Components;
using VRage.ObjectBuilders;

namespace AquaExpansion.UnderwaterFarmPlot
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_FunctionalBlock), false, "UnderwaterFarmPlot")]
    public class UnderwaterFarmPlotBasic :UnderwaterFarmPlotBase
    {
        public static Guid UnderwaterFarmPlotKey = new Guid("4F12FFB0-2FE7-4510-AB7B-F2190DC584B4");
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            HasModStorage = true;
            HaseInventory = true;
            FarmBlockType = Core.AquaFarmingBlockType.UnderwaterFarmPlot;
            base.Init(objectBuilder);
        }

        protected override void SaveUnderwaterFarmPlotData()
        {
            base.SaveUnderwaterFarmPlotData();
            SaveStats(UnderwaterFarmPlotKey);
        }

        protected override void LoadingUnderwaterFarmPlotData()
        {
            base.LoadingUnderwaterFarmPlotData();
            LoadStats(UnderwaterFarmPlotKey);
        }
    }
}
