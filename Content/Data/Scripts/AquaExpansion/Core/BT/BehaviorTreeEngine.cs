using AquaExpansion.Core.Animals;
using Jakaria.API;
using Sandbox.Game.Components;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace AquaExpansion.Core.BT
{
    /// <summary>
    /// Node state
    /// </summary>
    public enum NodeState
    { Running,Success,Failure }
    // === Example Leaf Nodes ===
    /// <summary>
    /// This node makes the character idle. (Action)
    /// </summary>
    public class Idle : BehaviorNode
    {
        private float timer;
        private float duration;
        public override void Start(IMyCharacter character)
        {
            timer = 0f;
            duration = 2f + (float)MyUtils.GetRandomDouble(1, 3);
            State = NodeState.Running;
        }
        public override NodeState Tick(IMyCharacter character,float dt)
        {
            SetCurrentNode();
            if (character == null ||
                character.MarkedForClose)
            {
                return SetState(NodeState.Failure);
            }
            timer += dt;
            if (timer >= duration)
            { 
                //Log($"idle action compete at {duration}s"); 
                return SetState(NodeState.Success); }
            return SetState(NodeState.Running);
        }
        public override void End(IMyCharacter character)
        {
        }
    }
    /// <summary>
    /// This node makes the character wander around randomly. (Action)
    /// </summary>
    public class Wander : BehaviorNode
    {
        private readonly float duration;
        private readonly float baseDepth;
        private readonly float minDepth;
        private readonly AnimalSensor sensor;
        private float timer;
        private float startDepth;
        private float targetDepth;
        private float startSpeed;
        private float targetSpeed;
        private Vector3 direction;

        public Wander(float time,float depth,float minimumDepth,AnimalSensor animalSensor)
        {
            duration = time;
            baseDepth = depth;
            minDepth = minimumDepth;
            sensor = animalSensor;
        }
        public override void Start(IMyCharacter character)
        {
            State = NodeState.Running;
            timer = 0f;
            if (character == null ||
                character.MarkedForClose)
                return;
            startDepth = Math.Abs(AquaExpansionSession.Insance.GetWaterDepthbyCharacter(character));
            float depthVariation = (float)MyUtils.GetRandomDouble(-2.0,2.0);
            targetDepth = Math.Max(0.5f,baseDepth + depthVariation);
            startSpeed = character.Physics != null ? character.Physics.LinearVelocity.Length() : 0f;
            targetSpeed = (float)MyUtils.GetRandomDouble(1.0,4.0);
            double angle = MyUtils.GetRandomDouble(0.0,Math.PI * 2.0);
            direction = new Vector3((float)Math.Cos(angle),0f,(float)Math.Sin(angle));
            if (direction.LengthSquared() > 0.001f)
                direction.Normalize();
        }
        public override NodeState Tick(IMyCharacter character,float dt)
        {
            SetCurrentNode();
            if (character == null ||
                character.MarkedForClose)
                return SetState(NodeState.Failure);
            if (sensor == null ||
                sensor.IsDangerousDepth(character,minDepth))
            {
                //Log($"Wander Critical Abort Dangerous Depth");
                return SetState(NodeState.Failure);
            }
            float progress = duration > 0f ? MathHelper.Clamp(timer / duration,0f,1f) : 1f;
            float desiredSpeed = MathHelper.Lerp(startSpeed,targetSpeed,progress);
            float desiredDepth = MathHelper.Lerp(startDepth,targetDepth,progress);
            BB.Set<Vector3>("DesiredDirection",direction);
            BB.Set<float>("DesiredSpeed",desiredSpeed);
            BB.Set<float>("DesiredDepth",desiredDepth);
            timer += dt;
            if (timer >= duration)
            {
                //Log($"Wander in {duration}s {desiredSpeed}m/s at {desiredDepth}m");
                return SetState(NodeState.Success);
            } 
            return SetState(NodeState.Running);
        }
        public override void End(IMyCharacter character)
        {
            if (BB == null)
                return;
            BB.Clear("DesiredDirection");
            BB.Clear("DesiredSpeed");
            BB.Clear("DesiredDepth");
        }
    }
    /// <summary>
    /// This node makes the character chase a target character. (Action)
    /// </summary>
    public class ChaseTarget : BehaviorNode
    {
        private readonly double chaseRange;
        private readonly float depthOffset;
        private readonly float minDepth;
        private readonly AnimalSensor sensor;
        private int GroundAvoidanceTimer;
        private float GroundAvoidanceDepth;
        private int GridAvoidanceTimer;
        private float GridAvoidanceDepth;
        public ChaseTarget(double range,float offset,float minimumDepth,AnimalSensor animalSensor)
        {
            chaseRange = range;
            depthOffset = offset;
            minDepth = minimumDepth;
            sensor = animalSensor;
            GroundAvoidanceTimer = 0;
            GroundAvoidanceDepth = 0.0f;
            GridAvoidanceTimer = 0;
            GridAvoidanceDepth = 0.0f;
        }
        private void ClearTarget()
        {
            if (BB == null)
                return;
            BB.Set<bool>("HasTarget", false);
            BB.Clear("Target");
            BB.Clear("TargetPlayer");
            BB.Clear("TargetPosition");
            BB.Clear("TargetDistance");
            BB.Clear("DesiredDirection");
            BB.Clear("DesiredDepth");
            BB.Clear("TargetDepth");
            BB.Clear("AvoidanceDepth");
            BB.Clear("GridAvoidanceDepth");
        }
        private bool GetTarget(out IMyPlayer player,out IMyCharacter target)
        {
            player = null;
            target = null;
            if (BB == null ||
                !BB.Get<bool>("HasTarget"))
                return false;
            player = BB.Get<IMyPlayer>("TargetPlayer");
            target = BB.Get<IMyCharacter>("Target");
            if (player == null ||
                player.IsBot)
                return false;
            if (target == null ||
                target.MarkedForClose ||
                target.IsDead)
                return false;
            if (!WaterModAPI.IsUnderwater(target.GetPosition()))
                return false;
            if (AnimalUtils.isPlayerCovered(player))
                return false;
            if (AnimalUtils.isPlayersGridPressurated(target))
                return false;
            return true;
        }
        private float GetTargetDepth(IMyCharacter target)
        {
            if (target == null ||
                target.MarkedForClose ||
                target.IsDead)
                return minDepth;
            float depth = Math.Abs(AquaExpansionSession.Insance.GetWaterDepthbyCharacter(target));
            depth -= depthOffset;
            if (depth < minDepth)
                depth = minDepth;
            return depth;
        }
        private bool IsGridDanger()
        {
            if (sensor == null)
                return false;
            if (sensor.DownGridObstacleDetected)
                return true;
            if (sensor.IsObstacleAhead)
                return true;
            return false;
        }
        private void UpdateGridAvoidance()
        {
            const int delay = 8;
            const float initialDepth = 2.0f;
            const float step = 1.0f;
            const float recovery = 0.08f;
            const float maxDepth = 8.0f;
            bool danger =
                IsGridDanger();
            if (danger)
            {
                GridAvoidanceTimer++;
                if (GridAvoidanceDepth <= 0.0f)
                {
                    GridAvoidanceDepth = initialDepth;
                    GridAvoidanceTimer = 0;
                }
                else if (GridAvoidanceTimer >= delay)
                {
                    GridAvoidanceDepth += step;
                    if (GridAvoidanceDepth > maxDepth)
                        GridAvoidanceDepth = maxDepth;
                    GridAvoidanceTimer = 0;
                }
            }
            else
            {
                GridAvoidanceTimer = 0;
                if (GridAvoidanceDepth > 0.0f)
                {
                    GridAvoidanceDepth -= recovery;
                    if (GridAvoidanceDepth < 0.0f)
                        GridAvoidanceDepth = 0.0f;
                }
            }
        }
        private void UpdateGroundAvoidance()
        {
            const float safeDistance = 3.0f;
            const float initialDepth = 2.0f;
            const float step = 1.0f;
            const float recovery = 0.05f;
            const int delay = 20;
            bool danger = false;
            if (sensor != null)
            {
                if (sensor.IsGroundDetected &&
                    sensor.GroundDistance < safeDistance)
                {
                    danger = true;
                }

                if (sensor.DownGridObstacleDetected &&
                    sensor.GroundDistance < safeDistance)
                {
                    danger = true;
                }
            }
            if (danger)
            {
                GroundAvoidanceTimer++;
                if (GroundAvoidanceDepth <= 0.0f)
                {
                    GroundAvoidanceDepth = initialDepth;
                    GroundAvoidanceTimer = 0;
                }
                else if (GroundAvoidanceTimer >= delay)
                {
                    GroundAvoidanceDepth += step;
                    if (GroundAvoidanceDepth > 8.0f)
                        GroundAvoidanceDepth = 8.0f;
                    GroundAvoidanceTimer = 0;
                }
            }
            else
            {
                GroundAvoidanceTimer = 0;
                if (GroundAvoidanceDepth > 0.0f)
                {
                    GroundAvoidanceDepth -= recovery;
                    if (GroundAvoidanceDepth < 0.0f)
                        GroundAvoidanceDepth = 0.0f;
                }
            }
        }
        private Vector3 GetAvoidanceDirection(Vector3 chaseDirection)
        {
            if (sensor == null)
                return chaseDirection;
            Vector3 direction = chaseDirection;
            if (direction.LengthSquared() < 0.001f)
                return direction;
            direction.Normalize();
            bool frontDanger = sensor.IsObstacleAhead;
            bool downDanger = sensor.DownGridObstacleDetected ||
                              sensor.IsGroundDetected;
            if (!frontDanger &&
                !downDanger)
                return direction;
            Vector3 up = Vector3.Up;
            float verticalBias = 0.0f;
            if (frontDanger)
                verticalBias += 0.65f;
            if (downDanger)
                verticalBias += 0.9f;
            direction += up * verticalBias;
            if (direction.LengthSquared() > 0.001f)
            {
                direction.Normalize();
            }
            return direction;
        }
        private void UpdateTargetDepth(IMyCharacter target)
        {
            if (target == null ||
                target.MarkedForClose ||
                target.IsDead)
                return;
            float targetDepth = GetTargetDepth(target);
            UpdateGroundAvoidance();
            UpdateGridAvoidance();
            float avoidanceDepth = GroundAvoidanceDepth + GridAvoidanceDepth;
            if (avoidanceDepth > 10.0f)
                avoidanceDepth = 10.0f;
            float desiredDepth = targetDepth - avoidanceDepth;
            if (desiredDepth < minDepth)
                desiredDepth = minDepth;
            BB.Set<float>("TargetDepth",targetDepth);
            BB.Set<float>("AvoidanceDepth",GroundAvoidanceDepth);
            BB.Set<float>("GridAvoidanceDepth",GridAvoidanceDepth);
            BB.Set<float>("DesiredDepth",desiredDepth);
        }
        public override void Start(IMyCharacter character)
        {
            State = NodeState.Running;
            GroundAvoidanceTimer = 0;
            GroundAvoidanceDepth = 0.0f;
            GridAvoidanceTimer = 0;
            GridAvoidanceDepth = 0.0f;
        }
        public override NodeState Tick(IMyCharacter character,float dt)
        {
            SetCurrentNode();
            if (character == null ||
                character.MarkedForClose ||
                character.IsDead)
            {
                ClearTarget();
                return SetState(NodeState.Failure);
            }
            if (sensor == null ||
                sensor.IsDangerousDepth(character,minDepth))
            {
                ClearTarget();
                return SetState(NodeState.Failure);
            }
            IMyPlayer player;
            IMyCharacter target;
            if (!GetTarget(out player,out target))
            {
                ClearTarget();
                return SetState(NodeState.Failure);
            }
            Vector3D position = character.GetPosition();
            Vector3D targetPosition = target.GetPosition();
            Vector3D direction = targetPosition - position;
            double distanceSquared = direction.LengthSquared();
            if (distanceSquared > chaseRange * chaseRange)
            {
                ClearTarget();
                return SetState(NodeState.Failure);
            }
            BB.Set<Vector3D>("TargetPosition",targetPosition);
            BB.Set<double>("TargetDistance",Math.Sqrt(distanceSquared));
            if (distanceSquared > 0.0001)
            {
                direction.Normalize();
                Vector3 chaseDirection = (Vector3)direction;
                Vector3 avoidanceDirection = GetAvoidanceDirection(chaseDirection);
                BB.Set<Vector3>("DesiredDirection",avoidanceDirection);
            }
            else
            {
                BB.Set<Vector3>("DesiredDirection",Vector3.Zero);
            }
            UpdateTargetDepth(target);
            return SetState(NodeState.Running);
        }
        public override void End(IMyCharacter character)
        {
            GroundAvoidanceTimer = 0;
            GroundAvoidanceDepth = 0.0f;
            GridAvoidanceTimer = 0;
            GridAvoidanceDepth = 0.0f;
            ClearTarget();
        }
    }
    /// <summary>
    /// This node detects nearby player within a certain range (True  != 0) .(Conditional)
    /// </summary>
    public class DetectPlayer : BehaviorNode
    {
        private readonly double range;
        public DetectPlayer(double r)
        {
            range = r;
        }
        private bool GetNearbyPlayers(Vector3D position,double searchRange,out List<IMyPlayer> players)
        {
            List<IMyPlayer> tempP = new List<IMyPlayer>();
            BoundingSphereD sphere = new BoundingSphereD(position,searchRange);
            List<IMyEntity> tempE = MyAPIGateway.Entities.GetTopMostEntitiesInSphere(ref sphere);
            foreach (IMyEntity ent in tempE)
            {
                IMyCharacter ch = ent as IMyCharacter;
                if (ch == null ||
                    ch.Closed ||
                    ch.IsDead)
                    continue;
                if (!WaterModAPI.IsUnderwater(ch.GetPosition()))
                    continue;
                if (AnimalUtils.isPlayersGridPressurated(ch))
                    continue;
                IMyPlayer player = MyAPIGateway.Players.GetPlayerControllingEntity(ch);
                if (player == null ||
                    player.IsBot)
                    continue;
                if (AnimalUtils.isPlayerCovered(player))
                    continue;
                tempP.Add(player);
            }
            players = tempP;
            return tempP.Count > 0;
        }
        private void ClearTarget()
        {
            BB.Set<bool>("HasTarget",false);
            BB.Clear("Target");
            BB.Clear("TargetPlayer");
            BB.Clear("TargetPosition");
            BB.Clear("TargetDistance");
        }
        private bool SetRandomTarget(IMyCharacter character,List<IMyPlayer> players)
        {
            if (players == null ||
                players.Count == 0)
                return false;
            int index = MyUtils.GetRandomInt(0,players.Count);
            IMyPlayer player = players[index];
            if (player == null ||
                player.Character == null ||
                player.Character.MarkedForClose ||
                player.Character.Closed ||
                player.Character.IsDead ||
                !WaterModAPI.IsUnderwater(player.Character.GetPosition()) ||
                AnimalUtils.isPlayersGridPressurated(player.Character) ||
                AnimalUtils.isPlayerCovered(player))
                return false;
            IMyCharacter target = player.Character;
            Vector3D targetPosition = target.GetPosition();
            Vector3D difference = targetPosition - character.GetPosition();
            BB.Set<IMyPlayer>("TargetPlayer",player);
            BB.Set<IMyCharacter>("Target",target);
            BB.Set<Vector3D>("TargetPosition",targetPosition);
            BB.Set<double>("TargetDistance",difference.Length());
            BB.Set<bool>("HasTarget",true);
            return true;
        }
        public override void Start(IMyCharacter character)
        {
            State = NodeState.Running;
        }
        public override NodeState Tick(IMyCharacter character, float dt)
        {
            SetCurrentNode();
            if (character == null ||
                character.MarkedForClose ||
                BB == null)
                return SetState(NodeState.Failure);
            if (BB.Get<bool>("HasTarget"))
            {
                IMyCharacter target = BB.Get<IMyCharacter>("Target");
                IMyPlayer player = BB.Get<IMyPlayer>("TargetPlayer");
                if (target != null &&
                    player != null &&
                    !target.MarkedForClose &&
                    !target.IsDead &&
                    !player.IsBot &&
                    WaterModAPI.IsUnderwater(target.GetPosition()) &&
                    !AnimalUtils.isPlayersGridPressurated(target) &&
                    !AnimalUtils.isPlayerCovered(player))
                {
                    return SetState(NodeState.Success);
                }
                ClearTarget();
            }
            List<IMyPlayer> players;
            if (!GetNearbyPlayers(character.GetPosition(),range,out players))
            {
                ClearTarget();
                return SetState(NodeState.Failure);
            }
            if (!SetRandomTarget(character, players))
            {
                ClearTarget();
                return SetState(NodeState.Failure);
            }
            return SetState(NodeState.Success);
        }
        public override void End(IMyCharacter character)
        {
            
        }
    }
    /// <summary>
    /// This node checks if the characters health is above a certain threshold (True = >=) .(Conditional)
    /// </summary>
    public class HealthAbove : BehaviorNode
    {
        private readonly double minimumHealth;
        public HealthAbove(double health)
        {
            minimumHealth = health;
        }
        public override void Start(IMyCharacter character)
        {
            State = NodeState.Running;
        }
        public override NodeState Tick(IMyCharacter character,float dt)
        {
            SetCurrentNode();
            if (character == null ||
                character.MarkedForClose)
            {
                return SetState(NodeState.Failure);
            }
            if (character.Components == null)
                return SetState(NodeState.Failure);
            MyCharacterStatComponent stats = character.Components.Get<MyCharacterStatComponent>();
            if (stats == null)
                return SetState(NodeState.Failure);
            float health = stats.Health.Value;
            if (health >= minimumHealth)
            {
                //Log($"My Health is {health}");
                return SetState(NodeState.Success);
            }
            //Fail("HealthAbove",$"Health {health}");
            //Log($"Fail Health is {health}");
            return SetState(NodeState.Failure);
        }
        public override void End(IMyCharacter character)
        {
        }
    }
    /// <summary>
    /// This node waits for a specified duration before succeeding. Can be one-shot or resetable. (Logic)
    /// </summary>
    public class Timer : BehaviorNode
    {
        private readonly float duration;
        private readonly bool oneShot;
        private float elapsed;
        private bool finished;
        public Timer(float seconds,bool oneShot = false)
        {
            duration = Math.Max(0f,seconds);
            this.oneShot = oneShot;
            elapsed = 0f;
            finished = false;
        }
        public override void Start(IMyCharacter character)
        {
            if (oneShot && finished)
            {
                State = NodeState.Success;
                return;
            }
            elapsed = 0f;
            State = NodeState.Running;
        }
        public override NodeState Tick(IMyCharacter character,float dt)
        {
            SetCurrentNode();
            if (oneShot && finished)
                return SetState(NodeState.Success);
            elapsed += dt;
            if (elapsed < duration)
                return SetState(NodeState.Running);
            finished = true;
            //Log($"Waited {duration}s");
            return SetState(NodeState.Success);
        }
        public override void End(IMyCharacter character)
        {
        }
    }
    /// <summary>
    /// this node checks if the character depth not dangerous and no obstacles below in meters (True = >= && !obstacle).(Conditional)
    /// </summary>
    public class DangerDepth : BehaviorNode
    {
        private readonly float minDepth;
        private readonly AnimalSensor sensor;
        public DangerDepth(float depth,AnimalSensor animalSensor)
        {
            minDepth = depth;
            sensor = animalSensor;
        }
        public override void Start(
            IMyCharacter character)
        {
            State = NodeState.Running;
        }
        public override NodeState Tick(IMyCharacter character,float dt)
        {
            SetCurrentNode();
            if (sensor == null)
                return SetState(NodeState.Failure);
            if (sensor.IsDangerousDepth(character,minDepth))
            {
                //Log($"Danger Depth: {AquaExpansionSession.Insance.GetWaterDepthbyCharacter(character)}m");
                return SetState(NodeState.Failure);
            }
            //Log($"Safe Depth: {AquaExpansionSession.Insance.GetWaterDepthbyCharacter(character)}m");
            return SetState(NodeState.Success);
        }
        public override void End(IMyCharacter character)
        {
        }
    }
    /// <summary>
    /// This node  set character  direction away from current direction
    /// </summary>
    public class SwimAway : BehaviorNode
    {
        private readonly float duration;
        private float timer;
        private float targetDepth;
        private float startSpeed;
        private Vector3 fleeDirection;
        public SwimAway(float seconds)
        {
            duration = seconds;
        }
        public override void Start(IMyCharacter character)
        {
            timer = 0f;
            targetDepth = 0f;
            startSpeed = 0f;
            fleeDirection = Vector3.Zero;
            State = NodeState.Running;
            if (character == null ||
                character.Physics == null)
                return;
            float currentDepth = Math.Abs(AquaExpansionSession.Insance.GetWaterDepthbyCharacter(character));
            float depthVariation = (float)MyUtils.GetRandomDouble(-0.9, 0.9);
            targetDepth = Math.Max(0f,currentDepth + depthVariation);
            startSpeed = character.Physics.LinearVelocity.Length();
            Vector3 forward = character.WorldMatrix.Forward;
            Vector3 right = character.WorldMatrix.Right;
            forward.Y = 0f;
            right.Y = 0f;
            if (forward.LengthSquared() < 0.001f)
                return;
            forward.Normalize();
            right.Normalize();
            fleeDirection = -forward;
            float randomOffset = (float)MyUtils.GetRandomDouble(-1.0, 1.0);
            fleeDirection += right * randomOffset;
            fleeDirection.Y = 0f;
            if (fleeDirection.LengthSquared() > 0.001f)
                fleeDirection.Normalize();
            else
                fleeDirection = -forward;
        }
        public override NodeState Tick(IMyCharacter character,float dt)
        {
            SetCurrentNode();
            if (character == null ||
                character.MarkedForClose ||
                character.Physics == null)
                return SetState(NodeState.Failure);
            if (BB == null)
                return SetState(NodeState.Failure);
            if (fleeDirection.LengthSquared() < 0.001f)
                return SetState(NodeState.Failure);
            float progress;
            if (duration > 0f)
            {
                progress = MathHelper.Clamp(timer / duration,0f,1f);
            }
            else
            {
                progress = 1f;
            }
            float desiredSpeed = MathHelper.Lerp(startSpeed,5f,progress);
            if (desiredSpeed > 5f)
                desiredSpeed = 5f;
            float currentDepth = Math.Abs(AquaExpansionSession.Insance.GetWaterDepthbyCharacter(character));
            float desiredDepth = MathHelper.Lerp(currentDepth,targetDepth,MathHelper.Clamp(dt * 2f,0f,1f));
            BB.Set<Vector3>("DesiredDirection",fleeDirection);
            BB.Set<float>("DesiredSpeed",desiredSpeed);
            BB.Set<float>("DesiredDepth",desiredDepth);
            timer += dt;
            if (timer >= duration)
            {
                //Log($"SwimAway for {duration}s {desiredSpeed} m/s at {desiredDepth}m");
                return SetState(NodeState.Success);
            }
            return SetState(NodeState.Running);
        }
        public override void End(IMyCharacter character)
        {
            timer = 0f;
            fleeDirection = Vector3.Zero;
            if (BB != null)
            {
                BB.Clear("DesiredDirection");
                BB.Clear("DesiredSpeed");
                BB.Clear("DesiredDepth");
            }
        }
    }
    // ======== Base Behavior Nodes / Blackboard  ========
    /// <summary>
    /// This is base class for all behavior tree nodes. (Node)
    /// </summary>
    public abstract class BehaviorNode
    {
        protected Blackboard BB;
        public NodeState State { get; protected set; }
        protected BehaviorNode()
        {
            State = NodeState.Running;
        }
        public void Bind(Blackboard bb)
        {
            BB = bb;
        }
        public abstract void Start(IMyCharacter character);
        public abstract NodeState Tick(IMyCharacter character,float dt);
        public abstract void End(IMyCharacter character);
        protected void SetCurrentNode()
        {
            if (BB == null)
                return;
            BB.SetCurrentNode(this);
        }
        protected void Log(string message)
        {
            if (AquaExpansionSession.Insance != null)
            {
                AquaExpansionSession.Insance.Log(true, message);
            }
        }
        protected NodeState Fail(string node,string reason)
        {
            Log($"{node} Failure: {reason}");
            return SetState(NodeState.Failure);
        }
        protected NodeState SetState(NodeState state)
        {
            State = state;
            return state;
        }
    }
    /// <summary>
    /// This is a simple blackboard implementation for sharing data between nodes. (Memory)
    /// </summary>
    public class Blackboard
    {
        private readonly Dictionary<string, object> data = new Dictionary<string, object>();
        private BehaviorNode currentNode;
        public BehaviorNode CurrentNode
        {
            get { return currentNode; }
        }
        public string CurrentNodeName
        {
            get
            {
                if (currentNode == null)
                    return "None";

                return currentNode.GetType().Name;
            }
        }
        public void SetCurrentNode(BehaviorNode node)
        {
            currentNode = node;
        }
        public void Set<T>(string key, T value)
        {
            data[key] = value;
        }
        public T Get<T>(string key)
        {
            object value;
            if (data.TryGetValue(key, out value))
            {
                if (value is T)
                    return (T)value;
            }
            return default(T);
        }
        public bool Has(string key)
        {
            return data.ContainsKey(key);
        }
        public void Clear(string key)
        {
            data.Remove(key);
        }
        public void ClearAll()
        {
            data.Clear();
            currentNode = null;
        }
    }
    // === Composite Nodes ===
    /// <summary>
    /// Selects one child randomly each time the node starts and runs only that child.
    /// The selected child remains active until it returns Success or Failure.
    /// A new random child is selected on the next execution.
    /// </summary>
    public class RandomSelectorNode : BehaviorNode
    {
        public readonly List<BehaviorNode> children;
        private int current;
        private bool childStarted;
        public RandomSelectorNode(params BehaviorNode[] inChildren)
        {
            if (inChildren == null ||
                inChildren.Length == 0)
            {
                children = new List<BehaviorNode>();
            }
            else
            {
                children = new List<BehaviorNode>(inChildren);
            }
            current = -1;
            childStarted = false;
        }
        public override void Start(IMyCharacter character)
        {
            childStarted = false;
            State = NodeState.Running;
            int i;
            for (i = 0; i < children.Count; i++)
            {
                children[i].Bind(BB);
            }
            if (children.Count == 0)
            {
                State = NodeState.Success;
                return;
            }
            current = MyUtils.GetRandomInt(0,children.Count);
            children[current].Start(character);
            childStarted = true;
        }
        public override NodeState Tick(IMyCharacter character,float dt)
        {
            if (children.Count == 0)
                return SetState(NodeState.Success);
            if (current < 0 ||
                current >= children.Count)
                return SetState(NodeState.Failure);
            BehaviorNode child = children[current];
            NodeState result = child.Tick(character,dt);
            if (result == NodeState.Running)
                return SetState(NodeState.Running);
            if (childStarted)
            {
                child.End(character);
                childStarted = false;
            }
            return SetState(result);
        }
        public override void End(IMyCharacter character)
        {
            if (!childStarted)
                return;
            if (current >= 0 &&
                current < children.Count)
            {
                children[current].End(character);
            }
            childStarted = false;
        }
    }
    /// <summary>
    /// This node ticks its children is sequentally until one fails. AND logic, stops on first Failure. (Composite)
    /// </summary>
    public class SequenceNode : BehaviorNode
    {
        public readonly List<BehaviorNode> children;
        private int current;
        private bool childStarted;
        public SequenceNode(params BehaviorNode[] inChildren)
        {
            if (inChildren == null ||
                inChildren.Length == 0)
            {
                children = new List<BehaviorNode>();
            }
            else
            {
                children = new List<BehaviorNode>(inChildren);
            }
            current = 0;
            childStarted = false;
        }
        public override void Start(IMyCharacter character)
        {
            current = 0;
            childStarted = false;
            State = NodeState.Running;
            int i;
            for (i = 0; i < children.Count; i++)
            {
                children[i].Bind(BB);
            }
            if (children.Count > 0)
            {
                children[0].Start(character);
                childStarted = true;
            }
        }
        public override NodeState Tick(IMyCharacter character,float dt)
        {
            if (children.Count == 0)
                return SetState(NodeState.Success);
            if (current >= children.Count)
                return SetState(NodeState.Success);
            BehaviorNode child = children[current];
            NodeState result = child.Tick(character,dt);
            if (result == NodeState.Running)
                return SetState(NodeState.Running);
            if (childStarted)
            {
                child.End(character);
                childStarted = false;
            }
            if (result == NodeState.Failure)
                return SetState(NodeState.Failure);
            current++;
            if (current >= children.Count)
                return SetState(NodeState.Success);
            children[current].Start(character);
            childStarted = true;
            return SetState(NodeState.Running);
        }
        public override void End(IMyCharacter character)
        {
            if (!childStarted)
                return;
            if (current >= 0 &&
                current < children.Count)
            {
                children[current].End(character);
            }
            childStarted = false;
        }
    }
    /// <summary>
    /// This node ticks its children sequentually until one succeeds. OR logic, stops on first Success. Support many Children Nodes (Composite)
    /// </summary>
    public class SelectorNode : BehaviorNode
    {
        public readonly List<BehaviorNode> children;
        private int current;
        private bool childStarted;
        public SelectorNode(params BehaviorNode[] inChildren)
        {
            if (inChildren == null ||
                inChildren.Length == 0)
            {
                children = new List<BehaviorNode>();
            }
            else
            {
                children = new List<BehaviorNode>(inChildren);
            }
            current = 0;
            childStarted = false;
        }
        public override void Start(IMyCharacter character)
        {
            current = 0;
            childStarted = false;
            State = NodeState.Running;
            int i;
            for (i = 0; i < children.Count; i++)
            {
                children[i].Bind(BB);
            }
            if (children.Count > 0)
            {
                children[0].Start(character);
                childStarted = true;
            }
        }
        public override NodeState Tick(IMyCharacter character,float dt)
        {
            if (children.Count == 0)
                return SetState(NodeState.Failure);
            if (current >= children.Count)
                return SetState(NodeState.Failure);
            BehaviorNode child = children[current];
            NodeState result = child.Tick(character,dt);
            if (result == NodeState.Running)
                return SetState(NodeState.Running);
            if (childStarted)
            {
                child.End(character);
                childStarted = false;
            }
            if (result == NodeState.Success)
                return SetState(NodeState.Success);
            current++;
            if (current >= children.Count)
                return SetState(NodeState.Failure);
            children[current].Start(character);
            childStarted = true;
            return SetState(NodeState.Running);
        }
        public override void End(IMyCharacter character)
        {
            if (!childStarted)
                return;
            if (current >= 0 &&
                current < children.Count)
            {
                children[current].End(character);
            }
            childStarted = false;
        }
    }
    /// <summary>
    /// This node repeats its child indefinitely Only ONE Child Node . (Composite)
    /// </summary>
    public class LoopNode : BehaviorNode
    {
        private readonly BehaviorNode child;
        private bool childStarted;
        public LoopNode(
            BehaviorNode inChild)
        {
            child = inChild;
            childStarted = false;
        }
        public override void Start(IMyCharacter character)
        {
            State = NodeState.Running;
            if (child == null)
            {
                childStarted = false;
                return;
            }
            child.Bind(BB);
            child.Start(character);
            childStarted = true;
        }
        public override NodeState Tick(IMyCharacter character,float dt)
        {
            if (child == null)
                return SetState(NodeState.Failure);
            if (!childStarted)
            {
                child.Start(character);
                childStarted = true;
            }
            NodeState result = child.Tick(character,dt);
            if (result == NodeState.Running)
                return SetState(NodeState.Running);
            if (childStarted)
            {
                child.End(character);
                childStarted = false;
            }
            child.Start(character);
            childStarted = true;
            return SetState(NodeState.Running);
        }
        public override void End(IMyCharacter character)
        {
            if (!childStarted)
                return;
            child.End(character);
            childStarted = false;
        }
    }
    // ================= Behavior Tree =================
    /// <summary>
    /// This is the main Behavior Tree class that manages the execution of the tree.
    /// </summary>
    public class BehaviorTree
    {
        public readonly BehaviorNode root;
        private readonly Blackboard bb;
        private bool started;
        private BehaviorNode currentNode;
        public Blackboard Blackboard
        {
            get { return bb; }
        }
        public bool IsRunning
        {
            get { return started; }
        }
        public BehaviorNode CurrentNode
        {
            get
            {
                return currentNode;
            }
        }
        public string CurrentNodeName
        {
            get
            {
                if (bb == null || bb.CurrentNode == null)
                    return "None";
                return bb.CurrentNode.GetType().Name;
            }
        }
        public BehaviorTree(BehaviorNode inRoot)
        {
            bb = new Blackboard();
            root = inRoot;
            started = false;
            currentNode = null;
            if (root != null)
                root.Bind(bb);
        }
        public void SetCurrentNode(BehaviorNode node)
        {
            currentNode = node;
        }
        public void Update(IMyCharacter character, float dt)
        {
            if (root == null)
                return;
            if (character == null ||
                character.MarkedForClose)
            {
                Stop(character);
                return;
            }
            if (dt < 0f)
                dt = 0f;
            if (!started)
            {
                root.Start(character);
                started = true;
            }
            NodeState state = root.Tick(character, dt);
            if (state != NodeState.Running)
            {
                root.End(character);
                started = false;
                currentNode = null;
            }
        }
        public void Stop(IMyCharacter character)
        {
            if (!started)
                return;
            if (root != null)
                root.End(character);
            started = false;
            currentNode = null;
        }
        public void Reset()
        {
            started = false;
            currentNode = null;
            bb.ClearAll();
        }
    }
}

