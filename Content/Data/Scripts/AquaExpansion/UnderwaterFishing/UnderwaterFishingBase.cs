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

namespace AquaExpansion.UnderwaterFishing
{

    public abstract class UnderwaterFishingBase : MyGameLogicComponent
    {
        private IMyFunctionalBlock block;
        private IMyCubeGrid grid;
        private IMyEntity entity;
        public AquaExpansionUtils utils;
        private string Ttitle = "Underwater Fishing Trap";
        private string TError = "ERROR";
        private float WaterDepth;
        private float saltLevel = 0f;
        private float saltP = 0f;
        private string Tdepth = "Water Depth:";
        private string Tsalt = "Salt level:";
        private MyInventory inv;
        private IMyInventory blockinv;
        protected AquaFarmingBlockType FarmBlockType;
        private MyResourceSinkComponent sink;
        protected bool HaseInventory = false;
        protected bool HasModStorage = false;
        protected float PowerWorkDrain = 0.05f; // Mw
        protected float PowerIddleDrain = 0.01f; // Mw
        private float CurrentPowerInput = 0f;
        private string Tcupower = "Current Power Input:";
        private string Tmaxpower = "Max Power Input:";
        private AquaFishBlockStage currentStage;
        private float time = 0f;
        private int attractstage;
        private int catchstage;
        private int catchresultstage;
        private int baitStorage;
        private int FishStorage;
        private float processTime;
        private AquaFishingRecipe ActiveFishingRecipe;
        private AquaFishInstance ActiveFish;
        private float CatchingProbability;
        private bool ready;
        public bool usebigfishdata = false;
        private MyEntity Bait;
        protected Matrix BaitMatrix = Matrix.CreateTranslation(0f, 2f, 0f);
        private int baitstage = 0;
        private int baitlastStage = -1;
        private MyEntity Fish;
        protected Matrix FishMatrix = Matrix.CreateTranslation(0f, 2f, 0f);
        private int fishstage = 0;
        private int fishlastStage = -1;
        private float fishAngle;
        private float showfishtimer;
        private int showfishstage = 0;
        private int showfishlaststage = -2;
        private MyEntity InventoryFish;
        protected Matrix InventoryFishMatrix = Matrix.CreateTranslation(0f, 0f, 0f);

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            block = Entity as IMyFunctionalBlock;
            if (block == null)
                return;
            grid = block.CubeGrid;
            entity = block;
            utils = new AquaExpansionUtils();
            ModStorageHandler();
            InventoryHandler();
            SetSink();
            //AquaExpansionSession.Insance.Log(true, $"Underwater fising block initialized for block: {block.EntityId}");
            NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME | MyEntityUpdateEnum.BEFORE_NEXT_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.EACH_FRAME;
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
            block.AppendingCustomInfo += AppendCustomInfo;
            UnderwaterFishingUI.Instance.ConnectToBlock(block);
            UnderwaterFishingUI.Instance.BlockSaveRequest += OnSessionSave;
            UnderwaterFishingUI.Instance.RunControlls();
            utils.ModelStageChanged += OnStageChanged;
            utils.FishModelStageChanged += OnFishStageChanged;
            utils.InventoryFishShow += OnInventoryFishChanged;
            Load();
            base.UpdateOnceBeforeFrame();
        }

        private void OnInventoryFishChanged(int obj)
        {
            showfishstage = obj;
            if (showfishstage == showfishlaststage)
                return;
            showfishlaststage = showfishstage;
            //AquaExpansionSession.Insance.Log(true, $"fish stage {showfishstage}");
            SetInventoryFishModel(GetModelbyInventoryFish(showfishstage));
        }

        private void OnFishStageChanged(int obj)
        {
            fishstage = obj;
            if (fishstage == fishlastStage)
                return;
            fishlastStage = fishstage;
            SetFishModel(GetModelByActiveFish(fishstage));
        }

        private void OnStageChanged(int obj)
        {
            baitstage = obj;
            if (baitstage == baitlastStage)
                return;
            baitlastStage = baitstage;
            //AquaExpansionSession.Insance.Log(true, $" model idx {baitstage}");
            SetBaitModel(GetModelByBait(baitstage));
        }

