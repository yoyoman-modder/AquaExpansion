using Sandbox.Common.ObjectBuilders;
using VRage.Game.Components;
using VRage.ObjectBuilders;

namespace AquaExpansion.UndewaterEngines
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Thrust), false, "UnderwaterEngineBasicFlatSmall", "UnderwaterEngineBasicDFlatSmall")]
    public class UnderWaterEngineBasicFlatSmallGroup : UnderWaterEngineBase
    {
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            CavitationDepth = 2f;
            base.Init(objectBuilder);
        }
    }
}
