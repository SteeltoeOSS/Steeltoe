using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Census.Trace.Test
{
    public class BlankSpanTest
    {
        [Fact]
        public void HasInvalidContextAndDefaultSpanOptions()
        {
            Assert.Equal(SpanContext.INVALID, BlankSpan.INSTANCE.Context);
            Assert.True(BlankSpan.INSTANCE.Options.HasFlag(SpanOptions.NONE));
        }

        [Fact]
        public void DoNotCrash()
        {
            IDictionary<string, IAttributeValue> attributes = new Dictionary<string, IAttributeValue>();
            attributes.Add(
                "MyStringAttributeKey", AttributeValue<string>.Create("MyStringAttributeValue"));
            IDictionary<string, IAttributeValue> multipleAttributes = new Dictionary<string, IAttributeValue>();
            multipleAttributes.Add(
                "MyStringAttributeKey", AttributeValue<string>.Create("MyStringAttributeValue"));
            multipleAttributes.Add("MyBooleanAttributeKey", AttributeValue<bool>.Create(true));
            multipleAttributes.Add("MyLongAttributeKey", AttributeValue<long>.Create(123));
            // Tests only that all the methods are not crashing/throwing errors.
            BlankSpan.INSTANCE.PutAttribute(
                "MyStringAttributeKey2", AttributeValue<string>.Create("MyStringAttributeValue2"));
            BlankSpan.INSTANCE.PutAttributes(attributes);
            BlankSpan.INSTANCE.PutAttributes(multipleAttributes);
            BlankSpan.INSTANCE.AddAnnotation("MyAnnotation");
            BlankSpan.INSTANCE.AddAnnotation("MyAnnotation", attributes);
            BlankSpan.INSTANCE.AddAnnotation("MyAnnotation", multipleAttributes);
            BlankSpan.INSTANCE.AddAnnotation(Annotation.FromDescription("MyAnnotation"));
            //BlankSpan.INSTANCE.addNetworkEvent(NetworkEvent.builder(NetworkEvent.Type.SENT, 1L).build());
            BlankSpan.INSTANCE.AddMessageEvent(MessageEvent.Builder(MessageEventType.SENT, 1L).Build());
            BlankSpan.INSTANCE.AddLink(
                Link.FromSpanContext(SpanContext.INVALID, LinkType.CHILD_LINKED_SPAN));
            BlankSpan.INSTANCE.Status = Status.OK;
            BlankSpan.INSTANCE.End(EndSpanOptions.DEFAULT);
            BlankSpan.INSTANCE.End();
        }
    }
}
