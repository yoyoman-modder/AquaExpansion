namespace AquaExpansionExperimental.Core.Animals
{
    public static class SeaAnimalDefinitions
    {
        public static readonly SeaCreatureDefinition Shark =
        new SeaCreatureDefinition
        {
            CruiseSpeed = 2f,
            MaxSpeed = 5f,
            Response = 0.08f,
            Buoyancy = 0.08f,
            DepthStrength = 0.08f,
            MaxVerticalSpeed = 1.5f
        };
    }
}
