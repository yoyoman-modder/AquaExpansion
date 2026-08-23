using AquaExpansion.Core;
using AquaExpansion.SaltBattery;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Game.Components;
using VRage.Utils;

namespace AquaExpansionExperimental.SaltBattery
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class SaltBatteryUI : MySessionComponentBase
    {
        public static SaltBatteryUI instance;
        private SaltBatteryBase blocklogic;
        private bool ready;
        public override void LoadData()
        {
            instance = this;
            base.LoadData();
        }
        public void ConnectToBlock(IMyBatteryBlock block)
        {
            if (block != null && !block.Closed || !block.MarkedForClose)
            {
                var logic = block?.GameLogic?.GetAs<SaltBatteryBase>();
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
            CreateControls<IMyBatteryBlock>();
        }
        private void CreateControls<T>() where T : IMyBatteryBlock
        {
            var helpbutton = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyTerminalBlock>("help_button");
            helpbutton.Title = MyStringId.GetOrCompute("Help");
            helpbutton.SupportsMultipleBlocks = false;
            helpbutton.Visible = CustomVisibleCondition;
            helpbutton.Enabled = HelpEnabled;
            var htooltip = AquaHelpDatabase.GetHelpLineByID(1);
            helpbutton.Tooltip = MyStringId.GetOrCompute(htooltip);
            helpbutton.Action = (b) =>
            {
                HelpAction(b);
            };
            MyAPIGateway.TerminalControls.AddControl<T>(helpbutton);
        }
        private bool CustomVisibleCondition(IMyTerminalBlock b)
        {
            return b?.GameLogic?.GetAs<SaltBatteryBase>() != null;
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
