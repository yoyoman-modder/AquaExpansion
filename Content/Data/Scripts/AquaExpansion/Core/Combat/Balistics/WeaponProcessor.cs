using AquaExpansion.Core.Combat.WeaponBlocks;
using Jakaria.API;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Weapons;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRageMath;

namespace AquaExpansion.Core.Combat.Balistics
{
    public static class WeaponProcessor
    {
        private static bool IsShot(IMyAutomaticRifleGun weapon, AquaHandWeaponHandler handler)
        {
            if (weapon == null ||
                handler == null ||
                !weapon.IsShooting)
                return false;
            int intervalMs = weapon.GunBase.ShootIntervalInMiliseconds;
            int intervalFrames = Math.Max(
                1,
                (int)Math.Ceiling(
                    intervalMs /
                    (MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS * 1000.0)));
            int currentTick = MyAPIGateway.Session.GameplayFrameCounter;
            if (handler.LastShotTick >= 0 &&
                currentTick - handler.LastShotTick < intervalFrames)
            {
                return false;
            }
            handler.LastShotTick = currentTick;
            /*AquaExpansionSession.Insance.Log(
                true,
                $"Weapon interval {intervalMs} intervalframes {intervalFrames}");*/
            return true;
        }
        private static bool IsGatlingShot(IMySmallGatlingGun weapon, AquaWeaponBlockBase handler)
        {
            if (weapon == null ||
                handler == null ||
                !weapon.IsShooting)
                return false;
            int interval = handler.GetWeaponDeff.ShootDirectionUpdateTime;
            int intervalFrames = Math.Max(
                1,
                (int)Math.Ceiling(
                    interval /
                    (MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS * 1000.0)));
            int currentTick = MyAPIGateway.Session.GameplayFrameCounter;
            if (handler.LastShotTick >= 0 &&
               currentTick - handler.LastShotTick < intervalFrames)
            {
                return false;
            }
            handler.LastShotTick = currentTick;
            /*AquaExpansionSession.Insance.Log(
                true,
                $"Weapon interval {interval} intervalframes {intervalFrames}");*/
            return true;
        }
        private static void Update(IMyAutomaticRifleGun weapon)
        {
            if (weapon == null ||
                weapon.Closed ||
                weapon.MarkedForClose)
                return;
            AquaHandWeaponHandler handler = weapon.GameLogic.GetAs<AquaHandWeaponHandler>();
            if (handler == null ||
                !handler.HasMuzzle ||
                handler.GetMuzzle == null)
                return;
            MatrixD muzzleMatrix = handler.GetMuzzle.Matrix * weapon.WorldMatrix;
            Vector3D muzzlePosition = muzzleMatrix.Translation;
            if (!WaterModAPI.IsUnderwater(muzzlePosition))
                return;
            if (!IsShot(weapon, handler))
                return;
            weapon.GunBase.RemoveOldEffects();
            CombatUtils.CreateHandWeaponEffect(weapon, muzzleMatrix, UnderwaterWeaponBurstDatabase.Get(handler.GetWeaponSubtype), 1f);
        }
        private static void UpdateSmallGating(IMySmallGatlingGun gat)
        {
            if (gat == null ||
                gat.Closed ||
                gat.MarkedForClose)
                return;
            AquaWeaponBlockBase handler = gat.GameLogic.GetAs<AquaWeaponBlockBase>();
            if (handler == null ||
                !handler.HasMuzzle ||
                handler.GetMuzzle == null)
                return;
            MatrixD muzzleLocalMatrix = handler.GetMuzzle.Matrix;
            MatrixD muzzleWorldMatrix = muzzleLocalMatrix * gat.WorldMatrix;
            Vector3D muzzlePosition = muzzleWorldMatrix.Translation;
            Vector3D muzzleForward = muzzleWorldMatrix.Forward;
            muzzleForward.Normalize();
            double muzzleDistance = -handler.MuzzleOffset.Z;
            Vector3D actualMuzzlePosition = muzzlePosition + muzzleForward * muzzleDistance;
            /*CombatUtils.DebugPoint(
                muzzlePosition,
                Color.Red,
                0.10f);
            CombatUtils.DebugPoint(
                actualMuzzlePosition,
                Color.Green,
                0.10f);*/
            var watercheck = Vector3D.Zero;
            if (handler.UseVirtualOffset)
            {
                watercheck = actualMuzzlePosition;
            }
            else
            {
                watercheck = muzzlePosition;
            }
            if (!WaterModAPI.IsUnderwater(watercheck))
            { handler.FireEffects = true; return; }
            if (!gat.Enabled)
                gat.Enabled = true;
            handler.FireEffects = false;
            if (!IsGatlingShot(gat, handler))
                return;
            string effectName = UnderwaterWeaponBurstDatabase.Get(handler.GetWeaponDeff.Id.SubtypeId.String);
            if (string.IsNullOrEmpty(effectName))
                return;
            Vector3D localOffset =
                new Vector3D(
                    handler.MuzzleOffset.X,
                    handler.MuzzleOffset.Y,
                    handler.MuzzleOffset.Z);
            MatrixD effectMatrix = muzzleLocalMatrix;
            effectMatrix.Translation += localOffset;
            Vector3D effectPosition = actualMuzzlePosition;
            CombatUtils.CreateWeaponEffect(gat,effectMatrix,effectPosition,effectName,1f);
        }
        private static void UpdateSmallMissile(IMySmallMissileLauncher mis)
        {
            if (mis == null ||
                mis.Closed ||
                mis.MarkedForClose)
                return;
            AquaWeaponBlockBase handler = mis.GameLogic.GetAs<AquaWeaponBlockBase>();
            /*if (handler == null ||
                !handler.HasMuzzle ||
                handler.GetMuzzle == null)
                return;*/
            if (handler == null)
                return;
            /*Vector3D muzzlePosition = Vector3D.Transform(handler.GetMuzzle.Matrix.Translation, mis.WorldMatrix);
            Vector3D muzzleForward = Vector3D.TransformNormal(handler.GetMuzzle.Matrix.Forward, mis.WorldMatrix);
            muzzleForward.Normalize();
            Vector3D offset = handler.MuzzleOffset;
            double muzzleDistance = -offset.Z;
            Vector3D actualMuzzlePosition = muzzlePosition + muzzleForward * muzzleDistance;
            CombatUtils.DebugPoint(
                muzzlePosition,
                Color.Red,
                0.15f);
            CombatUtils.DebugPoint(
                actualMuzzlePosition,
                Color.Green,
                0.15f);
            if (!WaterModAPI.IsUnderwater(actualMuzzlePosition))
                return;*/
            if (!mis.Enabled)
                mis.Enabled = true;
        }
        private static void UpdateTurret(IMyLargeTurretBase turret)
        {
            if (turret == null || turret.Closed || turret.MarkedForClose)
                return;
            AquaExpansionSession.Insance.CheckUnderwaterBlockRules(turret, true);
        }
        private static void UpdateRailgun(IMySmallMissileLauncher block)
        {
            if (block == null || block.Closed || block.MarkedForClose)
                return;
            AquaExpansionSession.Insance.CheckUnderwaterBlockRules(block, true);
        }
        public static void UpdateHandWeapons(HashSet<IMyAutomaticRifleGun> handweapons)
        {
            foreach (IMyAutomaticRifleGun weapon in handweapons)
            {
                if (weapon == null ||
                    weapon.Closed ||
                    weapon.MarkedForClose)
                    continue;
                Update(weapon);
            }
        }
        public static void UpdatwWeaponBlocks(HashSet<IMySmallGatlingGun> gatlings, HashSet<IMySmallMissileLauncher> missiles)
        {
            foreach (var gat in gatlings)
            {
                if (gat == null ||
                    gat.Closed ||
                    gat.MarkedForClose)
                    continue;
                UpdateSmallGating(gat);
            }
            foreach (var mis in missiles)
            {
                if (mis == null ||
                    mis.Closed ||
                    mis.MarkedForClose)
                    continue;
                UpdateSmallMissile(mis);
            }
        }
        public static void UpdateWeaponTurrets(int tick, HashSet<IMyLargeTurretBase> turrets)
        {
            if (tick % 10 != 0)
                return;
            foreach (var block in turrets)
            {
                if (block == null || block.Closed || block.MarkedForClose)
                    continue;
                UpdateTurret(block);
            }
        }
        public static void UpdateWeaponRailguns(int tick, HashSet<IMySmallMissileLauncher> blocks)
        {
            if (tick % 10 != 0)
                return;
            foreach (var block in blocks)
            {
                if (block == null || block.Closed || block.MarkedForClose)
                    continue;
                UpdateRailgun(block);
            }
        }
    }
}
