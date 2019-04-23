using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace
{
    [Obsolete("Use OpenCensus project packages")]
    public class EndSpanOptionsBuilder
    {
        private bool? sampleToLocalSpanStore;
        private Status status;
        internal EndSpanOptionsBuilder()
        {
        }
        public EndSpanOptionsBuilder SetSampleToLocalSpanStore(bool sampleToLocalSpanStore)
        {
            this.sampleToLocalSpanStore = sampleToLocalSpanStore;
            return this;
        }
        public EndSpanOptionsBuilder SetStatus(Status status)
        {
            this.status = status;
            return this;
        }
        public EndSpanOptions Build()
        {
            String missing = "";
            if (!this.sampleToLocalSpanStore.HasValue)
            {
                missing += " sampleToLocalSpanStore";
            }
            if (!string.IsNullOrEmpty(missing))
            {
                throw new ArgumentOutOfRangeException("Missing required properties:" + missing);
            }
            return new EndSpanOptions(
                this.sampleToLocalSpanStore.Value,
                this.status);
        }
    }
}
