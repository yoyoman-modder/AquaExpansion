using AquaNetwork.AquaPackets;
using ProtoBuf;

namespace Digi.NetworkLib
{
    [ProtoInclude(10, typeof(PacketSimpleExample))]
    [ProtoInclude(19, typeof(AquaPacketNetLog))]
    //[ProtoInclude(11, typeof(SomeOtherPacketClass))]
    //[ProtoInclude(12, typeof(Etc...))]
    public abstract partial class PacketBase
    {
    }
}