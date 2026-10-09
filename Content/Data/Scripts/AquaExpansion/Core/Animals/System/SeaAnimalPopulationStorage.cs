using System;
using System.Collections.Generic;
using System.IO;
using Sandbox.ModAPI;
using VRage.Utils;
using VRageMath;

namespace AquaExpansion.Core.Animals.System
{
    public static class SeaAnimalPopulationStorage
    {
        private const string FileName = "AquaExpansion_SeaAnimalPopulation.bin";
        private const int FileVersion = 1;
        private const int MaxRecords = 100000;
        /// <summary>
        /// Saves the active sea animal spawn records to a binary file in world storage.
        /// </summary>
        /// <param name="activeAnimals">The dictionary of active sea animal spawn records.</param>
        /// <returns>True if the save operation was successful; otherwise, false.</returns>
        public static bool Save(Dictionary<long, SeaAnimalSpawnRecord> activeAnimals)
        {
            if (activeAnimals == null)
                return false;
            if (MyAPIGateway.Multiplayer != null &&
                !MyAPIGateway.Multiplayer.IsServer)
            {
                return false;
            }
            try
            {
                int validCount = 0;
                foreach (KeyValuePair<long, SeaAnimalSpawnRecord> pair in activeAnimals)
                {
                    SeaAnimalSpawnRecord record = pair.Value;
                    if (record == null ||
                        record.EntityId == 0 ||
                        record.WaterPlanetId == 0 ||
                        string.IsNullOrWhiteSpace(record.SubtypeId))
                    {
                        continue;
                    }
                    validCount++;
                }
                using (BinaryWriter writer = MyAPIGateway.Utilities.WriteBinaryFileInWorldStorage(FileName,typeof(SeaAnimalPopulationStorage)))
                {
                    if (writer == null)
                        return false;
                    writer.Write(FileVersion);
                    writer.Write(validCount);
                    foreach (KeyValuePair<long, SeaAnimalSpawnRecord> pair in activeAnimals)
                    {
                        SeaAnimalSpawnRecord record = pair.Value;
                        if (record == null ||
                            record.EntityId == 0 ||
                            record.WaterPlanetId == 0 ||
                            string.IsNullOrWhiteSpace(record.SubtypeId))
                        {
                            continue;
                        }
                        writer.Write(record.EntityId);
                        writer.Write(record.WaterPlanetId);
                        writer.Write(record.SubtypeId);
                        writer.Write(record.SpawnPosition.X);
                        writer.Write(record.SpawnPosition.Y);
                        writer.Write(record.SpawnPosition.Z);
                    }
                    writer.Flush();
                }
                MyLog.Default.WriteLineAndConsole("[SeaAnimalPopulationStorage] Saved records: " +
                    validCount);
                /*AquaExpansionSession.Insance.Log(
                    true,
                    "[SeaAnimalPopulationStorage] Saved records: " +
                    validCount);*/
                return true;
            }
            catch (Exception exception)
            {
                MyLog.Default.WriteLineAndConsole("[SeaAnimalPopulationStorage] Save failed: " +
                    exception.Message);
                /*AquaExpansionSession.Insance.Log(
                    true,
                    "[SeaAnimalPopulationStorage] Save failed: " +
                    exception.Message);*/
                return false;
            }
        }
        /// <summary>
        /// Load the sea animal spawn records from a binary file in world storage.
        /// </summary>
        /// <param name="records">The list to populate with loaded sea animal spawn records.</param>
        /// <returns>True if the load operation was successful; otherwise, false.</returns>
        public static bool Load(out List<SeaAnimalSpawnRecord> records)
        {
            /*records = new List<SeaAnimalSpawnRecord>();
            if (MyAPIGateway.Multiplayer != null &&
                !MyAPIGateway.Multiplayer.IsServer)
            {
                return false;
            }
            try
            {
                using (BinaryReader reader = MyAPIGateway.Utilities.ReadBinaryFileInWorldStorage(FileName,typeof(SeaAnimalPopulationStorage)))
                {
                    if (reader == null)
                        return false;
                    int version = reader.ReadInt32();
                    if (version != FileVersion)
                    {
                        records.Clear();
                        AquaExpansionSession.Insance.Log(true,"[SeaAnimalPopulationStorage] Unsupported file version: " + version);
                        return false;
                    }
                    int count = reader.ReadInt32();
                    if (count < 0 || count > MaxRecords)
                    {
                        records.Clear();
                        return false;
                    }
                    for (int i = 0; i < count; i++)
                    {
                        long entityId = reader.ReadInt64();
                        long waterPlanetId = reader.ReadInt64();
                        string subtypeId = reader.ReadString();
                        Vector3D spawnPosition =
                            new Vector3D(
                                reader.ReadDouble(),
                                reader.ReadDouble(),
                                reader.ReadDouble());
                        if (entityId == 0 ||
                            waterPlanetId == 0 ||
                            string.IsNullOrWhiteSpace(subtypeId))
                        {
                            continue;
                        }
                        records.Add(
                            new SeaAnimalSpawnRecord(
                                entityId,
                                waterPlanetId,
                                subtypeId,
                                spawnPosition));
                    }
                }
                AquaExpansionSession.Insance.Log(true,"[SeaAnimalPopulationStorage] Loaded records: " + records.Count);
                return true;
            }
            catch (Exception exception)
            {
                records.Clear();
                AquaExpansionSession.Insance.Log(true,"[SeaAnimalPopulationStorage] Load failed: " + exception.Message);
                return false;
            }*/
            records = new List<SeaAnimalSpawnRecord>();
            if (MyAPIGateway.Multiplayer != null &&
                !MyAPIGateway.Multiplayer.IsServer)
            {
                return false;
            }
            try
            {
                BinaryReader reader = MyAPIGateway.Utilities.ReadBinaryFileInWorldStorage(FileName,typeof(SeaAnimalPopulationStorage));
                if (reader == null)
                    return false;
                using (reader)
                {
                    int version = reader.ReadInt32();
                    if (version != FileVersion)
                    {
                        MyLog.Default.WriteLineAndConsole("[SeaAnimalPopulationStorage] Unsupported file version: " +
                            version);
                        /*AquaExpansionSession.Insance.Log(
                            true,
                            "[SeaAnimalPopulationStorage] Unsupported file version: " +
                            version);*/
                        return false;
                    }
                    int count = reader.ReadInt32();
                    if (count < 0 || count > MaxRecords)
                    {
                        MyLog.Default.WriteLineAndConsole("[SeaAnimalPopulationStorage] Invalid record count: " +
                            count);
                        /*AquaExpansionSession.Insance.Log(
                            true,
                            "[SeaAnimalPopulationStorage] Invalid record count: " +
                            count);*/
                        return false;
                    }
                    for (int i = 0; i < count; i++)
                    {
                        long entityId = reader.ReadInt64();
                        long waterPlanetId = reader.ReadInt64();
                        string subtypeId = reader.ReadString();
                        Vector3D spawnPosition =
                            new Vector3D(
                                reader.ReadDouble(),
                                reader.ReadDouble(),
                                reader.ReadDouble());
                        if (entityId == 0 ||
                            waterPlanetId == 0 ||
                            string.IsNullOrWhiteSpace(subtypeId))
                        {
                            continue;
                        }
                        records.Add(
                            new SeaAnimalSpawnRecord(
                                entityId,
                                waterPlanetId,
                                subtypeId,
                                spawnPosition));
                    }
                }
                MyLog.Default.WriteLineAndConsole("[SeaAnimalPopulationStorage] Loaded records: " +
                    records.Count);
                /*AquaExpansionSession.Insance.Log(
                    true,
                    "[SeaAnimalPopulationStorage] Loaded records: " +
                    records.Count);*/
                return true;
            }
            catch (Exception exception)
            {
                records.Clear();
                MyLog.Default.WriteLineAndConsole("[SeaAnimalPopulationStorage] Load failed: " +
                    exception.Message);
                /*AquaExpansionSession.Insance.Log(
                    true,
                    "[SeaAnimalPopulationStorage] Load failed: " +
                    exception.ToString());*/
                return false;
            }
        }
    }
}
