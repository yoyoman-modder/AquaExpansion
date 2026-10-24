using AquaExpansion.Core.BT;
using Jakaria.API;
using VRage.Game;
using VRage.Game.Components;
using VRage.ObjectBuilders;
using VRageMath;

namespace AquaExpansion.Core.Animals
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Character), false, "AquaWhaleShark")]
    public class SeaCreatureWhaleShark :SeaCreatureBase
    {
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
        }
        protected override BehaviorTree BuildTree()
        {
            return new BehaviorTree(
                new LoopNode(
                    new SequenceNode(
                        new SelectorNode(
                            new SequenceNode(
                                new DangerDepth(Deffinition.DangerDepth,Sensor),
                                new HealthAbove(200),
                                new RandomSelectorNode(
                                        new Wander(5, Deffinition.DesiredDepth, Deffinition.DangerDepth, Sensor),
                                        new SwimAway(6,Deffinition.MaxSpeed),
                                        new Wander(8, Deffinition.DesiredDepth, Deffinition.DangerDepth, Sensor)
                                )
                            ),
                            new RandomSelectorNode(
                                new SwimAway(5,Deffinition.MaxSpeed),
                                new SwimAway(10,Deffinition.MaxSpeed),
                                new SwimAway(7,Deffinition.MaxSpeed)
                            )
                        )
                    )
                )
            );
        }
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
                }
                if (BT.Blackboard.Has("DesiredSpeed"))
                {
                    Movement.DesiredSpeed = BT.Blackboard.Get<float>("DesiredSpeed");
                }
                if (BT.Blackboard.Has("DesiredDepth"))
                {
                    Movement.DesiredDepth = BT.Blackboard.Get<float>("DesiredDepth");
                }
                SeaCreatureMovement.Update(Character,Movement,Sensor,Movement.IsMoving,MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS);
            }
        }
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
        protected override void SetAttackData()
        {
            base.SetAttackData();
            Attack.AttackRadius = Deffinition.AttackRadius;
            Attack.AttackInterval = Deffinition.AttackInterval;
            Attack.HealhDamage = Deffinition.HealthDamage;
            Attack.AttackCenter = Deffinition.AttackCenter;
            Attack.Agression = Deffinition.Agression;
        }
        protected override void InitHitZones()
        {
            base.InitHitZones();
            var head = SeaAnimalHitZoneDatabase.Get(Character.Definition.Id.SubtypeId.String, 4);
            var body = SeaAnimalHitZoneDatabase.Get(Character.Definition.Id.SubtypeId.String, 5);
            var tail = SeaAnimalHitZoneDatabase.Get(Character.Definition.Id.SubtypeId.String, 6);
            AnimalHitZoneUtils.AddZone(hitZones, this, head.Name, GetBoneLocation(head.Bonename), head.Radius, head.DamageMultiplier);
            AnimalHitZoneUtils.AddZone(hitZones, this, body.Name, GetBoneLocation(body.Bonename), body.Radius, body.DamageMultiplier);
            AnimalHitZoneUtils.AddZone(hitZones, this, tail.Name, GetBoneLocation(tail.Bonename), tail.Radius, tail.DamageMultiplier);
        }
        protected override Vector3D[] UpdateBoneLocations()
        {
            if (boneLocations == null || boneLocations.Length < 3)
                return boneLocations;
            boneLocations[0] = GetBoneLocation(SeaAnimalHitZoneDatabase.Get(Character.Definition.Id.SubtypeId.String, 4).Bonename);
            boneLocations[1] = GetBoneLocation(SeaAnimalHitZoneDatabase.Get(Character.Definition.Id.SubtypeId.String, 5).Bonename);
            boneLocations[2] = GetBoneLocation(SeaAnimalHitZoneDatabase.Get(Character.Definition.Id.SubtypeId.String, 6).Bonename);
            return boneLocations;
        }
        protected override void InitCollisionZones()
        {
            base.InitCollisionZones();

        }
    }
}
