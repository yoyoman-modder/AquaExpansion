using AquaExpansion.Core;
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
using VRageMath;

namespace AquaExpansionExperimental.Core.Animals
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
        protected string AnimalEnergyFood = "food";
        private MyFixedPoint FoodAmount = new MyFixedPoint();
        private int FoodCount;
        protected SeadCreatureMovementData Movement;
        protected float speed = 0.0f;
        protected float desireddepth;
        protected SeaCreatureNavigator SeaNavigator;
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            Character = Entity as IMyCharacter;
            if (Character == null)
                return;
            showmarker = true;
            GetAnimalInventory();
            FillFoodSubtypes();
            scheduler = new LatentScheduler();
            Movement = new SeadCreatureMovementData();
            Movement.Definition = GetDefinition();
            SeaNavigator = new SeaCreatureNavigator();
            //AquaExpansionSession.Insance.Log(true, $"Init start in {GetType().Name}");
            /*foreach (var comp in Character.Components)
            {
                AquaExpansionSession.Insance.Log(true, comp.GetType().Name);
            }*/
            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME;
        }
        public override void UpdateAfterSimulation()
        {
            if (!IsValid())
                return;
            UpdateMarker();
            UpdateCreature();
            scheduler.Update();
            base.UpdateAfterSimulation();
        }
        public override void UpdateAfterSimulation10()
        {
            if (!IsValid())
                return;
            LifeSupport(true);
            CountInventoryFood(out FoodCount);
            base.UpdateAfterSimulation10();
        }
        /// <summary>
        /// Update
        /// </summary>
        protected virtual void UpdateCreature()
        {
           
        }
        protected abstract SeaCreatureDefinition GetDefinition();
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
        /// Crate Dead marker
        /// </summary>
        private void CreateDeadmarker()
        {
            if (Animalinfomarker == null)
            {
                Vector3D deadpos = Character.GetPosition();
                Animalinfomarker = CreateInfoMarker();
                Animalinfomarker.Name = "DEAD";
                Animalinfomarker.GPSColor = Color.White;
                Animalinfomarker.Coords = deadpos;
                Animalinfomarker.ShowOnHud = true;
            }
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
        /// Update Info marker
        /// </summary>
        public void UpdateInfoMarker()
        {
            if (!IsValid())
                return;
            Vector3D pos = Character.GetPosition();
            Vector3D up = Character.WorldMatrix.Up;
            float height = 1f;
            if (showmarker)
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
                speed = Character.Physics.LinearVelocity.Length();
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
        private bool IsValid()
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
            AnimalFoodSubtypes.Add("AquaAnimalMeatRaw");
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
            if (string.IsNullOrEmpty(AnimalEnergyFood) || !AnimalFoodSubtypes.Contains(AnimalEnergyFood))
                return;
            FoodAmount = 1;
            var itemdef = GetSubtypebyObjectBuilder(AnimalEnergyFood);
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
            var itemdef = GetSubtypebyObjectBuilder(AnimalEnergyFood);
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
        /// Clear
        /// </summary>
        protected void DebugAnimaState()
        {
            if (Character != null)
            {
                string animation = Character.CurrentMovementState.ToString();
                AquaExpansionSession.Insance.Log(true, ($"Movement state: {animation}"));
            }
        }
        private void Clear()
        {
            SeaNavigator = null;
            Movement = null;
            scheduler = null;
            Character = null;
        }
        public override void Close()
        {
            Clear();
            base.Close();
        }
    }
}
