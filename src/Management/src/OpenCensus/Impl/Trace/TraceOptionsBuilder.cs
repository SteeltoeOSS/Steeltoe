using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace
{
    [Obsolete("Use OpenCensus project packages")]
    public class TraceOptionsBuilder
    {
        private byte options;

        internal TraceOptionsBuilder()
            : this(TraceOptions.DEFAULT_OPTIONS)
        {
        }

        internal TraceOptionsBuilder(byte options)
        {
            this.options = options;
        }

        public TraceOptionsBuilder SetIsSampled(bool isSampled)
        {
            if (isSampled)
            {
                options = (byte)(options | TraceOptions.IS_SAMPLED);
            }
            else
            {
                options = (byte)(options & ~TraceOptions.IS_SAMPLED);
            }
            return this;
        }

        public TraceOptions Build()
        {
            return new TraceOptions(options);
        }
    }
}
