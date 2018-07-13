using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Export
{
    public class SampledSpanStoreSummary : ISampledSpanStoreSummary
    {
        public static ISampledSpanStoreSummary Create(IDictionary<string, ISampledPerSpanNameSummary> perSpanNameSummary)
        {
            if (perSpanNameSummary == null)
            {
                throw new ArgumentNullException(nameof(perSpanNameSummary));
            }
            IDictionary<string, ISampledPerSpanNameSummary> copy = new Dictionary<string, ISampledPerSpanNameSummary>(perSpanNameSummary);
            return new SampledSpanStoreSummary(new ReadOnlyDictionary<string, ISampledPerSpanNameSummary>(copy));

        }

        internal SampledSpanStoreSummary(IDictionary<string, ISampledPerSpanNameSummary> perSpanNameSummary)
        {
            if (perSpanNameSummary == null)
            {
                throw new ArgumentNullException(nameof(perSpanNameSummary));
            }
            PerSpanNameSummary = perSpanNameSummary;
        }

        public IDictionary<string, ISampledPerSpanNameSummary> PerSpanNameSummary { get; }

        public override string ToString()
        {
            return "SampledSummary{"
                + "perSpanNameSummary=" + PerSpanNameSummary
                + "}";
        }

        public override bool Equals(object o)
        {
            if (o == this)
            {
                return true;
            }
            if (o is SampledSpanStoreSummary)
            {
                SampledSpanStoreSummary that = (SampledSpanStoreSummary)o;
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
