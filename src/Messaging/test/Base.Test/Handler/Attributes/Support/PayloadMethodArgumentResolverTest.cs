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
using Steeltoe.Messaging.Support;
using System;
using System.Globalization;
using System.Reflection;
using System.Text;
using Xunit;

namespace Steeltoe.Messaging.Handler.Attributes.Support.Test
{
    public class PayloadMethodArgumentResolverTest
    {
        private readonly PayloadMethodArgumentResolver resolver;

        private readonly ParameterInfo paramAnnotated;

        private readonly ParameterInfo paramAnnotatedNotRequired;

        private readonly ParameterInfo paramAnnotatedRequired;

        private readonly ParameterInfo paramWithSpelExpression;

        private readonly ParameterInfo paramNotAnnotated;

        public PayloadMethodArgumentResolverTest()
        {
            resolver = new PayloadMethodArgumentResolver(new StringMessageConverter());

            var payloadMethod = typeof(PayloadMethodArgumentResolverTest).GetMethod("HandleMessage", BindingFlags.NonPublic | BindingFlags.Instance);

            paramAnnotated = payloadMethod.GetParameters()[0];
            paramAnnotatedNotRequired = payloadMethod.GetParameters()[1];
            paramAnnotatedRequired = payloadMethod.GetParameters()[2];
            paramWithSpelExpression = payloadMethod.GetParameters()[3];
            paramNotAnnotated = payloadMethod.GetParameters()[4];
        }

        [Fact]
        public void SupportsParameter()
        {
            Assert.True(resolver.SupportsParameter(paramAnnotated));
            Assert.True(resolver.SupportsParameter(paramNotAnnotated));

            var strictResolver = new PayloadMethodArgumentResolver(
                    new StringMessageConverter(), false);

            Assert.True(strictResolver.SupportsParameter(paramAnnotated));
            Assert.False(strictResolver.SupportsParameter(paramNotAnnotated));
        }

        [Fact]
        public void ResolveRequired()
        {
            IMessage message = MessageBuilder.WithPayload(Encoding.UTF8.GetBytes("ABC")).Build();
            var actual = resolver.ResolveArgument(paramAnnotated, message);
            Assert.Equal("ABC", actual);
        }

        [Fact]
        public void ResolveRequiredEmpty()
        {
            IMessage message = MessageBuilder.WithPayload(string.Empty).Build();
            Assert.Throws<MethodArgumentNotValidException>(() => resolver.ResolveArgument(paramAnnotated, message));
        }

        [Fact]
        public void ResolveRequiredEmptyNonAnnotatedParameter()
        {
            IMessage message = MessageBuilder.WithPayload(string.Empty).Build();
            Assert.Throws<MethodArgumentNotValidException>(() => resolver.ResolveArgument(paramNotAnnotated, message));
        }

        [Fact]
        public void ResolveNotRequired()
        {
            var emptyByteArrayMessage = MessageBuilder.WithPayload(new byte[0]).Build();
            Assert.Null(resolver.ResolveArgument(paramAnnotatedNotRequired, emptyByteArrayMessage));

            var emptyStringMessage = MessageBuilder.WithPayload(string.Empty).Build();
            Assert.Null(resolver.ResolveArgument(paramAnnotatedNotRequired, emptyStringMessage));

            var notEmptyMessage = MessageBuilder.WithPayload(Encoding.UTF8.GetBytes("ABC")).Build();
            Assert.Equal("ABC", resolver.ResolveArgument(paramAnnotatedNotRequired, notEmptyMessage));
        }

        [Fact]
        public void ResolveNonConvertibleParam()
        {
            var notEmptyMessage = MessageBuilder.WithPayload(123).Build();
            var ex = Assert.Throws<MessageConversionException>(() => resolver.ResolveArgument(paramAnnotatedRequired, notEmptyMessage));
            Assert.Contains("Cannot convert", ex.Message);
        }

        [Fact]
        public void ResolveSpelExpressionNotSupported()
        {
            var message = MessageBuilder.WithPayload(Encoding.UTF8.GetBytes("ABC")).Build();
            Assert.Throws<InvalidOperationException>(() => resolver.ResolveArgument(paramWithSpelExpression, message));
        }

        [Fact]
        public void ResolveNonAnnotatedParameter()
        {
            var notEmptyMessage = MessageBuilder.WithPayload(Encoding.UTF8.GetBytes("ABC")).Build();
            Assert.Equal("ABC", resolver.ResolveArgument(paramNotAnnotated, notEmptyMessage));
        }

        internal void HandleMessage(
                    [Payload] string param,
                    [Payload(Required = false)] string paramNotRequired,
                    [Payload(Required = true)] CultureInfo nonConvertibleRequiredParam,
                    [Payload("foo.bar")] string paramWithExpression,
                    string paramNotAnnotated)
        {
        }
    }
}
