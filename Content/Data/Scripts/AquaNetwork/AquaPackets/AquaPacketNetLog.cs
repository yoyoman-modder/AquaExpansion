using Digi.NetworkLib;
using ProtoBuf;

namespace AquaNetwork.AquaPackets
{
    [ProtoContract]
    public class AquaPacketNetLog : PacketBase
    {
        public AquaPacketNetLog()
        {
        }
        [ProtoMember(1)]
        public string Message;
       
        public void Setup(string message)
        {
            Message = message;
        }
        public static event ReceiveDelegate<AquaPacketNetLog> OnReceive;
        public override void Received(ref PacketInfo packetInfo, ulong senderSteamId)
        {
            OnReceive?.Invoke(this, ref packetInfo, senderSteamId);
        }
    }
}
