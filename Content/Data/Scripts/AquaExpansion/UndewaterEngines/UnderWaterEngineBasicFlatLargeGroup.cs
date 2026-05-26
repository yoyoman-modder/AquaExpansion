using Sandbox.Common.ObjectBuilders;
using VRage.Game.Components;
using VRage.ObjectBuilders;

namespace AquaExpansion.UndewaterEngines
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Thrust), false, "UnderwaterEngineBasicFlatLarge", "UnderwaterEngineBasicDFlatLarge")]
    public class UnderWaterEngineBasicFlatLargeGroup : UnderWaterEngineBase
    {
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            CavitationDepth = 5f;
            base.Init(objectBuilder);
        }
    }
}
