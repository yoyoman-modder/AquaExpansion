using VRage.Game;
using VRage.Game.Components;
using VRage.ObjectBuilders;

namespace AquaExpansion.UnderwaterTurbine
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_FunctionalBlock), false, "LargeBlockUnderWaterTurbneS")]
    public class UnderWaterTurbineStandart :UnderWaterTurbineBase
    {
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            PowerBoost = 1.75f;
            TSubpartname = "WindTurbineRotorReskin";
        }
    }
}
