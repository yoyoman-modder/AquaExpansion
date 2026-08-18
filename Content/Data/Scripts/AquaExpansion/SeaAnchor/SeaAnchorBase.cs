using AquaExpansion.Core;
using Jakaria.API;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using System;
using System.Text;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace AquaExpansion.SeaAnchor
{
    public abstract class SeaAnchorBase : MyGameLogicComponent
    {
        private IMyFunctionalBlock block;
        private IMyCubeGrid grid;
        public AquaExpansionUtils utils;
        private string Ttitle = "Sea Anchor";
        private string TError = "ERROR";
        private string TEwaterdata = "NO WATER DATA";
        private string Tdepth = "Water Depth:";
        private string Tsalt = "Salt level:";
        private string Tcupower = "Current Power Input:";
        private string Tmaxpower = "Max Power Input:";
        private float WaterDepth;
        private float saltLevel = 0f;
        private float saltP = 0f;
        private MyResourceSinkComponent sink;
        protected float PowerWorkDrain = 0.05f; // Mw
        protected float PowerIddleDrain = 0.01f; // Mw
        private float CurrentPowerInput = 0f;
        private AquaSeaAnchorState Currentstate;
        protected AquaSeaAnchorType AnchorType;
        protected float deploySpeed = 2f;      // meters per second
        protected float retractSpeed = 3f;
        protected bool HaseInventory = false;
        protected bool HasModStorage = false;
        private IMyEntity entity;
        private MyInventory inv;
        private IMyInventory blockinv;
        private MyEntity Anchorpart;
        private MyEntity AnchorRope;
        private Vector3D deployDirection;
        private int anchorpartstage = 0;
        private int anchorpartlaststage = -1;
        private bool armed;
        private bool signalequp;
        private AquaSeaAnchorInstance ActiveAnchor;
        private AquaSeaAnchorEquipOrder ActiveOrder;
        private Matrix RopeMatrix = Matrix.CreateTranslation(0f, 0f, 0f);
        private int ropestage = 0;
        private int ropelastStage = -1;
        private float currentRPM = 0f;
        private float targetRPM = 0f;
        private float ACCELERATION = 2f;   // how fast it speeds up
        private float DECELERATION = 2f;   // how fast it slows down
        private float angle = 0f;
        protected float drumRadius = 0.25f;
        private Vector3D gravity;
        private bool ready;
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            block = Entity as IMyFunctionalBlock;
            if (block == null)
                return;
            grid = block.CubeGrid;
            entity = block;
            utils = new AquaExpansionUtils();
            SetSink();
            //AquaExpansionSession.Insance.Log(true, $"Sea Ahchor initialized for block: {block.EntityId}");
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.EACH_FRAME;
        }
        public override void UpdateOnceBeforeFrame()
        {
            if (block == null || block.Closed || block.MarkedForClose)
                return;
            if (grid == null || grid.Closed || grid.MarkedForClose)
                return;
            if (grid.Physics == null)
                return;
            ModStorageHandler();
            InventoryHandler();
            block.AppendingCustomInfo += AppendCustomInfo;
            SeaAnchorUI.instance.ConnectToBlock(block);
            SeaAnchorUI.instance.BlockSaveRequest += OnSessionSave;
            utils.ModelStageChanged += OnStageChanged;
            utils.AnchormodelShow += OnAnchorModelChanged;
            utils.RevertSignalEquip += OnFailedEquip;
            SeaAnchorUI.instance.RunControlls();
            SeaAnchorUI.instance.RunActions();
            Load();
            base.UpdateOnceBeforeFrame();
        }
        private void OnFailedEquip(bool obj)
        {
            signalequp = false;
            //AquaExpansionSession.Insance.Log(true, $"signal {signalequp}");
        }
        private void OnAnchorModelChanged(int obj)
        {
            anchorpartstage = obj;
            if (anchorpartstage == anchorpartlaststage)
                return;
            anchorpartlaststage = anchorpartstage;
            //AquaExpansionSession.Insance.Log(true, $" model idx {anchorpartstage}");
            SetAnchorModel(GetModelByAnchor(anchorpartstage));
        }
        private void OnStageChanged(int obj)
        {
            ropestage = obj;
            if (ropestage == ropelastStage)
                return;
            ropelastStage = ropestage;
            //AquaExpansionSession.Insance.Log(true, $"  model idx {ropestage}");
            SetRopeModel(GetModelByRope(ropestage));
        }
        private void OnSessionSave()
        {
            if (block != null && !block.Closed && !block.MarkedForClose)
            {
                Save();
                //AquaExpansionSession.Insance.Log(true, $"Saved from Session");
            }
        }
        private void Save()
        {
            if (HasModStorage)
            {
                if (block != null && !block.Closed)
                {
                    SaveSeaAnchorData();
                }
            }
        }
        private void Load()
        {
            if (HasModStorage)
            {
                if (block != null && !block.Closed)
                {
                    LoadingSeaAnchorData();
                }
            }
        }
        protected virtual void SaveSeaAnchorData()
        {

        }
        protected virtual void LoadingSeaAnchorData()
        {

        }
        protected void LoadStats(Guid guid)
        {
            string raw;
            if (block?.Storage != null &&
              block.Storage.TryGetValue(guid, out raw) &&
               !string.IsNullOrEmpty(raw))
            {
                byte[] bytes = Convert.FromBase64String(raw);
                var data = MyAPIGateway.Utilities.SerializeFromBinary<AnchorStorage>(bytes);
                ToLoad(data);
                //AquaExpansionSession.Insance.Log(true, $"Loaded: {signalequp}");
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
                //AquaExpansionSession.Insance.Log(true, $"Saved: {base64} " +
                    //$"guid:{guid}");
            }
        }
        private AnchorStorage ToSave()
        {
            if (ActiveAnchor == null || ActiveOrder == null)
                return null;
            //AquaExpansionSession.Insance.Log(true, $"save: {signalequp}");
            return new AnchorStorage
            {
                OrderID = ActiveOrder.Id,
                Ropemodelstage = ropestage,
                Anchormodelstage = anchorpartstage,
                state = (int)Currentstate,
                armstate = armed,
                equiped = signalequp,
                Anchor = new AquaSeaAnchorSaveData
                {
                    DefId = ActiveAnchor.DefID,
                    Cablelength = ActiveAnchor.Cablelength,
                    AnchorPosition = ActiveAnchor.AnchorPosition,
                    AttachPoint = ActiveAnchor.AttachPoint
                }
            };
        }
        private void ToLoad(AnchorStorage data)
        {
            // rebuild recipe
            switch (AnchorType)
            {
                case AquaSeaAnchorType.L:
                    ActiveOrder = AquaSeaAnchorEquipOrderDatabase.Get(data.OrderID); 
                break;
                case AquaSeaAnchorType.S:
                    ActiveOrder = AquaSeaAnchorEquipOrderDatabase.GetSmall(data.OrderID);
                    break;
            }
            ropestage = data.Ropemodelstage;
            anchorpartstage = data.Anchormodelstage;
            // rebuild anchor instance
            ActiveAnchor = new AquaSeaAnchorInstance
            {
                DefID = data.Anchor.DefId,
                anchordef = AquaSeaAnchorDatabase.Get(data.Anchor.DefId),
                Cablelength = data.Anchor.Cablelength,
                AnchorPosition = data.Anchor.AnchorPosition,
                AttachPoint = data.Anchor.AttachPoint
            };
            Currentstate = (AquaSeaAnchorState)data.state;
            armed = data.armstate;
            signalequp = data.equiped;
        }
        private void AppendCustomInfo(IMyTerminalBlock block, StringBuilder info)
        {
            if (block != null && !block.Closed)
            {
                if (block.IsFunctional && grid != null && !grid.Closed)
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
            utils.AnchorStatusInfo(info, block, inv, Currentstate);
            utils.CurrentAnchor(info, block, inv, ActiveOrder);
            utils.AnchorCableinfo(info, block, ActiveAnchor);
            if (WaterModAPI.IsUnderwater(block.GetPosition()))
            {
                info.AppendLine($"{Tdepth} {(float)Math.Round(WaterDepth)} m");
                info.AppendLine($"{Tsalt} {(float)Math.Round(saltP)}%");
            }
            else
            {
                info.AppendLine(TEwaterdata);
            }
        }
        public override void UpdateBeforeSimulation()
        {
            if (block == null || block.Closed || block.MarkedForClose)
                return;
            if (grid.Physics == null)
                return;
            if (!HasPower())
                block.Enabled = false;
            if (!InAtmosphere())
                block.Enabled = false;
            base.UpdateBeforeSimulation();
        }
        public override void UpdateAfterSimulation()
        {
            if (block == null || block.Closed || block.MarkedForClose)
                return;
            //if (grid == null || grid.Closed || grid.MarkedForClose)
                //return;
            if (grid.Physics == null)
                return;
            GravitiDirection();
            CreateRopePart();
            UpdateRopeMatrix();
            CreateAnchorPart();
            utils.UpdateAnchoring(block, inv, ref Currentstate, AnchorType, ref ActiveAnchor, ref ActiveOrder, signalequp,ref armed, deploySpeed,
               deployDirection, AnchorStartPosition,retractSpeed, RopeStartPosition, RopeEndPosition);
            UpdateAnchorMatrix();
            utils.SetEmissivebyAnchorStatus(block, Currentstate, 1.0f);
            //AquaExpansionSession.Insance.Log(true, $"equiped {signalequp}");
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
            GetCurrentWaterData();
            /*if (!block.Enabled)
            {
                if (ready)
                    return;
                ready = true;
                Save();
            }*/
            UpdateSink();
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
            float power = Currentstate == AquaSeaAnchorState.Idle || Currentstate == AquaSeaAnchorState.Deployed || Currentstate == AquaSeaAnchorState.Attached
                ? PowerIddleDrain : PowerWorkDrain;
            CurrentPowerInput = power;
            return power;
        }
        private bool HasPower()
        {
            if (block == null || block.Closed || !block.Enabled || !block.IsFunctional || grid == null || grid.Closed || grid.MarkedForClose)
                return false;
            return sink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId);
        }
        private bool InAtmosphere()
        {
            if (block == null || block.Closed || !block.Enabled || !block.IsFunctional || grid == null || grid.Closed || grid.MarkedForClose)
                return false;
            var eox = MyAPIGateway.Session.OxygenProviderSystem.GetOxygenInPoint(block.GetPosition());
            return eox > AquaExpansionSession.Insance.MIN_ENVOXYGENLEVEL;
        }
        private void UpdateSink()
        {
            if (block != null && !block.Closed && block.IsFunctional)
            {
                if (sink != null)
                {
                    sink.Update();
                }
            }
        }
        public void ToggleAnchor()
        {
            if (block == null || block.Closed|| block.MarkedForClose || !block.Enabled || !block.IsFunctional || grid == null || grid.Closed || grid.MarkedForClose)
                return;
            if (ActiveAnchor == null || ActiveOrder == null)
                return;
            switch (Currentstate)
            {
                case AquaSeaAnchorState.Idle:
                    BeginDeploy();
                    break;
                case AquaSeaAnchorState.Deploying:
                case AquaSeaAnchorState.Deployed:
                case AquaSeaAnchorState.Attached:
                    BeginRetract();
                    break;
                case AquaSeaAnchorState.Retracting:
                    break;
            }
        }
        public void ArmAnchorSet()
        {
            if (block == null || block.Closed || block.MarkedForClose || !block.IsFunctional || grid == null || grid.Closed || grid.MarkedForClose)
                return;
            if (Currentstate != AquaSeaAnchorState.Idle)
                return;
            signalequp = !signalequp;
            //AquaExpansionSession.Insance.Log(true, signalequp ? "Sent Equip signal" : "Sent Unequip signal");
        }
        public void BeginDeploy()
        {
            if (ActiveAnchor == null)
               return;
            if (Currentstate != AquaSeaAnchorState.Idle)
                return;
            // Reset deployed anchor state
            ActiveAnchor.AnchorPosition = AnchorStartPosition;
            ActiveAnchor.Cablelength = 0f;
            Currentstate = AquaSeaAnchorState.Deploying;
            //AquaExpansionSession.Insance.Log(true, "Anchor deployment started.");
        }
        public void BeginRetract()
        {
            if (ActiveAnchor == null)
                return;
            if (Currentstate != AquaSeaAnchorState.Deploying &&
                Currentstate != AquaSeaAnchorState.Deployed &&
                Currentstate != AquaSeaAnchorState.Attached)
                return;
            Currentstate = AquaSeaAnchorState.Retracting;
            //AquaExpansionSession.Insance.Log(true, "Anchor retraction started.");
        }
        private void GravitiDirection()
        {
            if (grid == null || grid.Closed || grid.MarkedForClose)
                return;
            float gravityIntensity;
            gravity = MyAPIGateway.Physics.CalculateNaturalGravityAt(block.GetPosition(), out gravityIntensity);
            deployDirection = gravity.LengthSquared() > 0 ? Vector3D.Normalize(gravity) : Vector3D.Down;
        }
        private void InventoryHandler()
        {
            if (HaseInventory)
            {
                blockinv = block.GetInventory() as IMyInventory;
                inv = blockinv as MyInventory;
                if (blockinv != null)
                {
                    utils.SetupSeaAnchorDefinitions(AnchorType);
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
                //AquaExpansionSession.Insance.Log(true, $"No ModStorage used");
            }
        }
        private void CreateAnchorPart()
        {
            if (Anchorpart != null || !block.IsFunctional)
                return;
            if (grid == null || grid.Closed || grid.MarkedForClose)
                return;
            Anchorpart = new MyEntity();
            Anchorpart.Save = false;
            Anchorpart.Init(null, GetModelByAnchor(anchorpartstage), null, 1f, null);
            MyEntities.Add(Anchorpart);
        }
        private void CreateRopePart()
        {
            if (AnchorRope != null || !block.IsFunctional)
                return;
            if (grid == null || grid.Closed || grid.MarkedForClose)
                return;
            AnchorRope = new MyEntity();
            AnchorRope.Save = false;
            AnchorRope.Init(null, GetModelByRope(ropestage), null, 1f, null);
            block.Hierarchy.AddChild(AnchorRope, true, true);
            Matrix local = RopeMatrix;
            AnchorRope.PositionComp.SetLocalMatrix(ref local);
            block.NeedsWorldMatrix = true;
            AnchorRope.NeedsWorldMatrix = true;
        }
        private string GetModelByAnchor(int anchorpartstage)
        {
            if (ActiveOrder == null)
                return null;
            switch (ropestage)
            {
                case 1: return ActiveOrder.Anchorresult.Anchormodel; //active anchor
                default: return null; // stage 0 = no anchorpart
            }
        }
        private string GetModelByRope(int ropestage)
        {
            if (ActiveOrder == null)
                return null;
            switch (ropestage)
            {
                case 1: return ActiveOrder.Anchorresult.CableModel; //active rope
                default: return null; // stage 0 = no rope
            }
        }
        private void SetRopeModel(string model)
        {
            if (AnchorRope == null || !AnchorRope.InScene)
            {
                //AquaExpansionSession.Insance.Log(true, $"AnchorRope not in scene or null");
                return;
            }
            if (string.IsNullOrEmpty(model))
            {
                AnchorRope.Render.Visible = false;
                return;
            }
            AnchorRope.RefreshModels(model, null);
            AnchorRope.Render.Visible = true;
            AnchorRope.Render.RemoveRenderObjects();
            AnchorRope.Render.AddRenderObjects();
            AnchorRope.Render.UpdateRenderObject(true);
        }
        private void SetAnchorModel(string model)
        {
            if (Anchorpart == null || !Anchorpart.InScene)
            {
                //AquaExpansionSession.Insance.Log(true, $"AnchorPart not in scene or null");
                return;
            }
            if (string.IsNullOrEmpty(model))
            {
                Anchorpart.Render.Visible = false;
                return;
            }
            Anchorpart.RefreshModels(model, null);
            Anchorpart.Render.Visible = true;
            Anchorpart.Render.RemoveRenderObjects();
            Anchorpart.Render.AddRenderObjects();
            Anchorpart.Render.UpdateRenderObject(true);
        }
        private void UpdateRopeMatrix()
        {
            if (AnchorRope == null || block == null || block.Closed || block.MarkedForClose)
                return;
            if (grid == null || grid.Closed || grid.MarkedForClose)
                return;
            MatrixD local = RopeMatrix;
            MatrixD world = local * block.WorldMatrix;
            AnchorRope.PositionComp.SetWorldMatrix(ref world);
            UpdateRopeBase();
        }
        private void UpdateAnchorMatrix()
        {
            if (Anchorpart == null || block == null || block.Closed || block.MarkedForClose || ActiveAnchor == null)
                return;
            if (grid == null || grid.Closed || grid.MarkedForClose)
                return;
            MatrixD world;
            if (Currentstate == AquaSeaAnchorState.Idle)
            {
                // Position follows the block
                Vector3D pos = AnchorStartPosition;
                // Orientation follows gravity
                Vector3D up = -deployDirection;
                Vector3D reference = block.WorldMatrix.Forward;
                if (Math.Abs(Vector3D.Dot(reference, up)) > 0.99)
                    reference = block.WorldMatrix.Right;
                Vector3D right = Vector3D.Normalize(Vector3D.Cross(reference, up));
                Vector3D forward = Vector3D.Normalize(Vector3D.Cross(up, right));
                world = MatrixD.CreateWorld(pos, forward, up);
                // world *= MatrixD.CreateRotationY(MathHelper.Pi);
                ActiveAnchor.AnchorPosition = pos;
            }
            else
            {
                // Anchor deployed
                Vector3D up = -deployDirection;
                Vector3D reference = block.WorldMatrix.Forward;
                if (Math.Abs(Vector3D.Dot(reference, up)) > 0.99)
                    reference = block.WorldMatrix.Right;
                Vector3D right = Vector3D.Normalize(Vector3D.Cross(reference, up));
                Vector3D forward = Vector3D.Normalize(Vector3D.Cross(up, right));
                world = MatrixD.CreateWorld(
                    ActiveAnchor.AnchorPosition,
                    forward,
                    up);
            }
            Anchorpart.PositionComp.SetWorldMatrix(ref world);
        }
        private Vector3D AnchorStartPosition
        {
            get
            {
                switch (AnchorType)
                {
                    case AquaSeaAnchorType.S:
                        return RopeStartPosition + deployDirection * 0.5;
                    case AquaSeaAnchorType.L:
                        return RopeStartPosition + deployDirection * 2.0;
                    default:
                        return RopeStartPosition + deployDirection * 0.5;
                }
            }
        }
        private Vector3D RopeStartPosition
        {
            get
            {
                return block.GetPosition();
            }
        }
        private Vector3D RopeEndPosition
        {
            get
            {
                switch(AnchorType)
                {
                    case AquaSeaAnchorType.S:
                        return RopeStartPosition + deployDirection * 0.5;
                    case AquaSeaAnchorType.L:
                        return RopeStartPosition + deployDirection * 2.0;
                    default:
                        return RopeStartPosition + deployDirection * 2.0;
                }
            }
        }
        public bool isClearedforChange
        {
            get
            {
                if (Currentstate == AquaSeaAnchorState.Idle)
                { return true; }
                return false;
            }
        }
        public bool isArmed
        {
            get
            {
                if (armed)
                { return true; }
                return false;
            }
        }
        private void UpdateRopeBase()
        {
            if (AnchorRope == null || block == null || block.Closed || !block.IsFunctional)
                return;
            float deltaTime = MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
            float cableSpeed = 0f;
            switch (Currentstate)
            {
                case AquaSeaAnchorState.Deploying:
                    cableSpeed = deploySpeed;
                    break;
                case AquaSeaAnchorState.Retracting:
                    cableSpeed = retractSpeed;
                    break;
                default:
                    targetRPM = 0f;
                    return;
            }
            targetRPM = (cableSpeed / (MathHelper.TwoPi * drumRadius)) * 60f;
            float lerpSpeed = targetRPM > currentRPM? ACCELERATION : DECELERATION;
            currentRPM = MathHelper.Lerp(currentRPM,targetRPM,lerpSpeed * deltaTime);
            if (float.IsNaN(currentRPM) || float.IsInfinity(currentRPM))
                currentRPM = 0f;
            float angularSpeed = currentRPM * MathHelper.TwoPi / 60f;
            if (Currentstate == AquaSeaAnchorState.Deploying)
                angle -= angularSpeed * deltaTime;
            else
                angle += angularSpeed * deltaTime;
            angle %= MathHelper.TwoPi;
            if (angle < 0f)
                angle += MathHelper.TwoPi;
            Matrix rotation = Matrix.CreateFromAxisAngle(RopeMatrix.Right,angle);
            Matrix finalMatrix = rotation * RopeMatrix;
            AnchorRope.PositionComp.SetLocalMatrix(ref finalMatrix);
        }
        private void ClearbyGrid()
        {
            if (block?.CubeGrid?.Physics != null)
            {
                utils.ModelStageChanged -= OnStageChanged;
                utils.AnchormodelShow -= OnAnchorModelChanged;
                utils.RevertSignalEquip -= OnFailedEquip;
                SeaAnchorUI.instance.BlockSaveRequest -= OnSessionSave;
                block.AppendingCustomInfo -= AppendCustomInfo;
                Anchorpart.Render.Visible = false;
                Anchorpart = null;
                AnchorRope = null;
                ActiveAnchor = null;
                ActiveOrder = null;
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
