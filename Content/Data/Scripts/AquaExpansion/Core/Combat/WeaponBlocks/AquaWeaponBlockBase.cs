using Sandbox.Definitions;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace AquaExpansion.Core.Combat.WeaponBlocks
{
    public abstract class AquaWeaponBlockBase : MyGameLogicComponent
    {
        protected AquaWeaponBlockType type;
        private IMySmallGatlingGun gat;
        private IMySmallMissileLauncher missile;
        private IMySmallMissileLauncherReload missilereload;
        private IMyCubeGrid grid;
        private readonly Dictionary<string, IMyModelDummy> dummies = new Dictionary<string, IMyModelDummy>();
        private IMyModelDummy Muzzle;
        private MyWeaponDefinition weapon;
        public int LastShotTick = -1;
        private int internalLastShoottick = -1;
        private Vector3D MuzzlePoint;
        private MatrixD muzzleLocalMatrix;
        private Vector3D actualMuzzlePosition;
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            InitByType();
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME | MyEntityUpdateEnum.EACH_FRAME;
        }
        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            if (!AquaExpansionSession.Insance.HasBalistics)
                return;
            LoadWeaponData();
        }
        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();
        }
        private void GetWeaponDummies()
        {
            Muzzle = null;
            dummies.Clear();
            switch (type)
            {
                case AquaWeaponBlockType.SmallGatlingGun:
                    {
                        if (gat == null ||
                            gat.Closed ||
                            gat.MarkedForClose)
                            return;
                        gat.Model.GetDummies(dummies);
                        // Primary muzzle dummy
                        if (dummies.TryGetValue(WeaponDummiesDatabase.Get(1),out Muzzle))
                        {
                            //AquaExpansionSession.Insance.Log(true, $"origin detected {Muzzle.Name}");
                            return;
                        }
                        // Fallback: barrel subpart used as muzzle reference
                        dummies.TryGetValue(WeaponDummiesDatabase.Get(2),out Muzzle);
                        //AquaExpansionSession.Insance.Log(true, $"alt detected {Muzzle.Name}");
                        break;
                    }
                case AquaWeaponBlockType.SmallMissileLauncher:
                    {
                        if (missile == null ||
                            missile.Closed ||
                            missile.MarkedForClose)
                            return;
                        missile.Model.GetDummies(dummies);
                        break;
                    }
                case AquaWeaponBlockType.SmallMissileLauncherReload:
                    if (missilereload == null ||
                            missilereload.Closed ||
                            missilereload.MarkedForClose)
                        return;
                    missilereload.Model.GetDummies(dummies);
                    /*LogDummyData(missilereload);
                    foreach (var d in dummies)
                    {
                        AquaExpansionSession.Insance.Log(true, $"{d.Key}");
                    }*/
                    break;
            }
        }
        private void LoadWeaponData()
        {
            if (grid?.Physics == null)
                return;
            GetWeaponDummies();
            switch (type)
            {
                case AquaWeaponBlockType.SmallGatlingGun:
                    weapon = CombatUtils.LoadWeaponDefinition(gat);

                    break;
                case AquaWeaponBlockType.SmallMissileLauncher:
                    weapon = CombatUtils.LoadWeaponDefinition(missile);
                    break;
            }
            MuzzlePoint = GetWeaponMuzzleOffset();
        }
        private void InitByType()
        {
            switch (type)
            {
                case AquaWeaponBlockType.SmallGatlingGun:
                    gat = Entity as IMySmallGatlingGun;
                    if (gat == null || gat.Closed || gat.MarkedForClose)
                        return;
                    if (!AquaExpansionSession.Insance.HasBalistics)
                        return;
                    grid = gat.CubeGrid;
                    break;
                case AquaWeaponBlockType.SmallMissileLauncher:
                    missile = Entity as IMySmallMissileLauncher;
                    if (missile == null || missile.Closed || missile.MarkedForClose)
                        return;
                    if (!AquaExpansionSession.Insance.HasBalistics)
                        return;
                    grid = missile.CubeGrid;
                    break;
                case AquaWeaponBlockType.SmallMissileLauncherReload:
                    missilereload = Entity as IMySmallMissileLauncherReload;
                    if (missilereload == null || missilereload.Closed || missilereload.MarkedForClose)
                        return;
                    if (!AquaExpansionSession.Insance.HasBalistics)
                        return;
                    grid = missilereload.CubeGrid;
                    break;
                default:
                    return;
            }
            if (grid == null)
                return;
        }
        public bool HasMuzzle
        {
            get { return Muzzle != null; }
        }
        public IMyModelDummy GetMuzzle
        {
            get { return Muzzle; }
        }
        private Vector3D GetWeaponMuzzleOffset()
        {
            if (weapon == null ||
                weapon.WeaponEffects == null)
                return Vector3D.Zero;
            foreach (var e in weapon.WeaponEffects)
            {
                if (e == null)
                    continue;
                if ((e.Dummy != null && e.Dummy.Contains(WeaponDummiesDatabase.Get(1))))
                {
                    return e.Offset;
                }
            }
            return Vector3D.Zero;
        }
        public MyWeaponDefinition GetWeaponDeff
        {
            get { return weapon; }
        }
        public Vector3D MuzzleOffset
        {
            get
            {
                return MuzzlePoint;
            }
        }
        public bool UseVirtualOffset
        {
            get 
            {
                if (Muzzle.Name.Contains(WeaponDummiesDatabase.Get(1)))
                {
                    //AquaExpansionSession.Insance.Log(true,$"dont use offset {Muzzle.Name}");
                    return false;
                }
                //AquaExpansionSession.Insance.Log(true, $"use offset {Muzzle.Name}");
                return true;
            }
        }
        private void Clear()
        {
            //AquaExpansionSession.Insance.Log(true, $"weapon block closed");
            weapon = null;
            Muzzle = null;
            dummies.Clear();
            grid = null;
            gat = null;
            missile = null;
            missilereload = null;
        }
        private void LogDummyData(IMyTerminalBlock block)
        {
            AquaExpansionSession.Insance.Log(true,
            string.Format(
             "DummyMissileData\n" +
             "Block      : {0}",
             block.BlockDefinition.SubtypeId));
        }
        protected bool isShooting()
        {
            if (gat == null ||
               gat == null ||
               !gat.IsShooting)
                return false;
            int interval = weapon.ShootDirectionUpdateTime;
            int intervalFrames = Math.Max(
                1,
                (int)Math.Ceiling(
                    interval /
                    (MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS * 1000.0)));
            int currentTick = MyAPIGateway.Session.GameplayFrameCounter;
            if (internalLastShoottick >= 0 &&
               currentTick - internalLastShoottick < intervalFrames)
            {
                return false;
            }
            internalLastShoottick = currentTick;
            /*AquaExpansionSession.Insance.Log(
                true,
                $"Weapon interval {interval} intervalframes {intervalFrames}");*/
            return true;
        }
        public bool FireEffects { get; set; }
        private void SetGatlingEffectData()
        {
            muzzleLocalMatrix = GetMuzzle.Matrix;
            MatrixD muzzleWorldMatrix = muzzleLocalMatrix * gat.WorldMatrix;
            Vector3D muzzlePosition = muzzleWorldMatrix.Translation;
            Vector3D muzzleForward = muzzleWorldMatrix.Forward;
            muzzleForward.Normalize();
            double muzzleDistance = -MuzzleOffset.Z;
            actualMuzzlePosition = muzzlePosition + muzzleForward * muzzleDistance;
            var pos = Vector3D.Zero;
            if (UseVirtualOffset)
            {
                pos = actualMuzzlePosition;
            }
            else
            {
                pos = muzzlePosition;
            }
        }
        private void FireEffect()
        {
            string effectName = GlobalEffects.GetWeaponEffect(weapon.Id.SubtypeId.String);
            if (string.IsNullOrEmpty(effectName))
                return;
            Vector3D localOffset =
                new Vector3D(
                    MuzzleOffset.X,
                    MuzzleOffset.Y,
                    MuzzleOffset.Z);
            MatrixD effectMatrix = muzzleLocalMatrix;
            effectMatrix.Translation += localOffset;
            Vector3D effectPosition = actualMuzzlePosition;
            CombatUtils.CreateWeaponEffect(gat, effectMatrix, effectPosition, effectName, 1f);
        }
        protected void UpdateFireEffects()
        {
            if (!AquaExpansionSession.Insance.HasBalistics)
                return;
            if (gat == null || gat.Closed || gat.MarkedForClose)
                return;
            if (grid == null || grid.Closed || grid.MarkedForClose)
                return;
            if (grid.Physics == null)
                return;
            if (!FireEffects)
                return;
            SetGatlingEffectData();
            if (!isShooting())
                return;
            FireEffect();
        }
        public override void Close()
        {
            Clear();
            base.Close();
        }
    }
}
