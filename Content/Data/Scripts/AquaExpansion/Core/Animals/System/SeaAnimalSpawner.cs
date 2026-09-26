using System.Collections.Generic;

namespace AquaExpansion.Core.Animals.System
{
    public sealed class SeaAnimalSpawner
    {
        private readonly Dictionary<long, SeaAnimalSpawnRecord> activeAnimals = new Dictionary<long, SeaAnimalSpawnRecord>();
        private readonly Dictionary<string, int> populationBySubtype = new Dictionary<string, int>();
        public void Init()
        {
            SeaAnimalSpawnDatabase.Init();
            SeaAnimalGuidDatabase.Init();
            //AquaExpansionSession.Insance.Log(true,$"Animal Spawner Init");
        }
        public void Update(LatentScheduler scheduler)
        {
            if (scheduler == null)
                return;

        }
        public void Close()
        {
            activeAnimals.Clear();
            populationBySubtype.Clear();
        }
        private int GetPopulation(string subtypeId)
        {
            if (string.IsNullOrWhiteSpace(subtypeId))
                return 0;
            int population;
            if (populationBySubtype.TryGetValue(
                subtypeId,
                out population))
            {
                return population;
            }
            return 0;
        }

    }

}
