using System;
using System.Collections.Generic;
using System.Text;
using Steeltoe.Management.Census.Trace.Export;

namespace Steeltoe.Management.Census.Trace
{
    public sealed class BlankSpan : SpanBase
    {
        public static readonly BlankSpan INSTANCE = new BlankSpan();

        private BlankSpan()
            : base(SpanContext.INVALID, default(SpanOptions))
        {
        }
        public override string Name { get; }
        public override Status Status { get; set; }

        public override long EndNanoTime
        {
            get
            {
                return 0;
            }
        }

        public override long LatencyNs
        {
            get
            {
                return 0;
            }
        }

        public override bool IsSampleToLocalSpanStore
        {
            get
            {
                return false;
            }
        }

        public override ISpanId ParentSpanId
        {
            get
            {
                return null;
            }
        }

        public override bool HasEnded => true;

        public override void PutAttributes(IDictionary<string, IAttributeValue> attributes)
        {
        }

        public override void PutAttribute(string key, IAttributeValue value)
        {
        }

        public override void AddAnnotation(string description, IDictionary<string, IAttributeValue> attributes)
        {
        }

        public override void AddAnnotation(IAnnotation annotation)
        {
        }

        public override void AddMessageEvent(IMessageEvent messageEvent)
        {
        }

        public override void AddLink(ILink link)
        {
        }

        public override void End(EndSpanOptions options)
        {
        }
 
        public override string ToString()
        {
            return "BlankSpan";
        }

        internal override ISpanData ToSpanData()
        {
            throw new NotImplementedException();
        }
    }
}
