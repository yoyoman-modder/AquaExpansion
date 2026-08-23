using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI.Weapons;
using System.Collections.Generic;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace AquaExpansion.Core.Combat.Balistics
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_AutomaticRifle), false)]
    public class AquaHandWeaponHandler : MyGameLogicComponent
    {
        private IMyAutomaticRifleGun handweapon;
        private readonly Dictionary<string, IMyModelDummy> dummies = new Dictionary<string, IMyModelDummy>();
        private IMyModelDummy Muzzle;
        public int LastShotTick = -1;
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            LoadTypeDefinition();
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }
        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            if (handweapon == null || handweapon.Closed || handweapon.MarkedForClose)
                return;
            if (!AquaExpansionSession.Insance.HasBalistics)
                return;
            GetWeaponDummies();
        }
        private void LoadTypeDefinition()
        {
            handweapon = Entity as IMyAutomaticRifleGun;
            if (handweapon == null ||
                handweapon.Closed ||
                handweapon.MarkedForClose)
                return;
            if (!AquaExpansionSession.Insance.HasBalistics)
                return;
            Register();
            //AquaExpansionSession.Insance.Log(true,$"hand weapon {handweapon.Weapon.DefinitionId.Value.SubtypeId}");
        }
        private void GetWeaponDummies()
        {
            Muzzle = null;
            dummies.Clear();
            if (handweapon == null ||
                handweapon.Closed ||
                handweapon.MarkedForClose)
                return;
            handweapon.Model.GetDummies(dummies);
            dummies.TryGetValue(WeaponDummiesDatabase.Get(1),out Muzzle);
        }
        public bool HasMuzzle
        {
            get { return Muzzle != null; }
        }
        public IMyModelDummy GetMuzzle 
        { 
            get { return Muzzle; } 
        }
        public string GetWeaponSubtype
        {
            get
            {
                if (handweapon == null)
                    return null;
                return handweapon.DefinitionId.SubtypeId.String;
            }
        }
        private void Register()
        {
            if(handweapon == null)
                return;
            AquaExpansionSession.Insance.OnRegisterHandWeapon(handweapon);
        }
        private void Unregister()
        {
            AquaExpansionSession.Insance.OnUnregisterHandWeapon(handweapon);
        }
        private void Clear()
        {
            Unregister();
            //AquaExpansionSession.Insance.Log(true, $"hand weapon closed {handweapon.Weapon.DefinitionId.Value.SubtypeId}");
            Muzzle = null;
            dummies.Clear();
            handweapon = null;
        }
        public override void Close()
        {
            Clear();
            base.Close();
        }
    }
}
