using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace
{
    [Obsolete("Use OpenCensus project packages")]
    public class EndSpanOptions
    {
        public static readonly EndSpanOptions DEFAULT = new EndSpanOptions(false); 

        public bool SampleToLocalSpanStore { get; }
        public Status Status { get; }

        internal EndSpanOptions()
        {
        }

        internal EndSpanOptions(bool sampleToLocalSpanStore, Status status = null)
        {
            SampleToLocalSpanStore = sampleToLocalSpanStore;
            Status = status;
        }

        public static EndSpanOptionsBuilder Builder()
        {
            return new EndSpanOptionsBuilder().SetSampleToLocalSpanStore(false);
        }

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }
            if (obj is EndSpanOptions) {
                EndSpanOptions that = (EndSpanOptions)obj;
                return (this.SampleToLocalSpanStore == that.SampleToLocalSpanStore)
                     && ((this.Status == null) ? (that.Status == null) : this.Status.Equals(that.Status));
            }
            return false;
        }

        public override int GetHashCode()
        {
            int h = 1;
            h *= 1000003;
            h ^= this.SampleToLocalSpanStore ? 1231 : 1237;
            h *= 1000003;
            h ^= (Status == null) ? 0 : this.Status.GetHashCode();
            return h;
        }

        public override string ToString()
        {
            return "EndSpanOptions{"
                + "sampleToLocalSpanStore=" + SampleToLocalSpanStore + ", "
                + "status=" + Status
                + "}";
        }
    }
}
