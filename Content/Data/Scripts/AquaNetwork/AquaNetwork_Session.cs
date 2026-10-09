using AquaNetwork.AquaPackets;
using Digi.NetworkLib;
using Sandbox.ModAPI;
using System;
using VRage.Game.Components;
using VRage.Input;
using VRage.Library.Utils;
using VRage.Utils;

namespace AquaNetwork
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class AquaNetwork_Session : MySessionComponentBase
    {
        // IMPORTANT: other mods using the same ID will send packets to you and receive your packets, which likely means deserialization errors.
        // Therefore pick a unique ID, one way is your workshopID % ushort.MaxValue, or just pick a random one that's higher than the low numbers (keen might be using those).
        public const ushort NetworkId = (ushort)(3782581813 % ushort.MaxValue);
        public static AquaNetwork_Session NetInstance;
        public Network AquaNet;
        private PacketSimpleExample PacketExample;
        private AquaDiverState AquaDiverState;
        private int NetLogTestTick;
        public override void LoadData()
        {
            // The ID in this must be unique between other mods.
            // Usually suggested to be the last few numbers of your workshopId.
            NetInstance = this;
            AquaNet = new Network(NetworkId, ModContext.ModName);
            // Also don't create multiple instances of Network (like instancing it in gamelogic, that would be very bad).
            // If you want errors to use your logger then you can do:
            //Net.ExceptionHandler = (e) => Log.Error(e);
            //Net.ErrorHandler = (msg) => Log.Error(msg);
            // To test if serialization works in singleplayer when using SendToServer().
            AquaNet.SerializeTest = true;
            // Re-usable for sending
            PacketExample = new PacketSimpleExample();
            AquaDiverState = new AquaDiverState();
            // For receiving (will be a different instance than the sending one because the receiver code creates it from bytes)
            // because this is a global event you should only hook it in global cases
            PacketSimpleExample.OnReceive += PacketSimpleExample_OnReceive;
            AquaDiverState.OnReceive += AquaDiverState_OnReceive;
            // For packets that are for a specific entity you still should do the event hooking here in session comp,
            //   but you can still trigger code on the entity once you have its instance.
            //   Refer to the commented-out example at the end of this file.
        }

        private void AquaDiverState_OnReceive(AquaDiverState packet, ref PacketInfo packetInfo, ulong senderSteamId)
        {
            MyLog.Default.WriteLineAndConsole(
           "[AquaNet] Received DiverState: Identity=" +
           packet.IdentityId +
           " Gear=" +
           packet.GearLevel +
           " O2=" +
           packet.OxygenRefillActive +
           " Sender=" +
           senderSteamId);
            MyAPIGateway.Utilities.ShowMessage(
       "AquaNet",
       "DiverState received: Gear=" +
       packet.GearLevel +
       " O2=" +
       packet.OxygenRefillActive);
        }
        public void SendDiverState(ulong steamId,long identityId,int gearLevel,bool oxygenRefillActive)
        {
            if (AquaNet == null ||
                AquaDiverState == null)
                return;
            AquaDiverState.Setup(
                identityId,
                gearLevel,
                oxygenRefillActive);
            MyLog.Default.WriteLineAndConsole(
        "[AquaNet] Sending DiverState: Identity=" +
        identityId +
        " Gear=" +
        gearLevel +
        " O2=" +
        oxygenRefillActive +
        " SteamId=" +
        steamId);
            AquaNet.SendToPlayer(AquaDiverState,steamId);

        }
        private void ClearAquaPackets()
        {
            if (AquaDiverState != null)
            {
                AquaDiverState.OnReceive -= AquaDiverState_OnReceive;
                AquaDiverState = null;
            }
        }
        protected override void UnloadData()
        {
            PacketSimpleExample.OnReceive -= PacketSimpleExample_OnReceive;
            PacketExample = null;
            ClearAquaPackets();
            AquaNet?.Dispose();
            AquaNet = null;
            NetInstance = null;
        }
        public override void UpdateAfterSimulation()
        {
            // example for testing in-game, press L in a world with this mod loaded

          
        }

        void PacketSimpleExample_OnReceive(PacketSimpleExample packet, ref PacketInfo packetInfo, ulong senderSteamId)
        {
            // This is called on everyone that receives the packet.
            //
            // packet.OriginalSenderSteamId is the original sender of the packet and validated serverside to ensure it's not spoofed.
            // Your defined data is in the packet. variable, in this example would be Text and Number fields.
            //
            // Things in packetInfo. can be set depdending on what you want to happen when server receives this packet:
            //
            // packetInfo.Relay = RelayMode.<value> -- to decide if the packet is sent to other players automatically.
            // The way you do stuff in packets depends on how the action works.
            // A few practical examples:
            //  - an action that only works serverside and from there the game automatically synchronizes it, for this you'd use Relay.None (or just not set it, this is the default).
            //  - an action that is needed locally on all players:
            //   - you did the action on sender: Relay.ToOthers
            //   - you only do the action in here: Relay.ToEveryone - which will send to sender too; this way is also nice to validate if sync works while alone in a DS.
            //  - an action that needs to be done on a specific player, leave relaying off and use Net.SendToPlayer(), you do need to have the target player's steamId as part of the packet.
            //
            // packetInfo.Reserialize -- set true you modified the packet, niche purpose.


            string msg = $"[Example] Received {packet.GetType().Name}: text={packet.Text}; number={packet.Number}";
            MyLog.Default.WriteLineAndConsole(msg);

            if(MyAPIGateway.Session.Player != null)
            {
                MyAPIGateway.Utilities.ShowNotification(msg);
            }


            // to see how this works in practice, try it in both singleplayer (you're the server) and as a MP client in a dedicated server (you can start one from steam tools).
            packetInfo.Relay = RelayMode.ToEveryone;


            // example of changing the data serverside before relaying to clients.
            packet.Text = "modified text";
            packetInfo.Reserialize = true;
        }

        /*
        void PacketForSomeEntity_OnReceive(PacketForSomeEntity packet, ref PacketInfo packetInfo, ulong senderSteamId)
        {
            IMyEntity ent = MyEntities.GetEntityById(packet.EntityId);
            if(ent == null)
            {
                // log some error if this is unexpected, but do remember that clients do NOT have all entities available to them, only server does.
                return;
            }

            // from here if you have a gamelogic component on that entity you can do something like:
            var logic = ent.GameLogic?.GetAs<YourGameLogicClass>();
            if(logic == null)
            {
                return;
            }

            logic.ReceivedStuff(packet.Stuff);
        }
        */
    }
}
