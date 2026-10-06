using AquaExpansion.API;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Utils;

namespace AquaExpansionAPITestMod
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation | MyUpdateOrder.BeforeSimulation)]
    public class APITesSession : MySessionComponentBase
    {
        private string System = "AquaTestAPI";
        public static APITesSession I;
        private bool _apiRequested;
        public override void LoadData()
        {
            I = this;
            MyLog.Default.WriteLine($"[AquaTestAPI] LoadData started.");
            //mod
            AquaExpansionAPI.OnAvailable +=OnAquaExpansionAvailable;
            AquaExpansionAPI.Initialize();
        }
        private void RequestProvider()
        {
            if (_apiRequested)
                return;
            if (MyAPIGateway.Session == null)
                return;
            _apiRequested = true;
            AquaExpansionAPI.RequestProvider();
        }
        public override void UpdateBeforeSimulation()
        {
            RequestProvider();
            //MyAPIGateway.Utilities.ShowMessage(System, "Update");
        }
        //Register new ammo profiles
        private void OnAquaExpansionAvailable()
        {
            MyAPIGateway.Utilities.ShowMessage(System,"AquaExpansion API AVAILABLE!");
            //register new ammo profile
            HydroAmmoProfileData profile = new HydroAmmoProfileData();
            profile.SubtypeId = "Missile200mm";
            profile.Mass = 10f;
            profile.DragCoefficient = 0.5f;
            profile.SplashType = AquaSplashType.Missile;
            profile.MinimumSpeed = 20f;
            profile.MaxRange = 3000f;
            profile.WaterStability = 1f;
            profile.ExitVelocityMultiplier = 1f;
            profile.ExitDamageMultiplier = 1f;
            bool result = AquaExpansionAPI.RegisterAmmoProfile(profile);
            MyAPIGateway.Utilities.ShowMessage(System,"Ammo registration: " + result);
            bool bresult = AquaExpansionAPI.RegisterMuzzleBurst("MyHandWeapon", "MyBurstEffect");
            MyAPIGateway.Utilities.ShowMessage(System, "Muzzle registration: " + bresult);
            if (AquaExpansionAPI.RegisterWeaponEffect("MySubtype","MyWeaponEffect"))
            {
                MyAPIGateway.Utilities.ShowMessage(System, "Weapon Effect registration: ");
            }
        }
        //Unload
        protected override void UnloadData()
        {
            AquaExpansionAPI.OnAvailable -=OnAquaExpansionAvailable;
            AquaExpansionAPI.Close();
            I = null;
        }
    }
}
