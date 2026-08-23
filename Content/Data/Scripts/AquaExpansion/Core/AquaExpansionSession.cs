using AquaExpansion.Core.Combat;
using AquaExpansion.Core.Combat.Balistics;
using AquaExpansionExperimental.Core.Animals;
using Draygo.API;
using Jakaria.API;
using ProtoBuf;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Weapons;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Input;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace AquaExpansion.Core
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation | MyUpdateOrder.BeforeSimulation)]
    public class AquaExpansionSession : MySessionComponentBase
    {
        private string System = "AquaSystem";
        public string AquaAPI = "AquaSystemAPI";
        public static AquaExpansionSession Insance;
        private float maxSaltDepth = 100f;
        public AquaJetpackUnderWaterSystem JetpackUnderWaterSystem;
        private int tick;
        private int weapontick;
        //shared effects
        private HashSet<IMyTerminalBlock> TerminalTracedBlocks = new HashSet<IMyTerminalBlock>(); // Terminal
        private HashSet<IMyThrust> TrackedThrusters = new HashSet<IMyThrust>(); // Thrusters
        private HashSet<IMyWindTurbine> BannedTurbines = new HashSet<IMyWindTurbine>(); // Wind turbines
        private HashSet<IMySolarPanel> BannedSolars = new HashSet<IMySolarPanel>(); // Solar Panels
        private HashSet<IMyThrust> BannedThrusters = new HashSet<IMyThrust>(); // not mod thrusters
        private HashSet<IMyFunctionalBlock> TrackedAlgaeFarms = new HashSet<IMyFunctionalBlock>();
        private HashSet<IMyCubeGrid> TrackedGrids = new HashSet<IMyCubeGrid>(); // grids
        private Dictionary<long, MyParticleEffect> effects = new Dictionary<long, MyParticleEffect>(); // Damage Effect
        private Dictionary<long, MyParticleEffect> Engineeffects = new Dictionary<long, MyParticleEffect>(); // Engine Effect
        private float effectLod1disSq = 30f;
        private float effectLod2disSq = 40f;
        private float scale;
        private readonly List<IMyPlayer> playersL = new List<IMyPlayer>();
        //HUD 
        private HudAPIv2 TextAPI;
        private bool hudConnected;
        private HudAPIv2.HUDMessage gearHUD;
        private HudAPIv2.HUDMessage OxygenHUD;
        private HudAPIv2.HUDMessage PressureHUD;
        private HudAPIv2.HUDMessage SaltHUD;
        private HudAPIv2.HUDMessage DirectionLHUD;
        private HudAPIv2.HUDMessage DirectionRHUD;
        private HudAPIv2.HUDMessage compassBar;
        private HudAPIv2.HUDMessage geardepthmeter;
        public bool Heartbeat => TextAPI.Heartbeat && hudConnected;
        public readonly float MIN_ENVOXYGENLEVEL = 0.20f;
        private int ticksPerUpdate = 30; // ~1 second if 60 ticks/sec
        public float ApexFarmMaxworkDepth = 8f;
        private const string STORAGE_FILE = "WaterClientSettings.xml";
        private LatentScheduler latentScheduler;
        private MyEnvironmentDefinition enviromentdef;
        private Color GlobaldepthbasedHUDColor;
        private Color OriginalInteractionColor;
        public string WatermodLink;
        private Dictionary<long, MyParticleEffect> Weldereffects = new Dictionary<long, MyParticleEffect>(); // Welder Effect
        private Dictionary<long, MyParticleEffect> WeldereffectsShip = new Dictionary<long, MyParticleEffect>(); // Welder Ship Effect
        private HashSet<IMyShipWelder> TrackedShipWelders = new HashSet<IMyShipWelder>(); // ShipWelders
        private HashSet<IMyWelder> Trackedhandwelders = new HashSet<IMyWelder>();
        //balistics
        private bool usebalisticsFallback = true;
        private UnderwaterBalisticsSystem balistics;
        public event Action<IMyAutomaticRifleGun> RegisterHandWeapon;
        public event Action<IMyAutomaticRifleGun> UnregisterHandWeapon;
        private HashSet<IMyFunctionalBlock> TrackedFarms = new HashSet<IMyFunctionalBlock>();
        //modding
        private bool ModdingAllowed = false;
        private bool HydroAmmoModdingAllowed = false;
        private enum ChatCommandPermission { Public,ServerOnly,AdminOnly }
        public override void LoadData()
        {
            Insance = this;
            InitModdingTools();
            JetpackUnderWaterSystem = new AquaJetpackUnderWaterSystem();
            latentScheduler = new LatentScheduler();
            TextAPI = new HudAPIv2(onRegisteredCallback);
            balistics = new UnderwaterBalisticsSystem();
            MyAPIGateway.Entities.OnEntityAdd += OnEntityAdd;
            MyAPIGateway.Entities.OnEntityRemove += OnEntityRemove;
            MyAPIGateway.Utilities.MessageEntered += OnMessageEntered;
            GetMods();
            InitDatabases();
            CheckBalisticFallback();
            base.LoadData();
        }
        private  void LogFilteredBlocks()
        {
            AquaExpansionSession.Insance.Log(true,
            string.Format(
             "Filtered grids & Blocks\n" +
             "Grids      : {0}\n" +
             "Blocks     : {1} \n" +
             "Algae      : {2}\n" +
             "Farms      : {3}",
             TrackedGrids.Count,
             TerminalTracedBlocks.Count,
             TrackedAlgaeFarms.Count,
             TrackedFarms.Count));
        }
        private void GetMods()
        {
            var mods = MyAPIGateway.Session.Mods;
            if (mods == null || mods.Count == 0)
                return;
            foreach (var mod in mods)
            {
                if (mod.PublishedFileId == 2200451495)
                {
                    WatermodLink = mod.GetModContext().ModPath;
                }
                if (mod.PublishedFileId == 3154371364)
                {
                    GetWCConnected();
                }
            }
        }
        private void GetWCConnected()
        {
            Log(true, $"WeaponCore Found! Balistics System disabled!");
            usebalisticsFallback = false;
        }
        private void CheckBalisticFallback()
        {
            if (usebalisticsFallback)
            {
                balistics.Load();
                //Log(true, $"Balistics System init");
            }
        }
        private void ClearBalisticksFallback()
        {
            if (usebalisticsFallback)
            {
                balistics.Unload();
            }
        }
        private void UpdateBalisticsFallback()
        {
            if (usebalisticsFallback)
            {
                UpdateWeaponTick();
                balistics.Update(TerminalTracedBlocks);
            }
        }
        private void UpdateWeaponTick()
        {
            weapontick++;
        }
        private void UpdateGlobalTick()
        {
            tick++;
        }
        private void UpdateTrackedGrids()
        {
            if (tick % 60 != 0)
                return;
            foreach (var grid in TrackedGrids)
            {
                if (grid == null ||
                    grid.Closed ||
                    grid.MarkedForClose)
                    continue;
                if (CombatUtils.WaterPlanet(grid.GetPosition()) == null)
                {
                    RemoveGridBlocks(grid);
                    continue;
                }
                RescanGrid(grid);
            }
        }
        private void RescanGrid(IMyCubeGrid grid)
        {
            if (grid == null ||
                grid.Closed ||
                grid.MarkedForClose)
                return;
            var blocks = new List<IMySlimBlock>();
            grid.GetBlocks(blocks);
            foreach (var slim in blocks)
            {
                if (slim == null)
                    continue;
                var block = slim.FatBlock as IMyTerminalBlock;
                if (block == null ||
                    block.Closed ||
                    block.MarkedForClose)
                    continue;
                AddBlock(block);
            }
        }
        private void RemoveGridBlocks(IMyCubeGrid grid)
        {
            if (grid == null)
                return;
            List<IMyTerminalBlock> toRemove = null;
            foreach (var block in TerminalTracedBlocks)
            {
                if (block == null)
                    continue;
                if (block.CubeGrid != grid)
                    continue;
                if (toRemove == null)
                    toRemove = new List<IMyTerminalBlock>();
                toRemove.Add(block);
            }
            if (toRemove == null)
                return;
            foreach (var block in toRemove)
            {
                RemoveBlock(block);
            }
        }
        private void RegisterGrid(IMyCubeGrid grid)
        {
            /*if (grid == null || grid.Closed || TrackedGrids.Contains(grid))
                return;
            TrackedGrids.Add(grid);
            grid.OnBlockAdded += OnBlockAdded;
            grid.OnBlockRemoved += OnBlockRemoved;
            // collect existing blocks
            var blocks = new List<IMySlimBlock>();
            grid.GetBlocks(blocks);
            foreach (var slim in blocks)
                AddBlock(slim.FatBlock as IMyTerminalBlock);*/
            if (grid == null ||
                grid.Closed ||
                grid.MarkedForClose ||
                TrackedGrids.Contains(grid))
                return;
            TrackedGrids.Add(grid);
            grid.OnBlockAdded += OnBlockAdded;
            grid.OnBlockRemoved += OnBlockRemoved;
            RescanGrid(grid);
        }
        private void OnBlockRemoved(IMySlimBlock block)
        {
            RemoveBlock(block.FatBlock as IMyTerminalBlock);
        }
        private void RemoveBlock(IMyTerminalBlock block)
        {
            /*if (block == null || block.Closed)
                return;
            TerminalTracedBlocks.Remove(block);
            GlobalEffects.StopEffect(block.EntityId, effects);*/
            if (block == null)
                return;
            TerminalTracedBlocks.Remove(block);
            GlobalEffects.StopEffect(block.EntityId,effects);
        }
        private void OnBlockAdded(IMySlimBlock block)
        {
            AddBlock(block.FatBlock as IMyTerminalBlock);
        }
        private void AddBlock(IMyTerminalBlock block)
        {
            if (block == null || block.Closed || TerminalTracedBlocks.Contains(block))
                return;
            TerminalTracedBlocks.Add(block);
            GlobalEffects.CreateEffect(block, GetEffectByBlockType(block),scale, effects);
        }
        private void OnEntityRemove(IMyEntity entity)
        {
            var grid = entity as IMyCubeGrid;
            /*if (grid != null && TrackedGrids.Contains(grid))
            {
                grid.OnBlockAdded -= OnBlockAdded;
                grid.OnBlockRemoved -= OnBlockRemoved;
                TrackedGrids.Remove(grid);
            }*/
            UnRegisterGrid(grid);
        }
        private void UnRegisterGrid(IMyCubeGrid grid)
        {
            if (grid != null && TrackedGrids.Contains(grid))
            {
                grid.OnBlockAdded -= OnBlockAdded;
                grid.OnBlockRemoved -= OnBlockRemoved;
                TrackedGrids.Remove(grid);
            }
        }
        public HashSet<IMyTerminalBlock> GetTerminalBlocks()
        {
            return TerminalTracedBlocks;
        }
        private void UpdateTrackedBlocks()
        {
            if (tick % 10 != 0)
                return;
            foreach (var block in TerminalTracedBlocks)
            {
                if (block == null || block.Closed || block.MarkedForClose)
                    continue;
                ProcessBlock(block);
            }
            //Log(true, $" Terminal blocks {TerminalTracedBlocks.Count}");
        }
        private void UpdateEngineBlocks()
        {
            if (tick % 10 != 0)
                return;
            foreach (var block in TrackedThrusters)
            {
                if (block == null || block.Closed || block.MarkedForClose)
                    continue;
                ProcessEngineBlock(block);
            }
            //Log(true, $"Engine blocks {TrackedThrusters.Count}");
        }
        private void FilterThrusters()
        {
            if (tick % 10 != 0)
                return;
            //Cleanup tracked
            List<IMyThrust> toRemove = null;
            foreach (var thruster in TrackedThrusters)
            {
                if (thruster == null ||
                    thruster.Closed ||
                    thruster.MarkedForClose ||
                    !WaterModAPI.IsUnderwater(thruster.GetPosition()))
                {
                    if (toRemove == null)
                        toRemove = new List<IMyThrust>();

                    toRemove.Add(thruster);
                }
            }
            if (toRemove != null)
            {
                foreach (var t in toRemove)
                {
                    //StopEngineEffect(t);
                    GlobalEffects.StopEngineEffect(t.EntityId,Engineeffects);
                    TrackedThrusters.Remove(t);
                }
            }
            //Cleanup banned (auto re-enable if above water)
            BannedThrusters.RemoveWhere(thrust =>
            {
                if (thrust == null || thrust.Closed || thrust.MarkedForClose)
                    return true;
                bool isUnderwater = WaterModAPI.IsUnderwater(thrust.GetPosition());
                if (!isUnderwater)
                {
                    thrust.Enabled = true;
                    return true;
                }
                return false;
            });
            //Main scan
            foreach (var block in TerminalTracedBlocks)
            {
                if (block == null || block.Closed || block.MarkedForClose)
                    continue;
                var thruster = block as IMyThrust;
                if (thruster == null || thruster.Closed || thruster.MarkedForClose)
                    continue;
                var isUnderwater = WaterModAPI.IsUnderwater(thruster.GetPosition());
                var subtype = thruster.BlockDefinition.SubtypeId;
                if (subtype.Contains("UnderwaterEngineBasic"))
                {
                    if (isUnderwater && !TrackedThrusters.Contains(thruster))
                    {
                        TrackedThrusters.Add(thruster);
                        GlobalEffects.CreateEngineEffect(thruster, GetEngineEffect(thruster), scale, Engineeffects);
                    }
                    continue;
                }
                if (isUnderwater)
                {
                    if (!BannedThrusters.Contains(thruster))
                    {
                        thruster.Enabled = false;
                        BannedThrusters.Add(thruster);
                    }
                    if (thruster.Enabled)
                    {
                        thruster.Enabled = false;
                        thruster.ThrustOverridePercentage = 0f;
                    }
                }
                else
                {
                    if (BannedThrusters.Contains(thruster))
                    {
                        thruster.Enabled = true;
                        BannedThrusters.Remove(thruster);
                    }
                }
            }
        }
        private void UpdateTrackedAlgaeFarms()
        {
            if (tick % 10 != 0)
                return;
            foreach (var block in TrackedAlgaeFarms)
            {
                if (block == null || block.Closed || block.MarkedForClose)
                    continue;
                ControllAlgaeFarms(block);
                //Log(true, $"Run on 10 frame");
            }
            foreach (var fblock in TrackedFarms)
            {
                if (fblock == null || fblock.Closed || fblock.MarkedForClose)
                    continue;
                ControllFarms(fblock);
                //Log(true, $"Run on 10 frame");
            }
        }
        private void ControllAlgaeFarms(IMyFunctionalBlock block)
        {
            if (block == null || block.Closed || block.MarkedForClose)
                return;
            CheckUnderwaterBlockRules(block, false);

        }
        private void ControllFarms(IMyFunctionalBlock block)
        {
            if (block == null || block.Closed || block.MarkedForClose)
                return;
            CheckUnderwaterBlockRules(block, true);
        }
        private void FilterAlgaeFarms()
        {
            if (tick % 10 != 0)
                return;
            // -----------------------------------------
            // Cleanup tracked
            // -----------------------------------------
            List<IMyFunctionalBlock> toRemove = null;
            foreach (var farm in TrackedAlgaeFarms)
            {
                if (farm == null ||
                    farm.Closed ||
                    farm.MarkedForClose ||
                    !WaterModAPI.IsUnderwater(farm.GetPosition()))
                {
                    if (toRemove == null)
                        toRemove = new List<IMyFunctionalBlock>();

                    toRemove.Add(farm);
                }
            }
            if (toRemove != null)
            {
                foreach (var farm in toRemove)
                {
                    TrackedAlgaeFarms.Remove(farm);
                }
            }
            // -----------------------------------------
            // Main scan
            // -----------------------------------------
            foreach (var block in TerminalTracedBlocks)
            {
                if (block == null ||
                    block.Closed ||
                    block.MarkedForClose)
                    continue;
                var farm = block as IMyFunctionalBlock;
                if (farm == null ||
                    farm.Closed ||
                    farm.MarkedForClose)
                    continue;
                if (!WaterModAPI.IsUnderwater(farm.GetPosition()))
                    continue;
                if (TrackedAlgaeFarms.Contains(farm))
                    continue;
                foreach (var comp in farm.Components)
                {
                    if (comp == null)
                        continue;
                    string registeredName = AquaForbiddenComponentsDatabase.GetComponentByID(1);
                    if (string.Equals(registeredName, comp.GetType().Name, StringComparison.OrdinalIgnoreCase))
                    {
                        //Log(true, $"Found {registeredName}");
                        TrackedAlgaeFarms.Add(farm);
                        break;
                    } 
                }
            }
        }
        private void FilterFarms()
        {
            if (tick % 10 != 0)
                return;
            // -----------------------------------------
            // Cleanup tracked farms
            // -----------------------------------------
            List<IMyFunctionalBlock> toRemove = null;
            foreach (var farm in TrackedFarms)
            {
                if (farm == null ||
                    farm.Closed ||
                    farm.MarkedForClose ||
                    !WaterModAPI.IsUnderwater(farm.GetPosition()))
                {
                    if (toRemove == null)
                        toRemove = new List<IMyFunctionalBlock>();
                    toRemove.Add(farm);
                }
            }
            if (toRemove != null)
            {
                foreach (var farm in toRemove)
                {
                    TrackedFarms.Remove(farm);
                }
            }
            // -----------------------------------------
            // Main scan
            // -----------------------------------------
            foreach (var block in TerminalTracedBlocks)
            {
                if (block == null ||
                    block.Closed ||
                    block.MarkedForClose)
                    continue;
                var farm = block as IMyFunctionalBlock;
                if (farm == null ||
                    farm.Closed ||
                    farm.MarkedForClose)
                    continue;
                if (!WaterModAPI.IsUnderwater(farm.GetPosition()))
                    continue;
                if (TrackedFarms.Contains(farm))
                    continue;
                // -------------------------------------
                // Check components
                // -------------------------------------
                foreach (var comp in farm.Components)
                {
                    if (comp == null)
                        continue;
                    string registeredName = AquaForbiddenComponentsDatabase.GetComponentByID(2);
                    if (string.Equals(registeredName,comp.GetType().Name, StringComparison.OrdinalIgnoreCase))
                    {
                        TrackedFarms.Add(farm);
                        break;
                    }
                }
            }
        }
        private void FilterForbiddenBlocks()
        {
            if (tick % 10 != 0)
                return;
            // Cleanup + restore turbines
            BannedTurbines.RemoveWhere(turbine =>
            {
                if (turbine == null || turbine.Closed || turbine.MarkedForClose)
                    return true;
                bool isUnderwater = WaterModAPI.IsUnderwater(turbine.GetPosition());
                if (!isUnderwater)
                {
                    turbine.Enabled = true;
                    return true;
                }
                return false;
            });

            // Cleanup + restore solars
            BannedSolars.RemoveWhere(solar =>
            {
                if (solar == null || solar.Closed || solar.MarkedForClose)
                    return true;
                bool isUnderwater = WaterModAPI.IsUnderwater(solar.GetPosition());
                if (!isUnderwater)
                {
                    solar.Enabled = true;
                    return true;
                }
                return false;
            });

            // Main scan
            foreach (var block in TerminalTracedBlocks)
            {
                if (block == null || block.Closed || block.MarkedForClose)
                    continue;
                var isUnderwater = WaterModAPI.IsUnderwater(block.GetPosition());
                // WIND TURBINES
                var turbine = block as IMyWindTurbine;
                if (turbine != null)
                {
                    if (isUnderwater)
                    {
                        if (!BannedTurbines.Contains(turbine))
                            BannedTurbines.Add(turbine);
                        if (turbine.Enabled)
                            turbine.Enabled = false;
                    }
                    else if (BannedTurbines.Contains(turbine))
                    {
                        turbine.Enabled = true;
                        BannedTurbines.Remove(turbine);
                    }
                    continue;
                }
                // SOLAR PANELS
                var solar = block as IMySolarPanel;
                if (solar != null)
                {
                    if (isUnderwater)
                    {
                        if (!BannedSolars.Contains(solar))
                            BannedSolars.Add(solar);
                        if (solar.Enabled)
                            solar.Enabled = false;
                    }
                    else if (BannedSolars.Contains(solar))
                    {
                        solar.Enabled = true;
                        BannedSolars.Remove(solar);
                    }
                    continue;
                }
            }
        }
        private void ProcessBlock(IMyTerminalBlock block)
        {
            if (block == null || block.Closed || block.MarkedForClose)
                return;
            var player = MyAPIGateway.Session?.Player;
            if (player == null)
                return;
            double dist = Vector3D.DistanceSquared(player.GetPosition(), block.GetPosition());
            //LOD 1
            if (dist > effectLod1disSq * effectLod1disSq)
            {
                GlobalEffects.StopEffect(block.EntityId, effects);
                return;
            }
            bool isUnderwater = WaterModAPI.IsUnderwater(block.GetPosition());
            bool isBroken = !block.IsFunctional;
            MyParticleEffect effect;
            effects.TryGetValue(block.EntityId, out effect);
            if (!isUnderwater || !isBroken)
            {
                GlobalEffects.StopEffect(block.EntityId, effects);
                return;
            }
            // LOD 2
            scale = dist > effectLod2disSq * effectLod2disSq ? 0.5f : 1f;
            if (effect == null)
            {
                //CreateEffect(block, scale);
                GlobalEffects.CreateEffect(block, GetEffectByBlockType(block), scale, effects);
            }
            else
            {
                effect.UserScale = scale;
            }
        }
        private void ProcessEngineBlock(IMyThrust block)
        {
            if (block == null || block.Closed || block.MarkedForClose)
                return;
            var player = MyAPIGateway.Session?.Player;
            if (player == null)
                return;
            double dist = Vector3D.DistanceSquared(player.GetPosition(), block.GetPosition());
            //LOD 1
            if (dist > effectLod1disSq * effectLod1disSq)
            {
                //StopEngineEffect(block);
                GlobalEffects.StopEngineEffect(block.EntityId, Engineeffects);
                return;
            }
            bool isUnderwater = WaterModAPI.IsUnderwater(block.GetPosition());
            bool isBroken = !block.IsFunctional;
            MyParticleEffect effect;
            Engineeffects.TryGetValue(block.EntityId, out effect);
            if (!isUnderwater || isBroken || !block.Enabled || !block.IsWorking)
            {
                //StopEngineEffect(block);
                GlobalEffects.StopEngineEffect(block.EntityId, Engineeffects);
                return;
            }
            // LOD 2
            scale = dist > effectLod2disSq * effectLod2disSq ? 0.5f : 1f;
            if (effect == null)
            {
                //CreateEngineEffect(block, scale);
                GlobalEffects.CreateEngineEffect(block, GetEngineEffect(block),scale,Engineeffects);
            }
            else
            {
                effect.UserScale = scale;
            }
        }
        private string GetEffectByBlockType(IMyTerminalBlock block)
        {
            if (block is IMyBatteryBlock)
            {
                var subtype = block.BlockDefinition.SubtypeId;
                // Most specific first
                if (subtype.EndsWith("Micro"))
                    return GlobalEffects.GetEffect("ElectricMicro");
                if (subtype.Contains("SmallBattery"))
                    return GlobalEffects.GetEffect("ElectricMicro");
                if (subtype.Contains("Small"))
                    return GlobalEffects.GetEffect("ElectricSmall");

                return GlobalEffects.GetEffect("Electric");
            }
            if (block is IMyCockpit)
                return GlobalEffects.GetEffect("Bubblespray");

            return GlobalEffects.GetEffect("Default");
        }
        private string GetEngineEffect(IMyThrust block)
        {
            if (block == null || block.Closed || block.MarkedForClose || !block.IsFunctional)
                return GlobalEffects.GetEngineEffect("Default");
            var subtype = block.BlockDefinition.SubtypeId;
            if (!subtype.StartsWith("UnderwaterEngineBasic"))
            {
                //Log(true, $"Unsupported engine subtype: {subtype}");
                return GlobalEffects.GetEngineEffect("Default");
            }
            bool isRunning = block.CurrentThrust > 0.01f;
            if (subtype.EndsWith("SL"))
                return isRunning ? GlobalEffects.GetEngineEffect("RunMedium") : GlobalEffects.GetEngineEffect("IddleMedium");
            if (subtype.EndsWith("LS"))
                return isRunning ? GlobalEffects.GetEngineEffect("RunLarge") : GlobalEffects.GetEngineEffect("IddleLarge");
            if (subtype.EndsWith("S"))
                return isRunning ? GlobalEffects.GetEngineEffect("RunSmall") : GlobalEffects.GetEngineEffect("IddleSmall");
            if (subtype.EndsWith("L"))
                return isRunning ? GlobalEffects.GetEngineEffect("RunXLarge") : GlobalEffects.GetEngineEffect("IddleXLarge");
            if (subtype.EndsWith("FlatSmall"))
                return isRunning ? GlobalEffects.GetEngineEffect("RunFlatSmall") : GlobalEffects.GetEngineEffect("IddleFlatSmall");
            if (subtype.EndsWith("FlatMedium"))
                return isRunning ? GlobalEffects.GetEngineEffect("RunFlatMedium") : GlobalEffects.GetEngineEffect("IddleFlatMedium");
            if (subtype.EndsWith("FlatLarge"))
                return isRunning ? GlobalEffects.GetEngineEffect("RunFlatLarge") : GlobalEffects.GetEngineEffect("IddleFlatLarge");
            if (subtype.EndsWith("FlatXLarge"))
                return isRunning ? GlobalEffects.GetEngineEffect("RunFlatXLarge") : GlobalEffects.GetEngineEffect("IddleFlatXLarge");
            return GlobalEffects.GetEngineEffect("Default");
        }
        private void OnEntityAdd(IMyEntity entity)
        {
            var grid = entity as IMyCubeGrid;
            if (grid != null)
                RegisterGrid(grid);
        }
        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            base.Init(sessionComponent);
            LineAnimationManager.Init(ticksPerUpdate);
            Log(true, $"Welcome back!");
            EnviromentHighlightControll();
            MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, BeforeDamage);
        }
        /// <summary>
        /// Cancel Asphyxia damage underwater for seaanimals
        /// </summary>
        /// <param name="target"></param>
        /// <param name="info"></param>
        private void BeforeDamage(object target, ref MyDamageInformation info)
        {
            if (info.Type != MyDamageType.Asphyxia)
                return;
            var animal = target as IMyCharacter;
            if (animal == null || animal.Closed || animal.MarkedForClose || animal.IsDead)
                return;
            if (animal.GameLogic.GetAs<SeaCreatureBase>() != null)
            {
                if (WaterModAPI.IsUnderwater(animal.GetPosition()))
                {
                    info.Amount = 0f;
                }
                //AquaExpansionSession.Insance.Log(true, $"Damage  {info.Type}");
            }
        }
        private void onRegisteredCallback()
        {
            hudConnected = true;
        }
        //simulation
        public override void UpdateBeforeSimulation()
        {
            UpdateGlobalTick();
            UpdateTrackedGrids();
            UpdateTrackedBlocks();
            FilterThrusters();
            FilterForbiddenBlocks();
            FilterAlgaeFarms();
            FilterFarms();
            FilterShipWelders();
            UpdateEngineBlocks();
            UpdateTrackedAlgaeFarms();
            UpdateShipWelderBlocks();
            UpdateHandTools();
            latentScheduler.Update();
            SetDephbasedColor();
            UpdateBalisticsFallback();
            base.UpdateBeforeSimulation();
            //LogFilteredBlocks();
        }
        public override void UpdateAfterSimulation()
        {
            LineAnimationManager.Update();
            UpdateAll();
            base.UpdateAfterSimulation();
            PressToSpawnAnimal();
        }
        //
        private void EnviromentHighlightControll()
        {
            enviromentdef = MyDefinitionManager.Static.EnvironmentDefinition;
            if (enviromentdef != null)
            {
                OriginalInteractionColor = enviromentdef.ContourHighlightColor;
                //Log(true, $" envdata {OriginalInteractionColor.ToVector4()}");
                
            }
        }
        private void SetDephbasedColor()
        {
            if (enviromentdef != null)
            {
                UpdateGlobalHUDColor();
                Vector4 linearColor = RGBtoXYZW(GlobaldepthbasedHUDColor);
                //Log(true, $"current color is {GlobaldepthbasedHUDColor} to linear {linearColor}");
                Vector4 hcolor = new Vector4(linearColor.X, linearColor.Y, linearColor.Z, linearColor.W);
                enviromentdef.ContourHighlightColor = hcolor;
            }
        }
        private Vector4 RGBtoXYZW(Color color)
        {
            float r = color.R * (1f / 255f);
            float g = color.G * (1f / 255f);
            float b = color.B * (1f / 255f);
            return new Vector4(
                r ,
                g,
                b,
                0.1f
            );
        }
        //Utils
        public List<IMyPlayer> GetPlayersWithCharacters()
        {
            playersL.Clear();
            MyAPIGateway.Players.GetPlayers(playersL);
            playersL.RemoveAll(p => p?.Character == null);
            return playersL;
        }
        //get waterData
        public MyTuple<float, float, float, int> GetPlanetWaveData(IMyFunctionalBlock block)
        {
            var waterdata = new MyTuple<float, float, float, int>(0f, 0f, 0f, 0);
            if (block != null && block.Enabled && block.IsFunctional && WaterModAPI.IsUnderwater(block.GetPosition()))
            {
                MyPlanet planet = MyGamePruningStructure.GetClosestPlanet(block.GetPosition());
                waterdata = WaterModAPI.GetWaveData(planet);
            }
            return waterdata;
        }
        //Get waterdata in grid
        public MyTuple<float, float, float, int> GetPlanetWaveDatabyGrid(IMyCubeGrid grid)
        {
            var waterdata = new MyTuple<float, float, float, int>(0f, 0f, 0f, 0);
            if (grid != null)
            {
                double bottomY = grid.WorldAABB.Min.Y;
                Vector3D bottomPos = new Vector3D(grid.GetPosition().X, bottomY, grid.GetPosition().Z);
                MyPlanet planet = MyGamePruningStructure.GetClosestPlanet(bottomPos);
                waterdata = WaterModAPI.GetWaveData(planet);
            }
            return waterdata;
        }
        //Get Depth
        public float GetWaterDepth(IMyFunctionalBlock block)
        {
            var depth = 0f;
            if (block != null && block.Enabled && block.IsFunctional && WaterModAPI.IsUnderwater(block.GetPosition()))
            {
                depth = (float)WaterModAPI.GetDepth(block.GetPosition());
            }
            return depth;
        }
        //get Depth in grid
        public float GetWaterDepthbyGrid(IMyCubeGrid grid)
        {
            if (grid == null || grid.Closed)
                return 0f;
            double bottomY = grid.WorldAABB.Min.Y;
            Vector3D bottomPos = new Vector3D(grid.GetPosition().X, bottomY, grid.GetPosition().Z);
            if (!WaterModAPI.IsUnderwater(bottomPos))
                return 0f;
            double depth = 0;
            var e = grid as IMyEntity;
            if (e != null)
            {
                depth = WaterModAPI.Entity_FluidDepth((VRage.Game.Entity.MyEntity)e);
            }
            return (float)depth;
        }
        public float GetWaterDepthbyCharacter(IMyCharacter character)
        {
            if (character == null || character.IsDead || character.Closed)
                return 0f;
            if (!WaterModAPI.IsUnderwater(character.GetPosition()))
                return 0f;
            double depth = 0;
            var e = character as IMyEntity;
            if (e != null)
            {
                depth = WaterModAPI.Entity_FluidDepth((VRage.Game.Entity.MyEntity)e);
            }
            return (float)depth;
        }
        public float GetWaterDepthbyEntity(MyEntity entity)
        {
            if (entity == null || entity.Closed || entity.MarkedForClose)
                return 0f;
            if (!WaterModAPI.IsUnderwater(entity.WorldMatrix.Translation))
                return 0f;
            double depth = 0;
            depth = WaterModAPI.Entity_FluidDepth(entity);
            return (float)depth;
        }
        public float GetBouyancybyCharacter(IMyCharacter character)
        {
            if (character == null || character.IsDead || character.Closed)
                return 0f;
            if (!WaterModAPI.IsUnderwater(character.GetPosition()))
                return 0f;
            float b = 0f;
            var e = character as IMyEntity;
            if (e != null)
            {
                b = WaterModAPI.Entity_BuoyancyForce((VRage.Game.Entity.MyEntity)e).Length();
            }
            return b;
        }
        //get salt level
        public float GetSaltLevel(IMyFunctionalBlock block, float depth)
        {
            if (block == null || !block.Enabled || !block.IsFunctional || block.Closed)
                return 0f;
            if (!WaterModAPI.IsUnderwater(block.GetPosition()))
                return 0f;
            float depthNorm = MathHelper.Clamp(depth / -maxSaltDepth, 0f, 1f);
            depthNorm = (float)Math.Pow(depthNorm, 0.5f);
            return 1f + depthNorm * 2f;
        }
        //Saltlevel by grid less precise
        public float GetSaltLevelbyGrid(IMyCubeGrid grid, float depth)
        {
            if (grid == null || grid.Closed)
                return 0f;
            double bottomY = grid.WorldAABB.Min.Y;
            Vector3D bottomPos = new Vector3D(grid.GetPosition().X, bottomY, grid.GetPosition().Z);
            if (!WaterModAPI.IsUnderwater(bottomPos))
                return 0f;
            float depthNorm = MathHelper.Clamp(depth / -maxSaltDepth, 0f, 1f);
            depthNorm = (float)Math.Pow(depthNorm, 0.5f);
            return 1f + depthNorm * 2f;
        }
        //Saltlevel byPlayer
        public float GetSaltlevelbyPlayer(IMyCharacter character, float depth)
        {
            if (character == null || character.IsDead || character.Closed)
                return 0f;
            if (!WaterModAPI.IsUnderwater(character.GetPosition()))
                return 0f;
            float depthNorm = MathHelper.Clamp(depth / -maxSaltDepth, 0f, 1f);
            depthNorm = (float)Math.Pow(depthNorm, 0.5f);
            return 1f + depthNorm * 2f;
        }
        //get salt2%
        public float SaltToPercent(float salt)
        {
            salt = MathHelper.Clamp(salt, 1f, 3f);
            return 10f + ((salt - 1f) / 2f) * 90f;
        }
        //get Pressure in grid
        public float GetPressurebyGrid(IMyCubeGrid grid)
        {
            if (grid == null || grid.Closed)
                return 0f;
            double bottomY = grid.WorldAABB.Min.Y;
            Vector3D bottomPos = new Vector3D(grid.GetPosition().X, bottomY, grid.GetPosition().Z);
            if (!WaterModAPI.IsUnderwater(bottomPos))
                return 0f;
            double pressure = 0;
            var e = grid as IMyEntity;
            if (e != null)
            {
                pressure = WaterModAPI.Entity_FluidPressure((VRage.Game.Entity.MyEntity)e);
            }
            return (float)pressure;
        }
        public float GetPressurebyPlayer(IMyCharacter character)
        {
            if (character == null || character.IsDead || character.Closed)
                return 0f;
            double pressure = 0;
            var e = character as IMyEntity;
            if (e != null && WaterModAPI.IsUnderwater(character.GetPosition()))
            {
                pressure = WaterModAPI.Entity_FluidPressure((VRage.Game.Entity.MyEntity)e);
            }
            return (float)pressure;
        }
        //GetFlow in grid
        public Vector3 GetFlowbyGrid(IMyCubeGrid grid)
        {
            Vector3 flow = new Vector3(0, 0, 0);
            if (grid == null || grid.Closed)
                return flow = Vector3.Zero;
            double bottomY = grid.WorldAABB.Min.Y;
            Vector3D bottomPos = new Vector3D(grid.GetPosition().X, bottomY, grid.GetPosition().Z);
            if (!WaterModAPI.IsUnderwater(bottomPos))
                return Vector3.Zero;
            var e = grid as IMyEntity;
            if (e != null)
            {
                flow = WaterModAPI.Entity_FluidVelocity((VRage.Game.Entity.MyEntity)e);
            }
            return flow;
        }
        public Vector3 GetFlowbyCharacter(IMyCharacter character)
        {
            Vector3 flow = new Vector3(0, 0, 0);
            if (character == null || character.Closed || character.IsDead)
                return flow = Vector3.Zero;
            if (!WaterModAPI.IsUnderwater(character.GetPosition()))
                return Vector3.Zero;
            var e = character as IMyEntity;
            if (e != null)
            {
                flow = WaterModAPI.Entity_FluidVelocity((VRage.Game.Entity.MyEntity)e);
            }
            return flow;
        }
        public Vector3D GetWaterDragbyCharacter(IMyCharacter character)
        {
            Vector3D drag = new Vector3D(0, 0, 0);
            if (character == null || character.Closed || character.IsDead)
                return Vector3D.Zero;
            if (!WaterModAPI.IsUnderwater(character.GetPosition()))
                return Vector3D.Zero;
            var e = character as IMyEntity;
            if (e != null)
            {
                drag = WaterModAPI.Entity_DragForce((VRage.Game.Entity.MyEntity)e);
            }
            return drag;
        }
        public float GetUnderWaterPercent(IMyCharacter character)
        {
            if (character == null || character.IsDead || character.Closed)
                return 0f;
            if (!WaterModAPI.IsUnderwater(character.GetPosition()))
                return 0f;
            float percent = 0f;
            var e = character as IMyEntity;
            if (e != null)
            {
                percent = WaterModAPI.Entity_PercentUnderwater((VRage.Game.Entity.MyEntity)e);
            }
            return percent;
        }
        public bool CheckNodeinWater(Vector3D p)
        {
            return WaterModAPI.IsUnderwater(p);
        }
        //Log
        public void Log(bool inlogging, string message)
        {
            if (inlogging && !string.IsNullOrEmpty(message))
            {
                MyAPIGateway.Utilities.ShowMessage(System, message);
            }
        }
        //UpdateTerminal
        public void UpdateTerminal(IMyFunctionalBlock block)
        {
            if (block != null && !block.Closed)
            {
                block.RefreshCustomInfo();
                block.SetDetailedInfoDirty();
            }
        }
        //Get player Inventory
        public void GetCharacterInventory(IMyCharacter character, out IMyInventory inv, out MyInventory invE)
        {
            inv = null;
            invE = null;
            if (character != null && !character.Closed && !character.IsDead)
            {
                IMyInventory Linv;
                MyInventory LinvE;
                Linv = character.GetInventory();
                if (Linv != null)
                {
                    inv = Linv;
                    LinvE = Linv as MyInventory;
                    if (LinvE != null)
                    {
                        invE = LinvE;
                    }
                }
            }
        }
        //DiverMode
        private void DiveMode()
        {
            foreach (var player in GetPlayersWithCharacters())
            {
                JetpackUnderWaterSystem.SetDiverMode(player.Character, player.IdentityId, tick);
                JetpackUnderWaterSystem.AddPID(player.IdentityId);
            }
        }
        private void ConstructHUD()
        {
            if (!Heartbeat)
                return;
            var player = MyAPIGateway.Session?.Player;
            var character = player?.Character;
            if (character == null || character.Closed || character.IsDead)
            {
                HideHUD();
                return;
            }
            bool isUnderwater = WaterModAPI.IsUnderwater(character.GetPosition());
            float Fullunderwater = GetUnderWaterPercent(character);
            var energy = MyVisualScriptLogicProvider.GetPlayersEnergyLevel(player.IdentityId);
            var helmet = MyVisualScriptLogicProvider.GetPlayersHelmetStatus(player.IdentityId);
            var eox = MyAPIGateway.Session.OxygenProviderSystem.GetOxygenInPoint(character.GetPosition());
            float ingridox;
            GetInAirtightGrid(character, out ingridox);
            if (!isUnderwater || Fullunderwater < 1f || energy <= 0f || !helmet)
            {
                HideHUD();
                return;
            }
            if (eox > MIN_ENVOXYGENLEVEL || ingridox > MIN_ENVOXYGENLEVEL || IsPlayerProtected(player))
            {
                HideHUD();
                return;
            }
            if (IsPlayerControlling(player) || isPlayeUseCamera())
            {
                HideHUD();
                return;
            }
            // --- INIT ---
            if (PressureHUD == null)
            {
                PressureHUD = new HudAPIv2.HUDMessage(new StringBuilder(""), Vector2D.Zero);
                PressureHUD.Origin = new Vector2D(0, 1); // Left/Bottom
                PressureHUD.Offset = new Vector2D(0.25, -0.95);
            }
            if (SaltHUD == null)
            {
                SaltHUD = new HudAPIv2.HUDMessage(new StringBuilder(""), Vector2D.Zero);
                SaltHUD.Origin = new Vector2D(0, 1); // Right/Bottom
                SaltHUD.Offset = new Vector2D(0.25, -1.0);
            }
            if (gearHUD == null)
            {
                gearHUD = new HudAPIv2.HUDMessage(new StringBuilder(""), Vector2D.Zero);
                gearHUD.Origin = new Vector2D(0, 1); // Center/Bottom
                gearHUD.Offset = new Vector2D(0.25, -0.85);
            }
            if (OxygenHUD == null)
            {
                OxygenHUD = new HudAPIv2.HUDMessage(new StringBuilder(""), Vector2D.Zero);
                OxygenHUD.Origin = new Vector2D(0, 1);
                OxygenHUD.Offset = new Vector2D(0.25, -1.05);
            }
            if (DirectionLHUD == null)
            {
                DirectionLHUD = new HudAPIv2.HUDMessage(new StringBuilder(""), Vector2D.Zero);
                DirectionLHUD.Origin = new Vector2D(0, 1);
                DirectionLHUD.Offset = new Vector2D(-0.15, -1.5);
            }
            if (DirectionRHUD == null)
            {
                DirectionRHUD = new HudAPIv2.HUDMessage(new StringBuilder(""), Vector2D.Zero);
                DirectionRHUD.Origin = new Vector2D(0, 1);
                DirectionRHUD.Offset = new Vector2D(0.15, -1.5);
            }
            if (compassBar == null)
            {
                compassBar = new HudAPIv2.HUDMessage(new StringBuilder(""), Vector2D.Zero);
                //compassBar.Origin = new Vector2D(0.5, 0.5);
                compassBar.Offset = new Vector2D(-compassBar.GetTextLength().X / 2, -0.5);
            }
            if (geardepthmeter == null)
            {
                geardepthmeter = new HudAPIv2.HUDMessage(new StringBuilder(""), Vector2D.Zero);
                geardepthmeter.Offset = new Vector2D(-geardepthmeter.GetTextLength().X / 2, 0);
            }
            // --- VISIBLE ---
            PressureHUD.Visible = true;
            SaltHUD.Visible = true;
            gearHUD.Visible = true;
            OxygenHUD.Visible = true;
            DirectionLHUD.Visible = true;
            DirectionRHUD.Visible = true;
            compassBar.Visible = true;
            geardepthmeter.Visible = true;
            // --- DATA ---
            float pressure = GetPressurebyPlayer(character);
            float depth = GetWaterDepthbyCharacter(character);
            float saltlevel = GetSaltlevelbyPlayer(character, depth);
            float saltP = SaltToPercent(saltlevel);
            int gear = JetpackUnderWaterSystem.PlayerGearlevelIndx;
            bool O2refil = JetpackUnderWaterSystem.PlayerOxygenRefillActive;
            float O2F = O2refil ? 1f : 0f;
            // --- TEXT ---
            if (gear > 0)
            {
                PressureHUD.Message.Clear().Append($"Pressure {pressure:0.0} Kpa");
                SaltHUD.Message.Clear().Append($"Salt {saltP:0}%");
                string gearName =
                   gear == 0 ? "No Gear" :
                   gear == 1 ? "T1" :
                   gear == 2 ? "T2" : 
                   gear == 3 ? "T3" : "T4";
                gearHUD.Message.Clear().Append($"Dive Gear {gearName}");
                string O2Status = O2refil ? "ON" : "OFF";
                OxygenHUD.Message.Clear().Append($"O2 Refill {O2Status}");
                DirectionLHUD.Message.Clear().Append($"{LineAnimationManager.GetFrame("Ldirection")}");
                DirectionRHUD.Message.Clear().Append($"{LineAnimationManager.GetFrame("Rdirection")}");
                UpdateCompassBar();
                geardepthmeter.Message.Clear().Append($"\n\n\nDepth {Math.Round(depth)}m");
            }
            else
            {
                PressureHUD.Message.Clear().Append($"");
                SaltHUD.Message.Clear().Append($"");
                OxygenHUD.Message.Clear().Append($"");
                gearHUD.Message.Clear().Append($"");
                DirectionLHUD.Message.Clear().Append($"");
                DirectionRHUD.Message.Clear().Append($"");
                compassBar.Message.Clear().Append($"");
                geardepthmeter.Message.Clear().Append($"");
            }
            // --- COLORS ---
            // depth base color //
            float adepth = Math.Abs(depth);
            float tintfactor = MathHelper.Clamp(adepth / maxSaltDepth, 0f, 1f);
            Color finaltint = Color.Lerp(new Color(80, 255, 120),   // shallow green
            new Color(0, 120, 255),  // deep blue
            tintfactor);
            float pressureT = MathHelper.Clamp(pressure, 0f, 1f);
            PressureHUD.InitialColor = Color.Lerp(finaltint, finaltint, pressureT);
            float saltT = MathHelper.Clamp(saltP, 0f, 1f);
            SaltHUD.InitialColor = Color.Lerp(finaltint, finaltint, saltT);
            float gearT = MathHelper.Clamp(gear / 3f, 0f, 1f);
            gearHUD.InitialColor = Color.Lerp(finaltint, finaltint, gearT);
            float O2T = MathHelper.Clamp(O2F, 0f, 1f);
            float depthT = MathHelper.Clamp(depth, 0f, 1f);
            OxygenHUD.InitialColor = Color.Lerp(Color.OrangeRed, finaltint, O2T);
            DirectionLHUD.InitialColor = Color.Lerp(finaltint, finaltint, 0.5f);
            DirectionRHUD.InitialColor = Color.Lerp(finaltint, finaltint, 0.5f);
            compassBar.InitialColor = Color.Lerp(finaltint, finaltint, 0.5f);
            geardepthmeter.InitialColor = Color.Lerp(finaltint, finaltint, depthT);
        }
        private void UpdateGlobalHUDColor()
        {
            var player = MyAPIGateway.Session?.Player;
            var character = player?.Character;
            if (character == null || character.Closed || character.IsDead)
                return;
            var isUnderwater = WaterModAPI.IsUnderwater(character.GetPosition());
            float Fullunderwater = GetUnderWaterPercent(character);
            if (!isUnderwater || isUnderwater && Fullunderwater < 1.0f)
            {
                GlobaldepthbasedHUDColor = OriginalInteractionColor;
                //Log(true, $"revert to original color {GlobaldepthbasedHUDColor.ToVector4().ToString()}");
                return;
            }
            float depth = GetWaterDepthbyCharacter(character);
            float adepth = Math.Abs(depth);
            float tintfactor = MathHelper.Clamp(adepth / maxSaltDepth, 0f, 1f);
            Color finaltint = Color.Lerp(new Color(80, 255, 120),   // shallow green
           new Color(0, 120, 255),  // deep blue
           tintfactor);
            float depthT = MathHelper.Clamp(depth, 0f, 1f);
            GlobaldepthbasedHUDColor = Color.Lerp(finaltint, finaltint, depthT);
        }
        private void HideHUD()
        {
            if (PressureHUD != null) PressureHUD.Visible = false;
            if (gearHUD != null) gearHUD.Visible = false;
            if (SaltHUD != null) SaltHUD.Visible = false;
            if (OxygenHUD != null) OxygenHUD.Visible = false;
            if (DirectionLHUD != null) DirectionLHUD.Visible = false;
            if (DirectionRHUD != null) DirectionRHUD.Visible = false;
            if (compassBar != null) compassBar.Visible = false;
            if (geardepthmeter != null) geardepthmeter.Visible = false;
        }
        private void UpdateAll()
        {
            DiveMode();
            ConstructHUD();
        }
        public void GetInAirtightGrid(IMyCharacter character, out float ingridoxygenlevel)
        {
            ingridoxygenlevel = 0f;
            if (character != null && !character.Closed && !character.IsDead)
            {
                var pos = character.GetPosition();
                BoundingSphereD sphere = new BoundingSphereD(pos, 1);
                List<IMyEntity> Ents = MyAPIGateway.Entities.GetTopMostEntitiesInSphere(ref sphere);
                foreach (var ent in Ents)
                {
                    var grid = ent as IMyCubeGrid;
                    if (grid?.GasSystem == null)
                        continue;
                    // Probe positions around the character
                    Vector3D[] probeOffsets = new Vector3D[]
                    {
                                Vector3D.Zero,
                                character.WorldMatrix.Up * 0.25,
                                character.WorldMatrix.Down * 0.25,
                                character.WorldMatrix.Left * 0.25,
                                character.WorldMatrix.Right * 0.25,
                                character.WorldMatrix.Forward * 0.25,
                                character.WorldMatrix.Backward * 0.25
                    };
                    foreach (var offset in probeOffsets)
                    {
                        Vector3D checkPos = pos + offset;
                        Vector3I cell = grid.WorldToGridInteger(checkPos);
                        var room = grid.GasSystem.GetOxygenRoomForCubeGridPosition(ref cell);
                        if (room != null && room.IsAirtight)
                        {
                            float roomOxygen = room.OxygenLevel(grid.GridSize);
                            // Take the highest oxygen found among all probes
                            if (roomOxygen > ingridoxygenlevel)
                            {
                                ingridoxygenlevel = roomOxygen;
                            }
                        }
                    }
                }
            }
        }
        public void GetBlockInAirtightGrid(IMyFunctionalBlock block, out float ingridoxygenlevel)
        {
            ingridoxygenlevel = 0f;
            if (block != null && !block.Closed && !block.MarkedForClose)
            {
                var pos = block.GetPosition();
                BoundingSphereD sphere = new BoundingSphereD(pos, 1);
                List<IMyEntity> Ents = MyAPIGateway.Entities.GetTopMostEntitiesInSphere(ref sphere);
                foreach (var ent in Ents)
                {
                    var grid = ent as IMyCubeGrid;
                    if (grid?.GasSystem == null)
                        continue;
                    Vector3D[] probeOffsets = new Vector3D[]
                   {
                                Vector3D.Zero,
                                block.WorldMatrix.Up * 0.25,
                                block.WorldMatrix.Down * 0.25,
                                block.WorldMatrix.Left * 0.25,
                                block.WorldMatrix.Right * 0.25,
                                block.WorldMatrix.Forward * 0.25,
                                block.WorldMatrix.Backward * 0.25
                   };
                    foreach (var offset in probeOffsets)
                    {
                        Vector3D checkPos = pos + offset;
                        Vector3I cell = grid.WorldToGridInteger(checkPos);
                        var room = grid.GasSystem.GetOxygenRoomForCubeGridPosition(ref cell);
                        if (room != null && room.IsAirtight)
                        {
                            float roomOxygen = room.OxygenLevel(grid.GridSize);
                            // Take the highest oxygen found among all probes
                            if (roomOxygen > ingridoxygenlevel)
                            {
                                ingridoxygenlevel = roomOxygen;
                            }
                        }
                    }
                }
            }
        }
        public void CheckUnderwaterBlockRules(IMyFunctionalBlock block, bool OnlyinAirtight)
        {
            if (block == null || block.Closed || block.MarkedForClose)
                return;
            bool isUnderwater = WaterModAPI.IsUnderwater(block.GetPosition());
            if (!isUnderwater)
                return;

            if (OnlyinAirtight)
            {
                float ingridox;
                GetBlockInAirtightGrid(block, out ingridox);
                if (isUnderwater && ingridox < MIN_ENVOXYGENLEVEL)
                {
                    block.Enabled = false;
                    //AquaExpansionSession.Insance.Log(true, $"{block.EntityId} disabled by underwater rules");
                    return;
                }
            }
            else
            {
                var depth = GetWaterDepth(block);
                if (isUnderwater && depth < -ApexFarmMaxworkDepth)
                {
                    block.Enabled = false;
                    //AquaExpansionSession.Insance.Log(true, $"{block.EntityId} disabled by underwater rules: depth > -{ApexFarmMaxworkDepth} m");
                    return;
                }
            }
        }
        public bool IsPlayerProtected(IMyPlayer player)
        {
            var seat = player.Controller?.ControlledEntity?.Entity as IMyShipController;
            if (seat != null)
            {
                var incocpit = seat as IMyCockpit;
                if (incocpit != null && incocpit.OxygenFilledRatio > 0)
                {
                    return true;
                }
            }
            return false;
        }
        public bool IsPlayerControlling(IMyPlayer player)
        {
            var controlled = player.Controller?.ControlledEntity?.Entity;
            // Large or small turret
            if (controlled is IMyLargeTurretBase)
            {
                return true;
            }
            // Remote control
            if (controlled is IMyRemoteControl)
            {
                return true;
            }
            return false;
        }
        public bool isPlayeUseCamera()
        {
            if (Session.CameraController?.Entity is MyCameraBlock)
                return true;
            return false;
        }
        private double GetHeading(Vector3D forward)
        {
            forward.Y = 0;
            if (forward.LengthSquared() < 1e-6)
                return 0;
            forward.Normalize();
            double angle = Math.Atan2(forward.X, -forward.Z);
            double deg = MathHelper.ToDegrees(angle);
            if (deg < 0)
                deg += 360;
            return deg;
        }
        private string ConstructCompassBar(double heading)
        {
            int segments = 1;
            StringBuilder sb = new StringBuilder();
            for (int i = -segments / 2; i <= segments / 2; i++)
            {
                double angle = heading + i;
                // wrap 0–360
                angle = (angle % 360 + 360) % 360;
                // center marker
                if (i == 0)
                    sb.Append($"[{Math.Round(MathHelper.Clamp(angle, 0f, 359f))}] ");
                else
                    sb.Append($"{angle} ");
            }
            return sb.ToString();
        }
        private void UpdateCompassBar()
        {
            if (MyAPIGateway.Session?.Camera == null)
                return;
            var cam = MyAPIGateway.Session.Camera;
            if (cam == null) return;
            double heading = GetHeading(cam.WorldMatrix.Forward);
            int shift = (int)((heading % 15.0) / 15.0 * 6);
            compassBar.Message.Clear().Append($"{ConstructCompassBar(heading)}");
        }
        public string UpdateGridCompass(IMyCubeBlock block)
        {
            if (block == null)
                return null;
            string data = "";
            double heading = GetHeading(block.WorldMatrix.Forward);
            int shift = (int)((heading % 15.0) / 15.0 * 6);
            return data = $"{ConstructCompassBar(heading)}";
        }
        private void ClearTrackedBlocks()
        {
            foreach (var e in effects.Values)
            { e?.Stop(); }
            foreach (var e in Engineeffects.Values)
            { e?.Stop(); }
            Engineeffects.Clear();
            effects.Clear();
            TerminalTracedBlocks.Clear();
            TrackedThrusters.Clear();
            TrackedGrids.Clear();
            BannedThrusters.Clear();
            BannedTurbines.Clear();
            BannedSolars.Clear();
            TrackedAlgaeFarms.Clear();
            latentScheduler.Clear();
            enviromentdef = null;
            //
            foreach (var e in Weldereffects.Values)
            { e?.Stop(); }
            Weldereffects.Clear();
            foreach (var e in WeldereffectsShip.Values)
            { e?.Stop(); }
            WeldereffectsShip.Clear();
            TrackedShipWelders.Clear();
            TrackedFarms.Clear();
            Trackedhandwelders.Clear();
        }
        private void ClearAll()
        {
            MyAPIGateway.Entities.OnEntityAdd -= OnEntityAdd;
            MyAPIGateway.Entities.OnEntityRemove -= OnEntityRemove;
            MyAPIGateway.Utilities.MessageEntered -= OnMessageEntered;
            ClearTrackedBlocks();
            latentScheduler = null;
            JetpackUnderWaterSystem = null;
            TextAPI.Close();
            ClearBalisticksFallback();
            balistics = null;
            Insance = null;
        }
        private void SpawnTestAnimal(String BotSubtype, string Creaturename)
        {
            var player = MyAPIGateway.Session?.Player;
            var character = player?.Character;
            if (character == null || character.Closed || character.IsDead)
                return;
            if (string.IsNullOrEmpty(BotSubtype))
            {
                Log(true, $"Invalid BotName");
                return;
            }
            Vector3D playerpos = character.GetPosition();
            var objforwardv = new Vector3(0, 0, 0);
            var objupv = new Vector3(0, 4, 0);
            double forwarddistance;
            double rightdistance;
            forwarddistance = 5;
            rightdistance = 0;
            Vector3D objectpos = new Vector3D(0, 0, 0);
            var posforward = playerpos + character.PositionComp.WorldMatrixRef.Forward * forwarddistance;
            var posright = playerpos + character.PositionComp.WorldMatrixRef.Right * rightdistance;
            long creatureid = MyVisualScriptLogicProvider.SpawnBot(BotSubtype, posforward, objforwardv, objupv, Creaturename);
            //Log(true, $"subtype {creatureid}");

        }
        private void PressToSpawnAnimal()
        {
            if (MyAPIGateway.Input.IsNewKeyPressed(MyKeys.T))
            {
                //SpawnTestAnimal("AquaShark_Bot", "White Shark");
            }
        }
        private string GetShipWelderEffect(IMyShipWelder block)
        {
            if (block == null || block.Closed || block.MarkedForClose || !block.IsFunctional)
                return GlobalEffects.GetWelderEffect("Default");
            var subtype = block.BlockDefinition.SubtypeId;
            if (subtype.Contains("LargeShipWelder"))
                return GlobalEffects.GetWelderEffect("WelderShip");
            if (subtype.Contains("LargeShipWelderReskin"))
                return GlobalEffects.GetWelderEffect("WelderShip");
            if (subtype.Contains("SmallShipWelder"))
                return GlobalEffects.GetWelderEffect("WelderShip2");
            if (subtype.Contains("SmallShipWelderReskin"))
                return GlobalEffects.GetWelderEffect("WelderShip2");
            return GlobalEffects.GetWelderEffect("Default");
        }
        private void ProcessShipWelderBlock(IMyShipWelder block)
        {
            if (block == null || block.Closed || block.MarkedForClose)
                return;
            var player = MyAPIGateway.Session?.Player;
            if (player == null)
                return;
            double dist = Vector3D.DistanceSquared(player.GetPosition(), block.GetPosition());
            //LOD 1
            if (dist > effectLod1disSq * effectLod1disSq)
            {
                //StopShipWelderEffect(block);
                GlobalEffects.StopShipWelderEffect(block.EntityId, WeldereffectsShip);
                return;
            }
            bool isUnderwater = WaterModAPI.IsUnderwater(block.GetPosition());
            bool isBroken = !block.IsFunctional;
            //var eox = MyAPIGateway.Session.OxygenProviderSystem.GetOxygenInPoint(block.GetPosition());
            float ingridox;
            GetBlockInAirtightGrid(block, out ingridox);
            MyParticleEffect effect;
            WeldereffectsShip.TryGetValue(block.EntityId, out effect);
            if (!isUnderwater || isBroken || !block.Enabled || !block.IsWorking || !block.IsActivated || 
                (isUnderwater && ingridox > MIN_ENVOXYGENLEVEL))
            {
                //StopShipWelderEffect(block);
                GlobalEffects.StopShipWelderEffect(block.EntityId, WeldereffectsShip);
                return;
            }
            // LOD 2
            scale = dist > effectLod2disSq * effectLod2disSq ? 0.5f : 1f;
            if (effect == null)
            {
                //CreateShipWelderEffect(block, scale);
                GlobalEffects.CreateShipWelderEffect(block,GetShipWelderEffect(block), scale, WeldereffectsShip);
            }
            else
            {
                effect.UserScale = scale;
            }
        }
        private void UpdateShipWelderBlocks()
        {
            if (tick % 10 != 0)
                return;
            foreach (var block in TrackedShipWelders)
            {
                if (block == null || block.Closed || block.MarkedForClose)
                    continue;
                ProcessShipWelderBlock(block);
                UpdateShipWelderEffects();
            }
            //Log(true, $"Ship Welder blocks {TrackedShipWelders.Count}");
        }
        private void FilterShipWelders()
        {
            if (tick % 10 != 0)
                return;
            //Cleanup tracked
            List<IMyShipWelder> toRemove = null;
            foreach (var welder in TrackedShipWelders)
            {
                if (welder == null ||
                    welder.Closed ||
                    welder.MarkedForClose ||
                    !WaterModAPI.IsUnderwater(welder.GetPosition()))
                {
                    if (toRemove == null)
                        toRemove = new List<IMyShipWelder>();

                    toRemove.Add(welder);
                }
            }
            if (toRemove != null)
            {
                foreach (var w in toRemove)
                {
                    GlobalEffects.StopShipWelderEffect(w.EntityId, WeldereffectsShip);
                    TrackedShipWelders.Remove(w);
                }
            }
            //Main scan
            foreach (var block in TerminalTracedBlocks)
            {
                if (block == null || block.Closed || block.MarkedForClose)
                    continue;
                var welder = block as IMyShipWelder;
                if (welder == null || welder.Closed || welder.MarkedForClose)
                    continue;
                var isUnderwater = WaterModAPI.IsUnderwater(welder.GetPosition());
                if (isUnderwater && !TrackedShipWelders.Contains(welder))
                {
                    TrackedShipWelders.Add(welder);
                    GlobalEffects.CreateShipWelderEffect(welder, GetShipWelderEffect(welder),scale, WeldereffectsShip);
                }
            }
        }
        private void UpdateShipWelderEffects()
        {
            foreach (var pair in WeldereffectsShip)
            {
                var block = MyEntities.GetEntityById(pair.Key) as IMyShipWelder;
                var effect = pair.Value;
                if (block == null || effect == null)
                    continue;
                MatrixD m = effect.WorldMatrix;
                m.Up = Vector3D.Up;
                effect.WorldMatrix = m;
            }
        }
        public int GetTick
        {
            get
            {
                return weapontick;
            }
        }
        private void OnMessageEntered(string messageText, ref bool sendToOthers)
        {
            IMyPlayer player = MyAPIGateway.Session?.LocalHumanPlayer;
            if (AllowModding(messageText, player,ref sendToOthers))
                return;
            if (AllowHydroAmmoModding(messageText,player,ref sendToOthers))
                return;
            if (DisableModding(messageText,player,ref sendToOthers))
                return;
            if (DisableHydroAmmoModding(messageText,player,ref sendToOthers))
            return;
            if (ChatTestSound(messageText,player,ref sendToOthers))
            {
                return;
            }
        }
        private bool HasCommandPermission(IMyPlayer player,ChatCommandPermission permission)
        {
            if (player == null)
                return false;
            switch (permission)
            {
                case ChatCommandPermission.Public:
                    return true;
                case ChatCommandPermission.ServerOnly:
                    return MyAPIGateway.Multiplayer.IsServer;
                case ChatCommandPermission.AdminOnly:
                    if (!MyAPIGateway.Multiplayer.IsServer)
                        return false;
                    return MyAPIGateway.Session.IsUserAdmin(player.SteamUserId);
            }
            return false;
        }
        private bool AllowModding(string chatMessage,IMyPlayer player, ref bool sendToOthers)
        {
            if (string.IsNullOrWhiteSpace(chatMessage))
                return false;
            if (!chatMessage.Trim().Equals(AquaModdingNamesDatabase.GetModCommandByID(1), StringComparison.OrdinalIgnoreCase))
                return false;
            sendToOthers = false;
            if (!HasCommandPermission(player,ChatCommandPermission.AdminOnly))
            {
                Log(true,AquaModdingNamesDatabase.GetModCommandByID(23));
                return true;
            }
            if (!ModdingAllowed)
            {
                ModdingAllowed = true;
                Log(true, AquaModdingNamesDatabase.GetModCommandByID(5));
            }
            else
            {
                Log(true, AquaModdingNamesDatabase.GetModCommandByID(6));
            }
            return true;
        }
        private bool AllowHydroAmmoModding(string chatMessage, IMyPlayer player, ref bool sendToOthers)
        {
            if (balistics == null)
                return false;
            if (!ModdingAllowed)
                return false;
            if (string.IsNullOrWhiteSpace(chatMessage))
                return false;
            string command = chatMessage.Trim();
            if (!HasCommandPermission(player, ChatCommandPermission.AdminOnly))
            {
                Log(true, AquaModdingNamesDatabase.GetModCommandByID(23));
                return true;
            }
            // Unlock Hydro ammo runtime balancing
            if (!HydroAmmoModdingAllowed)
            {
                if (!command.Equals(AquaModdingNamesDatabase.GetModCommandByID(2), StringComparison.OrdinalIgnoreCase))
                    return false;
                sendToOthers = false;
                HydroAmmoModdingAllowed = true;
                Log(true, AquaModdingNamesDatabase.GetModCommandByID(7));
                return true;
            }
            // Runtime balancing commands
            if (command.StartsWith(AquaModdingNamesDatabase.GetModCommandByID(8), StringComparison.OrdinalIgnoreCase))
            {
                sendToOthers = false;
                balistics.ChatHAmmo(command, sendToOthers);
                return true;
            }
            return false;
        }
        private bool DisableModding(string chatMessage, IMyPlayer player, ref bool sendToOthers)
        {
            if (string.IsNullOrWhiteSpace(chatMessage))
                return false;
            string command = chatMessage.Trim();
            string disableCommand = AquaModdingNamesDatabase.GetModCommandByID(10);
            if (!command.Equals(disableCommand,StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            // Do not send the modding command to other players.
            sendToOthers = false;
            if (!HasCommandPermission(player, ChatCommandPermission.AdminOnly))
            {
                Log(true, AquaModdingNamesDatabase.GetModCommandByID(23));
                return true;
            }
            if (!ModdingAllowed)
            {
                Log(true, AquaModdingNamesDatabase.GetModCommandByID(14));
                return true;
            }
            // Disable all modding features.
            ModdingAllowed = false;
            HydroAmmoModdingAllowed = false;
            LogsEnabled = false;
            RenderEnabled = false;
            Log(true,AquaModdingNamesDatabase.GetModCommandByID(12));
            return true;
        }
        private bool DisableHydroAmmoModding(string chatMessage, IMyPlayer player,ref bool sendToOthers)
        {
            if (balistics == null)
                return false;
            if (!ModdingAllowed)
                return false;
            if (string.IsNullOrWhiteSpace(chatMessage))
                return false;
            string command = chatMessage.Trim();
            string disableCommand = AquaModdingNamesDatabase.GetModCommandByID(11);
            if (!command.Equals(disableCommand,StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            // Consume the command locally.
            sendToOthers = false;
            if (!HasCommandPermission(player, ChatCommandPermission.AdminOnly))
            {
                Log(true, AquaModdingNamesDatabase.GetModCommandByID(23));
                return true;
            }
            if (!HydroAmmoModdingAllowed)
            {
                Log(true,AquaModdingNamesDatabase.GetModCommandByID(15));
                return true;
            }
            HydroAmmoModdingAllowed = false;
            LogsEnabled = false;
            RenderEnabled = false;
            Log(true,AquaModdingNamesDatabase.GetModCommandByID(13));
            return true;
        }
        private bool ChatTestSound(string chatMessage, IMyPlayer player,ref bool sendToOthers)
        {
            if (!ModdingAllowed)
                return false;
            if (string.IsNullOrWhiteSpace(chatMessage))
                return false;
            string command = chatMessage.Trim();
            string prefix = AquaModdingNamesDatabase.GetModCommandByID(16);
            if (!command.StartsWith(prefix,StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            // Consume the command locally.
            sendToOthers = false;
            if (!HasCommandPermission(player, ChatCommandPermission.AdminOnly))
            {
                Log(true, AquaModdingNamesDatabase.GetModCommandByID(23));
                return true;
            }
            string soundName = command.Substring(prefix.Length).Trim();
            if (string.IsNullOrWhiteSpace(soundName))
            {
                Log(true, AquaModdingNamesDatabase.GetModCommandByID(17));
                return true;
            }
            //IMyPlayer player = MyAPIGateway.Session?.LocalHumanPlayer;
            var controlled = player?.Controller?.ControlledEntity?.Entity;
            if (controlled == null)
            {
                Log(true, AquaModdingNamesDatabase.GetModCommandByID(18));
                return true;
            }
            try
            {
                MyVisualScriptLogicProvider.PlaySingleSoundAtEntity(soundName,controlled.EntityId.ToString());
                Log(true, AquaModdingNamesDatabase.GetModCommandByID(19) + soundName);
                //MyAPIGateway.Utilities.ShowMessage(System,"Trying to play: " + soundName);
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine("[AquaExpansion] Sound command error: " + e);
                Log(true, AquaModdingNamesDatabase.GetModCommandByID(20) + soundName);
                //MyAPIGateway.Utilities.ShowMessage(System,"Failed to play sound: " + soundName);
            }
            return true;
        }
        private void InitModdingTools()
        {
            AquaModdingNamesDatabase.Init();
            AquaModdingNamesDatabase.Validate();
            HydroAmmoDatabase.Init();
            UnderwaterWeaponBurstDatabase.Init();
        }
        private void InitDatabases()
        {
            GlobalEffects.Init();
            AquaForbiddenComponentsDatabase.Init();
            AquaForbiddenComponentsDatabase.Validate();
            AquaHelpDatabase.Init();
            AquaHelpDatabase.Validate();
            AquaFarmItemsDatabase.Init();
            AquaFarmItemsDatabase.Validate();
            AquaFishItemsDatabase.Init();
            AquaFishItemsDatabase.Validate();
            AquaSeaAnchorItemsDatabase.Init();
            AquaSeaAnchorItemsDatabase.Validate();
            AquaPlantDatabase.Init();
            AquaPlantDatabase.Validate();
            AquaFishDatabase.Init();
            AquaFishDatabase.Validate();
            AquaSeaAnchorDatabase.Init();
            AquaSeaAnchorDatabase.Validate();
            AquaRecipeDatabase.Init();
            AquaRecipeDatabase.Validate();
            AquaFishingRecipeDatabase.Init();
            AquaFishingRecipeDatabase.Validate();
            AquaSeaAnchorEquipOrderDatabase.Init();
            AquaSeaAnchorEquipOrderDatabase.Validate();
        }
        public bool isHydroModdingEnabled
        {
            get
            {
                return HydroAmmoModdingAllowed;
            }
        }
        public bool isModdingEnabled
        {
            get
            {
                return ModdingAllowed;
            }
        }
        public bool LogsEnabled { get; set; }
        public bool RenderEnabled { get; set; }
        public bool HasBalistics
        {
            get { return usebalisticsFallback; }
        }
        public void OnRegisterHandWeapon(IMyAutomaticRifleGun weapon)
        {
            RegisterHandWeapon?.Invoke(weapon);
        }
        public void OnUnregisterHandWeapon(IMyAutomaticRifleGun weapon)
        {
            UnregisterHandWeapon?.Invoke(weapon);
        }
        public void OnRegisterHandWelder(IMyWelder welder)
        {
            if (welder == null)
                return;
            Trackedhandwelders.Add(welder);
            /*Log(
            true,
            $"REGISTER welder={welder.EntityId} " +
            $"count={Trackedhandwelders.Count}");*/
        }
        public void OnUnRegisterHandWelder(IMyWelder welder)
        {
            if (welder == null)
                return;
            Trackedhandwelders.Remove(welder);
            /*Log(
            true,
            $"UNREGISTER welder={welder.EntityId} " +
            $"count={Trackedhandwelders.Count}");*/
        }
        private void UpdateHandTools()
        {
            ToolsProcessor.UpdateHandWelders(Trackedhandwelders, Weldereffects);
        }
        protected override void UnloadData()
        {
            ClearAll();
            base.UnloadData();
        }
    }
    [ProtoContract]
    [Serializable]
    [XmlRoot("WaterClientSettings")]
    public class AquaWaterSettings
    {
        [ProtoMember(1)]
        public int Quality;
        [ProtoMember(10)]
        public bool ShowCenterOfBuoyancy;
        [ProtoMember(15)]
        public bool ShowDepth;
        [ProtoIgnore()]
        public bool ShowFog;
        [ProtoMember(25)]
        public bool ShowDebug;
        [ProtoMember(30)]
        public float Volume;
        [ProtoMember(40)]
        public float MinAltitude;
        [ProtoMember(45)]
        public bool Silent;
    }
}

