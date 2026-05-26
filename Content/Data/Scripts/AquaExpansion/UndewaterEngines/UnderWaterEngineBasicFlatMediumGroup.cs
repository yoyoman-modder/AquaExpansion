using Sandbox.Common.ObjectBuilders;
using VRage.Game.Components;
using VRage.ObjectBuilders;

namespace AquaExpansion.UndewaterEngines
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Thrust), false, "UnderwaterEngineBasicFlatMedium", "UnderwaterEngineBasicDFlatMedium")]
    public class UnderWaterEngineBasicFlatMediumGroup : UnderWaterEngineBase
    {
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            CavitationDepth = 4f;
            base.Init(objectBuilder);
        }
    }
}
