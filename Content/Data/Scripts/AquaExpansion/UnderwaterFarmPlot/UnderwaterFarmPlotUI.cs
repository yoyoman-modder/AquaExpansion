using AquaExpansion.Core;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.Utils;

namespace AquaExpansion.UnderwaterFarmPlot
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class UnderwaterFarmPlotUI :MySessionComponentBase
    {
        public static UnderwaterFarmPlotUI Instance;
        private UnderwaterFarmPlotBase blocklogic;
        private bool ready;
        public event Action BlockSaveRequest;
        public override void LoadData()
        {
            Instance = this;
            base.LoadData();
        }

        public void ConnectToBlock(IMyFunctionalBlock block)
        {
            if (block != null && !block.Closed || !block.MarkedForClose)
            {
                var logic = block?.GameLogic?.GetAs<UnderwaterFarmPlotBase>();
                if (logic != null && !logic.Closed)
                {
                    blocklogic = logic;
                }
            }
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

        public void RunControlls()
        {
            if (ready)
                return;
            ready = true;
            CreateControls<IMyFunctionalBlock>();
        }

        private void CreateControls<T>() where T : IMyFunctionalBlock
        {
            //separetor
            var separator = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyFunctionalBlock>("");
            separator.SupportsMultipleBlocks = false;
            separator.Visible = CustomVisibleCondition;
            MyAPIGateway.TerminalControls.AddControl<T>(separator);
            //Label
            var label = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyFunctionalBlock>("Plants Database");
            label.SupportsMultipleBlocks = false;
            label.Visible = CustomVisibleCondition;
            label.Label = MyStringId.GetOrCompute("Plant Database");
            MyAPIGateway.TerminalControls.AddControl<T>(label);
            //Bp listbox
            var Bplist = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, IMyFunctionalBlock>("DatabaseList_Box");
            Bplist.SupportsMultipleBlocks = false;
            Bplist.Title = MyStringId.GetOrCompute("Plant Recipes");
            Bplist.Multiselect = false;
            Bplist.Visible = CustomVisibleCondition;
            Bplist.VisibleRowsCount = 5;
            Bplist.ListContent = (b, items, selected) =>
            {
                var logic = b.GameLogic.GetAs<UnderwaterFarmPlotBase>();
                if (logic == null)
                    return;
                foreach (var recipe in AquaRecipeDatabase.GetAll())
                {
                    if (recipe == null) continue;
                    var name = recipe;
                    var tooltip = logic.utils.GetVirtualPlantRecipe(name);
                    var item = new MyTerminalControlListBoxItem(
                        MyStringId.GetOrCompute(name.Displayname), MyStringId.GetOrCompute(tooltip),
                       name);
                    items.Add(item);
                }
            };
            Bplist.ItemSelected = (b, sel) =>
            {
                var logic = b.GameLogic.GetAs<UnderwaterFarmPlotBase>();
                if (logic == null)
                    return;
                if (sel != null)
                {

                }
            };
            MyAPIGateway.TerminalControls.AddControl<T>(Bplist);
        }

        private bool CustomVisibleCondition(IMyTerminalBlock b)
        {
            return b?.GameLogic?.GetAs<UnderwaterFarmPlotBase>() != null;
        }

        private void Clear()
        {
            blocklogic = null;
            Instance = null;
        }

        protected override void UnloadData()
        {
            Clear();
            base.UnloadData();
        }
    }
}
