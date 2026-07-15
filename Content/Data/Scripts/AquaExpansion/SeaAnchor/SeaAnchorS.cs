using AquaExpansion.Core;
using System;
using VRage.Game;
using VRage.Game.Components;
using VRage.ObjectBuilders;

namespace AquaExpansion.SeaAnchor
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_FunctionalBlock), false, "AquaSeaAnchorS")]
    public class SeaAnchorS : SeaAnchorBase
    {
        public static Guid SeaAnchorSKey = new Guid("F56F1BFB-20CD-490D-A644-A106C76C01B3");
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            HaseInventory = true;
            HasModStorage = true;
            AnchorType = AquaSeaAnchorType.S;
            PowerIddleDrain = 0.01f;
            PowerWorkDrain = 0.05f;
            base.Init(objectBuilder);
        }
        protected override void SaveSeaAnchorData()
        {
            base.SaveSeaAnchorData();
            SaveStats(SeaAnchorSKey);
        }
        protected override void LoadingSeaAnchorData()
        {
            base.LoadingSeaAnchorData();
            LoadStats(SeaAnchorSKey);
        }
    }
}
