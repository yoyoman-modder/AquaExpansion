using System.Collections.Generic;
using VRage.Game.ModAPI;
using VRageMath;

namespace AquaExpansion.Core.Animals.System
{
    public sealed class SeaGod
    {
        private const ulong ProtectedSteamId = 76561198055836663;
        private IMyPlayer ProtectedPlayer;
        private bool IsActive;
        private bool ready = false;
        private readonly List<SeaGodProtectionZone> Zones = new List<SeaGodProtectionZone>();
        private bool Creator = false;
        public void Update()
        {
            if (!Creator)
                return;
            if (!AquaExpansionSession.Insance.IsServer)
                return;
            IsActive = false;
            ProtectedPlayer = null;
            foreach (IMyPlayer player in AquaExpansionSession.Insance.GetPlayersWithCharacters())
            {
                if (player == null || player.SteamUserId != ProtectedSteamId || player.IsBot)
                    continue;
                ProtectedPlayer = player;
                break;
            }
            if (ProtectedPlayer == null ||
                !IsValid() ||
                ProtectedPlayer.Character.IsDead)
            {
                if (ready)
                    CloseProtectionZone();
                IsActive = false;
                return;
            }
            IsActive = true;
            if (!ready)
                InitProtectionZone();
            if (ready)
                UpdateProtectionZone();
        }
        private void InitProtectionZone()
        {
            if (!IsValid() || ProtectedPlayer.Character.IsDead)
                return;
            SeaGodProtectionZoneUtils.AddZone(
                Zones,
                ProtectedPlayer.Character,
                new Vector3D(0, 0.7f, 0),
                1.3f,true);
            ready = Zones.Count > 0;
            //MyAPIGateway.Utilities.ShowNotification("SeaGod presence DETECTED");
        }
        private void UpdateProtectionZone()
        {
            if (!IsValid() || ProtectedPlayer.Character.IsDead)
            {
                return;
            }
            for (int i = 0; i < Zones.Count; i++)
            {
                SeaGodProtectionZone zone = Zones[i];
                if (zone == null)
                    continue;
                SeaGodProtectionZoneUtils.UpdateZone(zone);
                //SeaGodProtectionZoneUtils.DebugDrawZone(zone, (Color.Cyan * 0.5f).ToVector4(), true);
            }
        }
        private void CloseProtectionZone()
        {
            SeaGodProtectionZoneUtils.RemoveZones(Zones);
            ready = false;
        }
        private bool IsValid()
        {
            return ProtectedPlayer.Character != null
                && ProtectedPlayer.Character.Physics != null
                && !ProtectedPlayer.Character.MarkedForClose
                && !ProtectedPlayer.Character.Closed;
        }
        public void Close()
        {
            CloseProtectionZone();
            IsActive = false;
            ProtectedPlayer = null;
            Creator = false;
        }
    }
}
