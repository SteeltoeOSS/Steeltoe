
using Steeltoe.Management.Census.Trace.Export;
using Steeltoe.Management.Census.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace
{
    public abstract class SpanBase : ISpan, IElement<SpanBase>
    {
        private static readonly IDictionary<string, IAttributeValue> EMPTY_ATTRIBUTES = new Dictionary<string, IAttributeValue>();

        public virtual ISpanContext Context { get; }
        public virtual SpanOptions Options { get; }
        public abstract Status Status { get; set; }
        public abstract string Name { get; }
        public SpanBase Next { get; set; }
        public SpanBase Previous { get; set; }
        public abstract long EndNanoTime { get; }
        public abstract long LatencyNs { get; }
        public abstract bool IsSampleToLocalSpanStore { get; }
        public abstract ISpanId ParentSpanId { get; }

        internal SpanBase() { }
        protected SpanBase(ISpanContext context, SpanOptions options = SpanOptions.NONE)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            if (context.TraceOptions.IsSampled && !options.HasFlag(SpanOptions.RECORD_EVENTS))
            {
                throw new ArgumentOutOfRangeException("Span is sampled, but does not have RECORD_EVENTS set.");
            }
            Context = context;
            Options = options;
   
        }
        public virtual void PutAttribute(String key, IAttributeValue value)
        {
            PutAttributes(new Dictionary<string, IAttributeValue>() { { key, value } });
        }
        public abstract void PutAttributes(IDictionary<string, IAttributeValue> attributes);
        public void AddAnnotation(string description)
        {
            AddAnnotation(description, EMPTY_ATTRIBUTES);
        }

        public abstract void AddAnnotation(string description, IDictionary<string, IAttributeValue> attributes);
        public abstract void AddAnnotation(IAnnotation annotation);
        public abstract void AddMessageEvent(IMessageEvent messageEvent);
        public abstract void AddLink(ILink link);
        public abstract void End(EndSpanOptions options);
        internal abstract ISpanData ToSpanData();
        public void End()
        {
            End(EndSpanOptions.DEFAULT);
        }

        public abstract bool HasEnded { get; }

        public override string ToString()
        {
            return "Span[" +
                Name +
                "]";
        }
    }
}
