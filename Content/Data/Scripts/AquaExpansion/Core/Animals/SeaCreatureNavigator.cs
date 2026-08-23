using VRage.Game.ModAPI;
using VRageMath;

namespace AquaExpansionExperimental.Core.Animals
{
    public class SeaCreatureNavigator
    {
        public Vector3D TargetPosition;
        public bool HasTarget;
        public double ArriveDistance = 2.0;
        public bool Update(IMyCharacter character, SeadCreatureMovementData movement)
        {
            if (!HasTarget)
                return false;
            Vector3D toTarget = TargetPosition - character.GetPosition();
            double distance = toTarget.Length();
            if (distance <= ArriveDistance)
            {
                movement.IsMoving = false;
                HasTarget = false;
                return true;
            }
            toTarget.Normalize();
            movement.IsMoving = true;
            movement.DesiredDirection = toTarget;
            return false;
        }
    }
}
