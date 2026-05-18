using AquaExpansion.Core;
using Jakaria.API;
using Sandbox.Game;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using System;
using System.Text;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;


namespace AquaExpansion.UnderwaterFarmPlot
{
    public abstract class UnderwaterFarmPlotBase : MyGameLogicComponent
    {
        private MyEntity plant;
        private Matrix dummyMatrix = Matrix.CreateTranslation(0f, -0.4f, 0f);
        private int stage = 0;
        private int lastStage = -1;
        private IMyFunctionalBlock  block;
        private IMyEntity entity;
        private IMyCubeGrid grid;
        public AquaExpansionUtils utils;
        private string Ttitle = "Underwater Farm Plot";
        private string TError = "ERROR";
        private float WaterDepth;
        private float saltLevel = 0f;
        private float saltP = 0f;
        private string Tdepth = "Water Depth:";
        private string Tsalt = "Salt level:";
        private MyInventory inv;
        private AquaFarmBlockStage currentStage = AquaFarmBlockStage.Iddle;
        protected AquaFarmingBlockType FarmBlockType;
        private IMyInventory blockinv;
        private MyResourceSinkComponent sink;
        protected bool HaseInventory = false;
        protected bool HasModStorage = false;
        protected float PowerWorkDrain = 0.05f; // Mw
        protected float PowerIddleDrain = 0.01f; // Mw
        private float CurrentPowerInput = 0f;
        private string Tcupower = "Current Power Input:";
        private string Tmaxpower = "Max Power Input:";
        private int plantstage;
        private int growstage;
        private int harvstage;
        private int SporeStorage;
        private int CropStorage;
        private AquaFarmingRecipe ActiveRecipe;
        private AquaPlantInstance ActivePlant;
        private float growEfficiency;
        private bool ready;
        private float processTime;
        private float time = 0f;
        long originalGridId;
        bool clear;
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            block = Entity as IMyFunctionalBlock;
            if (block == null)
                return;
            grid = block.CubeGrid;
            originalGridId = grid.EntityId;
            entity = block;
            utils = new AquaExpansionUtils();
            SetSink();
            //AquaExpansionSession.Insance.Log(true,$"Underwater FarmPlot initialized for block: {block.EntityId}");
            NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME | MyEntityUpdateEnum.BEFORE_NEXT_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.EACH_FRAME;
        }

        private void AppendCustomInfo(IMyTerminalBlock block, StringBuilder info)
        {
            if (block != null && !block.Closed)
            {
                if (block.IsFunctional && WaterModAPI.IsUnderwater(block.GetPosition()) && grid != null && !grid.Closed && grid.IsStatic)
                {
                    info.AppendLine(Ttitle);
                    SetInfo(info);
                }
                else
                {
                    info.AppendLine(Ttitle);
                    info.AppendLine(TError);
                }
            }
        }

        private void SetInfo(StringBuilder info)
        {
            if (grid == null || grid.Closed)
                return;
            StringBuilder max = new StringBuilder();
            StringBuilder cu = new StringBuilder();
            float MaxInput = sink.MaxRequiredInputByType(MyResourceDistributorComponent.ElectricityId);
            float CuInput = sink.RequiredInputByType(MyResourceDistributorComponent.ElectricityId);
            MyValueFormatter.AppendWorkInBestUnit(MaxInput, max);
            MyValueFormatter.AppendWorkInBestUnit(CuInput, cu);
            info.AppendLine($"{Tmaxpower} {max}");
            info.AppendLine($"{Tcupower} {cu}");
            utils.StatusInfo(info, block, inv, currentStage);
            utils.GetStorage(info, block, inv, SporeStorage, CropStorage);
            utils.CurrentPlant(info, block, inv, ActiveRecipe);
            utils.GrowLine(info, block, inv, currentStage ,growstage, plantstage, harvstage, processTime);
            utils.CurrentEfficiency(info, block, inv, ActivePlant ,growEfficiency);
            info.AppendLine($"{Tdepth} {(float)Math.Round(WaterDepth)} m");
            info.AppendLine($"{Tsalt} {(float)Math.Round(saltP)}%");
        }

        public override void UpdateOnceBeforeFrame()
        {
            if (block == null || block.Closed || block.MarkedForClose)
                return;
            if (grid == null || grid.Closed || grid.MarkedForClose)
                return;
            if (grid.Physics == null)
                return;
            if (!grid.IsStatic)
                return;
            ModStorageHandler();
            InventoryHandler();
            block.AppendingCustomInfo += AppendCustomInfo;
            UnderwaterFarmPlotUI.Instance.ConnectToBlock(block);
            UnderwaterFarmPlotUI.Instance.BlockSaveRequest += OnSessionSave;
            UnderwaterFarmPlotUI.Instance.RunControlls();
            utils.ModelStageChanged += OnStageChanged;
            Load();
            base.UpdateOnceBeforeFrame();
        }

