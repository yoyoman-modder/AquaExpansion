using Sandbox.Common.ObjectBuilders;
using VRage.Game.Components;
using VRage.ObjectBuilders;

namespace AquaExpansion.UndewaterEngines
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Thrust), false, "UnderwaterEngineBasicS")]
    public class UnderWaterEngineBasicS : UnderWaterEngineBase
    {

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            CavitationDepth = 2f;
            base.Init(objectBuilder);
        }
    }
}
