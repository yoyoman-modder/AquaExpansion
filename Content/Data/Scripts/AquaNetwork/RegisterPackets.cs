using AquaNetwork.AquaPackets;
using ProtoBuf;

namespace Digi.NetworkLib
{
    [ProtoInclude(10, typeof(PacketSimpleExample))]
    [ProtoInclude(11, typeof(AquaPacketNetLog))]
    [ProtoInclude(12, typeof(AquaDiverState))]
    //[ProtoInclude(12, typeof(Etc...))]
    public abstract partial class PacketBase
    {
    }
}