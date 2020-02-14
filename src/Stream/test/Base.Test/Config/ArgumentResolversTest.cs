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

using Steeltoe.Messaging;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Support;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Xunit;

namespace Steeltoe.Stream.Config
{
    public class ArgumentResolversTest
    {
        [Fact]
        public void TestSmartPayloadArgumentResolver()
        {
            var resolver = new SmartPayloadArgumentResolver(new TestMessageConverter());
            var payload = Encoding.UTF8.GetBytes("hello");
            var message = new GenericMessage(payload);
            var parameter = this.GetType().GetMethod("ByteArray").GetParameters()[0];
            var resolvedArgument = resolver.ResolveArgument(parameter, message);
            Assert.Same(payload, resolvedArgument);

            parameter = this.GetType().GetMethod("Object").GetParameters()[0];
            resolvedArgument = resolver.ResolveArgument(parameter, message);
            Assert.True(resolvedArgument is IMessage);

            var payload2 = new Dictionary<object, object>();
            var message2 = new GenericMessage(payload2);
            parameter = this.GetType().GetMethod("Dict").GetParameters()[0];
            resolvedArgument = resolver.ResolveArgument(parameter, message2);
            Assert.Same(payload2, resolvedArgument);

            parameter = this.GetType().GetMethod("Object").GetParameters()[0];
            resolvedArgument = resolver.ResolveArgument(parameter, message2);
            Assert.True(resolvedArgument is IMessage);
        }

#pragma warning disable xUnit1013 // Public method should be marked as test
        public void ByteArray(byte[] p)
        {
        }

        public void ByteArrayMessage(IMessage<byte[]> p)
        {
        }

        public void Object(object p)
        {
        }

        public void Dict(Dictionary<object, object> p)
        {
        }
#pragma warning restore xUnit1013 // Public method should be marked as test

        private class TestMessageConverter : IMessageConverter
        {
            public object FromMessage(IMessage message, Type targetClass)
            {
                return message;
            }

            public T FromMessage<T>(IMessage message)
            {
                return (T)message;
            }

            public IMessage ToMessage(object payload, IMessageHeaders headers = null)
            {
                return new GenericMessage(payload);
            }
        }
    }
}
