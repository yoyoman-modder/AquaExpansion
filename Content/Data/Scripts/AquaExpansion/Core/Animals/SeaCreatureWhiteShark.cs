using AquaExpansion.Core.BT;
using Jakaria.API;
using VRage.Game;
using VRage.Game.Components;
using VRage.ObjectBuilders;
using VRageMath;

namespace AquaExpansion.Core.Animals
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Character),false, "AquaWhiteShark")]
    public class SeaCreatureWhiteShark : SeaCreatureBase
    {
        /// <summary>
        /// Init
        /// </summary>
        /// <param name="objectBuilder"></param>
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
        }
        /// <summary>
        /// Behavior Tree
        /// </summary>
        /// <returns></returns>
        protected override BehaviorTree BuildTree()
        {
            return new BehaviorTree(//root node
                new LoopNode( // repeat forever child
                    new SequenceNode(// run children in order until one fails
                        new SelectorNode(//ticks its children sequentually until one succeeds
                            new SequenceNode(
                                new DangerDepth(Deffinition.DangerDepth, Sensor),//checks if depth not danger for animal
                                new HealthAbove(50),//checks health above  value
                                new SelectorNode(
                                    new SequenceNode(
                                        new HealthAbove(80),
                                        new DetectPlayer(100),//detect random player from nearby targets store target to chase
                                        new ChaseTarget(100,1.5f,Deffinition.Minseekdepth,Deffinition.DangerDepth,Sensor,8f,10f),//chase player target
                                        new FastAttack(8f,10f,0.7f,Deffinition.DesiredSpeed,Deffinition.MaxSpeed,2,Deffinition.DangerDepth,Sensor,Attack) //bring fast speed stright attack
                                    ),
                                    new RandomSelectorNode(
                                        new Wander(5,Deffinition.DesiredDepth,Deffinition.DangerDepth,Sensor),//wander random swimming
                                        new SwimAway(3, 6),
                                        new Wander(10,Deffinition.DesiredDepth,Deffinition.DangerDepth,Sensor)//wander random swimming
                                    )
                                )
                            ),
                            new RandomSelectorNode(
                                new SwimAway(3,6f),
                                new SwimAway(5,6f),
                                new SwimAway(10,6f)
                            )
                        )
                    )
                )
            );
        }
        /// <summary>
        /// Update
        /// </summary>
        protected override void UpdateCreature()
        {
            base.UpdateCreature();
            if (!IsValid() ||
                Character.IsDead)
                return;
            if (Character.Physics == null)
                return;
            if (!WaterModAPI.IsUnderwater(Character.GetPosition()))
            {
                Movement.IsMoving = false;
                return;
            }
            SetMovementData();
            SetAttackData();
            if (BT != null &&
                BT.Blackboard != null)
            {
                if (BT.Blackboard.Has("DesiredDirection"))
                {
                    Vector3 desiredDirection = BT.Blackboard.Get<Vector3>("DesiredDirection");
                    if (desiredDirection.LengthSquared() > 0.001f)
                    {
                        desiredDirection.Normalize();
                        Movement.DesiredDirection = desiredDirection;
                    }
                    //AquaExpansionSession.Insance.Log(true, $"dir {desiredDirection}");
                }
                if (BT.Blackboard.Has("DesiredSpeed"))
                {
                    Movement.DesiredSpeed = BT.Blackboard.Get<float>("DesiredSpeed");
                }
                if (BT.Blackboard.Has("DesiredDepth"))
                {
                    Movement.DesiredDepth = BT.Blackboard.Get<float>("DesiredDepth");
                }
                
            }
            SeaCreatureMovement.Update(Character,Movement,Sensor,Movement.IsMoving,MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS);
        }
        /// <summary>
        /// Set movement
        /// </summary>
        protected override void SetMovementData()
        {
            base.SetMovementData();
            Movement.IsMoving = true;
            Movement.DesiredSpeed = Deffinition.DesiredSpeed;
            Movement.DesiredDepth = Deffinition.DesiredDepth;
            Movement.MaxSpeed = Deffinition.MaxSpeed;
            Movement.Acceleration = Deffinition.Acceleration;
            Movement.ForwardForce = Deffinition.ForwardForce;
            Movement.TurnSpeed = Deffinition.TurnSpeed;
            Movement.DepthGain = Deffinition.DepthGain;
            Movement.MaxBuoyancyForce = Deffinition.MaxBuoyancyForce;
            Movement.VerticalDamping = Deffinition.VerticalDamping;
            Movement.UseDepthControl = true;
        }
        /// <summary>
        /// Set Attack
        /// </summary>
        protected override void SetAttackData()
        {
            base.SetAttackData();
            Attack.AttackRadius = Deffinition.AttackRadius;
            Attack.AttackInterval = Deffinition.AttackInterval;
            Attack.HealhDamage = Deffinition.HealthDamage;
            Attack.AttackCenter = Deffinition.AttackCenter;
            Attack.Agression = Deffinition.Agression;
        }
        /// <summary>
        /// Set Hit Zones
        /// </summary>
        protected override void InitHitZones()
        {
            base.InitHitZones();
            var head = SeaAnimalHitZoneDatabase.Get(Character.Definition.Id.SubtypeId.String,1);
            var body = SeaAnimalHitZoneDatabase.Get(Character.Definition.Id.SubtypeId.String,2);
            var tail = SeaAnimalHitZoneDatabase.Get(Character.Definition.Id.SubtypeId.String,3);
            AnimalHitZoneUtils.AddZone(hitZones,this, head.Name, GetBoneLocation(head.Bonename), head.Radius, head.DamageMultiplier);
            AnimalHitZoneUtils.AddZone(hitZones, this, body.Name, GetBoneLocation(body.Bonename), body.Radius, body.DamageMultiplier);
            AnimalHitZoneUtils.AddZone(hitZones, this, tail.Name, GetBoneLocation(tail.Bonename), tail.Radius, tail.DamageMultiplier);
        }
        protected override Vector3D[] UpdateBoneLocations()
        {
            if (boneLocations == null || boneLocations.Length < 3)
                return boneLocations;
            boneLocations[0] = GetBoneLocation(SeaAnimalHitZoneDatabase.Get(Character.Definition.Id.SubtypeId.String, 1).Bonename);
            boneLocations[1] = GetBoneLocation(SeaAnimalHitZoneDatabase.Get(Character.Definition.Id.SubtypeId.String, 2).Bonename);
            boneLocations[2] = GetBoneLocation(SeaAnimalHitZoneDatabase.Get(Character.Definition.Id.SubtypeId.String, 3).Bonename);
            return boneLocations;
        }
        protected override void InitCollisionZones()
        {
            base.InitCollisionZones();
        }   
    }
}
