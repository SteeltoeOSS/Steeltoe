using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Stats
{
    public sealed class CurrentStatsState
    {
        private StatsCollectionState currentState = StatsCollectionState.ENABLED;
        private object _lck = new object();
        private bool isRead;

        public StatsCollectionState Value
        {
            get
            {
                lock (_lck)
                {
                    isRead = true;
                    return Internal;
                }
            }
            set
            {
        
            }
        }

        internal StatsCollectionState Internal
        {
            get
            {
                return currentState;
            }
        }

        // Sets current state to the given state. Returns true if the current state is changed, false
        // otherwise.
        internal bool Set(StatsCollectionState state)
        {
            if (isRead)
            {
                throw new ArgumentException("State was already read, cannot set state.");
            }
            if (state == currentState)
            {
                return false;
            }
            else
            {
                currentState = state;
                return true;
            }
        }
    }
}
