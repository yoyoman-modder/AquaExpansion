using AquaExpansion.Core;
using Jakaria.API;
using Sandbox.ModAPI;
using System;
using System.Drawing.Drawing2D;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace AquaExpansionExperimental.Core.Animals
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Character),false, "AquaWhiteShark")]
    public class SeaCreatureWhiteShark : SeaCreatureBase
    {
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            AnimalEnergyFood = "AquaAnimalMeatRaw";
            base.Init(objectBuilder);
        }

        protected override void UpdateCreature()
        {
            base.UpdateCreature();
            // Assign target once
            /*Movement.TurnSpeed = 0.02f;
            Movement.TurnForce = 0.8f;
            Movement.DesiredSpeed = 2f;
            Movement.ForwardForce = 100f;
            Movement.Acceleration = 10f;
            Movement.DepthGain = 50f;
            Movement.MaxBuoyancyForce = 500f;
            Movement.VerticalDamping = 20f;
            desireddepth = Movement.DesiredDepth;
            if (!SeaNavigator.HasTarget)
            {
                IMyPlayer player = MyAPIGateway.Session?.Player;
                if (player?.Character != null)
                {
                    Vector3D playerPosition = player.Character.GetPosition();
                    SeaNavigator.TargetPosition = playerPosition;
                    float targetDepth = (float)Math.Abs((double)WaterModAPI.GetDepth(playerPosition));
                    Movement.DesiredDepth = targetDepth - 2f;
                    SeaNavigator.HasTarget = true;
                    AquaExpansionSession.Insance.Log(true, $"{Movement.DesiredDepth}");
                }
            }*/
            /*Movement.IsMoving = true;
            Movement.TurnSpeed = 0.03f;
            Movement.DesiredSpeed = 2f;
            Movement.MaxSpeed = 4f;
            Movement.UseDepthControl = true;
            if (!SeaNavigator.HasTarget)
            {
                IMyPlayer player = MyAPIGateway.Session?.Player;
                if (player?.Character != null)
                {
                    var pos  = player.Character.GetPosition();
                    SeaNavigator.TargetPosition = pos;
                    float depth = Math.Abs(AquaExpansionSession.Insance.GetWaterDepthbyCharacter(player.Character));
                    Movement.DesiredDepth = depth - 2f;
                    SeaNavigator.HasTarget = true;
                }
            }
            SeaNavigator.Update(Character,Movement);
            desireddepth = Movement.DesiredDepth;
            SeaCreatureMovement.Update(Character,Movement,true);
            SeaCreatureMovement.Debug(Character, Movement, true);*/
        }

        protected override SeaCreatureDefinition GetDefinition()
        {
            return SeaAnimalDefinitions.Shark;
        }
    }
}
