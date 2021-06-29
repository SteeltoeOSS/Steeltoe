﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Converter;
using Steeltoe.Messaging.Handler.Attributes.Test;
using Steeltoe.Messaging.Handler.Invocation.Test;
using Steeltoe.Messaging.Support;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Messaging.Handler.Attributes.Support.Test
{
    public class HeaderMethodArgumentResolverTest
    {
        private readonly HeaderMethodArgumentResolver resolver = new HeaderMethodArgumentResolver(new DefaultConversionService());

        private readonly ResolvableMethod resolvable = ResolvableMethod.On<HeaderMethodArgumentResolverTest>().Named("HandleMessage").Build();

        [Fact]
        public void SupportsParameter()
        {
            Assert.True(resolver.SupportsParameter(resolvable.Annot(MessagingPredicates.HeaderPlain()).Arg()));
            Assert.False(resolver.SupportsParameter(resolvable.AnnotNotPresent(typeof(HeaderAttribute)).Arg()));
        }

        [Fact]
        public void ResolveArgument()
        {
            var message = MessageBuilder.WithPayload(new byte[0]).SetHeader("param1", "foo").Build();
            var result = resolver.ResolveArgument(resolvable.Annot(MessagingPredicates.HeaderPlain()).Arg(), message);
            Assert.Equal("foo", result);
        }

        [Fact]
        public void ResolveArgumentNativeHeader()
        {
            var headers = new TestMessageHeaderAccessor();
            headers.SetNativeHeader("param1", "foo");
            var message = MessageBuilder.WithPayload(new byte[0]).SetHeaders(headers).Build();
            Assert.Equal("foo", resolver.ResolveArgument(resolvable.Annot(MessagingPredicates.HeaderPlain()).Arg(), message));
        }

        [Fact]
        public void ResolveArgumentNativeHeaderAmbiguity()
        {
            var headers = new TestMessageHeaderAccessor();
            headers.SetHeader("param1", "foo");
            headers.SetNativeHeader("param1", "native-foo");
            var message = MessageBuilder.WithPayload(new byte[0]).SetHeaders(headers).Build();

            Assert.Equal("foo", resolver.ResolveArgument(resolvable.Annot(MessagingPredicates.HeaderPlain()).Arg(), message));
            Assert.Equal("native-foo", resolver.ResolveArgument(resolvable.Annot(MessagingPredicates.Header("nativeHeaders.param1")).Arg(), message));
        }

        [Fact]
        public void ResolveArgumentNotFound()
        {
            var message = MessageBuilder.WithPayload(new byte[0]).Build();
            Assert.Throws<MessageHandlingException>(() => resolver.ResolveArgument(resolvable.Annot(MessagingPredicates.HeaderPlain()).Arg(), message));
        }

        [Fact]
        public void ResolveArgumentDefaultValue()
        {
            var message = MessageBuilder.WithPayload(new byte[0]).Build();
            var result = resolver.ResolveArgument(resolvable.Annot(MessagingPredicates.Header("name", "bar")).Arg(), message);
            Assert.Equal("bar", result);
        }

        [Fact]
        public void ResolveOptionalHeaderWithValue()
        {
            var message = MessageBuilder.WithPayload("foo").SetHeader("foo", "bar").Build();
            var param = resolvable.Annot(MessagingPredicates.Header("foo")).Arg();
            var result = resolver.ResolveArgument(param, message);
            Assert.Equal("bar", result);
        }

        [Fact]
        public void ResolveOptionalHeaderAsEmpty()
        {
            var message = MessageBuilder.WithPayload("foo").Build();
            var param = resolvable.Annot(MessagingPredicates.Header("foo")).Arg();
            var result = resolver.ResolveArgument(param, message);
            Assert.Null(result);
        }

        private void HandleMessage(
                [Header] string param1,
                [Header(Name = "name", DefaultValue = "bar")] string param2,
                [Header(Name = "name", DefaultValue = "#{systemProperties.systemProperty}")] string param3,
                [Header(Name = "#{systemProperties.systemProperty}")] string param4,
                string param5,
                [Header("nativeHeaders.param1")] string nativeHeaderParam1,
                [Header("foo")] string param6 = null)
        {
        }

        internal class TestMessageHeaderAccessor : NativeMessageHeaderAccessor
        {
            public TestMessageHeaderAccessor()
                : base((IDictionary<string, List<string>>)null)
            {
            }
        }
    }
}