        private void OnStageChanged(int obj)
        {
            stage = obj;
            if (stage == lastStage)
                return;
            lastStage = stage;
            //AquaExpansionSession.Insance.Log(true, $" model idx {stage}");
            SetPlantModel(GetModelForStagebyPlant(stage));
        }

        public void OnSessionSave()
        {
            if (block != null && !block.Closed && !block.MarkedForClose)
            {
                Save();
                //AquaExpansionSession.Insance.Log(true, $"Saved from Session");
            }
        }

        public override void UpdateBeforeSimulation()
        {
            if (block == null || block.Closed || block.MarkedForClose)
                return;
            //if (grid == null || grid.Closed || grid.MarkedForClose)
                //return;
            if (grid.Physics == null)
                return;
            //if (!grid.IsStatic)
                //return;
            if (!HasPower())
                block.Enabled = false;
            UnderwaterRules();
            base.UpdateBeforeSimulation();
        }

        private void UnderwaterRules()
        {
            if (block == null || block.Closed || block.MarkedForClose || grid == null || grid.Closed)
                return;
            bool isUnderwater = WaterModAPI.IsUnderwater(block.GetPosition());
            float ingridox;
            AquaExpansionSession.Insance.GetBlockInAirtightGrid(block, out ingridox);
            if (!isUnderwater || isUnderwater && ingridox > AquaExpansionSession.Insance.MIN_ENVOXYGENLEVEL)
            {
                block.Enabled = false;
            }
        }

        public override void UpdateAfterSimulation()
        {
            if (block == null || block.Closed || block.MarkedForClose)
                return;
            if (grid == null || grid.Closed || grid.MarkedForClose)
                return;
            if (grid.Physics == null)
                return;
            if (!grid.IsStatic)
                return;
            CreatePlant();
            UpdatePlantmatrix();
            time += 0.02f;
            utils.SetEnviromentalEmissive(time, block, currentStage, growEfficiency, WaterDepth, plantstage, growstage, harvstage);
            base.UpdateAfterSimulation();
        }

        public override void UpdateAfterSimulation10()
        {
            if (block == null || block.Closed || block.MarkedForClose)
                return;
            if (grid == null || grid.Closed || grid.MarkedForClose)
                return;
            if (grid.Physics == null)
                return;
            if (!grid.IsStatic)
                return;
            GetCurrentWaterData();
            utils.UpdateFarm(block, inv, ref currentStage, ref ActiveRecipe, ref ActivePlant, ref growEfficiency, WaterDepth, saltLevel, ref processTime);
            if (!block.Enabled && (currentStage == AquaFarmBlockStage.Planting || 
                currentStage == AquaFarmBlockStage.Growing || currentStage == AquaFarmBlockStage.Harvestable || currentStage == AquaFarmBlockStage.Full))
            {
                if (ready)
                    return;
                ready = true;
                Save();
            }
            UpdateSink();
            plantstage = utils.GetPlantingPercent(ActivePlant);
            growstage = utils.GetGrowthPercent(ActivePlant);
            harvstage = utils.GetHarvestingPercent(ActivePlant);
            utils.CountCargo(block, inv, out SporeStorage, out CropStorage);
            AquaExpansionSession.Insance.UpdateTerminal(block);
            base.UpdateAfterSimulation10();
        }

        private void GetCurrentWaterData()
        {
            if (block != null && !block.Closed && block.Enabled && block.IsFunctional && WaterModAPI.IsUnderwater(block.GetPosition()) && block.IsWorking)
            {
                WaterDepth = AquaExpansionSession.Insance.GetWaterDepth(block);
                saltLevel = AquaExpansionSession.Insance.GetSaltLevel(block, WaterDepth);
                saltP = AquaExpansionSession.Insance.SaltToPercent(saltLevel);
            }
        }

        public void ActivateEvent()
        {
            if (block == null || block.Closed || block.MarkedForClose)
                return;
            if (grid == null || grid.Closed || grid.MarkedForClose)
                return;
            if (!block.IsFunctional)
                return;
            if (entity != null)
            {
                MyAPIGateway.Gui.ShowTerminalPage(MyTerminalPageEnum.ControlPanel, null, entity, false);
            }
        }

        private void InventoryHandler()
        {
            if (HaseInventory)
            {
                blockinv = block.GetInventory() as IMyInventory;
                inv = blockinv as MyInventory;
                if (blockinv != null)
                {
                    utils.SetupFarmingDefinitions(FarmBlockType);
                }
            }
            else
            {
                AquaExpansionSession.Insance.Log(true, $"No inventory used");
            }
        }

