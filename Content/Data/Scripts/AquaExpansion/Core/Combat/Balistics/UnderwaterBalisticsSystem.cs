using Jakaria.API;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Weapons;
using System;
using System.Collections.Generic;

namespace AquaExpansion.Core.Combat.Balistics
{
    public sealed class UnderwaterBalisticsSystem
    {
        private readonly Dictionary<long, MissileState> Trackedmissiles = new Dictionary<long, MissileState>();
        private readonly HashSet<IMyAutomaticRifleGun> handWeapons = new HashSet<IMyAutomaticRifleGun>();
        private readonly HashSet<IMySmallGatlingGun> TrackedGatlings = new HashSet<IMySmallGatlingGun>();
        private readonly HashSet<IMySmallMissileLauncher> TrackedSmallMissile = new HashSet<IMySmallMissileLauncher>();
        private readonly HashSet<IMyLargeTurretBase> TrackedTurrets = new HashSet<IMyLargeTurretBase>();
        private readonly HashSet<IMySmallMissileLauncher> TrackedRailguns = new HashSet<IMySmallMissileLauncher>();
        public void Load()
        {
            MyAPIGateway.Projectiles.AddOnHitInterceptor(1, OnProjectileHit);
            MyAPIGateway.Projectiles.OnProjectileAdded += OnProjectileAddedRemoved;
            MyAPIGateway.Projectiles.OnProjectileRemoving += Projectiles_OnProjectileRemoving;
            MyAPIGateway.Missiles.OnMissileAdded += OnMissileAdded;
            MyAPIGateway.Missiles.OnMissileRemoved += OnMissileRemoved;
            MyAPIGateway.Missiles.OnMissileCollided += OnMissileHit;
            AquaExpansionSession.Insance.RegisterHandWeapon += OnRegisterHandWeapon;
            AquaExpansionSession.Insance.UnregisterHandWeapon += OnUnregisterHandWeapon;
            VoxelImpactDatabase.Init();
            CharacterImpactDatabase.Init();
            WaterImpactDatabase.Init();
            UnderwaterWeaponBurstDatabase.Init();
            WeaponDummiesDatabase.Init();
            WeaponDummiesDatabase.Validate();
        }
        public void Unload()
        {
            MyAPIGateway.Projectiles.RemoveOnHitInterceptor(OnProjectileHit);
            MyAPIGateway.Projectiles.OnProjectileAdded -= OnProjectileAddedRemoved;
            MyAPIGateway.Projectiles.OnProjectileRemoving -= Projectiles_OnProjectileRemoving;
            MyAPIGateway.Missiles.OnMissileAdded -= OnMissileAdded;
            MyAPIGateway.Missiles.OnMissileRemoved -= OnMissileRemoved;
            MyAPIGateway.Missiles.OnMissileCollided -= OnMissileHit;
            AquaExpansionSession.Insance.RegisterHandWeapon -= OnRegisterHandWeapon;
            AquaExpansionSession.Insance.UnregisterHandWeapon -= OnUnregisterHandWeapon;
            Clear();
        }
        public void Update(HashSet<IMyTerminalBlock> traced)
        {
            AquaProjectileManager.Update();
            MissileProcessor.UpdateMissiles(Trackedmissiles);
            WeaponProcessor.UpdateHandWeapons(handWeapons);
            FilterWeaponBlocks(traced);
            WeaponProcessor.UpdatwWeaponBlocks(TrackedGatlings, TrackedSmallMissile);
            WeaponProcessor.UpdateWeaponTurrets(AquaExpansionSession.Insance.GetTick, TrackedTurrets);
            //WeaponProcessor.UpdateWeaponRailguns(AquaExpansionSession.Insance.GetTick,TrackedRailguns);
        }
        private void OnProjectileAddedRemoved(ref MyProjectileInfo projectile, int index)
        {
            ProjectileProcessor.OnProjectileAdded(ref projectile, index);
        }
        private void Projectiles_OnProjectileRemoving(ref MyProjectileInfo projectile, int index)
        {
            /*AquaExpansionSession.Insance.Log(true,
            $"Destroyed projectile: {index}");*/
        }
        private void OnProjectileHit(ref MyProjectileInfo projectile, ref MyProjectileHitInfo hit)
        {
            ProjectileProcessor.Process(ref projectile, ref hit);
        }
        private void OnMissileAdded(IMyMissile missile)
        {
            MissileProcessor.OnMissileAdded(missile, Trackedmissiles);
        }
        private void OnMissileRemoved(IMyMissile missile)
        {
            MissileProcessor.OnMissileKilled(missile, Trackedmissiles);
        }
        private void OnMissileHit(IMyMissile missile)
        {
            MissileState state;
            if (Trackedmissiles.TryGetValue(missile.EntityId, out state))
            {
                MissileProcessor.Process(missile, state);
            }
        }
        private void Clear()
        {
            Trackedmissiles.Clear();
            handWeapons.Clear();
            TrackedGatlings.Clear();
            TrackedSmallMissile.Clear();
            TrackedRailguns.Clear();
            TrackedTurrets.Clear();
            ProjectileProcessor.Clear();
            MissileProcessor.ClearEffects();
            HydroAmmoDatabase.ClearRuntime();
        }
        public void ChatHAmmo(string chatMessage, bool sendToOthers)
        {
            if (string.IsNullOrWhiteSpace(chatMessage))
                return;
            string[] args = chatMessage.Split(
                new[] { ' ' },
                StringSplitOptions.RemoveEmptyEntries);
            if (args.Length < 2)
            {
                CombatUtils.ShowAmmoHelp();
                return;
            }
            string command = args[1].ToLower();
            AquaExpansionSession session = AquaExpansionSession.Insance;
            switch (command)
            {
                case "help":
                    CombatUtils.ShowAmmoHelp();
                    return;
                case "clear":
                    HydroAmmoDatabase.ClearRuntime();
                    MyAPIGateway.Utilities.ShowMessage(AquaExpansionSession.Insance.AquaAPI,
                    AquaModdingNamesDatabase.GetModCommandByID(9));
                    return;
                case "log":
                    session.LogsEnabled = !session.LogsEnabled;
                    MyAPIGateway.Utilities.ShowMessage(session.AquaAPI,
                    AquaModdingNamesDatabase.GetModCommandByID(session.LogsEnabled ? 21 : 22));
                    return;
                case "list":
                    CombatUtils.ListAllAmmo();
                    return;
                case "vis":
                    session.RenderEnabled = !session.RenderEnabled;
                    MyAPIGateway.Utilities.ShowMessage(session.AquaAPI,
                    AquaModdingNamesDatabase.GetModCommandByID(session.RenderEnabled ? 24 : 25));
                    return;
            }
            // Ammo-specific commands
            if (args.Length < 3)
            {
                CombatUtils.ShowAmmoHelp();
                return;
            }
            CombatUtils.ExecuteAmmoCommand(args);
        }
        private void OnRegisterHandWeapon(IMyAutomaticRifleGun weapon)
        {
            if (weapon == null)
                return;
            handWeapons.Add(weapon);
            /*AquaExpansionSession.Insance.Log(
            true,
            $"REGISTER rifle={weapon.EntityId} " +
            $"count={handWeapons.Count}");*/
        }
        private void OnUnregisterHandWeapon(IMyAutomaticRifleGun weapon)
        {
            if (weapon == null)
                return;
            handWeapons.Remove(weapon);
            /*AquaExpansionSession.Insance.Log(
            true,
            $"UNREGISTER rifle={weapon.EntityId} " +
            $"count={handWeapons.Count}");*/
        }
        private void FilterWeaponBlocks(HashSet<IMyTerminalBlock> traced)
        {
            FilterSmallGatling(AquaExpansionSession.Insance.GetTick, TrackedGatlings, traced);
            FilterSmallMissile(AquaExpansionSession.Insance.GetTick, TrackedSmallMissile, traced);
            FilterTurrets(AquaExpansionSession.Insance.GetTick, TrackedTurrets, traced);
            //FilterRailguns(AquaExpansionSession.Insance.GetTick,TrackedRailguns, traced);
        }
        private void FilterSmallGatling(int tick, HashSet<IMySmallGatlingGun> gatlings, HashSet<IMyTerminalBlock> traced)
        {
            if (tick % 10 != 0)
                return;
            // -----------------------------------------
            // Cleanup
            // -----------------------------------------
            List<IMySmallGatlingGun> toRemove = null;
            foreach (var sg in gatlings)
            {
                if (sg == null ||
                    sg.Closed ||
                    sg.MarkedForClose ||
                    CombatUtils.WaterPlanet(sg.GetPosition()) == null)
                {
                    if (toRemove == null)
                        toRemove =
                            new List<IMySmallGatlingGun>();
                    toRemove.Add(sg);
                }
            }
            if (toRemove != null)
            {
                foreach (var sg in toRemove)
                {
                    gatlings.Remove(sg);
                    /*AquaExpansionSession.Insance.Log(
                        true,
                        "Remove SmallGatlingGun " +
                        (sg != null
                            ? sg.EntityId.ToString()
                            : "null") +
                        " count " +
                        gatlings.Count);*/
                }
            }
            // -----------------------------------------
            // Main scan
            // -----------------------------------------

            foreach (var block in traced)
            {
                if (block == null ||
                    block.Closed ||
                    block.MarkedForClose)
                    continue;
                var sg = block as IMySmallGatlingGun;

                if (sg == null ||
                    sg.Closed ||
                    sg.MarkedForClose)
                    continue;
                if (gatlings.Contains(sg))
                    continue;
                // Space / non-water planet => don't register
                if (CombatUtils.WaterPlanet(sg.GetPosition()) == null)
                    continue;
                gatlings.Add(sg);
                /*AquaExpansionSession.Insance.Log(
                    true,
                    "Added SmallGatlingGun " +
                    sg.EntityId +
                    " count " +
                    gatlings.Count);*/
            }
        }
        private void FilterSmallMissile(int tick, HashSet<IMySmallMissileLauncher> missiles, HashSet<IMyTerminalBlock> traced)
        {
            if (tick % 10 != 0)
                return;
            // -----------------------------------------
            // Cleanup
            // -----------------------------------------
            List<IMySmallMissileLauncher> toRemove = null;
            foreach (var sm in missiles)
            {
                if (sm == null ||
                    sm.Closed ||
                    sm.MarkedForClose ||
                    CombatUtils.WaterPlanet(sm.GetPosition()) == null)
                {
                    if (toRemove == null)
                        toRemove =
                            new List<IMySmallMissileLauncher>();
                    toRemove.Add(sm);
                }
            }
            if (toRemove != null)
            {
                foreach (var sm in toRemove)
                {
                    missiles.Remove(sm);
                    /*AquaExpansionSession.Insance.Log(
                        true,
                        "Remove SmallMissile " +
                        (sm != null
                            ? sm.EntityId.ToString()
                            : "null") +
                        " count " +
                        missiles.Count);*/
                }
            }
            // -----------------------------------------
            // Main scan
            // -----------------------------------------
            foreach (var block in traced)
            {
                if (block == null ||
                    block.Closed ||
                    block.MarkedForClose)
                    continue;
                var sm = block as IMySmallMissileLauncher;

                if (sm == null ||
                    sm.Closed ||
                    sm.MarkedForClose)
                    continue;
                if (missiles.Contains(sm))
                    continue;
                // Space / non-water planet => don't register
                if (CombatUtils.WaterPlanet(sm.GetPosition()) == null)
                    continue;
                missiles.Add(sm);
                /*AquaExpansionSession.Insance.Log(
                    true,
                    "Added SmallMissile " +
                    sm.EntityId +
                    " count " +
                    missiles.Count);*/
            }
        }
        private void FilterTurrets(int tick, HashSet<IMyLargeTurretBase> turrets, HashSet<IMyTerminalBlock> traced)
        {
            if (tick % 10 != 0)
                return;
            //Cleanup tracked
            List<IMyLargeTurretBase> toRemove = null;
            foreach (var t in TrackedTurrets)
            {
                if (t == null ||
                    t.Closed ||
                    t.MarkedForClose ||
                    !WaterModAPI.IsUnderwater(t.GetPosition()))
                {
                    if (toRemove == null)
                        toRemove = new List<IMyLargeTurretBase>();
                    toRemove.Add(t);
                }
            }
            if (toRemove != null)
            {
                foreach (var tu in toRemove)
                {
                    TrackedTurrets.Remove(tu);
                }
            }
            //Main scan
            foreach (var block in traced)
            {
                if (block == null || block.Closed || block.MarkedForClose)
                    continue;
                var turret = block as IMyLargeTurretBase;
                if (turret == null || turret.Closed || turret.MarkedForClose)
                    continue;
                var isUnderwater = WaterModAPI.IsUnderwater(turret.GetPosition());
                if (isUnderwater && !TrackedTurrets.Contains(turret))
                {
                    TrackedTurrets.Add(turret);
                }
            }
        }
        private void FilterRailguns(int tick, HashSet<IMySmallMissileLauncher> missiles, HashSet<IMyTerminalBlock> traced)
        {
            if (tick % 10 != 0)
                return;
            // -----------------------------------------
            // Cleanup
            // -----------------------------------------
            List<IMySmallMissileLauncher> toRemove = null;
            foreach (var sm in missiles)
            {
                if (sm == null ||
                    sm.Closed ||
                    sm.MarkedForClose ||
                    CombatUtils.WaterPlanet(sm.GetPosition()) == null)
                {
                    if (toRemove == null)
                        toRemove =
                            new List<IMySmallMissileLauncher>();
                    toRemove.Add(sm);
                }
            }
            if (toRemove != null)
            {
                foreach (var sm in toRemove)
                {
                    missiles.Remove(sm);
                }
            }
            // -----------------------------------------
            // Get registered Railgun component
            // -----------------------------------------
            string registeredName =
                AquaForbiddenComponentsDatabase.GetComponentByID(3);

            if (string.IsNullOrWhiteSpace(registeredName))
                return;
            // -----------------------------------------
            // Main scan
            // -----------------------------------------
            foreach (var block in traced)
            {
                if (block == null ||
                    block.Closed ||
                    block.MarkedForClose)
                    continue;
                var sm = block as IMySmallMissileLauncher;
                if (sm == null ||
                    sm.Closed ||
                    sm.MarkedForClose)
                    continue;
                if (TrackedRailguns.Contains(sm))
                    continue;
                if (CombatUtils.WaterPlanet(sm.GetPosition()) == null)
                    continue;

                if (!WaterModAPI.IsUnderwater(sm.GetPosition()))
                    continue;

                foreach (var comp in sm.Components)
                {
                    if (comp == null)
                        continue;

                    if (!string.Equals(
                            registeredName,
                            comp.GetType().Name,
                            StringComparison.OrdinalIgnoreCase))
                        continue;

                    TrackedRailguns.Add(sm);
                    break;
                }
            }
        }
    }
}
