using VRage.Game;
using VRage.Game.Components;
using VRage.ObjectBuilders;

namespace AquaExpansion.UnderwaterTurbine
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_FunctionalBlock), false, "LargeBlockUnderWaterTurbneB")]
    public class UnderwaterTurbineBasic :  UnderWaterTurbineBase
    {
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            TSubpartname = "TurbineRotor";
            PowerBoost = 1.5f;
        }
    }
}