        private void ModStorageHandler()
        {
            if (HasModStorage)
            {
                if (block.Storage == null)
                {
                    block.Storage = new MyModStorageComponent();
                }
            }
            else
            {
                AquaExpansionSession.Insance.Log(true, $"No ModStorage used");
            }
        }

        private void Load()
        {
            if (HasModStorage)
            {
                if (block != null && !block.Closed)
                {
                    LoadingUnderwaterFarmPlotData();
                }
            }
        }

        protected virtual void LoadingUnderwaterFarmPlotData()
        {

        }

        private void Save()
        {
            if (HasModStorage)
            {
                if (block != null && !block.Closed)
                {
                    SaveUnderwaterFarmPlotData();
                }
            }
        }

        protected virtual void SaveUnderwaterFarmPlotData()
        {

        }

        private void CreatePlant()
        {
            /*if (plant != null || !block.IsFunctional || !block.Enabled)
                return;
            plant = new MyEntity();
            plant.Save = false;
            // INIT with model
            plant.Init(null, GetModelForStagebyPlant(stage), Entity as MyEntity, 1f, null);
            plant.Render.Visible = true;
            plant.Render.RemoveRenderObjects();
            plant.Render.AddRenderObjects();
            plant.InScene = true;
            // attach
            plant.Hierarchy.Parent = Entity.Hierarchy;
            AquaExpansionSession.Insance.Log(true, $"plant hierarchy {plant.Parent.DisplayName}");
            // position
            plant.PositionComp.SetLocalMatrix(ref dummyMatrix);
            var blockmatrix = block.WorldMatrix;
            plant.PositionComp.SetWorldMatrix(ref blockmatrix);
            if (!block.Hierarchy.Children.Any((MyHierarchyComponentBase x) => x.Entity == plant))
            {
                block.Hierarchy.AddChild(plant, true, true);
                //AquaExpansionSession.Insance.Log(true, $"block children {block.Hierarchy.Children.Count}");
            }
            AquaExpansionSession.Insance.Log(true, $"block children {block.Hierarchy.Children.Count}");
            //AquaExpansionSession.Insance.Log(true, $"Plant added to scene: {plant.InScene}");*/
            if (plant != null || !block.IsFunctional || !block.Enabled)
                return;

            plant = new MyEntity();
            plant.Save = false;

            plant.Init(null,GetModelForStagebyPlant(stage),null,1f,null);

            block.Hierarchy.AddChild(plant, true, true);
            Matrix local = dummyMatrix;
            plant.PositionComp.SetLocalMatrix(ref local);
            block.NeedsWorldMatrix = true;
            plant.NeedsWorldMatrix = true;
            //AquaExpansionSession.Insance.Log(true, $"block children {block.Hierarchy.Children.Count}");
        }

        private void UpdatePlantmatrix()
        {
            if (plant == null || block == null || block.MarkedForClose)
                return;

            MatrixD local = dummyMatrix;
            MatrixD world = local * block.WorldMatrix;
            plant.PositionComp.SetWorldMatrix(ref world);
        }

        private string GetModelForStagebyPlant(int stage)
        {
            if (ActivePlant == null)
                return null;
            if (ActivePlant.Def.StageModels == null || ActivePlant.Def.StageModels.Length == 0)
                return null;
            //AquaExpansionSession.Insance.Log(true, $"Plant stage models by: {ActivePlant.Def.Id}");
            switch (stage)
            {
                case 1: return ActivePlant.Def.StageModels[0]; //planting
                case 2: return ActivePlant.Def.StageModels[1]; //growing
                case 3: return ActivePlant.Def.StageModels[2]; //harvestable
                default: return null; // stage 0 = no plant
            }
        }

        private void SetPlantModel(string model)
        {
            if (plant == null || !plant.InScene)
            {
                //AquaExpansionSession.Insance.Log(true, $"Plant not in scene or null");
                return;
            }
            if (string.IsNullOrEmpty(model))
            {
                plant.Render.Visible = false;
                return;
            }
            plant.RefreshModels(model, null);
            plant.Render.Visible = true;
            plant.Render.RemoveRenderObjects();
            plant.Render.AddRenderObjects();
            plant.Render.UpdateRenderObject(true);
        }

        private void SetSink()
        {
            sink = new MyResourceSinkComponent();
            Entity.Components.Add(sink);
            MyResourceSinkInfo sinkInfo = new MyResourceSinkInfo
            {
                ResourceTypeId = MyResourceDistributorComponent.ElectricityId,
                MaxRequiredInput = PowerWorkDrain,
                RequiredInputFunc = () => ComputeRequiredPower()
            };
            sink.Init(MyStringHash.GetOrCompute("Utility"), sinkInfo);
            //AquaExpansionSession.Insance.Log(true, $"Sink OK");
        }

