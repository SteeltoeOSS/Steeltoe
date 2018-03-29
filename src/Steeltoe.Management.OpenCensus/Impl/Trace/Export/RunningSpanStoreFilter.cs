using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Export
{
    public sealed class RunningSpanStoreFilter : IRunningSpanStoreFilter
    {
        public static IRunningSpanStoreFilter Create(string spanName, int maxSpansToReturn)
        {
            if (maxSpansToReturn < 0)
            {
                throw new ArgumentOutOfRangeException("Negative maxSpansToReturn.");
            }
            return new RunningSpanStoreFilter(spanName, maxSpansToReturn);
        }

        internal RunningSpanStoreFilter(string spanName, int maxSpansToReturn)
        {
            if (spanName == null)
            {
                throw new ArgumentNullException("Null spanName");
            }
            SpanName = spanName;
            MaxSpansToReturn = maxSpansToReturn;
        }

        public string SpanName { get; }

        public int MaxSpansToReturn { get; }

        public override string ToString()
        {
            return "RunningFilter{"
                + "spanName=" + SpanName + ", "
                + "maxSpansToReturn=" + MaxSpansToReturn
                + "}";
        }

        public override bool Equals(Object o)
        {
            if (o == this)
            {
                return true;
            }
            if (o is RunningSpanStoreFilter)
            {
                RunningSpanStoreFilter that = (RunningSpanStoreFilter)o;
                return (this.SpanName.Equals(that.SpanName))
                     && (this.MaxSpansToReturn == that.MaxSpansToReturn);
            }
            return false;
        }

        public override int GetHashCode()
        {
            int h = 1;
            h *= 1000003;
            h ^= this.SpanName.GetHashCode();
            h *= 1000003;
            h ^= this.MaxSpansToReturn;
            return h;
        }
    }
}
