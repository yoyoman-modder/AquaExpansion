using System;
using System.Collections.Generic;

namespace AquaExpansionExperimental.Core
{
    /// <summary>
    /// Created by YOYOMAN_MODDER
    /// </summary>
    public class BioLatentScheduler
    {
        private class LatentEntry
        {
            public Action Action;
            public int Counter;
            public LatentEntry(Action action, int ticks)
            {
                Action = action;
                Counter = ticks;
            }
        }
     private readonly List<LatentEntry> latentActions = new List<LatentEntry>();
        /// <summary>
        /// Schedule a one-off action after N ticks
        /// </summary>
        public void Schedule(Action action, int ticks)
        {
            if (action == null || ticks <= 0)
                return;

            latentActions.Add(new LatentEntry(action, ticks));
        }
        /// <summary>
        /// Call this every tick to update and run actions
        /// </summary>
        public void Update()
        {
            for (int i = latentActions.Count - 1; i >= 0; i--)
            {
                var entry = latentActions[i];
                entry.Counter--;

                if (entry.Counter <= 0)
                {
                    entry.Action?.Invoke();
                    latentActions.RemoveAt(i);
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
        }
    }
}
