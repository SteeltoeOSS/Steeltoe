using System;
using System.Collections.Generic;
using System.Text;
using Steeltoe.Management.Census.Trace;

namespace Steeltoe.Management.Census.Trace.Propagation
{
    public abstract class BinaryFormatBase : IBinaryFormat
    {
        internal static readonly NoopBinaryFormat NOOP_BINARY_FORMAT = new NoopBinaryFormat();
        internal static IBinaryFormat NoopBinaryFormat
        {
            get
            {
                return NOOP_BINARY_FORMAT;
            }
        }

        public abstract ISpanContext FromByteArray(byte[] bytes);

        public abstract byte[] ToByteArray(ISpanContext spanContext);

    }
}
