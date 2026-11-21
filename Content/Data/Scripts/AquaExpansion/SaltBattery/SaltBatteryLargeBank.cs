using Sandbox.Common.ObjectBuilders;
using VRage.Game.Components;
using VRage.ObjectBuilders;

namespace AquaExpansion.SaltBattery
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_BatteryBlock), false, "LargeBlockSaltBankBattery", "LargeBlockSaltBankBatteryOffset")]
    public class SaltBatteryLargeBank : SaltBatteryBase
    {
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            depthBoost = 0.28f;
            saltBoost = 0.28f;
            base.Init(objectBuilder);
        }
    }
}
