using System;
using System.Collections.Generic;
using VRage.Game;

namespace AquaExpansion.Core
{
    public class LatentScheduler
    {
        /*double deltaTime = MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
        private class LatentEntry
        {
            public Action Action;
            public double TimeRemaining;
            public double Interval;
            public bool Repeat;
            public LatentEntry(Action action, double seconds, bool repeat, double interval)
            {
                Action = action;
                TimeRemaining = seconds;
                Repeat = repeat;
                Interval = interval;
            }
        }
        private readonly List<LatentEntry> latentActions = new List<LatentEntry>();
        /// <summary>
        /// Schedule a one-off action after N seconds
        /// </summary>
        public void Schedule(Action action, double seconds, bool repeat, double interval)
        {
            if (action == null || seconds <= 0)
                return;
            latentActions.Add(new LatentEntry(action, seconds, repeat, interval));
        }
        /// <summary>
        /// Call this every tick, pass delta time in seconds
        /// </summary>
        public void Update()
        {
            for (int i = latentActions.Count - 1; i >= 0; i--)
            {
                var entry = latentActions[i];
                entry.TimeRemaining -= deltaTime;

                if (entry.TimeRemaining <= 0)
                {
                    entry.Action?.Invoke();
                    if (entry.Repeat)
                    {
                        entry.TimeRemaining += entry.Interval;
                    }
                    else
                    {
                        latentActions.RemoveAt(i);
                    }
                }
            }
        }
        /// <summary>
        /// Cancel Action
        /// </summary>
        /// <param name="action"></param>
        public void Cancel(Action action)
        {
            if (action == null)
                return;
            latentActions.RemoveAll(e => e.Action == action);
        }
        /// <summary>
        /// Clear Actions
        /// </summary>
        public void Clear()
        {
            latentActions.Clear();
        }*/
        private readonly double deltaTime = MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
        private class LatentEntry
        {
            public string Key;
            public Action Action;
            public double TimeRemaining;
            public double Interval;
            public bool Repeat;
            public LatentEntry(
                Action action,
                double seconds,
                bool repeat,
                double interval)
            {
                Key = null;
                Action = action;
                TimeRemaining = seconds;
                Repeat = repeat;
                Interval = interval;
            }
            public LatentEntry(
                string key,
                Action action,
                double seconds,
                bool repeat,
                double interval)
            {
                Key = key;
                Action = action;
                TimeRemaining = seconds;
                Repeat = repeat;
                Interval = interval;
            }
        }
        private readonly List<LatentEntry> latentActions = new List<LatentEntry>();
        /// <summary>
        /// Shedule one-shot action/repeat action direct
        /// </summary>
        /// <param name="action"></param>
        /// <param name="seconds"></param>
        /// <param name="repeat"></param>
        /// <param name="interval"></param>
        public void Schedule(Action action,double seconds,bool repeat,double interval)
        {
            if (action == null || seconds < 0)
                return;
            if (repeat && interval <= 0)
                return;
            for (int i = 0; i < latentActions.Count; i++)
            {
                LatentEntry entry = latentActions[i];
                if (entry.Key == null &&
                    entry.Action == action)
                    return;
            }
            latentActions.Add(
                new LatentEntry(
                    action,
                    seconds,
                    repeat,
                    interval));
        }
        /// <summary>
        /// Shedule one-shot action/repeat action lambda keyed signature
        /// </summary>
        /// <param name="key"></param>
        /// <param name="action"></param>
        /// <param name="seconds"></param>
        /// <param name="repeat"></param>
        /// <param name="interval"></param>
        public void Schedule(string key,Action action,double seconds,bool repeat,double interval)
        {
            if (action == null || seconds < 0)
                return;
            if (repeat && interval <= 0)
                return;
            if (string.IsNullOrEmpty(key))
            {
                Schedule(action,seconds,repeat,interval);
                return;
            }
            for (int i = 0; i < latentActions.Count; i++)
            {
                LatentEntry entry = latentActions[i];
                if (entry.Key == key)
                    return;
            }
            latentActions.Add(
                new LatentEntry(key,action,seconds,repeat,interval));
        }
        /// <summary>
        /// Update timer
        /// </summary>
        public void Update()
        {
            for (int i = latentActions.Count - 1; i >= 0; i--)
            {
                LatentEntry entry = latentActions[i];
                entry.TimeRemaining -= deltaTime;
                if (entry.TimeRemaining > 0)
                    continue;
                if (!entry.Repeat)
                {
                    latentActions.RemoveAt(i);
                    entry.Action.Invoke();
                    continue;
                }
                entry.TimeRemaining += entry.Interval;
                entry.Action.Invoke();
            }
        }
        /// <summary>
        /// Cancel sheduled action direct signature
        /// </summary>
        /// <param name="action"></param>
        public void Cancel(Action action)
        {
            if (action == null)
                return;
            latentActions.RemoveAll(
                delegate (LatentEntry entry)
                {
                    return entry.Action == action;
                });
        }
        /// <summary>
        /// Cancel sheduled action lambdakeyed signature
        /// </summary>
        /// <param name="key"></param>
        public void Cancel(string key)
        {
            if (string.IsNullOrEmpty(key))
                return;
            latentActions.RemoveAll(
                delegate (LatentEntry entry)
                {
                    return entry.Key == key;
                });
        }
        /// <summary>
        /// Clear all
        /// </summary>
        public void Clear()
        {
            latentActions.Clear();
        }
    }
}
