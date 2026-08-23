using AquaExpansion.Core;
using System;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace AquaExpansionExperimental.Core.Animals
{
    public static class SeaCreatureMovement
    {
        public static void Update(IMyCharacter character, SeadCreatureMovementData movement, bool run)
        {
            /*if (!run)
                return;
            if (character?.Physics == null)
                return;
            if (movement == null || !movement.IsMoving)
                return;
            // --------------------------------------------------
            // Smooth turn
            // --------------------------------------------------
            if (movement.CurrentDirection.LengthSquared() < 0.001)
                movement.CurrentDirection = character.WorldMatrix.Forward;
            movement.CurrentDirection = Vector3D.Lerp(movement.CurrentDirection,movement.DesiredDirection,movement.TurnSpeed);
            movement.CurrentDirection.Normalize();
            // --------------------------------------------------
            // Forward movement
            // --------------------------------------------------
            float speed = character.Physics.LinearVelocity.Length();
            if (speed < movement.DesiredSpeed)
            {
                character.Physics.AddForce(
                    MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE,
                    movement.CurrentDirection *
                    movement.ForwardForce,
                    null,
                    null);
            }
            // --------------------------------------------------
            // Depth hold
            // --------------------------------------------------
            if (movement.UseDepthControl)
            {
                float depth = Math.Abs(AquaExpansionSession.Insance.GetWaterDepthbyCharacter(character));
                float error = movement.DesiredDepth - depth;
                if (Math.Abs(error) < 0.5f)
                    error = 0f;
                float buoyancyForce = MathHelper.Clamp(-error * movement.DepthGain,-movement.MaxBuoyancyForce,movement.MaxBuoyancyForce);
                // Damping
                float verticalSpeed = Vector3.Dot(character.Physics.LinearVelocity,(Vector3)character.WorldMatrix.Up);
                buoyancyForce -= verticalSpeed * movement.VerticalDamping;
                character.Physics.AddForce(
                    MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE,
                    character.WorldMatrix.Up *
                    buoyancyForce,
                    null,
                    null);
            }*/
            if (!run)
                return;
            if (character?.Physics == null)
                return;
            if (movement == null)
                return;
            if (!movement.IsMoving)
                return;
            MoveCharacter(character,movement);
            UpdateDepth(character,movement);
        }

        private static void UpdateDepth(IMyCharacter character, SeadCreatureMovementData movement)
        {
            if (!movement.UseDepthControl)
                return;
            float depth = Math.Abs(AquaExpansionSession.Insance.GetWaterDepthbyCharacter(character));
            float error = movement.DesiredDepth - depth;
            if (Math.Abs(error) < 0.5f)
                error = 0f;
            float buoyancyForce = MathHelper.Clamp(-error * movement.DepthGain,-movement.MaxBuoyancyForce,movement.MaxBuoyancyForce);
            float verticalSpeed = Vector3.Dot(character.Physics.LinearVelocity,(Vector3)character.WorldMatrix.Up);
            buoyancyForce -= verticalSpeed * movement.VerticalDamping;
            character.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE,
            character.WorldMatrix.Up *
            buoyancyForce,
        null,
        null);
        }

        public static void MoveCharacter(IMyCharacter character,SeadCreatureMovementData movement)
        {
            if (character == null)
                return;
            Vector3D forward = character.WorldMatrix.Forward;
            Vector3D desired = movement.DesiredDirection;
            // Horizontal steering only
            forward.Y = 0;
            desired.Y = 0;
            if (desired.LengthSquared() < 0.001)
                return;
            forward.Normalize();
            desired.Normalize();
            double angle = Math.Atan2(Vector3D.Cross(forward, desired).Y,Vector3D.Dot(forward, desired));
            float yaw = MathHelper.Clamp((float)angle,-movement.TurnSpeed,movement.TurnSpeed);
            float throttle = MathHelper.Clamp(movement.DesiredSpeed / movement.MaxSpeed,0f,1f);
            Vector3 move = new Vector3(0f,0f,throttle);
            Vector2 rotation = new Vector2(yaw,0f);
            character.MoveAndRotate(move,rotation,0f);
        }
        
        public static void Debug(IMyCharacter character,SeadCreatureMovementData movement, bool show)
        {
            if (!show)
                return; 
            if (character == null || movement == null)
                return;
                Vector3D start = character.GetPosition();
                Vector3D end = start + movement.CurrentDirection * 2;
                var lineColor = Color.PaleGreen.ToVector4();
                //Vector4 lineColorVec = lineColor.ToVector4() * 2;
                MySimpleObjectDraw.DrawLine(
                 start,
                 end,
                 MyStringId.GetOrCompute("Square"),
                ref lineColor,
                0.01f,
                MyBillboard.BlendTypeEnum.Standard);
            //axis debug
            var FC = Color.Blue.ToVector4();
            var RC = Color.Red.ToVector4();
            var UC = Color.Green.ToVector4();
            Vector3D forward = start + character.WorldMatrix.Forward * 10;
            Vector3D right = start + character.WorldMatrix.Right * 10;
            Vector3D up = start + character.WorldMatrix.Up * 10;
            MySimpleObjectDraw.DrawLine(
                start,
                forward,
                MyStringId.GetOrCompute("Square"),
               ref FC,
               0.01f,
               MyBillboard.BlendTypeEnum.Standard);
            MySimpleObjectDraw.DrawLine(
               start,
               right,
               MyStringId.GetOrCompute("Square"),
              ref RC,
              0.01f,
              MyBillboard.BlendTypeEnum.Standard);
            MySimpleObjectDraw.DrawLine(
               start,
               up,
               MyStringId.GetOrCompute("Square"),
              ref UC,
              0.01f,
              MyBillboard.BlendTypeEnum.Standard);
        }
    }
}
