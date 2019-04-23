using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Tags
{
    [Obsolete("Use OpenCensus project packages")]
    public sealed class CurrentTaggingState
    {


        private TaggingState currentState = TaggingState.ENABLED;
        private object _lck = new object();
        private bool isRead;

        public TaggingState Value
        {
            get
            {
                lock (_lck)
                {
                    isRead = true;
                    return Internal;
                }
            }
        }

        public TaggingState Internal
        {
            get
            {
                lock (_lck)
                {
                    return currentState;
                }
            }

        }

        // Sets current state to the given state.
        internal void Set(TaggingState state)
        {
            lock (_lck)
            {
                if (isRead)
                {
                    throw new InvalidOperationException("State was already read, cannot set state.");
                }
                currentState = state;
            }
        }
    }

}
