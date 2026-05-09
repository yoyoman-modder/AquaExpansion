using VRage.Game;
using VRage.Game.Components;
using VRage.ObjectBuilders;

namespace AquaExpansion.SubmarineBallastTank
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_FunctionalBlock), false, "LargeBlockSubmarineBallastTank")]
    public class SubmarineBallastTankLarge : SubmarineBallastTankBase
    {
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
        }
    }
}
