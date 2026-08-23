using Sandbox.Common.ObjectBuilders;
using VRage.Game.Components;
using VRage.ObjectBuilders;

namespace AquaExpansion.Core.Combat.WeaponBlocks
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_SmallMissileLauncherReload), false)]
    public class AquaWeaponBlock_SmallMissileReload :AquaWeaponBlockBase
    {
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            type = AquaWeaponBlockType.SmallMissileLauncherReload;
            base.Init(objectBuilder);
        }
    }
}
