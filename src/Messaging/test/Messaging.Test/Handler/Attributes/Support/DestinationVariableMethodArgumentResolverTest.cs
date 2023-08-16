// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Steeltoe.Common.Converter;
using Steeltoe.Messaging.Handler.Attributes;
using Steeltoe.Messaging.Handler.Attributes.Support;
using Steeltoe.Messaging.Support;
using Steeltoe.Messaging.Test.Handler.Invocation;
using Xunit;

namespace Steeltoe.Messaging.Test.Handler.Attributes.Support;

public sealed class DestinationVariableMethodArgumentResolverTest
{
    private readonly DestinationVariableMethodArgumentResolver _resolver = new(new DefaultConversionService());
    private readonly ResolvableMethod _resolvable = ResolvableMethod.On<DestinationVariableMethodArgumentResolverTest>().Named(nameof(HandleMessage)).Build();

    [Fact]
    public void SupportsParameter()
    {
        Assert.True(_resolver.SupportsParameter(_resolvable.Annotation(MessagingPredicates.DestinationVar().NoName()).Arg()));
        Assert.False(_resolver.SupportsParameter(_resolvable.AnnotationNotPresent(typeof(DestinationVariableAttribute)).Arg()));
    }

    [Fact]
    public void ResolveArgument()
    {
        var vars = new Dictionary<string, object>
        {
            { "foo", "bar" },
            { "name", "value" }
        };

        IMessage message = MessageBuilder.WithPayload(Array.Empty<byte>())
            .SetHeader(DestinationVariableMethodArgumentResolver.DestinationTemplateVariablesHeader, vars).Build();

        ParameterInfo param = _resolvable.Annotation(MessagingPredicates.DestinationVar().NoName()).Arg();
        object result = _resolver.ResolveArgument(param, message);
        Assert.Equal("bar", result);

        param = _resolvable.Annotation(MessagingPredicates.DestinationVar("name")).Arg();
        result = _resolver.ResolveArgument(param, message);
        Assert.Equal("value", result);
    }

    [Fact]
    public void ResolveArgumentNotFound()
    {
        IMessage message = MessageBuilder.WithPayload(Array.Empty<byte>()).Build();

        Assert.Throws<MessageHandlingException>(() =>
            _resolver.ResolveArgument(_resolvable.Annotation(MessagingPredicates.DestinationVar().NoName()).Arg(), message));
    }

    private void HandleMessage([DestinationVariable] string foo, [DestinationVariable("name")] string param1, string param3)
    {
    }
}
