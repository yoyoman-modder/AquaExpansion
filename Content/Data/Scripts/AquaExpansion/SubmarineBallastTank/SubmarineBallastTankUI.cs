using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Game.Components;
using VRage.Utils;

namespace AquaExpansion.SubmarineBallastTank
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class SubmarineBallastTankUI : MySessionComponentBase
    {
        private string System = "AquaExpansion";
        public static SubmarineBallastTankUI instance;
        private bool ready = false;

        public override void LoadData()
        {
            instance = this;
            base.LoadData();
        }

        private void CreateControl<T>() where T : IMyFunctionalBlock
        {
            //separetor
            var separator = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyFunctionalBlock>("");
            separator.SupportsMultipleBlocks = true;
            separator.Visible = CheckVisible;
            MyAPIGateway.TerminalControls.AddControl<T>(separator);
            //controll mode
            var controllmode = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, IMyFunctionalBlock>("ControllMode");
            controllmode.SupportsMultipleBlocks = true;
            controllmode.Visible = CheckVisible;
            controllmode.Title = MyStringId.GetOrCompute("Controll Mode");
            controllmode.Tooltip = MyStringId.GetOrCompute("Ballast Tank Controll Mode");
            controllmode.Getter = (b) =>
            {
                var logic = b?.GameLogic?.GetAs<SubmarineBallastTankBase>();
                
                return logic != null && logic.Controll == SubmarineBallastTankBase.ControllMode.AutoDepth;
            };
            controllmode.Setter = (b, v) =>
            {
                var logic = b?.GameLogic?.GetAs<SubmarineBallastTankBase>();
                if (logic != null)
                {
                    logic.Controll = v ? SubmarineBallastTankBase.ControllMode.AutoDepth : SubmarineBallastTankBase.ControllMode.Manual;
                }
            };
            controllmode.OnText = MyStringId.GetOrCompute("AutoDepth");
            controllmode.OffText = MyStringId.GetOrCompute("Manual");
            MyAPIGateway.TerminalControls.AddControl<T>(controllmode);
            //Tank mode
            var mode = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, IMyFunctionalBlock>("Mode");
            mode.SupportsMultipleBlocks = true;
            mode.Visible = CheckVisible;
            mode.Title = MyStringId.GetOrCompute("Tank Mode");
            mode.Tooltip = MyStringId.GetOrCompute("Ballast Tank Mode");
            mode.Getter = (b) =>
            {
                var logic = b?.GameLogic?.GetAs<SubmarineBallastTankBase>();
                // Return true if mode is CompressedAir, false if Water, default to false if null
                return logic != null && logic.Mode == SubmarineBallastTankBase.BallastMode.CompressedAir;
            };
            mode.Setter = (b, v) =>
            {
                var logic = b?.GameLogic?.GetAs<SubmarineBallastTankBase>();
                if (logic != null)
                {
                    logic.Mode = v ? SubmarineBallastTankBase.BallastMode.CompressedAir : SubmarineBallastTankBase.BallastMode.Water;
                }
            };
            mode.OnText = MyStringId.GetOrCompute("Air");
            mode.OffText = MyStringId.GetOrCompute("Water");
            MyAPIGateway.TerminalControls.AddControl<T>(mode);
            //slider
            var slider = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyFunctionalBlock>("BallastFill");
            slider.Title = MyStringId.GetOrCompute("Ballast Fill");
            slider.Tooltip = MyStringId.GetOrCompute("Set Ballast Fill");
            slider.SupportsMultipleBlocks = true;
            slider.Visible = CheckVisible;
            slider.SetLimits(0f, 1f);
            slider.Getter = (b) =>
            {
                var logic = b.GameLogic.GetAs<SubmarineBallastTankBase>();
                return logic?.TargetFill ?? 0f;
            };
            slider.Setter = (b, v) =>
            {
                var logic = b.GameLogic.GetAs<SubmarineBallastTankBase>();
                if (logic != null)
                    logic.TargetFill = v;
            };
            slider.Writer = (b, sb) =>
            {
                var logic = b.GameLogic.GetAs<SubmarineBallastTankBase>();
                if (logic != null)
                {
                    sb.Append($"{logic.TargetFill * 100f:0}%");
                }
            };
            MyAPIGateway.TerminalControls.AddControl<IMyFunctionalBlock>(slider);
        }

        private bool CheckVisible(IMyTerminalBlock T)
        {
            return T?.GameLogic?.GetAs<SubmarineBallastTankBase>() != null;
        }

        public void RunControlls()
        {
            if (ready)
                return;
            ready = true;
            CreateControl<IMyFunctionalBlock>();
        }

        protected override void UnloadData()
        {
            instance = null;
            base.UnloadData();
        }
    }
}
