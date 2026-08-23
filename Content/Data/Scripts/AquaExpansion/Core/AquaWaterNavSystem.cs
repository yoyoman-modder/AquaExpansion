using Jakaria.API;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace AquaExpansionExperimental.Core
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class AquaWaterNavSystem : MySessionComponentBase
    {
        public static AquaWaterNavSystem I;
        private static int SectorSize = 64;
        private static int NodeSpacing = 16;
        private double BuildRadius = 128;
        private double UnloadRadius = 256;
        private double DebugDistance = 128 * 128;
        private int HorizontalRadius = 2;
        private int VerticalRadius = 1;
        private long Tick;
        private readonly Dictionary<Vector3I, WaterSector> loaded = new Dictionary<Vector3I, WaterSector>();
        private readonly HashSet<Vector3I> pending = new HashSet<Vector3I>();
        private readonly ConcurrentQueue<WaterSector> completed = new ConcurrentQueue<WaterSector>();
        private bool showNodes = false;
        private bool showConnections = false;
        public override void LoadData()
        {
            I = this;
            //MyAPIGateway.Utilities.MessageEntered += OnMessageEntered;
            base.LoadData();
        }
        private void OnMessageEntered(string messageText, ref bool sendToOthers)
        {
            ChatNodes(messageText, sendToOthers);
            ChatConnections(messageText, sendToOthers);
        }
        private void ChatNodes(string ChatMessage, bool sendToOthers)
        {
            string locChatcommand = ChatMessage;
            bool locsend2others = sendToOthers;
            if (string.IsNullOrEmpty(locChatcommand))
                return;
            if (!locChatcommand.Equals("/AquaNodes"))
                return;
            locsend2others = false;
            IMyPlayer player = MyAPIGateway.Session?.LocalHumanPlayer;
            var controlled = player?.Controller?.ControlledEntity?.Entity;
            if (controlled == null)
                return;
            if (!showNodes)
            {
                showNodes = true;
            }
            else
            {
                showNodes = false;
            }
        }
        private void ChatConnections(string ChatMessage, bool sendToOthers)
        {
            string locChatcommand = ChatMessage;
            bool locsend2others = sendToOthers;
            if (string.IsNullOrEmpty(locChatcommand))
                return;
            if (!locChatcommand.Equals("/AquaConnections"))
                return;
            locsend2others = false;
            IMyPlayer player = MyAPIGateway.Session?.LocalHumanPlayer;
            var controlled = player?.Controller?.ControlledEntity?.Entity;
            if (controlled == null)
                return;
            if (!showConnections)
            {
                showConnections = true;
            }
            else
            {
                showConnections = false;
            }
        }
        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            base.Init(sessionComponent);
            //AquaExpansionSession.Insance.Log(true, $"AquaWaterNavSystem initailized");
        }
        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();
            Update();
        }
        /// <summary>
        /// Update system by streaming sectors around the player.
        /// </summary>
        private void Update()
        {
            Tick++;
            var player = MyAPIGateway.Session?.Player?.Character;
            if (player == null)
                return;
            bool underwater = WaterModAPI.IsUnderwater(player.GetPosition());
            if (!underwater)
            {
                if (loaded.Count > 0 || pending.Count > 0 || completed.Count > 0)
                {
                    loaded.Clear();
                    pending.Clear();
                    WaterSector sector;
                    while (completed.TryDequeue(out sector))
                    {
                    }
                    //AquaExpansionSession.Insance.Log(true,"Water navigation unloaded");
                }
                return;
            }
            if (Tick % 60 == 0)
            {
                StreamSectors();
            }
            if (Tick % 300 == 0)
            {
                UnloadFarSectors();
            }
            ApplyCompleted();
            DebugDraw();
            DebugSectors();
        }
        /// <summary>
        /// Debug sectors
        /// </summary>
        private void DebugSectors()
        {
            int totalNodes = 0;
            foreach (var sector in loaded.Values)
            {
                totalNodes += sector.Nodes.Count;
            }
            //AquaExpansionSession.Insance.Log(true,$"Loaded={loaded.Count} Nodes={totalNodes}");
        }
        /// <summary>
        /// Debug draw water nodes.
        /// </summary>
        private void DebugDraw()
        {
            if (!showNodes && !showConnections)
                return;
            var player = MyAPIGateway.Session?.Player?.Character;
            if (player == null)
                return;
            Vector3D pos = player.GetPosition();
            MyPlanet planet = MyGamePruningStructure.GetClosestPlanet(player.GetPosition());
            foreach (var sector in loaded.Values)
            {
                foreach (var node in sector.Nodes)
                {
                    if (Vector3D.DistanceSquared(pos, node.Position) > DebugDistance)
                        continue;
                    //
                    // Draw node cube
                    //
                    if (showNodes)
                    {
                        Vector3D up = Vector3D.Normalize(node.Position - planet.PositionComp.WorldAABB.Center);
                        Vector3D forward = Vector3D.CalculatePerpendicularVector(up);
                        Vector3D right = Vector3D.Cross(forward, up);
                        MatrixD world = MatrixD.Identity;
                        world.Right = right;
                        world.Up = up;
                        world.Forward = forward;
                        world.Translation = node.Position;
                        BoundingBoxD box = new BoundingBoxD(new Vector3D(-0.5), new Vector3D(0.5));
                        Color c =
                             node.Neighbors.Count <= 2 ? Color.Red :
                             node.Neighbors.Count <= 4 ? Color.Yellow :
                             Color.Green;
                        MySimpleObjectDraw.DrawTransparentBox(
                            ref world,
                             ref box,
                             ref c,
                            MySimpleObjectRasterizer.Wireframe,
                            1,
                            0.02f,
                            MyStringId.GetOrCompute("Square"),
                            MyStringId.GetOrCompute("Square"),
                            false,
                             -1,
                            MyBillboard.BlendTypeEnum.PostPP,
                            5.0f
                        );
                    }
                    if (showConnections)
                    {
                        //
                        // Draw connections
                        //
                        foreach (var neighbor in node.Neighbors)
                        {
                            if (neighbor == null)
                                continue;
                            if (neighbor.Id < node.Id)
                                continue;
                            Color lineColor = Color.Cyan;
                            Vector4 lineColorVec = lineColor.ToVector4() * 10;
                            MySimpleObjectDraw.DrawLine(
                             node.Position,
                             neighbor.Position,
                             MyStringId.GetOrCompute("Square"),
                            ref lineColorVec,
                            0.02f,
                            MyBillboard.BlendTypeEnum.PostPP);
                        }
                    }
                } 
            }
        }
        /// <summary>
        /// Apply completed sector builds to the loaded dictionary and remove them from pending.
        /// </summary>
        private void ApplyCompleted()
        {
            WaterSector sector;
            while (completed.TryDequeue(out sector))
            {
                loaded[sector.Id] = sector;
                pending.Remove(sector.Id);
            }
        }
        /// <summary>
        /// Unload sectors that are too far from the player to save memory and processing time.
        /// </summary>
        private void UnloadFarSectors()
        {
            var player = MyAPIGateway.Session?.Player?.Character;
            if (player == null)
                return;
            Vector3D pos = player.GetPosition();
            List<Vector3I> remove = new List<Vector3I>();
            foreach (var kv in loaded)
            {
                if (Vector3D.DistanceSquared(pos, kv.Value.Bounds.Center) > UnloadRadius * UnloadRadius)
                {
                    remove.Add(kv.Key);
                }
            }
            foreach (var id in remove)
            {
                loaded.Remove(id);
            }
        }
        /// <summary>
        /// Stream sectors around the player within the build radius. This is done in a separate thread to avoid blocking the main thread. Sectors are built in a breadth-first manner to prioritize loading.
        /// </summary>
        private void StreamSectors()
        {
            var player = MyAPIGateway.Session?.Player?.Character;
            if (player == null)
                return;
            Vector3I center = WorldToSector(player.GetPosition());
            double buildRadiusSq = BuildRadius * BuildRadius;
            for (int x = -HorizontalRadius; x <= HorizontalRadius; x++)
            {
                for (int y = -VerticalRadius; y <= VerticalRadius; y++)
                {
                    for (int z = -HorizontalRadius; z <= HorizontalRadius; z++)
                    {
                        Vector3D offset = new Vector3D(x * SectorSize,y * SectorSize,z * SectorSize);
                        if (offset.LengthSquared() > buildRadiusSq)
                            continue;
                        Vector3I id = center + new Vector3I(x, y, z);
                        RequestSector(id);
                    }
                }
            }
        }
        /// <summary>
        /// Request a sector to be built if its not already loaded or pending. The sector will be built in a separate thread and added to the completed  queue when done.
        /// </summary>
        /// <param name="id"></param>
        private void RequestSector(Vector3I id)
        {
            if (loaded.ContainsKey(id))
                return;
            if (pending.Contains(id))
                return;
            pending.Add(id);
            MyAPIGateway.Parallel.Start(() =>
            {
                WaterSector sector = BuildSector(id);
                if (sector.Nodes.Count > 0)
                {
                    completed.Enqueue(sector);
                }
                //completed.Enqueue(sector);
            });
        }
        /// <summary>
        /// Build a sector by sampling points within the sector bounds to check if they are underwater. If a point is underwater, a water node is created at that position. The sector is rejected if it has 
        /// too few water nodes or if the water density is too low to avoid creating navigation for tiny water pockets that are not navigateable or relevant for fish spawnimg.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private static WaterSector BuildSector(Vector3I id)
        {
            WaterSector sector = new WaterSector();
            sector.Id = id;
            Vector3D min = new Vector3D(id.X * SectorSize,id.Y * SectorSize,id.Z * SectorSize);
            Vector3D max = min + new Vector3D(SectorSize);
            sector.Bounds = new BoundingBoxD(min, max);
            int nodeId = 0;
            int totalSamples = 0;
            int waterSamples = 0;
            for (int x = 0; x < SectorSize; x += NodeSpacing)
            {
                for (int y = 0; y < SectorSize; y += NodeSpacing)
                {
                    for (int z = 0; z < SectorSize; z += NodeSpacing)
                    {
                        Vector3D pos = min + new Vector3D(x + NodeSpacing * 0.5,y + NodeSpacing * 0.5,z + NodeSpacing * 0.5);
                        if (!WaterCheck(pos))
                            continue;
                        waterSamples++;
                        WaterNode node = new WaterNode();
                        node.Id = nodeId++;
                        node.Position = pos;
                        sector.Nodes.Add(node);
                    }
                }
            }
            // Completely dry sector
            if (waterSamples == 0)
            {
                sector.Nodes.Clear();
                return sector;
            }
            double density = (double)waterSamples / totalSamples;
            // Reject tiny water pockets
            if (sector.Nodes.Count < 8)
            {
                sector.Nodes.Clear();
                return sector;
            }
            // Reject sectors with almost no water
            if (density < 0.10)
            {
                sector.Nodes.Clear();
                return sector;
            }
            ConnectNodes(sector);
            return sector;
        }
        /// <summary>
        /// Connect water nodes within a sector by checking their disatances.
        /// </summary>
        /// <param name="sector"></param>
        private static void ConnectNodes(WaterSector sector)
        {
            double maxDist = NodeSpacing * 1.75;
            double maxDistSq = maxDist * maxDist;
            for (int i = 0; i < sector.Nodes.Count; i++)
            {
                WaterNode a = sector.Nodes[i];
                for (int j = i + 1; j < sector.Nodes.Count; j++)
                {
                    WaterNode b =  sector.Nodes[j];
                    if (Vector3D.DistanceSquared(a.Position,b.Position) > maxDistSq)
                        continue;
                    a.Neighbors.Add(b);
                    b.Neighbors.Add(a);
                }
            }
        }
        /// <summary>
        /// Water check a position.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        private static bool WaterCheck(Vector3D pos)
        {
            return WaterModAPI.IsUnderwater(pos);
        }
        /// <summary>
        /// World to sector coordinates.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        private Vector3I WorldToSector(Vector3D pos)
        {
            return new Vector3I((int)Math.Floor(pos.X / SectorSize),(int)Math.Floor(pos.Y / SectorSize),(int)Math.Floor(pos.Z / SectorSize));
        }
        /// <summary>
        /// Find the nearest water node to a position
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public WaterNode FindNearestNode(Vector3D position)
        {
            WaterNode best = null;
            double bestDist = double.MaxValue;
            foreach (var sector in loaded.Values)
            {
                foreach (var node in sector.Nodes)
                {
                    double d = Vector3D.DistanceSquared(position,node.Position);

                    if (d < bestDist)
                    {
                        bestDist = d;
                        best = node;
                    }
                }
            }
            return best;
        }
        /// <summary>
        /// Clear all sectors and close instance.
        /// </summary>
        private void Clear()
        {
            loaded.Clear();
            pending.Clear();
            WaterSector dummy;
            while (completed.TryDequeue(out dummy))
            {
            }
            //MyAPIGateway.Utilities.MessageEntered -= OnMessageEntered;
            I = null;
        }

        protected override void UnloadData()
        {
            Clear();
            base.UnloadData();
        }
    }
    /// <summary>
    /// This class represents a single water node in the navigation graph.
    /// </summary>
    public class WaterNode
    {
        public int Id;
        public Vector3D Position;
        public List<WaterNode> Neighbors = new List<WaterNode>();
    }
    /// <summary>
    /// This class represents a sector of water in the world.
    /// </summary>
    public class WaterSector
    {
        public Vector3I Id;
        public BoundingBoxD Bounds;
        public List<WaterNode> Nodes = new List<WaterNode>();
        public long LastUsedTick;
        public int FishCount;
    }
}

   
  
        


