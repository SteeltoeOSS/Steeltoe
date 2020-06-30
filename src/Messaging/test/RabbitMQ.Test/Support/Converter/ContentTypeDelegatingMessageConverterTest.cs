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

using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Rabbit.Extensions;
using System;
using System.Text;
using Xunit;

namespace Steeltoe.Messaging.Rabbit.Support.Converter
{
    public class ContentTypeDelegatingMessageConverterTest
    {
        [Fact]
        public void TestDelegationOutbound()
        {
            var converter = new ContentTypeDelegatingMessageConverter();
            var messageConverter = new JsonMessageConverter();
            converter.AddDelegate("foo/bar", messageConverter);
            converter.AddDelegate(MessageHeaders.CONTENT_TYPE_JSON, messageConverter);

            var props = new RabbitHeaderAccessor();
            var foo = new Foo();
            foo.FooString = "bar";

            props.ContentType = "foo/bar";
            var message = converter.ToMessage(foo, props.MessageHeaders);
            Assert.Equal(MessageHeaders.CONTENT_TYPE_JSON, message.Headers.ContentType());
            Assert.Equal("{\"fooString\":\"bar\"}", Encoding.UTF8.GetString((byte[])message.Payload));
            var converted = converter.FromMessage<Foo>(message);
            Assert.Equal("bar", converted.FooString);

            props = new RabbitHeaderAccessor();
            props.ContentType = MessageHeaders.CONTENT_TYPE_JSON;
            message = converter.ToMessage(foo, props.MessageHeaders);
            Assert.Equal("{\"fooString\":\"bar\"}", Encoding.UTF8.GetString((byte[])message.Payload));
            converted = converter.FromMessage<Foo>(message);
            Assert.Equal("bar", converted.FooString);

            converter = new ContentTypeDelegatingMessageConverter(null); // no default
            props = new RabbitHeaderAccessor();
            try
            {
                converter.ToMessage(foo, props.MessageHeaders);
                throw new Exception("Expected exception");
            }
            catch (Exception e)
            {
                Assert.IsType<MessageConversionException>(e);
                Assert.Contains("No delegate converter", e.Message);
            }
        }

        public class Foo
        {
            public string FooString { get; set; }
        }
    }
}
