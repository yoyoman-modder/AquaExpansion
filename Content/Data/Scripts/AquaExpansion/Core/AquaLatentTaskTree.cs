using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;

namespace AquaExpansion.Core
{
    // all possible scheduled actions
    public enum AquaTaskType
    {
        StartGrowing,
        AdvanceStage,
        Harvest,
        Remind
    }
    [ProtoContract]
    public class AquaLatentEntry
    {
        [ProtoMember(1)] public AquaTaskType Type;
        [ProtoMember(2)] public int ExecuteAtTick;
        [ProtoMember(3)] public string Tag;
    }
    [ProtoContract]
    public class AquaSchedulerSaveData
    {
        [ProtoMember(1)] public List<AquaLatentEntry> Entries;
    }
    /// <summary>
    /// Persistent scheduler for Aqua farming system
    /// Created by YOYOMAN_MODDER
    /// </summary>
    public class AquaLatentTaskTree
    {
        private List<AquaLatentEntry> entries = new List<AquaLatentEntry>();
        private int GetTick()
        {
            return MyAPIGateway.Session.GameplayFrameCounter;
        }
        public void Schedule(AquaTaskType type, int delayTicks, string tag = null)
        {
            if (delayTicks <= 0)
                return;

            entries.Add(new AquaLatentEntry
            {
                Type = type,
                ExecuteAtTick = GetTick() + delayTicks,
                Tag = tag
            });
        }
        public void Update(Action<AquaTaskType> executor)
        {
            int now = GetTick();

            for (int i = entries.Count - 1; i >= 0; i--)
            {
                var e = entries[i];

                if (now >= e.ExecuteAtTick)
                {
                    executor?.Invoke(e.Type);
                    entries.RemoveAt(i);
                }
            }
        }
        public void Cancel(string tag)
        {
            if (string.IsNullOrEmpty(tag))
                return;
            entries.RemoveAll(e => e.Tag == tag);
        }
        public void Clear()
        {
            entries.Clear();
        }
        public string Save()
        {
            var data = new AquaSchedulerSaveData
            {
                Entries = entries
            };
            var bytes = MyAPIGateway.Utilities.SerializeToBinary(data);
            return Convert.ToBase64String(bytes);
        }
        public void Load(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return;
            try
            {
                var data = MyAPIGateway.Utilities.SerializeFromBinary<AquaSchedulerSaveData>(Convert.FromBase64String(raw));

                entries = data?.Entries ?? new List<AquaLatentEntry>();
            }
            catch
            {
                entries = new List<AquaLatentEntry>();
            }
        }
    }
}

