using Sandbox.Common.ObjectBuilders;
using VRage.Game.Components;
using VRage.ObjectBuilders;

namespace AquaExpansion.UndewaterEngines
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Thrust), false, "UnderwaterEngineBasicLS")]
    public class UnderWaterEnginebasicLS : UnderWaterEngineBase
    {
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            CavitationDepth = 5f;
            base.Init(objectBuilder);
        }
    }
}
