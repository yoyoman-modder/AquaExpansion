using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI.Weapons;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace AquaExpansion.Core
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Welder), false)]
    public class AquaHantToolWelderHandler : MyGameLogicComponent
    {
        private IMyWelder handwelder;
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            LoadTypeDefinition();
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }
        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            if (handwelder == null || handwelder.Closed || handwelder.MarkedForClose)
                return;
        }
        private void LoadTypeDefinition()
        {
            handwelder = Entity as IMyWelder;
            if (handwelder == null ||
                handwelder.Closed ||
                handwelder.MarkedForClose)
                return;
            Register();
            //AquaExpansionSession.Insance.Log(true,$"hand welder {handwelder.DefinitionId.SubtypeId}");
        }
        public string GetWeaponSubtype
        {
            get
            {
                if (handwelder == null)
                    return null;
                return handwelder.DefinitionId.SubtypeId.String;
            }
        }
        private void Register()
        {
            if (handwelder == null)
                return;
            AquaExpansionSession.Insance.OnRegisterHandWelder(handwelder);
        }
        private void Unregister()
        {
            AquaExpansionSession.Insance.OnUnRegisterHandWelder(handwelder);
        }
        private void Clear()
        {
            Unregister();
            //AquaExpansionSession.Insance.Log(true, $"hand welder closed {handwelder.DefinitionId.SubtypeId}");
            handwelder = null;
        }
        public override void Close()
        {
            Clear();
            base.Close();
        }
    }
}
