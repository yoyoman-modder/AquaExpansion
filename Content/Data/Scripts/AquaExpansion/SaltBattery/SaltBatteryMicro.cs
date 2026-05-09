using Sandbox.Common.ObjectBuilders;
using VRage.Game.Components;
using VRage.ObjectBuilders;

namespace AquaExpansion.SaltBattery
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_BatteryBlock), false, "SmallBlockSaltBatteryMicro")]
    public class SaltBatteryMicro : SaltBatteryBase
    {
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            depthBoost = 0.05f;
            saltBoost = 0.05f;
            base.Init(objectBuilder);
        }
    }
}
