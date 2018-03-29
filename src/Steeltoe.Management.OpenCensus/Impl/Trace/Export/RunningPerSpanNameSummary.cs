using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Export
{
    public sealed class RunningPerSpanNameSummary : IRunningPerSpanNameSummary
    {
        internal RunningPerSpanNameSummary(int numRunningSpans)
        {
            NumRunningSpans = numRunningSpans;
        }

        public int NumRunningSpans { get; }

        public static IRunningPerSpanNameSummary Create(int numRunningSpans)
        {
            if (numRunningSpans < 0)
            {
                throw new ArgumentOutOfRangeException("Negative numRunningSpans.");
            }
            return new RunningPerSpanNameSummary(numRunningSpans);
        }
        public override string ToString()
        {
            return "RunningPerSpanNameSummary{"
                + "numRunningSpans=" + NumRunningSpans
                + "}";
        }

        public override bool Equals(object o)
        {
            if (o == this)
            {
                return true;
            }
            if (o is RunningPerSpanNameSummary)
            {
                RunningPerSpanNameSummary that = (RunningPerSpanNameSummary)o;
                return (this.NumRunningSpans == that.NumRunningSpans);
            }
            return false;
        }

        public override int GetHashCode()
        {
            int h = 1;
            h *= 1000003;
            h ^= this.NumRunningSpans;
            return h;
        }

    }
}
