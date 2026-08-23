using Sandbox.Common.ObjectBuilders;
using VRage.Game.Components;
using VRage.ObjectBuilders;

namespace AquaExpansion.Core.Combat.WeaponBlocks
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_SmallGatlingGun), false)]
    public class AquaWeaponBlock_SmallGatling : AquaWeaponBlockBase
    {
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            type = AquaWeaponBlockType.SmallGatlingGun;
            FireEffects = true;
            base.Init(objectBuilder);
        }
        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();
            UpdateFireEffects();
        }
    } 
}
