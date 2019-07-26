// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.Census.Trace.Test
{
    [Obsolete]
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
            IDictionary<string, IAttributeValue> attributes = new Dictionary<string, IAttributeValue>
            {
                { "MyStringAttributeKey", AttributeValue<string>.Create("MyStringAttributeValue") }
            };
            IDictionary<string, IAttributeValue> multipleAttributes = new Dictionary<string, IAttributeValue>
            {
                { "MyStringAttributeKey", AttributeValue<string>.Create("MyStringAttributeValue") },
                { "MyBooleanAttributeKey", AttributeValue<bool>.Create(true) },
                { "MyLongAttributeKey", AttributeValue<long>.Create(123) }
            };

            // Tests only that all the methods are not crashing/throwing errors.
            BlankSpan.INSTANCE.PutAttribute(
                "MyStringAttributeKey2", AttributeValue<string>.Create("MyStringAttributeValue2"));
            BlankSpan.INSTANCE.PutAttributes(attributes);
            BlankSpan.INSTANCE.PutAttributes(multipleAttributes);
            BlankSpan.INSTANCE.AddAnnotation("MyAnnotation");
            BlankSpan.INSTANCE.AddAnnotation("MyAnnotation", attributes);
            BlankSpan.INSTANCE.AddAnnotation("MyAnnotation", multipleAttributes);
            BlankSpan.INSTANCE.AddAnnotation(Annotation.FromDescription("MyAnnotation"));

            // BlankSpan.INSTANCE.addNetworkEvent(NetworkEvent.builder(NetworkEvent.Type.SENT, 1L).build());
            BlankSpan.INSTANCE.AddMessageEvent(MessageEvent.Builder(MessageEventType.SENT, 1L).Build());
            BlankSpan.INSTANCE.AddLink(
                Link.FromSpanContext(SpanContext.INVALID, LinkType.CHILD_LINKED_SPAN));
            BlankSpan.INSTANCE.Status = Status.OK;
            BlankSpan.INSTANCE.End(EndSpanOptions.DEFAULT);
            BlankSpan.INSTANCE.End();
        }
    }
}
