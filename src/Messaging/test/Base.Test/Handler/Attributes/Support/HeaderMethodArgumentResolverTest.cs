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
            var message = MessageBuilder<byte[]>.WithPayload(System.Array.Empty<byte>()).SetHeader("param1", "foo").Build();
            var result = resolver.ResolveArgument(resolvable.Annot(MessagingPredicates.HeaderPlain()).Arg(), message);
            Assert.Equal("foo", result);
        }

        [Fact]
        public void ResolveArgumentNativeHeader()
        {
            var headers = new TestMessageHeaderAccessor();
            headers.SetNativeHeader("param1", "foo");
            var message = MessageBuilder<byte[]>.WithPayload(System.Array.Empty<byte>()).SetHeaders(headers).Build();
            Assert.Equal("foo", resolver.ResolveArgument(resolvable.Annot(MessagingPredicates.HeaderPlain()).Arg(), message));
        }

        [Fact]
        public void ResolveArgumentNativeHeaderAmbiguity()
        {
            var headers = new TestMessageHeaderAccessor();
            headers.SetHeader("param1", "foo");
            headers.SetNativeHeader("param1", "native-foo");
            var message = MessageBuilder<byte[]>.WithPayload(System.Array.Empty<byte>()).SetHeaders(headers).Build();

            Assert.Equal("foo", resolver.ResolveArgument(resolvable.Annot(MessagingPredicates.HeaderPlain()).Arg(), message));
            Assert.Equal("native-foo", resolver.ResolveArgument(resolvable.Annot(MessagingPredicates.Header("nativeHeaders.param1")).Arg(), message));
        }

        [Fact]
        public void ResolveArgumentNotFound()
        {
            var message = MessageBuilder<byte[]>.WithPayload(System.Array.Empty<byte>()).Build();
            Assert.Throws<MessageHandlingException>(() => resolver.ResolveArgument(resolvable.Annot(MessagingPredicates.HeaderPlain()).Arg(), message));
        }

        [Fact]
        public void ResolveArgumentDefaultValue()
        {
            var message = MessageBuilder<byte[]>.WithPayload(System.Array.Empty<byte>()).Build();
            var result = resolver.ResolveArgument(resolvable.Annot(MessagingPredicates.Header("name", "bar")).Arg(), message);
            Assert.Equal("bar", result);
        }

        [Fact]
        public void ResolveOptionalHeaderWithValue()
        {
            var message = MessageBuilder<string>.WithPayload("foo").SetHeader("foo", "bar").Build();
            var param = resolvable.Annot(MessagingPredicates.Header("foo")).Arg();
            var result = resolver.ResolveArgument(param, message);
            Assert.Equal("bar", result);
        }

        [Fact]
        public void ResolveOptionalHeaderAsEmpty()
        {
            var message = MessageBuilder<string>.WithPayload("foo").Build();
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
