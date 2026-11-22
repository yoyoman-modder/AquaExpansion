using Digi.NetworkLib;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace AquaNetwork.AquaPackets
{
    [ProtoContract]
    public class AquaDiverInput : PacketBase
    {
        [ProtoMember(1)]
        public Vector3 MoveDirection;
        [ProtoMember(2)]
        public bool JumpPressed;
        [ProtoMember(3)]
        public bool CrouchPressed;
        public void Setup(
            Vector3 moveDirection,
            bool jumpPressed,
            bool crouchPressed)
        {
            MoveDirection = moveDirection;
            JumpPressed = jumpPressed;
            CrouchPressed = crouchPressed;
        }
        public static event ReceiveDelegate<AquaDiverInput> OnReceive;
        public override void Received(ref PacketInfo packetInfo, ulong senderSteamId)
        {
            if (OnReceive != null)
                OnReceive(this, ref packetInfo, senderSteamId);
        }
    }
}
