using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Steeltoe.Management.Census.Trace.Export;

namespace Steeltoe.Management.Census.Trace.Test
{
    public class NoopSpan : SpanBase
    {
        public NoopSpan(ISpanContext context, SpanOptions options)
            :base(context, options)
        {
        }

        public override long EndNanoTime { get; }
        public override long LatencyNs { get; }
        public override bool IsSampleToLocalSpanStore { get; }
        public override Status Status { get; set; }
        public override string Name { get; }

        public override void AddAnnotation(string description, IDictionary<string, IAttributeValue> attributes)
        {
        }

        public override void AddAnnotation(IAnnotation annotation)
        {
        }

        public override void AddLink(ILink link)
        {
        }

        public override void AddMessageEvent(IMessageEvent messageEvent)
        {
        }

        public override void End(EndSpanOptions options)
        {
        }

        public override void PutAttributes(IDictionary<string, IAttributeValue> attributes)
        {
        }

        internal override ISpanData ToSpanData()
        {
            return null;
        }
    }
}
