using AquaExpansion.Core;
using System;
using VRage.Game;
using VRage.Game.Components;
using VRage.ObjectBuilders;
using VRageMath;

namespace AquaExpansion.UnderwaterFishing
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_FunctionalBlock), false, "UnderwaterFishingStation")]
    public class UnderwaterFishingBlockAdvanced : UnderwaterFishingBase
    {
        public static Guid UnderwaterFishingStationKey = new Guid("4C6CC8D7-D511-440B-A45A-7F40940900E4");

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            Ttitle = "Underwater Fishing Station";
            FarmBlockType = AquaFarmingBlockType.FishingBlockAdvance;
            HaseInventory = true;
            HasModStorage = true;
            usebigfishdata = true;
            PowerIddleDrain = 0.1f;
            PowerWorkDrain = 0.5f;
            BaitMatrix = Matrix.CreateTranslation(0f, 7f, 0f);
            FishMatrix = Matrix.CreateTranslation(0f, 7f, 0f);
            fishRadius = 2f;
            inventoryfishRadius = 3f;
            fishoffcet = 7f;
            base.Init(objectBuilder);
        }

        protected override void LoadingUnderwaterFishingData()
        {
            base.LoadingUnderwaterFishingData();
            LoadStats(UnderwaterFishingStationKey);
        }

        protected override void SaveUnderwaterFishingData()
        {
            base.SaveUnderwaterFishingData();
            SaveStats(UnderwaterFishingStationKey);
        }
    }
}
