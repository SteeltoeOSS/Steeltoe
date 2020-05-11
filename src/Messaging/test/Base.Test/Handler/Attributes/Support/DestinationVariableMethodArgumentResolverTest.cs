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
    public class DestinationVariableMethodArgumentResolverTest
    {
        private readonly DestinationVariableMethodArgumentResolver resolver = new DestinationVariableMethodArgumentResolver(new DefaultConversionService());
        private readonly ResolvableMethod resolvable = ResolvableMethod.On<DestinationVariableMethodArgumentResolverTest>().Named("HandleMessage").Build();

        [Fact]
        public void SupportsParameter()
        {
            Assert.True(resolver.SupportsParameter(resolvable.Annot(MessagingPredicates.DestinationVar().NoName()).Arg()));
            Assert.False(resolver.SupportsParameter(resolvable.AnnotNotPresent(typeof(DestinationVariableAttribute)).Arg()));
        }

        [Fact]
        public void ResolveArgument()
        {
            var vars = new Dictionary<string, object>();
            vars.Add("foo", "bar");
            vars.Add("name", "value");

            var message = MessageBuilder<byte[]>.WithPayload(System.Array.Empty<byte>()).SetHeader(DestinationVariableMethodArgumentResolver.DESTINATION_TEMPLATE_VARIABLES_HEADER, vars).Build();

            var param = resolvable.Annot(MessagingPredicates.DestinationVar().NoName()).Arg();
            var result = resolver.ResolveArgument(param, message);
            Assert.Equal("bar", result);

            param = resolvable.Annot(MessagingPredicates.DestinationVar("name")).Arg();
            result = resolver.ResolveArgument(param, message);
            Assert.Equal("value", result);
        }

        [Fact]
        public void ResolveArgumentNotFound()
        {
            var message = MessageBuilder<byte[]>.WithPayload(System.Array.Empty<byte>()).Build();
            Assert.Throws<MessageHandlingException>(() => resolver.ResolveArgument(resolvable.Annot(MessagingPredicates.DestinationVar().NoName()).Arg(), message));
        }

        private void HandleMessage([DestinationVariable] string foo, [DestinationVariable("name")] string param1, string param3)
        {
        }
    }
}
