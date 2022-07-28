// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Listener.Adapters;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.Messaging.RabbitMQ.Listener;

[Trait("Category", "Integration")]
public sealed class DirectMessageListenerContainerIntegrationTest : IDisposable
{
    public const string Q1 = "testQ1.DirectMessageListenerContainerIntegrationTests";
    public const string Q2 = "testQ2.DirectMessageListenerContainerIntegrationTests";
    public const string Eq1 = "eventTestQ1.DirectMessageListenerContainerIntegrationTests";
    public const string Eq2 = "eventTestQ2.DirectMessageListenerContainerIntegrationTests";
    public const string Dlq1 = "testDLQ1.DirectMessageListenerContainerIntegrationTests";

    private static int _testNumber = 1;

    private readonly CachingConnectionFactory _adminCf;

    private readonly RabbitAdmin _admin;

    private readonly string _testName;

    private readonly ITestOutputHelper _output;

    public DirectMessageListenerContainerIntegrationTest(ITestOutputHelper output)
    {
        _adminCf = new CachingConnectionFactory("localhost");
        _admin = new RabbitAdmin(_adminCf);
        _admin.DeclareQueue(new Config.Queue(Q1));
        _admin.DeclareQueue(new Config.Queue(Q2));
        _testName = $"DirectMessageListenerContainerIntegrationTest-{_testNumber++}";
        _output = output;
    }

    public void Dispose()
    {
        _admin.DeleteQueue(Q1);
        _admin.DeleteQueue(Q2);
        _adminCf.Dispose();
    }

    [Fact]
    public async Task TestDirect()
    {
        var cf = new CachingConnectionFactory("localhost");
        var container = new DirectMessageListenerContainer(null, cf);
        container.SetQueueNames(Q1, Q2);
        container.ConsumersPerQueue = 2;
        var listener = new ReplyingMessageListener();
        var adapter = new MessageListenerAdapter(null, listener);
        container.MessageListener = adapter;
        container.ServiceName = "simple";
        container.ConsumerTagStrategy = new TestConsumerTagStrategy(_testName);
        await container.Start();
        Assert.True(container.StartedLatch.Wait(TimeSpan.FromSeconds(10)));
        var template = new RabbitTemplate(cf);
        Assert.Equal("FOO", template.ConvertSendAndReceive<string>(Q1, "foo"));
        Assert.Equal("BAR", template.ConvertSendAndReceive<string>(Q2, "bar"));
        await container.Stop();
        Assert.True(await ConsumersOnQueue(Q1, 0));
        Assert.True(await ConsumersOnQueue(Q2, 0));
        Assert.True(await ActiveConsumerCount(container, 0));
        Assert.Empty(container.ConsumersByQueue);
        await template.Stop();
        cf.Destroy();
    }

    private async Task<bool> ConsumersOnQueue(string queue, int expected)
    {
        var n = 0;
        var currentQueueCount = -1;
        _output.WriteLine(queue + " waiting for " + expected);
        while (n++ < 600)
        {
            var queueProperties = _admin.GetQueueProperties(queue);
            if (queueProperties != null && queueProperties.TryGetValue(RabbitAdmin.QueueConsumerCount, out var count))
            {
                currentQueueCount = (int)(uint)count;
            }

            if (currentQueueCount == expected)
            {
                return true;
            }

            await Task.Delay(100);

            _output.WriteLine(queue + " waiting for " + expected + " : " + currentQueueCount);
        }

        return currentQueueCount == expected;
    }

    private async Task<bool> ActiveConsumerCount(DirectMessageListenerContainer container, int expected)
    {
        var n = 0;
        var consumers = container.Consumers;
        while (n++ < 600 && consumers.Count != expected)
        {
            await Task.Delay(100);
        }

        return consumers.Count == expected;
    }

    private sealed class ReplyingMessageListener : IReplyingMessageListener<string, string>
    {
        public string HandleMessage(string input)
        {
            if ("foo".Equals(input) || "bar".Equals(input))
            {
                return input.ToUpper();
            }
            else
            {
                return null;
            }
        }
    }

    private sealed class TestConsumerTagStrategy : IConsumerTagStrategy
    {
        private readonly string _testName;
        private int _n;

        public TestConsumerTagStrategy(string testName)
        {
            _testName = testName;
        }

        public string ServiceName { get; set; } = nameof(TestConsumerTagStrategy);

        public string CreateConsumerTag(string queue)
        {
            return $"{queue}/{_testName}{_n++}";
        }
    }
}
