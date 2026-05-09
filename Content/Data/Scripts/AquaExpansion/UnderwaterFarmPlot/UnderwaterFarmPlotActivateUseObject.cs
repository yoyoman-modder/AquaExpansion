using AquaExpansion.Core;
using Sandbox.ModAPI;
using System;
using VRage.Game.Entity.UseObject;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;

namespace AquaExpansion.UnderwaterFarmPlot
{
    [MyUseObject("underwaterfarmplot")]
    public class UnderwaterFarmPlotActivateUseObject : MyUseObjectBase
    {
        private IMyFunctionalBlock block;
        private UnderwaterFarmPlotBase logic;

        public UnderwaterFarmPlotActivateUseObject(IMyEntity owner, string dummyName, IMyModelDummy dummyData, uint shapeKey) : base(owner, dummyData)
        {
            block = owner as IMyFunctionalBlock;
        }

        public override UseActionEnum SupportedActions => UseActionEnum.Manipulate
                                                        | UseActionEnum.Close
                                                        | UseActionEnum.BuildPlanner
                                                        | UseActionEnum.OpenInventory
                                                        | UseActionEnum.OpenTerminal
                                                        | UseActionEnum.PickUp
                                                        | UseActionEnum.UseFinished; // gets called when releasing manipulate
        public override UseActionEnum PrimaryAction => UseActionEnum.Manipulate;
        public override UseActionEnum SecondaryAction => UseActionEnum.OpenTerminal;

        public override MyActionDescription GetActionInfo(UseActionEnum actionEnum)
        {
            switch (actionEnum)
            {
                default:
                    return default(MyActionDescription);

                case UseActionEnum.Manipulate:
                    return new MyActionDescription()
                    {
                        Text = MyStringId.GetOrCompute("Show"),
                        IsTextControlHint = true,
                        JoystickText = MyStringId.GetOrCompute("Show"),
                        ShowForGamepad = true
                    };
            }
        }
        

        public override void Use(UseActionEnum actionEnum, IMyEntity user)
        {
            switch (actionEnum)
            {
                case UseActionEnum.Manipulate:

                    ActivateEvent();
                    break;
            }
        }

        private void ActivateEvent()
        {
            logic = block.GameLogic.GetAs<UnderwaterFarmPlotBase>();
            if (logic != null)
            {
                logic.ActivateEvent();
            }
        }
    }
}
