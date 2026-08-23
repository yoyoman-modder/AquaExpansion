using VRageMath;

namespace AquaExpansionExperimental.Core.Animals
{
    public class SeadCreatureMovementData
    {
        public bool IsMoving = true;
        public Vector3D VisualForward;
        public Vector3D DesiredDirection;
        public Vector3D CurrentDirection;
        public float DesiredSpeed = 2f;
        public float DesiredDepth = 10f;
        public float TurnSpeed = 0.05f;
        public bool UseDepthControl = true;
        public float ForwardForce = 100f;
        public float MaxForwardForce = 150f;
        public double VerticalBlend;
        public float DepthGain = 50f;
        public float MaxBuoyancyForce = 500f;
        public float VerticalDamping = 20f;
        public float Acceleration = 10f;
        public float TurnForce = 2f;
        public float CurrentYaw;
        public float MaxSpeed = 4f;
        public SeaCreatureDefinition Definition;
    }

    public class SeaCreatureDefinition
    {
        public float CruiseSpeed = 2f;
        public float MaxSpeed = 5f;
        public float Response = 0.08f;
        public float Buoyancy = 0.05f;
        public float DepthStrength = 0.08f;
        public float MaxVerticalSpeed = 1.5f;
    }
}
