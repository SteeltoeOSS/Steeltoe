// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
            var message = MessageBuilder.WithPayload(Encoding.UTF8.GetBytes("ABC")).Build();
            var actual = resolver.ResolveArgument(paramAnnotated, message);
            Assert.Equal("ABC", actual);
        }

        [Fact]
        public void ResolveRequiredEmpty()
        {
            var message = MessageBuilder.WithPayload(string.Empty).Build();
            Assert.Throws<MethodArgumentNotValidException>(() => resolver.ResolveArgument(paramAnnotated, message));
        }

        [Fact]
        public void ResolveRequiredEmptyNonAnnotatedParameter()
        {
            var message = MessageBuilder.WithPayload(string.Empty).Build();
            Assert.Throws<MethodArgumentNotValidException>(() => resolver.ResolveArgument(paramNotAnnotated, message));
        }

        [Fact]
        public void ResolveNotRequired()
        {
            var emptyByteArrayMessage = MessageBuilder.WithPayload(Array.Empty<byte>()).Build();
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