        private void OnSessionSave()
        {
            if (block != null && !block.Closed && !block.MarkedForClose)
            {
                Save();
                //AquaExpansionSession.Insance.Log(true, $"Saved from Session");
            }
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
            utils.FishingStatusInfo(info, block, inv, currentStage);
            utils.GetFishingStorage(info, block, inv, baitStorage, FishStorage);
            utils.CurrentFish(info, block, inv, ActiveFishingRecipe);
            utils.ProgressLine(info, block, inv, currentStage, attractstage, catchstage, catchresultstage, processTime);
            utils.CurrentProbability(info, block, inv, ActiveFish, CatchingProbability);
            info.AppendLine($"{Tdepth} {(float)Math.Round(WaterDepth)} m");
            info.AppendLine($"{Tsalt} {(float)Math.Round(saltP)}%");
        }

        public override void UpdateBeforeSimulation()
        {
            if (block == null || block.Closed || block.MarkedForClose)
                return;
            if (grid == null || grid.Closed || grid.MarkedForClose)
                return;
            if (grid.Physics == null)
                return;
            if (!grid.IsStatic)
                return;
            if (!HasPower())
            { block.Enabled = false; utils.OnModelStageChanged(0); utils.OnFishModelStageChanged(0); }
            UnderwaterRules();
            base.UpdateBeforeSimulation();
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
            CreateBait();
            UpdateBaitMatrix();
            CreateFish();
            UpdateFishMatrix();
            CreateInventoryFish();
            UpdateInventoryFishmatrix();
            Opimization();
            InventoryFishOptimization();
            time += 0.02f;
            utils.SetEnviromentalFishingEmissive(time, block, currentStage, WaterDepth, attractstage, catchstage, catchresultstage);
            base.UpdateAfterSimulation();
        }

        private void Opimization()
        {
            if (ActiveFish == null || ActiveFishingRecipe == null)
                return;
            if (baitstage == 1)
            {
                utils.ModelOptimization(Bait, block);
            }
            if (fishstage == 1)
            {
                utils.ModelOptimization(Fish, block);
            }
        }

