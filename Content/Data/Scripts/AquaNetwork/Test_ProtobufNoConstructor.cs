using AquaNetwork.AquaPackets;
using Digi.NetworkLib;
using Sandbox.ModAPI;
using System;
using VRage.Game.Components;
using VRage.Utils;

namespace AquaNetwork
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class Test_ProtobufNoConstructor : MySessionComponentBase
    {
        public override void BeforeStart()
        {
            /*try
            {
                var packet = new AquaPacketNetLog();
                packet.Setup("hellow");

                var bytes = MyAPIGateway.Utilities.SerializeToBinary(packet);
                var packet2 = MyAPIGateway.Utilities.SerializeFromBinary<PacketBase>(bytes);

                MyAPIGateway.Utilities.ShowMessage("DEBUG", $"packet test succesful; type={packet2.GetType().Name}");
                MyLog.Default.WriteLineAndConsole(
               "[AquaNet] Sending: " + packet2.GetType().Name);
                
            }
            catch (Exception e)
            {
                MyAPIGateway.Utilities.ShowMessage("DEBUG", "error in packet test code!");
                MyLog.Default.WriteLine(e);
            }*/
        }
    }
}
