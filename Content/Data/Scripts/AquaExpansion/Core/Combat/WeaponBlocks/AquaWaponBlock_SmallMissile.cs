using Sandbox.Common.ObjectBuilders;
using VRage.Game.Components;
using VRage.ObjectBuilders;

namespace AquaExpansion.Core.Combat.WeaponBlocks
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_SmallMissileLauncher), false)]
    public class AquaWaponBlock_SmallMissile : AquaWeaponBlockBase
    {
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            type = AquaWeaponBlockType.SmallMissileLauncher;
            base.Init(objectBuilder);
        }
    }
}