        private void InventoryFishOptimization()
        {
            if (showfishstage != -1 || showfishstage != -2)
            {
                utils.ModelOptimization(InventoryFish, block);
            }
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
            utils.UpdateFishing(block, inv, FarmBlockType, ref currentStage, ref ActiveFishingRecipe, ref ActiveFish, ref CatchingProbability, WaterDepth, saltLevel, ref processTime);
            if (!block.Enabled && (currentStage == AquaFishBlockStage.Atracting))
            {
                if (ready)
                    return;
                ready = true;
                Save();
            }
            UpdateSink();
            attractstage = utils.GetAttractingpercent(ActiveFish);
            catchstage = utils.GetCatchingngpercent(ActiveFish);
            catchresultstage = utils.GetCatchresultngpercent(ActiveFish);
            utils.CountFishingCargo(block, inv, out baitStorage, out FishStorage, FarmBlockType);
            AquaExpansionSession.Insance.UpdateTerminal(block);
            utils.ShowInventoryFish(FishStorage, ref showfishtimer);
            base.UpdateAfterSimulation10();
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

        private void GetCurrentWaterData()
        {
            if (block != null && !block.Closed && block.Enabled && block.IsFunctional && WaterModAPI.IsUnderwater(block.GetPosition()) && block.IsWorking)
            {
                WaterDepth = AquaExpansionSession.Insance.GetWaterDepth(block);
                saltLevel = AquaExpansionSession.Insance.GetSaltLevel(block, WaterDepth);
                saltP = AquaExpansionSession.Insance.SaltToPercent(saltLevel);
            }
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
            float power = currentStage == AquaFishBlockStage.Idlle
                ? PowerIddleDrain : PowerWorkDrain;
            CurrentPowerInput = power;
            return power;
        }

        private bool HasPower()
        {
            if (block == null || block.Closed || !block.Enabled || !block.IsFunctional)
                return false;
            return sink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId);
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

        protected virtual void SaveUnderwaterFishingData()
        {
            
        }

        protected virtual void LoadingUnderwaterFishingData()
        {
            
        }

        private void Save()
        {
            if (HasModStorage)
            {
                if (block != null && !block.Closed)
                {
                    SaveUnderwaterFishingData();
                }
            }
        }

        private void Load()
        {

            if (HasModStorage)
            {
                if (block != null && !block.Closed)
                {
                    LoadingUnderwaterFishingData();
                }
            }
        }

        protected void LoadStats(Guid guid)
        {
            string raw;
            if (block?.Storage != null &&
               block.Storage.TryGetValue(guid, out raw) &&
                !string.IsNullOrEmpty(raw))
            {
                byte[] bytes = Convert.FromBase64String(raw);
                var data = MyAPIGateway.Utilities.SerializeFromBinary<AquaFishingSaveData>(bytes);
                ToLoad(data);
                //AquaExpansionSession.Insance.Log(true, "Loaded");
            }
        }

        protected void SaveStats(Guid guid)
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

        private AquaFishingSaveData ToSave()
        {
            if (ActiveFish == null || ActiveFishingRecipe == null)
                return null;
            return new AquaFishingSaveData
            {
                RecipeId = ActiveFishingRecipe.Id,
                BaitModelStageId = baitstage,
                FishModelStageId = fishstage,
                Fish = new AquaSaveFishData
                {
                    DefId = ActiveFish.DefID,
                    Stage = (int)ActiveFish.Stage,
                    Attract = ActiveFish.Attract,
                    Catch = ActiveFish.Catch,
                    CatchResult = ActiveFish.CatchResult
                }
            };
        }

        private void ToLoad(AquaFishingSaveData data)
        {
            // rebuild recipe
            ActiveFishingRecipe = AquaFishingRecipeDatabase.Get(data.RecipeId);
            baitstage = data.BaitModelStageId;
            fishstage = data.FishModelStageId;
            // rebuild fish instance
            ActiveFish = new AquaFishInstance
            {
                DefID = data.Fish.DefId,
                fishDef = AquaFishDatabase.Get(data.Fish.DefId),
                Stage = (AquaFishBlockStage)data.Fish.Stage,
                Attract = data.Fish.Attract,
                Catch = data.Fish.Catch,
                CatchResult = data.Fish.CatchResult
            };
            // restore runtime-only values
            RestoreEnvironment(ActiveFish);
            RelinkLoadData();
        }

        private void RestoreEnvironment(AquaFishInstance fish)
        {
            fish.CurrentDepth = WaterDepth;
            fish.CurrentSalt = saltLevel;
        }

        private void RelinkLoadData()
        {
            if (ActiveFish == null || ActiveFishingRecipe == null)
                return;
            // sync block stage with plant
            currentStage = ActiveFish.Stage;
        }

        private void CreateBait()
        {
            if (Bait != null || !block.IsFunctional || !block.Enabled)
                return;
            Bait = new MyEntity();
            Bait.Save = false;
            Bait.Init(null, GetModelByBait(baitstage), null, 1f, null);
            block.Hierarchy.AddChild(Bait, true, true);
            Matrix local = BaitMatrix;
            Bait.PositionComp.SetLocalMatrix(ref local);
            block.NeedsWorldMatrix = true;
            Bait.NeedsWorldMatrix = true;
            //AquaExpansionSession.Insance.Log(true, $"block children {block.Hierarchy.Children.Count}");
        }

        private void CreateFish()
        {
            if (Fish != null || !block.IsFunctional || !block.Enabled)
                return;
            Fish = new MyEntity();
            Fish.Save = false;
            Fish.Init(null, GetModelByActiveFish(fishstage), null, 1f, null);
            block.Hierarchy.AddChild(Fish, true, true);
            Matrix local = FishMatrix;
            Fish.PositionComp.SetLocalMatrix(ref local);
            block.NeedsWorldMatrix = true;
            Fish.NeedsWorldMatrix = true;
            //AquaExpansionSession.Insance.Log(true, $"block children {block.Hierarchy.Children.Count}");
        }

        private void CreateInventoryFish()
        {
            if (InventoryFish != null || !block.IsFunctional || !block.Enabled)
                return;
            InventoryFish = new MyEntity();
            InventoryFish.Save = false;
            InventoryFish.Init(null, GetModelbyInventoryFish(showfishlaststage), null, 1f, null);
            block.Hierarchy.AddChild(InventoryFish, true, true);
            Matrix local = InventoryFishMatrix;
            InventoryFish.PositionComp.SetLocalMatrix(ref local);
            block.NeedsWorldMatrix = true;
            InventoryFish.NeedsWorldMatrix = true;
        }

        private string GetModelByBait(int baitstage)
        {
            if (ActiveFishingRecipe == null)
                return null;
            switch (baitstage)
            {
                case 1: return AquaModpathUtils.GetModPaths(AquaModpathUtils.GetDetailedItemModelPath(AquaFishingRecipeDatabase.GetBaitModelfromRecipe(ActiveFishingRecipe))); //active bait
                default: return null; // stage 0 = no bait
            }
        }

        private string GetModelByActiveFish(int fishstage)
        {
            if (ActiveFishingRecipe == null)
                return null;
            switch (fishstage)
            {
                case 1: return ActiveFishingRecipe.FishResult.Model; //active fish
                default: return null; // stage 0 = no fish
            }
        }

        private string GetModelbyInventoryFish(int inventoryfishstage)
        {
            if (inventoryfishstage == -1 || inventoryfishstage == -2)
                return null;
            var list = utils.inventorymodelpathsList;
            if (list == null || list.Count == 0)
                return null;
            if ((uint)inventoryfishstage >= (uint)list.Count)
                return null;

            return list[inventoryfishstage];
        }

        private void SetBaitModel(string model)
        {
            if (Bait == null || !Bait.InScene)
            {
                //AquaExpansionSession.Insance.Log(true, $"Bait not in scene or null");
                return;
            }
            if (string.IsNullOrEmpty(model))
            {
                Bait.Render.Visible = false;
                return;
            }
            Bait.RefreshModels(model, null);
            Bait.Render.Visible = true;
            Bait.Render.RemoveRenderObjects();
            Bait.Render.AddRenderObjects();
            Bait.Render.UpdateRenderObject(true);
        }

        private void SetFishModel(string model)
        {
            if (Fish == null || !Fish.InScene)
            {
                //AquaExpansionSession.Insance.Log(true, $"Fish not in scene or null");
                return;
            }
            if (string.IsNullOrEmpty(model))
            {
                Fish.Render.Visible = false;
                return;
            }
            Fish.RefreshModels(model, null);
            Fish.Render.Visible = true;
            Fish.Render.RemoveRenderObjects();
            Fish.Render.AddRenderObjects();
            Fish.Render.UpdateRenderObject(true);
        }

        private void SetInventoryFishModel(string model)
        {
            if (InventoryFish == null || !InventoryFish.InScene)
            {
                //AquaExpansionSession.Insance.Log(true, $"InventoryFish not in scene or null");
                return;
            }
            if (string.IsNullOrEmpty(model))
            {
                InventoryFish.Render.Visible = false;
                return;
            }
            InventoryFish.RefreshModels(model, null);
            InventoryFish.Render.Visible = true;
            InventoryFish.Render.RemoveRenderObjects();
            InventoryFish.Render.AddRenderObjects();
            InventoryFish.Render.UpdateRenderObject(true);
        }

        private void UpdateBaitMatrix()
        {
            if (Bait == null || block == null || block.Closed || block.MarkedForClose)
                return;
            MatrixD local = BaitMatrix;
            MatrixD world = local * block.WorldMatrix;
            Bait.PositionComp.SetWorldMatrix(ref world);
        }

        private void UpdateFishMatrix()
        {
             if(Fish == null || block == null || block.Closed || block.MarkedForClose)
                return;
            float currentYaw = 0f;
            Vector3 previousLocalPos = new Vector3(0f, 2f, 0f);
            if (fishstage == 0)
            {
                MatrixD worldIdle = FishMatrix * block.WorldMatrix;
                Fish.PositionComp.SetWorldMatrix(ref worldIdle);
            }
            else
            {
                fishAngle += 0.01f;
                float radius = 1f;
                // orbit position
                Vector3 localOffset = new Vector3(
                    (float)Math.Cos(fishAngle) * radius,
                    2.5f + (float)Math.Sin(fishAngle * 2f) * 0.2f,
                    (float)Math.Sin(fishAngle) * radius
                );
                // movement direction
                Vector3 moveDir = localOffset - previousLocalPos;
                if (moveDir.LengthSquared() > 0.0001f)
                {
                    moveDir.Normalize();
                    // tangent/orbit facing
                    currentYaw = (float)Math.Atan2(moveDir.X, moveDir.Z);
                }
                previousLocalPos = localOffset;
                float sway = (float)Math.Sin(fishAngle * 2f) * 0.1f;
                // model forward correction
                Matrix correction = Matrix.CreateRotationY(MathHelper.PiOver2);
                Matrix localMatrix =
                    correction *
                    Matrix.CreateRotationY(currentYaw) *
                    Matrix.CreateRotationZ(sway) *
                    Matrix.CreateTranslation(localOffset);
                MatrixD world = block.WorldMatrix;
                Fish.PositionComp.SetLocalMatrix(ref localMatrix);
                Fish.PositionComp.SetWorldMatrix(ref world);
            }
        }

        private void UpdateInventoryFishmatrix()
        {
            if (InventoryFish == null || block == null || block.Closed || block.MarkedForClose)
                return;
            float currentYaw = 0f;
            Vector3 previousLocalPos = new Vector3(0f, 0f, 0f);
            if (showfishstage == -1)
            {
                MatrixD worldIdle = FishMatrix * block.WorldMatrix;
                InventoryFish.PositionComp.SetWorldMatrix(ref worldIdle);
            }
            else
            {
                fishAngle += 0.01f;
                float radius = 0.8f;
                // orbit position
                Vector3 localOffset = new Vector3(
                    (float)Math.Cos(fishAngle) * radius,
                    0f + (float)Math.Sin(fishAngle * 0.5f) * 0.2f,
                    (float)Math.Sin(fishAngle) * radius
                );
                // movement direction
                Vector3 moveDir = localOffset - previousLocalPos;
                if (moveDir.LengthSquared() > 0.0001f)
                {
                    moveDir.Normalize();
                    // tangent/orbit facing
                    currentYaw = (float)Math.Atan2(moveDir.X, moveDir.Z);
                }
                previousLocalPos = localOffset;
                float sway = (float)Math.Sin(fishAngle * 0.5f) * 0.1f;
                // model forward correction
                Matrix correction = Matrix.CreateRotationY(MathHelper.PiOver2);
                Matrix localMatrix =
                    correction *
                    Matrix.CreateRotationY(currentYaw) *
                    Matrix.CreateRotationZ(sway) *
                    Matrix.CreateTranslation(localOffset);
                MatrixD world = block.WorldMatrix;
                InventoryFish.PositionComp.SetLocalMatrix(ref localMatrix);
                InventoryFish.PositionComp.SetWorldMatrix(ref world);
            }
        }
        private void ClearbyGrid()
        {
            if (block?.CubeGrid?.Physics != null) // ignore projected and other non-physical grids
            {
                utils.ModelStageChanged -= OnStageChanged;
                utils.FishModelStageChanged -= OnFishStageChanged;
                utils.InventoryFishShow -= OnInventoryFishChanged;
                UnderwaterFishingUI.Instance.BlockSaveRequest -= OnSessionSave;
                block.AppendingCustomInfo -= AppendCustomInfo;
                Bait = null;
                Fish = null;
                InventoryFish = null;
                ActiveFish = null;
                ActiveFishingRecipe = null;
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
