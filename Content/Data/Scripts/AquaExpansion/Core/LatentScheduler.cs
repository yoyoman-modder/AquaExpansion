using System;
using System.Collections.Generic;
using VRage.Game;

namespace AquaExpansion.Core
{
    public class LatentScheduler
    {
        double deltaTime = MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
       
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

        public void Cancel(Action action)
        {
            if (action == null)
                return;

            latentActions.RemoveAll(e => e.Action == action);
        }

        public void Clear()
        {
            latentActions.Clear();
        }
    }
}