        private float ComputeRequiredPower()
        {
            if (block == null || block.Closed || !block.Enabled || !block.IsFunctional)
                return 0f;
            float power = currentStage == AquaFarmBlockStage.Iddle
                ? PowerIddleDrain : PowerWorkDrain;
            CurrentPowerInput = power;
            return power;
        }

        private bool HasPower()
        {
            if (block == null || block.Closed || !block.Enabled || !block.IsFunctional || grid == null || grid.Closed || !grid.IsStatic)
                return false;
            return sink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId);
        }

        private void UpdateSink()
        {
            if (block != null && !block.Closed && WaterModAPI.IsUnderwater(block.GetPosition()) && block.IsFunctional)
            {
                if (sink != null)
                {
                    sink.Update();
                }
            } 
        }

        private AquaFarmSaveData ToSave()
        {
            if (ActivePlant == null || ActiveRecipe == null)
                return null;
            return new AquaFarmSaveData
            {
               RecipeId = ActiveRecipe.Id,
               ModelStageId = stage,
               Plant = new AquaSavePlantData
                {
                    DefId = ActivePlant.DefID,
                    Stage = (int)ActivePlant.Stage,
                    Growth = ActivePlant.Growth,
                    Planting = ActivePlant.Planting,
                    Harvesting = ActivePlant.Harvesting
                }
            };
        }

        private void ToLoad(AquaFarmSaveData data)
        {
            // rebuild recipe
            ActiveRecipe = AquaRecipeDatabase.Get(data.RecipeId);
            stage = data.ModelStageId;
            // rebuild plant instance
            ActivePlant = new AquaPlantInstance
            {
                DefID = data.Plant.DefId,
                Def = AquaPlantDatabase.Get(data.Plant.DefId),
                Stage = (AquaFarmBlockStage)data.Plant.Stage,
                Growth = data.Plant.Growth,
                Planting = data.Plant.Planting,
                Harvesting = data.Plant.Harvesting
            };
            // restore runtime-only values
            RestoreEnvironment(ActivePlant);
            RelinkLoadData();
            if (ActivePlant != null)
            {
                //AquaExpansionSession.Insance.Log(true, $"Loaded\n recipe {ActiveRecipe.Id},\n name {ActiveRecipe.Displayname}\n plant depth {ActivePlant.CurrentDepth}, " +
                    //$"\n plant growth {ActivePlant.Growth}\n plant planting {ActivePlant.Planting}\n plant stage {ActivePlant.Stage}\n block stage {currentStage}");
            }
        }

        private void RelinkLoadData()
        {
            if (ActivePlant == null || ActiveRecipe == null)
                return;
            // sync block stage with plant
            currentStage = ActivePlant.Stage;
        }

        private void RestoreEnvironment(AquaPlantInstance plant)
        {
            plant.CurrentDepth = WaterDepth;
            plant.CurrentSalt = saltLevel;
        }

        public void SaveStats(Guid guid)
        {
            if (block.Storage != null)
            {
                var data = ToSave();
                byte[] bytes = MyAPIGateway.Utilities.SerializeToBinary(data);
                string base64 = Convert.ToBase64String(bytes);
                block.Storage[guid] = base64;
                //AquaExpansionSession.Insance.Log(true, "Saved");
            }
        }

        public void LoadStats(Guid guid)
        {
            string raw;
            if (block?.Storage != null &&
               block.Storage.TryGetValue(guid, out raw) &&
                !string.IsNullOrEmpty(raw))
            {
                byte[] bytes = Convert.FromBase64String(raw);
                var data = MyAPIGateway.Utilities.SerializeFromBinary<AquaFarmSaveData>(bytes);
                ToLoad(data);
                //AquaExpansionSession.Insance.Log(true, $"Loaded\n recipe {data.RecipeId},\n growth {data.Plant.Growth}\n planting {data.Plant.Planting}\n plant stage {data.Plant.Stage} \nplant defID {data.Plant.DefId}\n block stage {currentStage}");
            }
        }

        private void ClearbyGrid()
        {
            if (block?.CubeGrid?.Physics != null) // ignore projected and other non-physical grids
            { 
            utils.ModelStageChanged -= OnStageChanged;
            UnderwaterFarmPlotUI.Instance.BlockSaveRequest -= OnSessionSave;
            block.AppendingCustomInfo -= AppendCustomInfo;
            plant = null;
            ActivePlant = null;
            ActiveRecipe = null;
            }
        }
        private void Clear()
        {
            ClearbyGrid();
            sink = null;
            entity = null;
            utils = null;
            grid = null;
            block = null;
        }

        public override void Close()
        {
            Clear();
            base.Close();
        }
    }
}
