using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game.Components;
using VRage.Utils;

namespace AquaExpansion.SeaAnchor
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class SeaAnchorUI :MySessionComponentBase
    {
        public static SeaAnchorUI instance = new SeaAnchorUI();
        private SeaAnchorBase blocklogic;
        private bool ready;
        private bool actionready;

        public event Action BlockSaveRequest;
        public override void LoadData()
        {
            instance = this;
            base.LoadData();
        }
        public void ConnectToBlock(IMyFunctionalBlock block)
        {
            if (block != null && !block.Closed || !block.MarkedForClose)
            {
                var logic = block?.GameLogic?.GetAs<SeaAnchorBase>();
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
        public void RunActions()
        {
            if (actionready)
                return;
            actionready = true;
            CreateActions<IMyFunctionalBlock>();
        }
        private void CreateActions<T>() where T : IMyFunctionalBlock
        {
            var armaction = MyAPIGateway.TerminalControls.CreateAction<T>("arm_action");
            armaction.Name = new StringBuilder("Arm/Disarm");
            armaction.ValidForGroups = true;
            armaction.Icon = @"Textures\GUI\Icons\Cubes\AquaSeaAnchorL.dds";
            armaction.Action = (b) =>
            {
                ArmAction(b);
            };
            MyAPIGateway.TerminalControls.AddAction<T>(armaction);
            var depaction = MyAPIGateway.TerminalControls.CreateAction<T>("deploy_action");
            depaction.Name = new StringBuilder("Deploy/Retract");
            depaction.ValidForGroups = true;
            depaction.Icon = @"Textures\GUI\Icons\Cubes\AquaSeaAnchorL.dds";
            depaction.Action = (b) =>
            {
                DeployAction(b);
            };
            MyAPIGateway.TerminalControls.AddAction<T>(depaction);
        }
        private void CreateControls<T>() where T : IMyFunctionalBlock
        {
            //separetor
            var separator = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyFunctionalBlock>("");
            separator.SupportsMultipleBlocks = false;
            separator.Visible = CustomVisibleCondition;
            MyAPIGateway.TerminalControls.AddControl<T>(separator);
            //Label
            var label = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyTerminalBlock>("mainlabel");
            label.SupportsMultipleBlocks = false;
            label.Visible = CustomVisibleCondition;
            label.Label = MyStringId.GetOrCompute("Wrinch Controll");
            MyAPIGateway.TerminalControls.AddControl<T>(label);
            var depbutton = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyTerminalBlock>("deploy_button");
            depbutton.Title = MyStringId.GetOrCompute("Deploy/Retract");
            depbutton.SupportsMultipleBlocks = false;
            depbutton.Visible = CustomVisibleCondition;
            depbutton.Tooltip = MyStringId.GetOrCompute("Deploy/Retract Anchor");
            depbutton.Action = (b) =>
            {
                DeployAction(b);
            };
            MyAPIGateway.TerminalControls.AddControl<T>(depbutton);
            var armbutton = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyTerminalBlock>("arm_button");
            armbutton.Title = MyStringId.GetOrCompute("Arm/Disarm");
            armbutton.SupportsMultipleBlocks = false;
            armbutton.Visible = CustomVisibleCondition;
            armbutton.Tooltip = MyStringId.GetOrCompute("Install/Remove Anchor");
            armbutton.Action = (b) =>
            {
                ArmAction(b);
            };
            MyAPIGateway.TerminalControls.AddControl<T>(armbutton);
            var helpbutton = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyTerminalBlock>("help_button");
            helpbutton.Title = MyStringId.GetOrCompute("Help");
            helpbutton.SupportsMultipleBlocks = false;
            helpbutton.Visible = CustomVisibleCondition;
            helpbutton.Enabled = HelpEnabled;
            var tooltip = blocklogic.utils.GetHelpText(5);
           helpbutton.Tooltip = MyStringId.GetOrCompute(tooltip);
            helpbutton.Action = (b) =>
            {
                HelpAction(b);
            };
            MyAPIGateway.TerminalControls.AddControl<T>(helpbutton);
        }
        private void HelpAction(IMyTerminalBlock b)
        {
            //empty
        }
        private void InternalClick(IMyTerminalBlock T)
        {
            List<IMyTerminalControl> actions = new List<IMyTerminalControl>();
            MyAPIGateway.TerminalControls.GetControls<IMyFunctionalBlock>(out actions);
            foreach (var item in actions)
            {
                if (item.Id == "help_button")
                {
                    var btn = item as IMyTerminalControlButton;
                    if (btn != null)
                    {
                        btn.Action(T as IMyFunctionalBlock);

                    }
                }
            }
        }
        private void DeployAction(IMyTerminalBlock b)
        {
            var logic = b.GameLogic.GetAs<SeaAnchorBase>();
            logic?.ToggleAnchor();
            InternalClick(b);
            b.RefreshCustomInfo();
        }
        private void ArmAction(IMyTerminalBlock b)
        {
            var logic = b.GameLogic.GetAs<SeaAnchorBase>();
            logic?.ArmAnchorSet();
            InternalClick(b);
            b.RefreshCustomInfo();
        }
        private bool CustomVisibleCondition(IMyTerminalBlock b)
        {
            return b?.GameLogic?.GetAs<SeaAnchorBase>() != null;
        }
        private bool HelpEnabled(IMyTerminalBlock b)
        {
            return false;
        }
        private void Clear()
        {
            blocklogic = null;
            instance = null;
        }
        public override void SaveData()
        {
            base.SaveData();
            if (blocklogic != null && !blocklogic.Closed && !blocklogic.MarkedForClose)
            {
                OnBlockRequestSave();
            }
        }
        public void OnBlockRequestSave()
        {
            BlockSaveRequest?.Invoke();
        }
        protected override void UnloadData()
        {
            Clear();
            base.UnloadData();
        }
    }
}
