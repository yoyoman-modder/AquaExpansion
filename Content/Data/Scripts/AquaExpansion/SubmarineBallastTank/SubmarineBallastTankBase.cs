using AquaExpansion.Core;
using Jakaria.API;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace AquaExpansion.SubmarineBallastTank
{
    public abstract class SubmarineBallastTankBase : MyGameLogicComponent
    {
        private IMyFunctionalBlock block;
        private IMyCubeGrid grid;
        private MyResourceSinkComponent sink;
        public enum BallastMode { Water, CompressedAir }
        public BallastMode Mode = BallastMode.Water;
        public float FillLevel = 0f;
        public float TargetFill = 0f;
        private float FillWater = 0.1f;
        private float DrainWater = 0.1f;
        private float FillAir = 0.2f;
        private float DrainAir = 0.25f;
        //mode
        public enum ControllMode { Manual, AutoDepth };
        public ControllMode Controll = ControllMode.Manual;
        public double TargetDepth = 20; // meters
        protected float PowerAir = 0.4f; // MW
        protected float PowerFill = 0.05f; //Mw
        protected float PowerDrain = 0.15f; // Mw
        protected float PowerRefill = 0.2f; // Mw
        private float AirPressure = 1f; // current pressure
        private float MaxPressure = 1f; //Max pressure
        protected float AirConsumeRate = 0.6f; // per second when blowing
        protected float AirRefillRate = 0.1f;  // per second
        private float dt;
        private string Ttitle = "Ballast Tank";
        private string TError = "Ballast Tank ERROR";
        private string Tmaxpower = "Max Power Input:";
        private string Tcupower = "Current Power Input:";
        private string TFilllevel = "Tank Fill State:";
        private long lastGridId = 0;
        private long registeredGridId = 0;
        private object TMode = "Mode:";
        private object TAirpressure = "Air Pressure:";
        private object TTargetFill = "Target Fill:";
        private string Tdepth = "Water Depth:";
        private string Tsalt = "Salt level:";
        private string TPressure = "Pressure:";
        protected float BallasteEffect = 0.3f; // how strong tank influences buoyancy

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            block = (IMyFunctionalBlock)Entity;
            grid = block.CubeGrid;
            if (block != null)
            {
                SetSink();
                block.AppendingCustomInfo += AppendCustomInfo;
                AquaExpansionSession.Insance.Log(true, $"Ballast init");
            }
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME | MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME;
        }

        private void AppendCustomInfo(IMyTerminalBlock block, StringBuilder info)
        {
            if (block != null && !block.Closed)
            {
                if (block.IsFunctional && WaterModAPI.IsUnderwater(block.GetPosition()) && (grid != null || !grid.Closed) && !grid.IsStatic)
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
            StringBuilder max = new StringBuilder();
            StringBuilder cu = new StringBuilder();
            string rmode;
            float MaxInput = sink.MaxRequiredInputByType(MyResourceDistributorComponent.ElectricityId);
            float CuInput = sink.RequiredInputByType(MyResourceDistributorComponent.ElectricityId);
            MyValueFormatter.AppendWorkInBestUnit(MaxInput, max);
            MyValueFormatter.AppendWorkInBestUnit(CuInput, cu);
            info.AppendLine($"{Tmaxpower} {max}");
            info.AppendLine($"{Tcupower} {cu}");
            if (Mode == BallastMode.Water)
            { 
                rmode = "Water"; 
            }
            else { 
                rmode = "Compressed Air"; 
            }
            info.AppendLine($"{TMode} {rmode}");
            info.AppendLine($"{TTargetFill} {Math.Round(TargetFill*100f)}%");
            info.AppendLine($"{TFilllevel} {Math.Round(FillLevel*100f)}%");
            info.AppendLine($"{TAirpressure} {Math.Round(AirPressure*100f)}%");

        }

        public override void UpdateOnceBeforeFrame()
        {
            Register();
            lastGridId = block?.CubeGrid?.EntityId ?? 0;
            SubmarineBallastTankUI.instance.RunControlls();
            base.UpdateOnceBeforeFrame();
        }

        public override void UpdateBeforeSimulation()
        {
           
            base.UpdateBeforeSimulation();
        }

        public override void UpdateAfterSimulation()
        {
            ApplyBallastForce();
            base.UpdateAfterSimulation();
        }

        public override void UpdateAfterSimulation10()
        {
            if (block == null || block.Closed)
                return;
            if (grid == null || grid.Closed || grid.Physics == null)
                return;
            if (grid.IsStatic)
                return;
            if (!WaterModAPI.IsUnderwater(block.GetPosition()))
                return;
            dt = MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
            UpdateAirSystem(dt);
            UpdateController();
            UpdateTank();
            
            sink.Update();
            AquaExpansionSession.Insance.UpdateTerminal(block);
            //AquaExpansionSession.Insance.Log(true, $"fill level {FillLevel}\n fill water {FillWater}\nFill Air {FillAir}, Air Pressure {AirPressure}");
            base.UpdateAfterSimulation10();
        }

        public override void UpdateAfterSimulation100()
        {
            UpdateTanksRegistration();
            base.UpdateAfterSimulation100();
        }

        private void UpdateTank()
        {
            float dt = MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
            // Clamp target
            TargetFill = MathHelper.Clamp(TargetFill, 0f, 1f);
            float diff = TargetFill - FillLevel;
            // Snap if very close
            if (Math.Abs(diff) < 0.001f)
            {
                FillLevel = TargetFill;
                return;
            }
            float speed;
            if (Mode == BallastMode.Water)
            {
                // Normal water behavior
                speed = (diff > 0f) ? FillWater : DrainWater;
            }
            else // Compressed Air
            {
                if (diff > 0f)
                {
                    speed = FillWater * 0.2f; // very slow or even 0
                }
                else
                {
                    // Fast drain using pressure
                    if (AirPressure > 0f)
                    {
                        float consume = AirConsumeRate * dt;
                        AirPressure -= consume;
                        speed = DrainAir * (1f + AirPressure * 2f);
                    }
                    else
                    {
                        speed = DrainAir * 0.05f; // fallback
                    }
                }
            }
            float step = speed * dt;
            if (Math.Abs(diff) <= step)
            { FillLevel = TargetFill; }
            else
            { FillLevel += Math.Sign(diff) * step; }
            FillLevel = MathHelper.Clamp(FillLevel, 0f, 1f);
            //AquaExpansionSession.Insance.Log(true, $"Fill {FillLevel}");
        }

        private void Register() // Register after build
        {
            if (block == null || block.Closed || grid == null || grid.Closed)
                return;
            registeredGridId = block.CubeGrid.EntityId;
            List<SubmarineBallastTankBase> list;
            if (!AquaExpansionSession.Insance.GridTanks.TryGetValue(registeredGridId, out list))
            {
                list = new List<SubmarineBallastTankBase>();
                AquaExpansionSession.Insance.GridTanks[registeredGridId] = list;
            }
            if (!list.Contains(this))
            { 
                list.Add(this); 
            }
            //AquaExpansionSession.Insance.Log(true, $"Tank registered");
        }

        private void Unregister() // unregiste after deletion
        {
            if (registeredGridId == 0)
                return;
            List<SubmarineBallastTankBase> list;
            if (!AquaExpansionSession.Insance.GridTanks.TryGetValue(registeredGridId, out list))
            {
                list.Remove(this);
                if (list.Count == 0)
                    AquaExpansionSession.Insance.GridTanks.Remove(registeredGridId);
            }
            registeredGridId = 0;
            //AquaExpansionSession.Insance.Log(true, $"Tank unregistered");
        }

        public float GetMassFactor()
        {
            return FillLevel; 
        }

        public void EmergencyBlow()
        {
            Mode = BallastMode.CompressedAir;
            TargetFill = 0f;
        }

        private void SetSink()
        {
            // Resource sink for compressed air mode
            sink = new MyResourceSinkComponent();
            Entity.Components.Add(sink);
            MyResourceSinkInfo sinkInfo = new MyResourceSinkInfo
            {
                ResourceTypeId = MyResourceDistributorComponent.ElectricityId,
                MaxRequiredInput = 0.4f,
                RequiredInputFunc = () => ComputeRequiredPower()
            };
            sink.Init(MyStringHash.GetOrCompute("Utility"), sinkInfo);
            //AquaExpansionSession.Insance.Log(true, $"Sink OK");
        }

        private void UpdatePower()
        {
            
        }

        private void UpdateController()
        {
            float diff = TargetFill - FillLevel;
            // Auto mode decides direction
            if (Controll == ControllMode.AutoDepth)
            {
                double depth = -block.CubeGrid.GetPosition().Y;
                double error = TargetDepth - depth;

                // Convert depth error to fill target
                TargetFill = MathHelper.Clamp((float)(0.5 + error * 0.05), 0f, 1f);
            }

            //AUTO MODE SWITCH
            if (diff > 0f)
                Mode = BallastMode.Water;          // need more water = flood
            else if (diff < 0f)
                Mode = BallastMode.CompressedAir;  // need less water = blow
        }

        private void ApplyBallastForce()
        {
            var physics = block.CubeGrid.Physics;
            if (physics == null)
                return;
            // 0 = empty (max buoyancy), 1 = full (min buoyancy)
            float buoyancyFactor = 1f - FillLevel;
            // Get grid mass
            float mass = (float)physics.Mass;
            // Neutral buoyancy force
            float neutralForce = mass * 9.81f;
            // Scale effect 
            float ballastEffect = BallasteEffect; // how strong tank influences buoyancy
            float forceValue = neutralForce * (buoyancyFactor - 0.5f) * ballastEffect;
            Vector3D force = Vector3D.Up * forceValue;

            physics.AddForce(
                MyPhysicsForceType.APPLY_WORLD_FORCE,
                force,
                block.GetPosition(),
                null
            );
            //AquaExpansionSession.Insance.Log(true,$"Force {force},  grid mass {mass} forcevalue {forceValue}, neutrall force = {neutralForce}, bfactor  {buoyancyFactor}");
        }

        private float ComputeRequiredPower()
        {
            if (block == null || block.Closed)
                return 0f;
            if (!block.Enabled || !block.IsFunctional)
                return 0f;
            float diff = Math.Abs(TargetFill - FillLevel);
            // refill needs power
            if (AirPressure < MaxPressure)
                return PowerRefill;
            if (diff < 0.001f)
                return 0f;
            if (Mode == BallastMode.CompressedAir)
                return PowerAir;
            if (TargetFill > FillLevel)
                return PowerFill;
            //AquaExpansionSession.Insance.Log(true, $"Power {this.GetType().Name} refill {PowerRefill}, air {PowerAir}, fill {PowerFill}, drain {PowerDrain}");
            return PowerDrain;
           
        }

        private void UpdateAirSystem(float dt)
        {
            if (sink == null)
                return;
            bool hasPower = sink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId);
            if (!hasPower)
                return;
            // Recharge always allowed (even in air mode)
            if (AirPressure < MaxPressure)
            {
                float recharge = AirRefillRate * dt;
                // Slower recharge when tank is active (more realistic)
                if (Mode == BallastMode.CompressedAir)
                    recharge *= 0.5f;
                // Smooth curve (faster when empty, slower when full)
                recharge *= (1f - AirPressure / MaxPressure);
                AirPressure += recharge;

                //AquaExpansionSession.Insance.Log(true,$"Air {this.GetType().Name} {AirPressure:0.00}");
            }
            AirPressure = MathHelper.Clamp(AirPressure, 0f, MaxPressure);
        }

        private void UpdateTanksRegistration()
        {
            if (block == null || block.CubeGrid == null)
                return;

            long current = block.CubeGrid?.EntityId ?? 0;
            if (current != lastGridId && current != 0)
            {
                Unregister();
                Register();
                lastGridId = current;
                //AquaExpansionSession.Insance.Log(true, $"Tank reRegister on Grid Update");
            }
        }

        public static List<SubmarineBallastTankBase> GetTanksForGrid(IMyCubeGrid grid)
        {
            if (grid == null || grid.Closed)
                return new List<SubmarineBallastTankBase>();

            List<SubmarineBallastTankBase> list;
            if (!AquaExpansionSession.Insance.GridTanks.TryGetValue(grid.EntityId, out list) || list == null)
                return new List<SubmarineBallastTankBase>();

            // Remove dead tanks safely
            list.RemoveAll(t => t == null || t.Entity == null || t.Entity.Closed);
            return list;
        }

        public override void Close()
        {
            Unregister();
            block.AppendingCustomInfo -= AppendCustomInfo;
            sink = null;
            grid = null;
            block = null;
            base.Close();
        }
    }
}
