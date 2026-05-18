using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.ModAPI;
using VRage;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace AquaExpansion.FoodProcessorUpdate
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Assembler), false, "FoodProcessor")]
    public class AquaFoodProcessorUpdate : MyGameLogicComponent
    {
        private IMyAssembler block;
        private IMyCubeGrid grid;
        private MyInventory invIn;
        private MyInventory invOut;
        private IMyInventory blockinvIn;
        private IMyInventory blockinvOut;
        private const string upgradename = "Cargo";
        private MyFixedPoint DefCargoVolumeIn;
        private MyFixedPoint DefCargoVolumeOut;
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            block = Entity as IMyAssembler;
            if (block == null)
                return;
            grid = block.CubeGrid;
            block.UpgradeValues.Add(upgradename, 0f);
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            if (block == null || block.Closed || block.MarkedForClose)
                return;
            if (grid == null || grid.Closed || grid.MarkedForClose)
                return;
            if (grid.Physics == null)
                return;
            SetInventory();
            block.OnUpgradeValuesChanged += UpgradesChanged;
            ResetVolume();
            base.UpdateOnceBeforeFrame();
        }

        private void UpgradesChanged()
        {
            var value = block.UpgradeValues[upgradename];
            SetCargoVolume(value);
        }

        private void ClearbyGrid()
        {
            if (block?.CubeGrid?.Physics != null)
            {
                block.OnUpgradeValuesChanged -= UpgradesChanged;
            }
        }

        private void SetInventory()
        {
            blockinvIn = block.GetInventory(0) as IMyInventory;
            blockinvOut = block.GetInventory(1) as IMyInventory;
            invIn = blockinvIn   as MyInventory;
            invOut = blockinvOut as MyInventory;
        }

        private void ResetVolume()
        {
            if (invIn == null || invOut == null)
                return;
            var def = block.SlimBlock.BlockDefinition as MyAssemblerDefinition;
            if (def == null)
                return;
            DefCargoVolumeIn = (MyFixedPoint)def.InventoryMaxVolume;
            DefCargoVolumeOut = (MyFixedPoint)def.InventoryMaxVolume;
        }

        private void SetCargoVolume(float value)
        {
            if (invIn == null || invOut == null)
                return;
            var newCargoVolumeIn = DefCargoVolumeIn * (1f + value);
            var newCargoVolumeOut = DefCargoVolumeOut * (1f + value);
            invIn.MaxVolume = newCargoVolumeIn;
            invOut.MaxVolume = newCargoVolumeOut;
        }

        private void Clear()
        {
            invIn = null;
            invOut = null;
            blockinvIn = null;
            blockinvOut = null;
            grid = null;
            block = null;
        }

        public override void Close()
        {
            ClearbyGrid();
            Clear();
            base.Close();
        }
    }
}
