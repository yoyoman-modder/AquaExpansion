using Sandbox.Common.ObjectBuilders;
using VRage.Game.Components;
using VRage.ObjectBuilders;

namespace AquaExpansion.SaltBattery
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_BatteryBlock), false, "SmallBlockSaltBankBattery")]
    public class SaltBatterySmallBank : SaltBatteryBase
    {
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            depthBoost = 0.13f;
            saltBoost = 0.13f;
            base.Init(objectBuilder);
        }
    }
}
