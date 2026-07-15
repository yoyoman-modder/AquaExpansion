using AquaExpansion.Core;
using System;
using VRage.Game;
using VRage.Game.Components;
using VRage.ObjectBuilders;

namespace AquaExpansion.SeaAnchor
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_FunctionalBlock), false, "AquaSeaAnchorL")]
    public class SeaAnchorL : SeaAnchorBase
    {
        public static Guid SeaAnchorLKey = new Guid("E3B642EE-1D33-4A07-AA9C-EAB5F1D423A1");
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            AnchorType = AquaSeaAnchorType.L;
            HaseInventory = true;
            HasModStorage = true;
            base.Init(objectBuilder);
        }
        protected override void LoadingSeaAnchorData()
        {
            base.LoadingSeaAnchorData();
            LoadStats(SeaAnchorLKey);
        }
        protected override void SaveSeaAnchorData()
        {
            base.SaveSeaAnchorData();
            SaveStats(SeaAnchorLKey);
        }
    }
}
