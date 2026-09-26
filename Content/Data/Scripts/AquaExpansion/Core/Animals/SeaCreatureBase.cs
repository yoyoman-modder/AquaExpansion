using AquaExpansion.Core.BT;
using Jakaria.API;
using Sandbox.Game;
using Sandbox.Game.Components;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace AquaExpansion.Core.Animals
{
    public abstract class SeaCreatureBase : MyGameLogicComponent
    {
        protected IMyCharacter Character;
        private IMyGps Animalinfomarker;
        private bool ready = false;
        private bool showmarker = false;
        private LatentScheduler scheduler;
        private MyInventory inv;
        private IMyInventory chinv;
        private HashSet<string> AnimalFoodSubtypes = new HashSet<string>();
        private HashSet<string> AnimalWasteSubtypes = new HashSet<string>();
        private MyFixedPoint FoodAmount = new MyFixedPoint();
        private int FoodCount;
        protected SeadCreatureMovementData Movement;
        protected SeaCreatureNavigator SeaNavigator;
        protected SeaCreatureDefinition Deffinition;
        protected AnimalSensor Sensor;
        private int animalTick;
        private IMyGps AnimalBTmarker;
        //Behavior Tree integration
        protected BehaviorTree BT;
        protected Blackboard BB;
        private bool BTInitializationScheduled = false;
        private bool BTready = false;
        private BioLatentScheduler biobuffer;
        protected SeaCreatureAttackData Attack;
        /// <summary>
        /// Init Sea Creature
        /// </summary>
        /// <param name="objectBuilder"></param>
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            Character = Entity as IMyCharacter;
            if (Character == null)
                return;
            showmarker = true;
            GetAnimalInventory();
            FillFoodSubtypes();
            FillWasteSubtypes();
            biobuffer = new BioLatentScheduler();
            scheduler = new LatentScheduler();
            Movement = new SeadCreatureMovementData();
            SeaNavigator = new SeaCreatureNavigator();
            Sensor = new AnimalSensor();
            Attack = new SeaCreatureAttackData();
            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME;
        }
        /// <summary>
        /// Update internal
        /// </summary>
        public override void UpdateBeforeSimulation()
        {
            UpdateAnimalTick();
            BehaviorTreeRun();
            if (!IsValid())
                return;
            UpdateSeaCreatureDeffinition();
            UpdateMarker();
            UpdateAnimalSensor();
            // Executes delayed callbacks.
            biobuffer.Update();
            // Runs BT if initialization has completed.
            UpdateBehaviorTree();
            // Applies movement/state produced by BT.
            UpdateBTMarker();
            UpdateCreature();
            AnimalCauseDeath();
            AnimalAttackSphere();
            scheduler.Update();
            base.UpdateBeforeSimulation();
        }
        public override void UpdateBeforeSimulation10()
        {
            if (!IsValid() || Character.IsDead)
                return;
            LifeSupport(true);
            CountInventoryFood(out FoodCount);
            base.UpdateBeforeSimulation10();
        }
        /// <summary>
        /// Update internal Animal Tick
        /// </summary>
        private void UpdateAnimalTick()
        {
            animalTick++;
        }
        /// <summary>
        /// Component Main Update
        /// </summary>
        protected virtual void UpdateCreature()
        {
           
        }
        /// <summary>
        /// Set Attack Data
        /// </summary>
        protected virtual void SetAttackData()
        {
            
        }
        /// <summary>
        /// Set MovementData
        /// </summary>
        protected virtual void SetMovementData()
        {
            
        }
        /// <summary>
        /// Update debug marker
        /// </summary>
        private void UpdateMarker()
        {
            if (!IsValid()||Character.IsDead)
            {
                if (Animalinfomarker != null)
                {
                    RemoveMarker(Animalinfomarker);
                    Animalinfomarker = null;
                }
            }
            else
            {
                UpdateInfoMarker();
            }
        }
        /// <summary>
        /// Update BehaviorTree marker
        /// </summary>
        private void UpdateBTMarker()
        {
            if (!IsValid() || Character.IsDead)
            {
                if (AnimalBTmarker != null)
                {
                    RemoveBTMarker(AnimalBTmarker);
                    AnimalBTmarker = null;
                }
            }
            else
            {
                UpdateBTRunMarker();
            }
        }
        /// <summary>
        /// Remove marker
        /// </summary>
        /// <param name="marker"></param>
        private void RemoveMarker(IMyGps marker)
        {
            MyAPIGateway.Session.GPS.AddLocalGps(marker);
            marker.ShowOnHud = true;
            MyAPIGateway.Session.GPS.RemoveLocalGps(marker);
        }
        /// <summary>
        /// Remove BehaviorTree marker
        /// </summary>
        /// <param name="marker"></param>
        private void RemoveBTMarker(IMyGps marker)
        {
            MyAPIGateway.Session.GPS.AddLocalGps(marker);
            marker.ShowOnHud = true;
            MyAPIGateway.Session.GPS.RemoveLocalGps(marker);
        }
        /// <summary>
        /// Create Debug marker
        /// </summary>
        /// <returns></returns>
        private IMyGps CreateInfoMarker()
        {
            IMyGps animalgps = MyAPIGateway.Session.GPS.Create(string.Empty, string.Empty, Vector3D.Zero, true, false);
            MyAPIGateway.Session.GPS.AddLocalGps(animalgps);
            animalgps.ShowOnHud = false;
            MyAPIGateway.Session.GPS.RemoveLocalGps(animalgps);
            return animalgps;
        }
        /// <summary>
        /// Create Debug BehaviorTree marker
        /// </summary>
        /// <returns></returns>
        private IMyGps CreateBTMarker()
        {
            IMyGps btgps = MyAPIGateway.Session.GPS.Create(string.Empty, string.Empty, Vector3D.Zero, true, false);
            MyAPIGateway.Session.GPS.AddLocalGps(btgps);
            btgps.ShowOnHud = false;
            MyAPIGateway.Session.GPS.RemoveLocalGps(btgps);
            return btgps;
        }
        /// <summary>
        /// Update Info marker
        /// </summary>
        private void UpdateInfoMarker()
        {
            if (!IsValid())
                return;
            Vector3D pos = Character.GetPosition();
            Vector3D up = Character.WorldMatrix.Up;
            float height = 1f;
            if (AquaExpansionSession.Insance.AnimalDebugRenderEnabled)
            {
                if (Animalinfomarker == null)
                {
                    Animalinfomarker = CreateInfoMarker();
                }
                Animalinfomarker.Coords = pos + (up * height);
                Animalinfomarker.ShowOnHud = true;
                float healthvalue = AnimalHealth();
                float energyvalue = AnimalEnergy();
                string AnimalName = Character.DisplayName;
                float depth = AquaExpansionSession.Insance.GetWaterDepthbyCharacter(Character);
                float speed = Character.Physics.LinearVelocity.Length();
                Animalinfomarker.Name = $"{AnimalName}\nHealth {healthvalue} Energy {energyvalue:0}% Food {FoodCount}\n" +
                    $"Speed {Math.Round(speed)} m/s Depth {Math.Round(depth)}m";
                Animalinfomarker.GPSColor = Color.PaleGreen;
            }
            else
            {
                if (Animalinfomarker != null)
                {
                    RemoveMarker(Animalinfomarker);
                    Animalinfomarker = null;
                }
            }
        }
        /// <summary>
        /// Update BehaviorTree Running marker
        /// </summary>
        private void UpdateBTRunMarker()
        {
            if (!IsValid())
                return;
            Vector3D pos = Character.GetPosition();
            Vector3D f = Character.WorldMatrix.Forward;
            float length = 2f;
            if (AquaExpansionSession.Insance.AnimalBTDebugEnabled)
            {
                if (AnimalBTmarker == null)
                {
                    AnimalBTmarker = CreateBTMarker();
                }
                AnimalBTmarker.Coords = pos + (f * length);
                AnimalBTmarker.ShowOnHud = true;
                AnimalBTmarker.Name = $"BT Run\n" +
                (BT?.CurrentNodeName ?? "None");
                AnimalBTmarker.GPSColor = Color.PaleGreen;
            }
            else
            {
                if (AnimalBTmarker != null)
                {
                    RemoveBTMarker(AnimalBTmarker);
                    AnimalBTmarker = null;
                }
            }
        }
        /// <summary>
        /// Life support
        /// </summary>
        /// <param name="always"></param>
        private void LifeSupport(bool always)
        {
            if (!IsValid() || Character.IsDead)
                return;
            if (always)
            {
                var e = AnimalEnergy();
                if (e > 75f)
                    return;
                if (ready)
                    return;
                ready = true;
                scheduler.Schedule(InsertEnergyFood, 5, false, 0);
            }
        }
        /// <summary>
        /// Get Inventory
        /// </summary>
        private void GetAnimalInventory()
        {
            chinv = Character.GetInventory() as IMyInventory;
            inv = chinv as MyInventory;
            if (chinv != null && inv != null)
            {
                //AquaExpansionSession.Insance.Log(true, "Inventory found");
            }
        }
        /// <summary>
        /// Validate Character
        /// </summary>
        /// <returns></returns>
        protected bool IsValid()
        {
            return Character != null
                && Character.Physics != null
                && !Character.MarkedForClose
                && !Character.Closed;
        }
        /// <summary>
        /// Fill Food subtypes
        /// </summary>
        private void FillFoodSubtypes()
        {
            AnimalFoodSubtypes = SeaAnimalFoodItemsDatabase.FillAnimalFoodItems();
        }
        /// <summary>
        /// Fill Waste subtypes
        /// </summary>
        private void FillWasteSubtypes()
        {
            AnimalWasteSubtypes = SeaAnimalFoodItemsDatabase.FillAnimalWasteData();
        }
        /// <summary>
        /// Animal Get Food
        /// </summary>
        private void InsertEnergyFood()
        {
            if (!IsValid() || Character.IsDead)
                return;
            if (inv == null || chinv == null || inv.IsFull)
                return;
            if (string.IsNullOrEmpty(Deffinition.Food) || !AnimalFoodSubtypes.Contains(Deffinition.Food))
                return;
            FoodAmount = 1;
            var itemdef = GetSubtypebyObjectBuilder(Deffinition.Food);
            var obj = (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(itemdef);
            inv.AddItems(FoodAmount, obj);
            ready = false;
            scheduler.Schedule(AnimalGetEnergy,2, false, 0);
            //AquaExpansionSession.Insance.Log(true, $"Food Added {itemdef.SubtypeId}");

        }
        /// <summary>
        /// Get definition by subtype
        /// </summary>
        /// <param name="subtype"></param>
        /// <returns></returns>
        private static MyDefinitionId GetSubtypebyObjectBuilder(string subtype)
        {
            return MyDefinitionId.Parse($"MyObjectBuilder_ConsumableItem/{subtype}");
        }
        /// <summary>
        /// Count Food cargo
        /// </summary>
        /// <param name="food"></param>
        private void CountInventoryFood(out int food)
        {
            food = 0;
            if (!IsValid() || Character.IsDead)
                return;
            if (inv == null)
                return;
            foreach (var item in inv.GetItems())
            {
                var subtype = item.Content.SubtypeId.String;
                int amount = (int)item.Amount;

                if (AnimalFoodSubtypes.Contains(subtype))
                {
                    food += amount;
                }
            }
        }
        /// <summary>
        /// Animal Consume food
        /// </summary>
        private void AnimalGetEnergy()
        {
            var itemdef = GetSubtypebyObjectBuilder(Deffinition.Food);
            inv.ConsumeItem(itemdef, 1, Character.EntityId);
            //AquaExpansionSession.Insance.Log(true, $"Animal get Energy");
        }
        /// <summary>
        /// Animal Health internal
        /// </summary>
        /// <returns></returns>
        private float AnimalHealth()
        {
            if (!IsValid())
                return 0f;
            return (float)((int)(Character.Components.Get<MyCharacterStatComponent>().Health.Value * 10)) / 10f; ;
        }
        /// <summary>
        /// Animal Energy internal
        /// </summary>
        /// <returns></returns>
        private float AnimalEnergy()
        {
            if (!IsValid())
                return 0f;
            return Character.SuitEnergyLevel * 100f; ;
        }
        /// <summary>
        /// Get Animal Health property
        /// </summary>
        public float GetAnimalHealth
        {
            get
            {
                return AnimalHealth();
            }
        }
        /// <summary>
        /// Get Animal Energy property
        /// </summary>
        public float GetAnimalEnergy
        {
            get
            {
                return AnimalEnergy();
            }
        }
        /// <summary>
        /// Get AnimalTick
        /// </summary>
        protected int GetAnimalTick
        {
            get 
            {
                return animalTick;
            }
        }
        /// <summary>
        /// Init Sea Creature Deffinition
        /// </summary>
        private void UpdateSeaCreatureDeffinition()
        {
            Deffinition = SeaAnimalDatabase.Get(Character.Definition.Id.SubtypeId.String) ?? SeaAnimalDatabase.DefaultAnimal();
        }
        /// <summary>
        /// Animal Death by Enviroment
        /// </summary>
        private void AnimalCauseDeath()
        {
            if (animalTick % 30 != 0)
                return;
            if (!IsValid() || Character.IsDead)
                return;
            if (!WaterModAPI.IsUnderwater(Character.GetPosition()))
            {
                Movement.IsMoving = false;
                Character.DoDamage(20f, MyStringHash.GetOrCompute("Asphyxia"),true);
                return;
            }
            bool voxelCollision = false;
            bool gridCollision = false;
            if (Sensor != null)
            {
                if (Sensor.IsGroundDetected &&
                    Sensor.GroundDistance >= 0f &&
                    Sensor.GroundDistance <= 0.1f)
                {
                    IMyVoxelBase voxel =
                        Sensor.GroundHitEntity as IMyVoxelBase;

                    if (voxel != null)
                        voxelCollision = true;
                }
                if (Sensor.FrontObstacleDetected &&
                    Sensor.FrontObstacleDistance <= 0.1f)
                {
                    gridCollision = true;
                }
                if (Sensor.LeftObstacleDetected &&
                    Sensor.LeftObstacleDistance <= 0.1f)
                {
                    gridCollision = true;
                }
                if (Sensor.RightObstacleDetected &&
                    Sensor.RightObstacleDistance <= 0.1f)
                {
                    gridCollision = true;
                }
                if (Sensor.UpGridObstacleDetected &&
                    Sensor.UpGridObstacleDistance <= 0.1f)
                {
                    gridCollision = true;
                }
                if (Sensor.DownGridObstacleDetected &&
                    Sensor.DownGridObstacleDistance <= 0.1f)
                {
                    gridCollision = true;
                }
            }
            if (voxelCollision)
            {
                Character.DoDamage(2f,MyStringHash.GetOrCompute("Enviroment"),true);
            }
            if (gridCollision)
            {
                Character.DoDamage(2f,MyStringHash.GetOrCompute("Enviroment"),true);
            }

        }
        /// <summary>
        /// Animal Attack
        /// </summary>
        private void AnimalAttackSphere()
        {
            if (!IsValid() || Character.IsDead)
                return;
            if (Deffinition == null)
                return;
            if (Deffinition.Behavior != SeaAnimalBehavior.Predator)
                return;
            if (Attack == null)
                return;
            if (Attack.AttackInterval <= 0)
                return;
            if (animalTick % Attack.AttackInterval != 0)
                return;
            Vector3D animalPosition = Character.GetPosition();
            Vector3D forward = Character.WorldMatrix.Forward;
            forward.Normalize();
            Vector3D spherePosition = animalPosition + forward * Deffinition.AttackCenter;
            float radius = Attack.AttackRadius;
            BoundingSphereD sphere = new BoundingSphereD(spherePosition,radius);
            AnimalUtils.DebugSphere(spherePosition, Color.Red, radius);
            List<IMyEntity> entities = MyAPIGateway.Entities.GetTopMostEntitiesInSphere(ref sphere);
            if (entities == null)
                return;
            IMyCharacter closestTarget = null;
            double closestDistanceSquared = double.MaxValue;
            double radiusSquared = radius * radius;
            foreach (IMyEntity entity in entities)
            {
                if (entity == null ||
                    entity == Character ||
                    entity.MarkedForClose)
                    continue;
                IMyCharacter target = entity as IMyCharacter;
                if (target == null ||
                    target.IsDead ||
                    target.MarkedForClose)
                    continue;
                Vector3D closestPoint = AnimalUtils.GetClosestPointOnAABB(target.PositionComp.WorldAABB,spherePosition);
                double distanceSquared = Vector3D.DistanceSquared(spherePosition,closestPoint);
                if (distanceSquared > radiusSquared)
                    continue;
                if (distanceSquared < closestDistanceSquared)
                {
                    closestDistanceSquared = distanceSquared;
                    closestTarget = target;
                }
            }
            if (closestTarget == null)
                return;
            closestTarget.DoDamage(Attack.HealhDamage,MyStringHash.GetOrCompute("Enviroment"),true,null,Character.EntityId);
        }
        /// <summary>
        /// Update Animal Sensor
        /// </summary>
        private void UpdateAnimalSensor()
        {
            if (!IsValid() || Character.IsDead || Sensor == null)
                return;
            if (AquaExpansionSession.Insance.isModdingEnabled && AquaExpansionSession.Insance.isAnimalModdingEnabled && AquaExpansionSession.Insance.AnimalSensorRenderEnabled)
            {
                Sensor.Render = true;
            }
            else
            {
                Sensor.Render = false;
            }
                Sensor.Update(Character);
        }
        /// <summary>
        /// BehaviorTree Run init on update
        /// </summary>
        private void BehaviorTreeRun()
        {
            if (BTready || BTInitializationScheduled)
                return;
            BTInitializationScheduled = true;
            biobuffer.Schedule(TryInitializeBT, 2,false,0);
        }
        /// <summary>
        /// Try init BehaviorTree in later update
        /// </summary>
        /// <returns></returns>
        private void TryInitializeBT()
        {
            BTInitializationScheduled = false;
            if (Entity == null)
            {
                BTready = false;
                AquaExpansionSession.Insance.Log(true,$"Failed to init for {Entity?.DisplayName}");
                return;
            }
            Character = Entity as IMyCharacter;
            if (!IsValid() || Character.IsDead)
            {
                BTready = false;
                AquaExpansionSession.Insance.Log(true, $"Failed to init for {Character?.Definition.Id.SubtypeId}");
                return;
            }
            BT = BuildTree();
            if (BT == null)
            {
                BTready = false;
                AquaExpansionSession.Insance.Log(true, $"BuildTree() returned null for {Entity?.DisplayName}");
                return;
            }
            BTready = true;
            AquaExpansionSession.Insance.Log(true, $"BuildTree() Initialized {Entity?.DisplayName ?? "Entity"}");
        }
        /// <summary>
        /// Construct Behavior Tree in Child Classes
        /// </summary>
        /// <returns></returns>
        protected abstract BehaviorTree BuildTree();
        /// <summary>
        /// Update behavior Tree Tick
        /// </summary>
        protected virtual void UpdateBehaviorTree()
        {
            // Never assume non-null, guard again
            if (!BTready || BT == null || !IsValid() || Character.IsDead)
                return;
            float deltaTime = (float)MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
            BT.Update(Character, deltaTime);
        }
        /// <summary>
        /// Clear Behavior Tree
        /// </summary>
        /// <param name="node"></param>
        private void ClearTree(BehaviorNode node)
        {
            if (node == null)
                return;
            SelectorNode sel = node as SelectorNode;
            if (sel != null)
            {
                for (int i = 0; i < sel.children.Count; i++)
                {
                    ClearTree(sel.children[i]);
                }
                sel.children.Clear();
                return;
            }
            SequenceNode seq = node as SequenceNode;
            if (seq != null)
            {
                for (int i = 0; i < seq.children.Count; i++)
                {
                    ClearTree(seq.children[i]);
                }
                seq.children.Clear();
                return;
            }
        }
        /// <summary>
        /// Close Tree
        /// </summary>
        private void CloseBehaviorTree()
        {
            if (BT != null)
            {
                ClearTree(BT.root);
                BT = null;
            }
            BB = null;
        }
        /// <summary>
        /// Clear
        /// </summary>
        private void Clear()
        {
            CloseBehaviorTree();
            biobuffer.Clear();
            scheduler.Clear();
            Attack = null;
            Sensor = null;
            SeaNavigator = null;
            Movement = null;
            biobuffer = null;
            scheduler = null;
            Character = null;
        }
        /// <summary>
        /// Close
        /// </summary>
        public override void Close()
        {
            Clear();
            base.Close();
        }
    }
}
