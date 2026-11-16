using AquaExpansion.Core.Combat;
using Jakaria.API;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace AquaExpansion.Core.Animals.System
{
    public sealed class SeaAnimalSpawner
    {
        private readonly Dictionary<long, SeaAnimalSpawnRecord> activeAnimals = new Dictionary<long, SeaAnimalSpawnRecord>();
        private readonly Dictionary<long, Dictionary<string, int>> populationByWaterPlanet  = new Dictionary<long, Dictionary<string, int>>();
        private readonly List<MyEntity> spawnProbeEntities = new List<MyEntity>();
        private readonly List<IMyPlayer> players = new List<IMyPlayer>();
        private readonly Random random = new Random();
        private bool populationLoaded;
        private bool populationInitialized;
        /// <summary>
        /// init
        /// </summary>
        public void Init()
        {
            SeaAnimalTaskKeysDatabase.Init();
            SeaAnimalSpawnDatabase.Init();
            SeaAnimalGuidDatabase.Init();
            MyLog.Default.WriteLineAndConsole("[SeaAnimalSpawner] Init");
            //AquaExpansionSession.Insance.Log(true,$"[SeaAnimalSpawner] Init");
        }
        /// <summary>
        /// Update the spawner with the provide latent scheduler. this will schedule the necessary tasks for spawninng and tracking the sea animals.
        /// </summary>
        /// <param name="scheduler"></param>
        public void Update(LatentScheduler scheduler)
        {
            if (scheduler == null)
                return;
            if (!populationInitialized)
            {
                InitializePopulation();
                if (!populationInitialized)
                    return;
            }
            scheduler.Schedule(SeaAnimalTaskKeysDatabase.Get(3), SavePopulationRecords, 60.0, true, 60.0);
            scheduler.Schedule(SeaAnimalTaskKeysDatabase.Get(2), UpdateTrackedAnimals, 0.3, true, 0.5);
            foreach (SeaAnimalSpawnDefinition definition in SeaAnimalSpawnDatabase.GetAllSpawnDeffinitions())
            {
                if (definition == null)
                    continue;
                if (!definition.Enabled)
                    continue;
                if (definition.SpawnInterval <= 0)
                    continue;
                string key = SeaAnimalTaskKeysDatabase.Get(1) + definition.SubtypeId;
                SeaAnimalSpawnDefinition currentDefinition = definition;
                scheduler.Schedule(
                    key,
                    delegate
                    {
                        ProcessSpawnTimer(currentDefinition);
                    },
                    definition.SpawnInterval,true,definition.SpawnInterval);
            }
        }
        /// <summary>
        /// Close
        /// </summary>
        public void Close()
        {
            ClearpopulationRecords();
            activeAnimals.Clear();
            populationByWaterPlanet.Clear();
            spawnProbeEntities.Clear();
            players.Clear();
            MyLog.Default.WriteLineAndConsole("[SeaAnimalSpawner] Close");
        }
        /// <summary>
        /// Spawn Timer
        /// </summary>
        /// <param name="definition"></param>
        private void ProcessSpawnTimer(SeaAnimalSpawnDefinition definition)
        {
            if (definition == null)
                return;
            MyPlanet waterPlanet;
            Vector3D position;
            if (!TryFindSpawnPosition(definition,out position,out waterPlanet))
            {
                /*AquaExpansionSession.Insance.Log(
                    true,
                    "[SeaAnimalSpawner] No valid spawn position: " +
                    definition.SubtypeId);*/
                return;
            }
            int population = GetPopulation(waterPlanet.EntityId,definition.SubtypeId);
            MyLog.Default.WriteLineAndConsole("[SeaAnimalSpawner]  Population " + definition.SubtypeId +
                " on planet " +
                waterPlanet.EntityId +
                ": " +
                population +
                "/" +
                definition.MaxPopulation);
            /*AquaExpansionSession.Insance.Log(
                true,
                "[SeaAnimalSpawner] Population " +
                definition.SubtypeId +
                " on planet " +
                waterPlanet.EntityId +
                ": " +
                population +
                "/" +
                definition.MaxPopulation);*/
            if (population >= definition.MaxPopulation)
            {
                /*AquaExpansionSession.Insance.Log(
                    true,
                    "[SeaAnimalSpawner] Population limit reached on planet: " +
                    waterPlanet.EntityId +
                    " Subtype=" +
                    definition.SubtypeId);*/
                return;
            }
            /*AnimalUtils.DebugSphere(
                position,
                Color.LimeGreen,
                definition.SpawnSafetyRadius);*/
            /*AquaExpansionSession.Insance.Log(
                true,
                "[SeaAnimalSpawner] Valid spawn position: " +
                definition.SubtypeId +
                " Planet=" +
                waterPlanet.EntityId +
                " Position=" +
                position.ToString());*/
            SpawnAnimal(definition,waterPlanet,position);
        }
        /// <summary>
        /// Spawn Animal
        /// </summary>
        /// <param name="definition"></param>
        /// <param name="waterPlanet"></param>
        /// <param name="position"></param>
        private void SpawnAnimal(SeaAnimalSpawnDefinition definition,MyPlanet waterPlanet,Vector3D position)
        {
            if (definition == null ||
                waterPlanet == null)
                return;
            if (!MyAPIGateway.Multiplayer.IsServer)
                return;
            if (string.IsNullOrWhiteSpace(definition.BotSubtype))
                return;
            long entityId =
                MyVisualScriptLogicProvider.SpawnBot(definition.BotSubtype,position,Vector3.Zero,Vector3.Zero,definition.Name);
            if (entityId == 0)
            {
                MyLog.Default.WriteLineAndConsole("[SeaAnimalSpawner] SpawnBot failed: " + definition.BotSubtype);
                /*AquaExpansionSession.Insance.Log(
                    true,
                    "[SeaAnimalSpawner] SpawnBot failed: " +
                    definition.BotSubtype);*/
                return;
            }
            if (!RegisterAnimal(entityId,waterPlanet.EntityId,definition.SubtypeId,position))
            {
                MyLog.Default.WriteLineAndConsole("[SeaAnimalSpawner] Failed to register spawned animal: " +
                    definition.SubtypeId +
                    " EntityId=" +
                    entityId);
                /*AquaExpansionSession.Insance.Log(
                    true,
                    "[SeaAnimalSpawner] Failed to register spawned animal: " +
                    definition.SubtypeId +
                    " EntityId=" +
                    entityId);*/
                return;
            }
            MyLog.Default.WriteLineAndConsole("[SeaAnimalSpawner] Spawned animal: " +
                definition.SubtypeId +
                " Planet=" +
                waterPlanet.EntityId +
                " EntityId=" +
                entityId +
                " Position=" +
                position.ToString());
           /*AquaExpansionSession.Insance.Log(
                true,
                "[SeaAnimalSpawner] Spawned animal: " +
                definition.SubtypeId +
                " Planet=" +
                waterPlanet.EntityId +
                " EntityId=" +
                entityId +
                " Position=" +
                position.ToString());*/
        }
        /// <summary>
        /// Get Animal population for a specific water planet and subtype
        /// </summary>
        /// <param name="waterPlanetId"></param>
        /// <param name="subtypeId"></param>
        /// <returns></returns>
        private int GetPopulation(long waterPlanetId,string subtypeId)
        {
            if (waterPlanetId == 0 ||
                string.IsNullOrWhiteSpace(subtypeId))
            {
                return 0;
            }
            Dictionary<string, int> populations;
            if (!populationByWaterPlanet.TryGetValue(waterPlanetId,out populations))
            {
                return 0;
            }
            int population;
            if (populations.TryGetValue(subtypeId,out population))
            {
                return population;
            }
            return 0;
        }
        /// <summary>
        /// Register Animal in the active animals and population tracking
        /// </summary>
        /// <param name="entityId"></param>
        /// <param name="waterPlanetId"></param>
        /// <param name="subtypeId"></param>
        /// <param name="spawnPosition"></param>
        /// <returns></returns>
        private bool RegisterAnimal(long entityId,long waterPlanetId,string subtypeId,Vector3D spawnPosition)
        {
            if (entityId == 0 ||
                waterPlanetId == 0 ||
                string.IsNullOrWhiteSpace(subtypeId))
            {
                return false;
            }
            if (activeAnimals.ContainsKey(entityId))
                return false;
            SeaAnimalSpawnRecord record =
                new SeaAnimalSpawnRecord(
                    entityId,
                    waterPlanetId,
                    subtypeId,
                    spawnPosition);
            activeAnimals.Add(entityId,record);
            Dictionary<string, int> populations;
            if (!populationByWaterPlanet.TryGetValue(waterPlanetId,out populations))
            {
                populations = new Dictionary<string, int>();
                populationByWaterPlanet.Add(waterPlanetId,populations);
            }
            int population;
            if (!populations.TryGetValue(subtypeId,out population))
            {
                population = 0;
            }
            population++;
            populations[subtypeId] = population;
            MyLog.Default.WriteLineAndConsole("[SeaAnimalSpawner] Registered animal: " +
                subtypeId +
                " Planet=" +
                waterPlanetId +
                " EntityId=" +
                entityId +
                " Population=" +
                population);
            /*AquaExpansionSession.Insance.Log(
                true,
                "[SeaAnimalSpawner] Registered animal: " +
                subtypeId +
                " Planet=" +
                waterPlanetId +
                " EntityId=" +
                entityId +
                " Population=" +
                population);*/
            return true;
        }
        /// <summary>
        /// Unregister Animal from the active animals and population tracking
        /// </summary>
        /// <param name="entityId"></param>
        /// <returns></returns>
        private bool UnregisterAnimal(long entityId)
        {
            SeaAnimalSpawnRecord record;
            if (!activeAnimals.TryGetValue(entityId,out record))
            {
                return false;
            }
            activeAnimals.Remove(entityId);
            Dictionary<string, int> populations;
            if (populationByWaterPlanet.TryGetValue(record.WaterPlanetId,out populations))
            {
                int population;
                if (populations.TryGetValue(record.SubtypeId,out population))
                {
                    population--;
                    if (population <= 0)
                    {
                        populations.Remove(record.SubtypeId);
                    }
                    else
                    {
                        populations[record.SubtypeId] = population;
                    }
                }
                if (populations.Count == 0)
                {
                    populationByWaterPlanet.Remove(record.WaterPlanetId);
                }
            }
            MyLog.Default.WriteLineAndConsole("[SeaAnimalSpawner] Unregistered animal: " +
                record.SubtypeId +
                " Planet=" +
                record.WaterPlanetId +
                " EntityId=" +
                entityId);
            /*AquaExpansionSession.Insance.Log(
                true,
                "[SeaAnimalSpawner] Unregistered animal: " +
                record.SubtypeId +
                " Planet=" +
                record.WaterPlanetId +
                " EntityId=" +
                entityId);*/
            return true;
        }
        /// <summary>
        /// Try Find a valid spawn position
        /// </summary>
        /// <param name="definition"></param>
        /// <param name="spawnPosition"></param>
        /// <param name="waterPlanet"></param>
        /// <returns></returns>
        private bool TryFindSpawnPosition(SeaAnimalSpawnDefinition definition,out Vector3D spawnPosition,out MyPlanet waterPlanet)
        {
            spawnPosition = Vector3D.Zero;
            waterPlanet = null;
            if (definition == null)
                return false;
            players.Clear();
            MyAPIGateway.Players.GetPlayers(players);
            if (players.Count == 0)
                return false;
            const int maxAttempts = 20;
            for (int attempt = 0;
                attempt < maxAttempts;
                attempt++)
            {
                IMyPlayer player = players[random.Next(players.Count)];
                if (player == null)
                    continue;
                if (player.Character == null)
                    continue;
                if (player.Character.Closed ||
                    player.Character.IsDead)
                {
                    continue;
                }
                Vector3D playerPosition = player.Character.GetPosition();
                Vector3D candidate;
                if (!TryGenerateCandidate(playerPosition,definition,out candidate))
                {
                    continue;
                }
                if (!IsValidSpawnDistance(candidate,playerPosition,definition))
                {
                    continue;
                }
                MyPlanet candidatePlanet = CombatUtils.WaterPlanet(candidate);
                if (candidatePlanet == null)
                    continue;
                if (!IsValidSpawnPosition(candidate,definition))
                {
                    continue;
                }
                spawnPosition = candidate;
                waterPlanet = candidatePlanet;
                return true;
            }
            return false;
        }
        /// <summary>
        /// Try Generate a candidate spawn position
        /// </summary>
        /// <param name="playerPosition"></param>
        /// <param name="definition"></param>
        /// <param name="candidate"></param>
        /// <returns></returns>
        private bool TryGenerateCandidate(Vector3D playerPosition,SeaAnimalSpawnDefinition definition,out Vector3D candidate)
        {
            candidate = Vector3D.Zero;
            if (definition == null)
                return false;
            double angle =
                random.NextDouble() *
                Math.PI *
                2.0;
            double distance =
                definition.MinSpawnDistance +
                random.NextDouble() *
                (definition.MaxSpawnDistance -
                 definition.MinSpawnDistance);
            candidate =
                playerPosition +
                new Vector3D(
                    Math.Cos(angle) * distance,
                    0.0,
                    Math.Sin(angle) * distance);
            return true;
        }
        /// <summary>
        /// Check Distance
        /// </summary>
        /// <param name="position"></param>
        /// <param name="playerPosition"></param>
        /// <param name="definition"></param>
        /// <returns></returns>
        private bool IsValidSpawnDistance(Vector3D position,Vector3D playerPosition,SeaAnimalSpawnDefinition definition)
        {
            double distance = Vector3D.Distance(position,playerPosition);
            return distance >= definition.MinSpawnDistance &&
                   distance <= definition.MaxSpawnDistance;
        }
        /// <summary>
        /// Check if the spawn position is valid based on depth and safety radius
        /// </summary>
        /// <param name="position"></param>
        /// <param name="definition"></param>
        /// <returns></returns>
        private bool IsValidSpawnPosition(Vector3D position,SeaAnimalSpawnDefinition definition)
        {
            if (definition == null)
                return false;
            if (!WaterModAPI.IsUnderwater(position))
            {
                return false;
            }
            double rawDepth = (double)WaterModAPI.GetDepth(position);
            float depth = (float)Math.Abs(rawDepth);
            if (depth < definition.MinSpawnDepth ||
                depth > definition.MaxSpawnDepth)
            {
                return false;
            }
            if (!IsSpawnAreaClear(position,definition.SpawnSafetyRadius))
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// Check Spawn Area Clearance
        /// </summary>
        /// <param name="position"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        private bool IsSpawnAreaClear(Vector3D position,float radius)
        {
            if (radius <= 0f)
                return false;
            /*AnimalUtils.DebugSphere(
                position,
                Color.Red,
                radius);*/
            spawnProbeEntities.Clear();
            BoundingSphereD sphere = new BoundingSphereD(position,radius);
            MyGamePruningStructure.GetAllEntitiesInSphere(ref sphere,spawnProbeEntities);
            for (int i = 0;i < spawnProbeEntities.Count;i++)
            {
                MyEntity entity = spawnProbeEntities[i];
                if (entity == null ||
                    entity.MarkedForClose)
                {
                    continue;
                }
                IMyCharacter character = entity as IMyCharacter;
                if (character != null)
                    return false;
                IMyCubeGrid grid = entity as IMyCubeGrid;
                if (grid != null)
                    return false;
                IMyVoxelBase voxelBase = entity as IMyVoxelBase;
                if (voxelBase != null)
                {
                    bool overlaps = voxelBase.DoOverlapSphereTest(radius,position);
                    if (overlaps)
                        return false;
                    continue;
                }
            }
            return true;
        }
        /// <summary>
        /// Update tracked animals
        /// </summary>
        private void UpdateTrackedAnimals()
        {
            if (activeAnimals.Count == 0)
                return;
            List<long> removedAnimals = new List<long>();
            foreach (KeyValuePair<long, SeaAnimalSpawnRecord> pair in activeAnimals)
            {
                long entityId = pair.Key;
                MyEntity entity = (MyEntity)MyAPIGateway.Entities.GetEntityById(entityId);
                if (entity == null ||
                    entity.MarkedForClose)
                {
                    removedAnimals.Add(entityId);
                }
            }
            for (int i = 0;i < removedAnimals.Count;i++)
            {
                UnregisterAnimal(removedAnimals[i]);
            }
        }
        /// <summary>
        /// Load records
        /// </summary>
        private void LoadPopulationRecords()
        {
            List<SeaAnimalSpawnRecord> records;
            activeAnimals.Clear();
            populationByWaterPlanet.Clear();
            bool loaded = SeaAnimalPopulationStorage.Load(out records);
            if (loaded &&
                records != null &&
                records.Count > 0)
            {
                for (int i = 0; i < records.Count; i++)
                {
                    SeaAnimalSpawnRecord record = records[i];
                    if (record == null)
                        continue;
                    if (record.EntityId == 0 ||
                        record.WaterPlanetId == 0 ||
                        string.IsNullOrWhiteSpace(record.SubtypeId))
                    {
                        continue;
                    }
                    IMyEntity entity = MyAPIGateway.Entities.GetEntityById(record.EntityId);
                    if (entity == null)
                        continue;
                    if (entity.MarkedForClose)
                        continue;
                    if (activeAnimals.ContainsKey(record.EntityId))
                        continue;
                    RegisterAnimal(
                        record.EntityId,
                        record.WaterPlanetId,
                        record.SubtypeId,
                        record.SpawnPosition);
                }
            }
            DiscoverExistingAnimals();
            populationLoaded = true;
            MyLog.Default.WriteLineAndConsole("[SeaAnimalSpawner] Population initialized. Active animals: " +
                activeAnimals.Count);
            /*AquaExpansionSession.Insance.Log(
                true,
                "[SeaAnimalSpawner] Population initialized. Active animals: " +
                activeAnimals.Count);*/
        }
        /// <summary>
        /// Save records
        /// </summary>
        private void SavePopulationRecords()
        {
            SavePopulation();
        }
        /// <summary>
        /// Clear records
        /// </summary>
        private void ClearpopulationRecords()
        {
            if (populationLoaded)
                SavePopulationRecords();
            populationLoaded = false;
        }
        /// <summary>
        /// Save records helper
        /// </summary>
        public void SavePopulation()
        {
            if (!populationLoaded)
                return;
            if (MyAPIGateway.Multiplayer != null &&
                !MyAPIGateway.Multiplayer.IsServer)
            {
                return;
            }
            SeaAnimalPopulationStorage.Save(activeAnimals);
        }
        /// <summary>
        /// init population
        /// </summary>
        private void InitializePopulation()
        {
            activeAnimals.Clear();
            populationByWaterPlanet.Clear();
            LoadPopulationRecords();
            populationInitialized = true;
        }
        /// <summary>
        /// Discover existing animals in the world
        /// </summary>
        private void DiscoverExistingAnimals()
        {
            if (MyAPIGateway.Entities == null)
                return;
            var entities = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(
                entities,
                delegate (IMyEntity entity)
                {
                    if (entity == null)
                        return false;
                    if (entity.MarkedForClose)
                        return false;
                    return entity is IMyCharacter;
                });
            foreach (IMyEntity ent in entities)
            {
                IMyCharacter character =
                    ent as IMyCharacter;
                if (character == null)
                    continue;
                if (character.EntityId == 0)
                    continue;
                if (activeAnimals.ContainsKey(character.EntityId))
                    continue;
                string subtypeId = character.Definition != null ? character.Definition.Id.SubtypeName : null;
                if (string.IsNullOrWhiteSpace(subtypeId))
                    continue;
                SeaAnimalSpawnDefinition definition = SeaAnimalSpawnDatabase.Get(subtypeId);
                if (definition == null ||
                    !definition.Enabled)
                {
                    continue;
                }
                MyPlanet waterPlanet = CombatUtils.WaterPlanet(character.GetPosition());
                if (waterPlanet == null)
                    continue;
                RegisterAnimal(
                    character.EntityId,
                    waterPlanet.EntityId,
                    subtypeId,
                    character.GetPosition());
            }
            MyLog.Default.WriteLineAndConsole("[SeaAnimalSpawner] Existing animals discovered: " + activeAnimals.Count);
            //AquaExpansionSession.Insance.Log(true,"[SeaAnimalSpawner] Existing animals discovered: " + activeAnimals.Count);
        }
    }
}
