using Digi.NetworkLib;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AquaNetwork.AquaPackets
{
    [ProtoContract]
    public class AquaDiverState : PacketBase
    {
        public AquaDiverState()
        {
        }
        [ProtoMember(1)]
        public long IdentityId;
        [ProtoMember(2)]
        public int GearLevel;
        [ProtoMember(3)]
        public bool OxygenRefillActive;
        public void Setup(
            long identityId,
            int gearLevel,
            bool oxygenRefillActive)
        {
            IdentityId = identityId;
            GearLevel = gearLevel;
            OxygenRefillActive = oxygenRefillActive;
        }
        public static event ReceiveDelegate<AquaDiverState> OnReceive;
        public override void Received(ref PacketInfo packetInfo,ulong senderSteamId)
        {
            if (OnReceive != null)
                OnReceive(this,ref packetInfo,senderSteamId);
        }
    }
}
