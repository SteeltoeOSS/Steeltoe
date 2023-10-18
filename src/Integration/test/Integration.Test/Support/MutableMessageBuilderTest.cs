// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using Xunit;

namespace Steeltoe.Integration.Test.Support;

public sealed class MutableMessageBuilderTest
{
    [Fact]
    public void Mutable()
    {
        MutableIntegrationMessageBuilder<string> builder = MutableIntegrationMessageBuilder<string>.WithPayload("test");
        IMessage<string> message1 = builder.SetHeader("foo", "bar").Build();
        IMessage<string> message2 = MutableIntegrationMessageBuilder<string>.FromMessage(message1).SetHeader("another", 1).Build();
        Assert.Equal("bar", message2.Headers["foo"]);
        Assert.Equal(message1.Headers.Id, message2.Headers.Id);
        Assert.True(message2 == message1);
    }

    [Fact]
    public void MutableFromImmutable()
    {
        IMessage<string> message1 = IntegrationMessageBuilder<string>.WithPayload("test").SetHeader("foo", "bar").Build();
        IMessage<string> message2 = MutableIntegrationMessageBuilder<string>.FromMessage(message1).SetHeader("another", 1).Build();
        Assert.Equal("bar", message2.Headers["foo"]);
        Assert.Equal(message1.Headers.Id, message2.Headers.Id);
        Assert.NotEqual(message1, message2);
        Assert.False(message2 == message1);
    }

    [Fact]
    public void MutableFromImmutableMutate()
    {
        IMessage<string> message1 = IntegrationMessageBuilder<string>.WithPayload("test").SetHeader("foo", "bar").Build();
        IMessage<string> message2 = new MutableIntegrationMessageBuilderFactory().FromMessage(message1).SetHeader("another", 1).Build();
        Assert.Equal("bar", message2.Headers["foo"]);
        Assert.Equal(message1.Headers.Id, message2.Headers.Id);
        Assert.NotEqual(message1, message2);
        Assert.False(message2 == message1);
    }

    [Fact]
    public void TestPushAndPopSequenceDetailsMutable()
    {
        IMessage<int> message1 = MutableIntegrationMessageBuilder<int>.WithPayload(1).PushSequenceDetails("foo", 1, 2).Build();
        Assert.False(message1.Headers.ContainsKey(IntegrationMessageHeaderAccessor.SequenceDetails));
        IMessage<int> message2 = MutableIntegrationMessageBuilder<int>.FromMessage(message1).PushSequenceDetails("bar", 1, 1).Build();
        Assert.True(message2.Headers.ContainsKey(IntegrationMessageHeaderAccessor.SequenceDetails));
        IMessage<int> message3 = MutableIntegrationMessageBuilder<int>.FromMessage(message2).PopSequenceDetails().Build();
        Assert.False(message3.Headers.ContainsKey(IntegrationMessageHeaderAccessor.SequenceDetails));
    }

    [Fact]
    public void TestPushAndPopSequenceDetailsWhenNoCorrelationIdMutable()
    {
        IMessage<int> message1 = MutableIntegrationMessageBuilder<int>.WithPayload(1).Build();
        Assert.False(message1.Headers.ContainsKey(IntegrationMessageHeaderAccessor.SequenceDetails));
        IMessage<int> message2 = MutableIntegrationMessageBuilder<int>.FromMessage(message1).PushSequenceDetails("bar", 1, 1).Build();
        Assert.False(message2.Headers.ContainsKey(IntegrationMessageHeaderAccessor.SequenceDetails));
        IMessage<int> message3 = MutableIntegrationMessageBuilder<int>.FromMessage(message2).PopSequenceDetails().Build();
        Assert.False(message3.Headers.ContainsKey(IntegrationMessageHeaderAccessor.SequenceDetails));
    }

    [Fact]
    public void TestPopSequenceDetailsWhenNotPoppedMutable()
    {
        IMessage<int> message1 = MutableIntegrationMessageBuilder<int>.WithPayload(1).Build();
        Assert.False(message1.Headers.ContainsKey(IntegrationMessageHeaderAccessor.SequenceDetails));
        IMessage<int> message2 = MutableIntegrationMessageBuilder<int>.FromMessage(message1).PopSequenceDetails().Build();
        Assert.False(message2.Headers.ContainsKey(IntegrationMessageHeaderAccessor.SequenceDetails));
    }

    [Fact]
    public void TestPushAndPopSequenceDetailsWhenNoSequenceMutable()
    {
        IMessage<int> message1 = MutableIntegrationMessageBuilder<int>.WithPayload(1).SetCorrelationId("foo").Build();
        Assert.False(message1.Headers.ContainsKey(IntegrationMessageHeaderAccessor.SequenceDetails));
        IMessage<int> message2 = MutableIntegrationMessageBuilder<int>.FromMessage(message1).PushSequenceDetails("bar", 1, 1).Build();
        Assert.True(message2.Headers.ContainsKey(IntegrationMessageHeaderAccessor.SequenceDetails));
        IMessage<int> message3 = MutableIntegrationMessageBuilder<int>.FromMessage(message2).PopSequenceDetails().Build();
        Assert.False(message3.Headers.ContainsKey(IntegrationMessageHeaderAccessor.SequenceDetails));
    }

    [Fact]
    public void TestNoIdAndTimestampHeaders()
    {
        IMessage<string> message = MutableIntegrationMessageBuilder<string>.WithPayload("foo", false).PushSequenceDetails("bar", 1, 1).Build();
        Assert.True(message.Headers.ContainsKey(IntegrationMessageHeaderAccessor.CorrelationId));
        Assert.True(message.Headers.ContainsKey(IntegrationMessageHeaderAccessor.SequenceNumber));
        Assert.True(message.Headers.ContainsKey(IntegrationMessageHeaderAccessor.SequenceSize));
        Assert.False(message.Headers.ContainsKey(IntegrationMessageHeaderAccessor.SequenceDetails));
        Assert.False(message.Headers.ContainsKey(MessageHeaders.IdName));
        Assert.False(message.Headers.ContainsKey(MessageHeaders.TimestampName));
    }
}
