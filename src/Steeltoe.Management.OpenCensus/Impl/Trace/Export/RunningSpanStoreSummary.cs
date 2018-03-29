using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Export
{
    public sealed class RunningSpanStoreSummary : IRunningSpanStoreSummary
    {
        public static IRunningSpanStoreSummary Create(IDictionary<string, IRunningPerSpanNameSummary> perSpanNameSummary)
        {
            if (perSpanNameSummary == null)
            {
                throw new ArgumentNullException(nameof(perSpanNameSummary));
            }
            IDictionary<string, IRunningPerSpanNameSummary> copy = new Dictionary<string, IRunningPerSpanNameSummary>(perSpanNameSummary);
            return new RunningSpanStoreSummary(new ReadOnlyDictionary<string, IRunningPerSpanNameSummary>(copy));
        }

        internal RunningSpanStoreSummary(IDictionary<string, IRunningPerSpanNameSummary> perSpanNameSummary)
        {
            PerSpanNameSummary = perSpanNameSummary;
        }

        public IDictionary<string, IRunningPerSpanNameSummary> PerSpanNameSummary { get; }

        public override string ToString()
        {
            return "RunningSummary{"
                + "perSpanNameSummary=" + PerSpanNameSummary
                + "}";
        }

        public override bool Equals(object o)
        {
            if (o == this)
            {
                return true;
            }
            if (o is RunningSpanStoreSummary)
            {
                RunningSpanStoreSummary that = (RunningSpanStoreSummary)o;
                return (this.PerSpanNameSummary.SequenceEqual(that.PerSpanNameSummary));
            }
            return false;
        }

        public override int GetHashCode()
        {
            int h = 1;
            h *= 1000003;
            h ^= this.PerSpanNameSummary.GetHashCode();
            return h;
        }
    }
}
