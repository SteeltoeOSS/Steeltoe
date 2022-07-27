// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Messaging.RabbitMQ.Config;

public class QueueBuilderTest
{
    [Fact]
    public void BuildsDurableQueue()
    {
        var queue = QueueBuilder.Durable("name").Build();

        Assert.True(queue.IsDurable);
        Assert.Equal("name", queue.QueueName);
    }

    [Fact]
    public void BuildsNonDurableQueue()
    {
        var queue = QueueBuilder.NonDurable("name").Build();

        Assert.False(queue.IsDurable);
        Assert.Equal("name", queue.QueueName);
    }

    [Fact]
    public void BuildsAutoDeleteQueue()
    {
        var queue = QueueBuilder.Durable("name").AutoDelete().Build();

        Assert.True(queue.IsAutoDelete);
    }

    [Fact]
    public void BuildsExclusiveQueue()
    {
        var queue = QueueBuilder.Durable("name").Exclusive().Build();

        Assert.True(queue.IsExclusive);
    }

    [Fact]
    public void AddsArguments()
    {
        var queue = QueueBuilder.Durable("name")
            .WithArgument("key1", "value1")
            .WithArgument("key2", "value2")
            .Build();

        var args = queue.Arguments;

        Assert.Equal("value1", args["key1"]);
        Assert.Equal("value2", args["key2"]);
    }

    [Fact]
    public void AddsMultipleArgumentsAtOnce()
    {
        var arguments = new Dictionary<string, object>
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };

        var queue = QueueBuilder.Durable("name").WithArguments(arguments).Build();
        var args = queue.Arguments;

        Assert.Equal("value1", args["key1"]);
        Assert.Equal("value2", args["key2"]);
    }
}