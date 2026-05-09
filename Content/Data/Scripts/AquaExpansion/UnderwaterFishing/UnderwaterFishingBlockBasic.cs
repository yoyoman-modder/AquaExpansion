using AquaExpansion.Core;
using System;
using VRage.Game;
using VRage.Game.Components;
using VRage.ObjectBuilders;

namespace AquaExpansion.UnderwaterFishing
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_FunctionalBlock), false, "UnderwaterFishingBlock")]
    public class UnderwaterFishingBlockBasic : UnderwaterFishingBase
    {
        public static Guid UnderwaterFishingBlockKey = new Guid("2673ED55-DB43-4849-BBA7-5120CFEE1B8F");
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            FarmBlockType = AquaFarmingBlockType.FishingBlock;
            HaseInventory = true;
            HasModStorage = true;
            usebigfishdata = false;
            base.Init(objectBuilder);
           
        }

        protected override void LoadingUnderwaterFishingData()
        {
            base.LoadingUnderwaterFishingData();
            base.LoadStats(UnderwaterFishingBlockKey);
        }

        protected override void SaveUnderwaterFishingData()
        {
            base.SaveUnderwaterFishingData();
            base.SaveStats(UnderwaterFishingBlockKey);
        }
    }
}
