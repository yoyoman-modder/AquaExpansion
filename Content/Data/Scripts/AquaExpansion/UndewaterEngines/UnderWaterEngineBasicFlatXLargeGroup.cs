using AquaExpansion.UndewaterEngines;
using Sandbox.Common.ObjectBuilders;
using VRage.Game.Components;
using VRage.ObjectBuilders;

namespace AquaExpansionExperimental.UndewaterEngines
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Thrust), false, "UnderwaterEngineBasicFlatXLarge", "UnderwaterEngineBasicDFlatXLarge")]
    public class UnderWaterEngineBasicFlatXLargeGroup : UnderWaterEngineBase
    {
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            CavitationDepth = 10f;
            base.Init(objectBuilder);
        }
    }
}
