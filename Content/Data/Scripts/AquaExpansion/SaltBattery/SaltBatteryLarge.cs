using Sandbox.Common.ObjectBuilders;
using VRage.Game.Components;
using VRage.ObjectBuilders;

namespace AquaExpansion.SaltBattery
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_BatteryBlock), false, "LargeBlockSaltBattery")]
    public class SaltBatteryLarge : SaltBatteryBase
    {
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            depthBoost = 0.25f;
            saltBoost = 0.25f;
            base.Init(objectBuilder);
        }
    }
}
