using AquaExpansion.Core;
using AquaExpansion.Core.Combat;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game.Components;
using VRage.Utils;

namespace AquaExpansion.API
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class AquaExpansionAPIProvider : MySessionComponentBase
    {
        public const ushort API_REQUEST_ID = 8888;
        public const ushort API_RESPONSE_ID = 8889;
        public const int API_VERSION = 1;
        private const string REQUEST_NAME = "AquaExpansionAPI";
        private bool _registered;
        private bool _unloading;
        private readonly Dictionary<string, Delegate> _apiMethods = new Dictionary<string, Delegate>(StringComparer.OrdinalIgnoreCase);
        // =====================================================
        // EXTERNAL AMMO OWNERSHIP
        // =====================================================
        private readonly Dictionary<string, string> _externalAmmoOwners = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _externalburstOwners = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _externalweaponeffectOwners = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        // =====================================================
        // LOAD
        // =====================================================
        public override void LoadData()
        {
            _unloading = false;
            _registered = false;
            _apiMethods.Clear();
            _externalAmmoOwners.Clear();
            _externalburstOwners.Clear();
            _externalweaponeffectOwners.Clear();
            MyLog.Default.WriteLine(
                "[AquaExpansion API] Provider LoadData.");
            BuildApiMethods();
            if (MyAPIGateway.Utilities == null)
            {
                MyLog.Default.WriteLine(
                    "[AquaExpansion API] " +
                    "Utilities is NULL.");
                return;
            }
            MyAPIGateway.Utilities.RegisterMessageHandler(API_REQUEST_ID,MessageHandler);
            _registered = true;
            MyLog.Default.WriteLine(
                "[AquaExpansion API] Provider registered. " +
                "RequestID=" + API_REQUEST_ID +
                " ResponseID=" + API_RESPONSE_ID);
            MyAPIGateway.Utilities.ShowMessage("AquaAPI","Provider registered.");
        }
        // =====================================================
        // UNLOAD
        // =====================================================
        protected override void UnloadData()
        {
            _unloading = true;
            MyLog.Default.WriteLine(
                "[AquaExpansion API] Provider unloading.");
            if (_registered)
            {
                try
                {
                    if (MyAPIGateway.Utilities != null)
                    {
                        MyAPIGateway.Utilities.UnregisterMessageHandler(API_REQUEST_ID,MessageHandler);
                    }
                }
                catch (Exception e)
                {
                    MyLog.Default.WriteLine(
                        "[AquaExpansion API] " +
                        "Error unregistering handler:");

                    MyLog.Default.WriteLine(e.ToString());
                }
                _registered = false;
            }
            _apiMethods.Clear();
            _externalAmmoOwners.Clear();
            _externalburstOwners.Clear();
            _externalweaponeffectOwners.Clear();
            MyLog.Default.WriteLine(
                "[AquaExpansion API] Provider unloaded.");
        }
        // =====================================================
        // API METHODS
        // =====================================================
        private void BuildApiMethods()
        {
            _apiMethods.Clear();
            _apiMethods.Add("VerifyVersion",new Func<int, bool>(VerifyVersion));
            _apiMethods.Add("RegisterAmmoProfile",new Func<Dictionary<string, object>, bool>(RegisterAmmoProfile));
            _apiMethods.Add("UnregisterAmmoProfile",new Func<Dictionary<string, object>, bool>(UnregisterAmmoProfile));
            _apiMethods.Add("HasAmmoProfile",new Func<string, bool>(HasAmmoProfile));
            _apiMethods.Add("GetAmmoOwner",new Func<string, string>(GetAmmoOwner));
            _apiMethods.Add("GetAmmoProfile",new Func<string, HydroAmmoProfileData>(GetAmmoProfile));
            _apiMethods.Add("HasMuzzleBurst", new Func<string, bool>(HasMuzzleBurst));
            _apiMethods.Add("GetMuzzleBurstOwner", new Func<string, string>(GetMuzzleBurstOwner));
            _apiMethods.Add("RegisterMuzzleBurst", new Func<Dictionary<string, object>, bool>(RegisterMuzzleBurst));
            _apiMethods.Add("GetWeaponEffectOwner", new Func<string, string>(GetWeaponEffectOwner));
            _apiMethods.Add("RegisterWeaponEffect", new Func<Dictionary<string, object>, bool>(RegisterWeaponEffect));
            MyLog.Default.WriteLine(
                "[AquaExpansion API] API methods created: " +
                _apiMethods.Count);
        }
        // =====================================================
        // MESSAGE HANDLER
        // =====================================================
        private void MessageHandler(object message)
        {
            if (_unloading)
                return;
            if (message == null)
                return;
            string request = message as string;
            if (request == null)
                return;
            if (!string.Equals(request,REQUEST_NAME,StringComparison.Ordinal))
            {
                return;
            }
            MyLog.Default.WriteLine(
                "[AquaExpansion API] " +
                "Client request received.");
            if (_unloading)
                return;
            try
            {
                MyAPIGateway.Utilities.SendModMessage(API_RESPONSE_ID,_apiMethods);
                MyLog.Default.WriteLine(
                    "[AquaExpansion API] " +
                    "API response sent.");
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine(
                    "[AquaExpansion API] " +
                    "Failed to send API response:");
                MyLog.Default.WriteLine(e.ToString());
            }
        }
        // =====================================================
        // VERSION
        // =====================================================
        private bool VerifyVersion(int version)
        {
            if (_unloading)
                return false;
            bool result = version == API_VERSION;
            MyLog.Default.WriteLine(
                "[AquaExpansion API] " +
                "Version check: " +
                version +
                " -> " +
                result);
            return result;
        }
        // =====================================================
        // REGISTER AMMO
        // =====================================================
        private bool RegisterAmmoProfile(Dictionary<string, object> data)
        {
            if (_unloading)
                return false;
            if (data == null)
                return false;
            string subtypeId = GetString(data, "SubtypeId");
            string owner = GetString(data, "Owner");
            if (string.IsNullOrWhiteSpace(subtypeId))
            {
                MyLog.Default.WriteLine(
                    "[AquaExpansion API] " +
                    "External registration rejected: " +
                    "empty SubtypeId.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(owner))
            {
                MyLog.Default.WriteLine(
                    "[AquaExpansion API] " +
                    "External registration rejected: " +
                    "empty owner.");
                return false;
            }
            subtypeId = subtypeId.Trim();
            owner = owner.Trim();
            // =================================================
            // DUPLICATE / OWNERSHIP CHECK
            // =================================================
            string existingOwner;
            if (_externalAmmoOwners.TryGetValue(subtypeId,out existingOwner))
            {
                // Same mod can update its own profile.
                if (!string.Equals(existingOwner,owner,StringComparison.OrdinalIgnoreCase))
                {
                    MyLog.Default.WriteLine(
                        "[AquaExpansion API] " +
                        "Ammo registration REJECTED. " +
                        "Subtype '" +
                        subtypeId +
                        "' is already owned by '" +
                        existingOwner +
                        "'. Requested by '" +
                        owner +
                        "'.");
                    return false;
                }
                MyLog.Default.WriteLine(
                    "[AquaExpansion API] " +
                    "Existing owner updating ammo: " +
                    subtypeId +
                    " / " +
                    owner);
            }
            try
            {
                HydroAmmoProfile profile = new HydroAmmoProfile(
                        subtypeId,
                        GetFloat(data, "Mass"),
                        GetFloat(data,"DragCoefficient"),
                        (SplashType)GetInt(data,"SplashType"),
                        GetFloat(data,"MinimumSpeed"),
                        GetFloat(data,"MaxRange"),
                        GetFloat(data,"WaterStability"),
                        GetFloat(data,"EnergyLossCoefficient"),
                        GetFloat(data,"ProjectileMassDamage"),
                        GetFloat(data,"ProjectileHealthDamage"),
                        GetFloat(data,"ExitVelocityMultiplier"),
                        GetFloat(data,"ExitDamageMultiplier"),
                        GetString(data,"TrailEffect"),
                        GetFloat(data,"UnderwaterTurnMultiplier"),
                        GetFloat(data,"EngineAcceleration"),
                        GetFloat(data,"UnderwaterEngineMultiplier"),
                        GetInt(data, "Torpedo"));
                bool result = HydroAmmoDatabase.RegisterAmmoProfile(profile);
                if (!result)
                {
                    MyLog.Default.WriteLine(
                        "[AquaExpansion API] " +
                        "Database rejected ammo: " +
                        subtypeId);
                    return false;
                }
                // Only claim ownership after successful registration.
                _externalAmmoOwners[subtypeId] = owner;
                MyLog.Default.WriteLine(
                    "[AquaExpansion API] " +
                    "External ammo registered: " +
                    subtypeId +
                    " Owner=" +
                    owner);
                return true;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine(
                    "[AquaExpansion API] " +
                    "Error registering external ammo: " +
                    subtypeId);
                MyLog.Default.WriteLine(e.ToString());
                return false;
            }
        }
        // =====================================================
        // UNREGISTER AMMO
        // =====================================================
        private bool UnregisterAmmoProfile(Dictionary<string, object> data)
        {
            if (_unloading)
                return false;
            if (data == null)
                return false;
            string subtypeId = GetString(data, "SubtypeId");
            string owner = GetString(data, "Owner");
            if (string.IsNullOrWhiteSpace(subtypeId) || string.IsNullOrWhiteSpace(owner))
            {
                return false;
            }
            subtypeId = subtypeId.Trim();
            owner = owner.Trim();
            string existingOwner;
            if (!_externalAmmoOwners.TryGetValue(subtypeId,out existingOwner))
            {
                MyLog.Default.WriteLine(
                    "[AquaExpansion API] " +
                    "Unregister rejected: " +
                    "no external owner for " +
                    subtypeId);
                return false;
            }
            // Only the owner can remove it.
            if (!string.Equals(existingOwner,owner,StringComparison.OrdinalIgnoreCase))
            {
                MyLog.Default.WriteLine(
                    "[AquaExpansion API] " +
                    "Unregister rejected: " +
                    subtypeId +
                    " belongs to " +
                    existingOwner);
                return false;
            }
            try
            {
                bool result = HydroAmmoDatabase.UnregisterExternalAmmoProfile(subtypeId);
                if (result)
                {
                    _externalAmmoOwners.Remove(subtypeId);
                    MyLog.Default.WriteLine(
                        "[AquaExpansion API] " +
                        "External ammo unregistered: " +
                        subtypeId +
                        " Owner=" +
                        owner);
                }
                return result;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine(
                    "[AquaExpansion API] " +
                    "Error unregistering ammo: " +
                    subtypeId);
                MyLog.Default.WriteLine(e.ToString());
                return false;
            }
        }
        // =====================================================
        // CHECK AMMO
        // =====================================================
        private bool HasAmmoProfile(string subtypeId)
        {
            if (_unloading)
                return false;
            if (string.IsNullOrWhiteSpace(subtypeId))
            {
                return false;
            }
            return HydroAmmoDatabase.HasAmmoProfile(subtypeId.Trim());
        }

        // =====================================================
        // GET OWNER
        // =====================================================
        private string GetAmmoOwner(string subtypeId)
        {
            if (_unloading)
                return string.Empty;
            if (string.IsNullOrWhiteSpace(subtypeId))
            {
                return string.Empty;
            }
            string owner;
            if (_externalAmmoOwners.TryGetValue(subtypeId.Trim(),out owner))
            {
                return owner;
            }
            return string.Empty;
        }
        // Get Ammo Profile
        private HydroAmmoProfileData GetAmmoProfile(string subtypeId)
        {
            HydroAmmoProfile profile = HydroAmmoDatabase.GetAmmoProfile(subtypeId);
            if (profile == null)
                return null;
            return new HydroAmmoProfileData
            {
                SubtypeId = profile.SubtypeId,
                Mass = profile.Mass,
                DragCoefficient = profile.DragCoefficient,
                SplashType = (AquaSplashType)profile.SplashType,
                MinimumSpeed = profile.MinimumSpeed,
                MaxRange = profile.MaxRange,
                WaterStability = profile.WaterStability,
                EnergyLossCoefficient = profile.EnergyLossCoefficient,
                ProjectileMassDamage = profile.ProjectileMassDamage,
                ProjectileHealthDamage = profile.ProjectileHealthDamage,
                ExitVelocityMultiplier = profile.ExitVelocityMultiplier,
                ExitDamageMultiplier = profile.ExitDamageMultiplier,
                TrailEffect = profile.TrailEffect,
                UnderwaterTurnMultiplier = profile.UnderwaterTurnMultiplier,
                EngineAcceleration = profile.EngineAcceleration,
                UnderwaterEngineMultiplier = profile.UnderwaterEngineMultiplier,
                Torpedo = profile.Torpedo
            };
        }

        // Check Muzzle effect
        private bool HasMuzzleBurst(string subtypeId)
        {
            if (_unloading)
                return false;
            if (string.IsNullOrWhiteSpace(subtypeId))
            {
                return false;
            }
            return UnderwaterWeaponBurstDatabase.HasMuzzleBurst(subtypeId);
        }
        private string GetMuzzleBurstOwner(string subtypeId)
        {
            if (_unloading)
                return string.Empty;
            if (string.IsNullOrWhiteSpace(subtypeId))
            {
                return string.Empty;
            }
            string owner;
            if (_externalburstOwners.TryGetValue(subtypeId.Trim(), out owner))
            {
                return owner;
            }
            return string.Empty;
        }
        private bool RegisterMuzzleBurst(Dictionary<string, object> data)
        {
            if (_unloading)
                return false;
            if (data == null)
                return false;
            string subtypeId = GetString(data, "SubtypeId");
            string effect = GetString(data, "Effect");
            string owner = GetString(data, "Owner");
            if (string.IsNullOrWhiteSpace(subtypeId))
            {
                MyLog.Default.WriteLine(
                    "[AquaExpansion API] " +
                    "External muzzle burst registration rejected: " +
                    "empty SubtypeId.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(effect))
            {
                MyLog.Default.WriteLine(
                    "[AquaExpansion API] " +
                    "External muzzle burst registration rejected: " +
                    "empty Effect.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(owner))
            {
                MyLog.Default.WriteLine(
                    "[AquaExpansion API] " +
                    "External muzzle burst registration rejected: " +
                    "empty Owner.");
                return false;
            }
            subtypeId = subtypeId.Trim();
            effect = effect.Trim();
            owner = owner.Trim();
            // =================================================
            // DUPLICATE / OWNERSHIP CHECK
            // =================================================
            string existingOwner;
            if (_externalburstOwners.TryGetValue(subtypeId,out existingOwner))
            {
                MyLog.Default.WriteLine(
                    "[AquaExpansion API] " +
                    "Muzzle burst registration REJECTED. " +
                    "Subtype '" +
                    subtypeId +
                    "' is already owned by '" +
                    existingOwner +
                    "'. Requested by '" +
                    owner +
                    "'.");
                return false;
            }
            // =================================================
            // REGISTER
            // =================================================
            try
            {
                if (!UnderwaterWeaponBurstDatabase.RegisterExternal(subtypeId,effect))
                {
                    MyLog.Default.WriteLine(
                        "[AquaExpansion API] " +
                        "Database rejected external muzzle burst: " +
                        subtypeId);
                    return false;
                }
                _externalburstOwners.Add(subtypeId,owner);
                MyLog.Default.WriteLine(
                    "[AquaExpansion API] " +
                    "External muzzle burst registered: " +
                    subtypeId +
                    " -> " +
                    effect +
                    " | Owner: " +
                    owner);
                return true;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine(
                    "[AquaExpansion API] " +
                    "Error registering external muzzle burst: " +
                    subtypeId);
                MyLog.Default.WriteLine(e.ToString());
                return false;
            }
        }
        //WeaponEffects
        private string GetWeaponEffectOwner(string subtypeId)
        {
            if (_unloading)
                return string.Empty;
            if (string.IsNullOrWhiteSpace(subtypeId))
            {
                return string.Empty;
            }
            string owner;
            if (_externalweaponeffectOwners.TryGetValue(subtypeId.Trim(), out owner))
            {
                return owner;
            }
            return string.Empty;
        }
        private bool RegisterWeaponEffect(Dictionary<string, object> data)
        {
            if (_unloading)
                return false;
            if (data == null)
                return false;
            string subtypeId = GetString(data, "SubtypeId");
            string effect = GetString(data, "Effect");
            string owner = GetString(data, "Owner");
            if (string.IsNullOrWhiteSpace(subtypeId))
            {
                MyLog.Default.WriteLine(
                    "[AquaExpansion API] " +
                    "External weapon effect registration rejected: " +
                    "empty SubtypeId.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(effect))
            {
                MyLog.Default.WriteLine(
                    "[AquaExpansion API] " +
                    "External weapon effect registration rejected: " +
                    "empty Effect.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(owner))
            {
                MyLog.Default.WriteLine(
                    "[AquaExpansion API] " +
                    "External weapon effect registration rejected: " +
                    "empty Owner.");
                return false;
            }
            subtypeId = subtypeId.Trim();
            effect = effect.Trim();
            owner = owner.Trim();
            // =================================================
            // DUPLICATE / OWNERSHIP CHECK
            // =================================================
            string existingOwner;
            if (_externalweaponeffectOwners.TryGetValue(subtypeId, out existingOwner))
            {
                MyLog.Default.WriteLine(
                    "[AquaExpansion API] " +
                    "Weapon effect registration REJECTED. " +
                    "Subtype '" +
                    subtypeId +
                    "' is already owned by '" +
                    existingOwner +
                    "'. Requested by '" +
                    owner +
                    "'.");
                return false;
            }
            // =================================================
            // REGISTER
            // =================================================
            try
            {
                if (!GlobalEffects.RegisterExternalWeaponEffect(subtypeId, effect))
                {
                    MyLog.Default.WriteLine(
                        "[AquaExpansion API] " +
                        "Database rejected external Weapon effect: " +
                        subtypeId);
                    return false;
                }
                _externalweaponeffectOwners.Add(subtypeId, owner);
                MyLog.Default.WriteLine(
                    "[AquaExpansion API] " +
                    "External weapon effect registered: " +
                    subtypeId +
                    " -> " +
                    effect +
                    " | Owner: " +
                    owner);
                return true;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine(
                    "[AquaExpansion API] " +
                    "Error registering external weapon effect: " +
                    subtypeId);
                MyLog.Default.WriteLine(e.ToString());
                return false;
            }
        }
        // =====================================================
        // DATA HELPERS
        // =====================================================
        private static string GetString(Dictionary<string, object> data,string key)
        {
            object value;
            if (!data.TryGetValue(key,out value))
            {
                return string.Empty;
            }
            return value as string ?? string.Empty;
        }
        private static float GetFloat(Dictionary<string, object> data,string key)
        {
            object value;
            if (!data.TryGetValue(key,out value))
            {
                return 0f;
            }
            if (value is float)
                return (float)value;
            if (value is double)
                return (float)(double)value;
            if (value is int)
                return (int)value;
            if (value is long)
                return (float)(long)value;
            if (value is decimal)
                return (float)(decimal)value;
            return 0f;
        }
        private static int GetInt(Dictionary<string, object> data,string key)
        {
            object value;
            if (!data.TryGetValue(key,out value))
            {
                return 0;
            }
            if (value is int)
                return (int)value;
            if (value is byte)
                return (byte)value;
            if (value is short)
                return (short)value;
            if (value is long)
                return (int)(long)value;
            return 0;
        }
    }
}