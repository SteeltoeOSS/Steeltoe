using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Export
{
    public sealed class SampledSpanStoreErrorFilter : ISampledSpanStoreErrorFilter
    {
        public static ISampledSpanStoreErrorFilter Create(string spanName, CanonicalCode? canonicalCode, int maxSpansToReturn)
        {
            if (canonicalCode == Trace.CanonicalCode.OK)
            {
                throw new ArgumentOutOfRangeException("Invalid canonical code.");
            }
            if (maxSpansToReturn < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxSpansToReturn));
            }
            return new SampledSpanStoreErrorFilter(spanName, canonicalCode, maxSpansToReturn);
        }

        public string SpanName { get; }

        public CanonicalCode? CanonicalCode { get; }

        public int MaxSpansToReturn { get; }

        internal SampledSpanStoreErrorFilter(string spanName, CanonicalCode? canonicalCode, int maxSpansToReturn)
        {
            if (spanName == null)
            {
                throw new ArgumentNullException(nameof(spanName));
            }

            SpanName = spanName;
            CanonicalCode = canonicalCode;
            MaxSpansToReturn = maxSpansToReturn;
        }

        public override string ToString()
        {
            return "ErrorFilter{"
                + "spanName=" + SpanName + ", "
                + "canonicalCode=" + CanonicalCode + ", "
                + "maxSpansToReturn=" + MaxSpansToReturn
                + "}";
        }

        public override bool Equals(object o)
        {
            if (o == this)
            {
                return true;
            }
            if (o is SampledSpanStoreErrorFilter)
            {
                SampledSpanStoreErrorFilter that = (SampledSpanStoreErrorFilter)o;
                return (this.SpanName.Equals(that.SpanName))
                     && (this.CanonicalCode == that.CanonicalCode)
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
            h ^= this.CanonicalCode.GetHashCode();
            h *= 1000003;
            h ^= this.MaxSpansToReturn;
            return h;
        }
    }
}
