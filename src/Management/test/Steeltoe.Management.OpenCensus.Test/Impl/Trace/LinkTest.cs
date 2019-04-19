using Steeltoe.Management.Census.Trace.Internal;
using Steeltoe.Management.Census.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Census.Trace.Test
{
    public class LinkTest
    {
        private readonly IDictionary<string, IAttributeValue> attributesMap = new Dictionary<string, IAttributeValue>();
        private readonly IRandomGenerator random = new RandomGenerator(1234);
        private readonly ISpanContext spanContext;
          

        public LinkTest()
        {
            spanContext = SpanContext.Create(TraceId.GenerateRandomId(random), SpanId.GenerateRandomId(random), TraceOptions.DEFAULT); ;
            attributesMap.Add("MyAttributeKey0", AttributeValue<string>.Create("MyStringAttribute"));
            attributesMap.Add("MyAttributeKey1", AttributeValue<long>.Create(10));
            attributesMap.Add("MyAttributeKey2", AttributeValue<bool>.Create(true));
        }

        [Fact]
        public void FromSpanContext_ChildLink()
        {
            ILink link = Link.FromSpanContext(spanContext, LinkType.CHILD_LINKED_SPAN);
            Assert.Equal(spanContext.TraceId, link.TraceId);
            Assert.Equal(spanContext.SpanId, link.SpanId);
            Assert.Equal(LinkType.CHILD_LINKED_SPAN, link.Type);
        }

        [Fact]
        public void FromSpanContext_ChildLink_WithAttributes()
        {
            ILink link = Link.FromSpanContext(spanContext, LinkType.CHILD_LINKED_SPAN, attributesMap);
            Assert.Equal(spanContext.TraceId, link.TraceId);
            Assert.Equal(spanContext.SpanId, link.SpanId);
            Assert.Equal(LinkType.CHILD_LINKED_SPAN, link.Type);
            Assert.Equal(attributesMap, link.Attributes);
        }

        [Fact]
        public void FromSpanContext_ParentLink()
        {
            ILink link = Link.FromSpanContext(spanContext, LinkType.PARENT_LINKED_SPAN);
            Assert.Equal(spanContext.TraceId, link.TraceId);
            Assert.Equal(spanContext.SpanId, link.SpanId);
            Assert.Equal(LinkType.PARENT_LINKED_SPAN, link.Type);
        }

        [Fact]
        public void FromSpanContext_ParentLink_WithAttributes()
        {
            ILink link = Link.FromSpanContext(spanContext, LinkType.PARENT_LINKED_SPAN, attributesMap);
            Assert.Equal(spanContext.TraceId, link.TraceId);
            Assert.Equal(spanContext.SpanId, link.SpanId);
            Assert.Equal(LinkType.PARENT_LINKED_SPAN, link.Type);
            Assert.Equal(attributesMap, link.Attributes);
        }

        [Fact]
        public void Link_EqualsAndHashCode()
        {
            //EqualsTester tester = new EqualsTester();
            //tester
            //    .addEqualityGroup(
            //        Link.fromSpanContext(spanContext, Type.PARENT_LINKED_SPAN),
            //        Link.fromSpanContext(spanContext, Type.PARENT_LINKED_SPAN))
            //    .addEqualityGroup(
            //        Link.fromSpanContext(spanContext, Type.CHILD_LINKED_SPAN),
            //        Link.fromSpanContext(spanContext, Type.CHILD_LINKED_SPAN))
            //    .addEqualityGroup(Link.fromSpanContext(SpanContext.INVALID, Type.CHILD_LINKED_SPAN))
            //    .addEqualityGroup(Link.fromSpanContext(SpanContext.INVALID, Type.PARENT_LINKED_SPAN))
            //    .addEqualityGroup(
            //        Link.fromSpanContext(spanContext, Type.PARENT_LINKED_SPAN, attributesMap),
            //        Link.fromSpanContext(spanContext, Type.PARENT_LINKED_SPAN, attributesMap));
            //tester.testEquals();


        }

        [Fact]
        public void Link_ToString()
        {
            ILink link = Link.FromSpanContext(spanContext, LinkType.CHILD_LINKED_SPAN, attributesMap);
            Assert.Contains(spanContext.TraceId.ToString(), link.ToString());
            Assert.Contains(spanContext.SpanId.ToString(), link.ToString());
            Assert.Contains("CHILD_LINKED_SPAN", link.ToString());
            Assert.Contains(Collections.ToString(attributesMap), link.ToString());
            link = Link.FromSpanContext(spanContext, LinkType.PARENT_LINKED_SPAN, attributesMap);
            Assert.Contains(spanContext.TraceId.ToString(), link.ToString());
            Assert.Contains(spanContext.SpanId.ToString(), spanContext.SpanId.ToString());
            Assert.Contains("PARENT_LINKED_SPAN", link.ToString());
            Assert.Contains(Collections.ToString(attributesMap), link.ToString());
        }
    }
}
