// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Converter;
using Steeltoe.Messaging.Handler.Attributes.Test;
using Steeltoe.Messaging.Handler.Invocation.Test;
using Steeltoe.Messaging.Support;
using System;
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
            var vars = new Dictionary<string, object>
            {
                { "foo", "bar" },
                { "name", "value" }
            };

            var message = MessageBuilder.WithPayload(Array.Empty<byte>()).SetHeader(DestinationVariableMethodArgumentResolver.DESTINATION_TEMPLATE_VARIABLES_HEADER, vars).Build();

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
            var message = MessageBuilder.WithPayload(Array.Empty<byte>()).Build();
            Assert.Throws<MessageHandlingException>(() => resolver.ResolveArgument(resolvable.Annot(MessagingPredicates.DestinationVar().NoName()).Arg(), message));
        }

        private void HandleMessage([DestinationVariable] string foo, [DestinationVariable("name")] string param1, string param3)
        {
        }
    }
}
