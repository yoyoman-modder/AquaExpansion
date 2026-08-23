using AquaExpansion.Core;
using AquaExpansion.UnderwaterTurbine;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Game.Components;
using VRage.Utils;

namespace AquaExpansionExperimental.UnderwaterTurbine
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class UnderWaterTurbineUI : MySessionComponentBase
    {
        public static UnderWaterTurbineUI instance;
        private UnderWaterTurbineBase blocklogic;
        private bool ready;
        public override void LoadData()
        {
            instance = this;
            base.LoadData();
        }
        public void ConnectToBlock(IMyFunctionalBlock block)
        {
            if (block != null && !block.Closed || !block.MarkedForClose)
            {
                var logic = block?.GameLogic?.GetAs<UnderWaterTurbineBase>();
                if (logic != null && !logic.Closed)
                {
                    blocklogic = logic;
                }
            }
        }
        public void RunControlls()
        {
            if (ready)
                return;
            ready = true;
            CreateControls<IMyFunctionalBlock>();
        }
        private void CreateControls<T>() where T : IMyFunctionalBlock
        {
            var helpbutton = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyTerminalBlock>("help_button");
            helpbutton.Title = MyStringId.GetOrCompute("Help");
            helpbutton.SupportsMultipleBlocks = false;
            helpbutton.Visible = CustomVisibleCondition;
            helpbutton.Enabled = HelpEnabled;
            var htooltip = AquaHelpDatabase.GetHelpLineByID(2);
            helpbutton.Tooltip = MyStringId.GetOrCompute(htooltip);
            helpbutton.Action = (b) =>
            {
                HelpAction(b);
            };
            MyAPIGateway.TerminalControls.AddControl<T>(helpbutton);
        }
        private bool CustomVisibleCondition(IMyTerminalBlock b)
        {
            return b?.GameLogic?.GetAs<UnderWaterTurbineBase>() != null;
        }
        private bool HelpEnabled(IMyTerminalBlock b)
        {
            return false;
        }
        private void HelpAction(IMyTerminalBlock b)
        {
            //empty
        }
        private void Clear()
        {
            blocklogic = null;
            instance = null;
        }
        protected override void UnloadData()
        {
            Clear();
            base.UnloadData();
        }
    }
}
