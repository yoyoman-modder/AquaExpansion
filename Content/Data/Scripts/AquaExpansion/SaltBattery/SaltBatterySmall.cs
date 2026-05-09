using Sandbox.Common.ObjectBuilders;
using VRage.Game.Components;
using VRage.ObjectBuilders;

namespace AquaExpansion.SaltBattery
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_BatteryBlock), false, "SmallBlockSaltBattery")]
    public class SaltBatterySmall : SaltBatteryBase
    {
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            depthBoost = 0.10f;
            saltBoost = 0.10f;
            base.Init(objectBuilder);
        }
    }
}
