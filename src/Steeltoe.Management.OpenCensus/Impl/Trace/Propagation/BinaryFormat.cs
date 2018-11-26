using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Propagation
{
    [Obsolete("Use OpenCensus project packages")]
    internal class BinaryFormat : BinaryFormatBase
    {

        private const byte VERSION_ID = 0;
        private const int VERSION_ID_OFFSET = 0;
        // The version_id/field_id size in bytes.
        private const byte ID_SIZE = 1;
        private const byte TRACE_ID_FIELD_ID = 0;
        private const int TRACE_ID_FIELD_ID_OFFSET = VERSION_ID_OFFSET + ID_SIZE;
        private const int TRACE_ID_OFFSET = TRACE_ID_FIELD_ID_OFFSET + ID_SIZE;
        private const byte SPAN_ID_FIELD_ID = 1;
        private const int SPAN_ID_FIELD_ID_OFFSET = TRACE_ID_OFFSET + TraceId.SIZE;
        private const int SPAN_ID_OFFSET = SPAN_ID_FIELD_ID_OFFSET + ID_SIZE;
        private const byte TRACE_OPTION_FIELD_ID = 2;
        private const int TRACE_OPTION_FIELD_ID_OFFSET = SPAN_ID_OFFSET + SpanId.SIZE;
        private const int TRACE_OPTIONS_OFFSET = TRACE_OPTION_FIELD_ID_OFFSET + ID_SIZE;
        private const int FORMAT_LENGTH = 4 * ID_SIZE + TraceId.SIZE + SpanId.SIZE + TraceOptions.SIZE;

        public override ISpanContext FromByteArray(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            if (bytes.Length == 0 || bytes[0] != VERSION_ID)
            {
                throw new SpanContextParseException("Unsupported version.");
            }

            ITraceId traceId = TraceId.INVALID;
            ISpanId spanId = SpanId.INVALID;
            TraceOptions traceOptions = TraceOptions.DEFAULT;

            int pos = 1;
            try
            {
                if (bytes.Length > pos && bytes[pos] == TRACE_ID_FIELD_ID)
                {
                    traceId = TraceId.FromBytes(bytes, pos + ID_SIZE);
                    pos += ID_SIZE + TraceId.SIZE;
                }
                if (bytes.Length > pos && bytes[pos] == SPAN_ID_FIELD_ID)
                {
                    spanId = SpanId.FromBytes(bytes, pos + ID_SIZE);
                    pos += ID_SIZE + SpanId.SIZE;
                }
                if (bytes.Length > pos && bytes[pos] == TRACE_OPTION_FIELD_ID)
                {
                    traceOptions = TraceOptions.FromBytes(bytes, pos + ID_SIZE);
                }
                return SpanContext.Create(traceId, spanId, traceOptions);
            }
            catch (Exception e)
            {
                throw new SpanContextParseException("Invalid input.", e);
            }
        }

        public override byte[] ToByteArray(ISpanContext spanContext)
        {
            if (spanContext == null)
            {
                throw new ArgumentNullException(nameof(spanContext));
            }

            byte[] bytes = new byte[FORMAT_LENGTH];
            bytes[VERSION_ID_OFFSET] = VERSION_ID;
            bytes[TRACE_ID_FIELD_ID_OFFSET] = TRACE_ID_FIELD_ID;
            spanContext.TraceId.CopyBytesTo(bytes, TRACE_ID_OFFSET);
            bytes[SPAN_ID_FIELD_ID_OFFSET] = SPAN_ID_FIELD_ID;
            spanContext.SpanId.CopyBytesTo(bytes, SPAN_ID_OFFSET);
            bytes[TRACE_OPTION_FIELD_ID_OFFSET] = TRACE_OPTION_FIELD_ID;
            spanContext.TraceOptions.CopyBytesTo(bytes, TRACE_OPTIONS_OFFSET);
            return bytes;
        }
    }
}
