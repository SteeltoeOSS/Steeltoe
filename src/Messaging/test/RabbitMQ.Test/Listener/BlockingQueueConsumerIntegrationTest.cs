// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.RabbitMQ.Config;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Listener.Exceptions;
using Steeltoe.Messaging.RabbitMQ.Support;
using Steeltoe.Messaging.RabbitMQ.Util;
using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;
using RC = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Listener;

[Trait("Category", "Integration")]
public class BlockingQueueConsumerIntegrationTest
{
    public const string Queue1Name = "test.queue1.BlockingQueueConsumerIntegrationTests";
    public const string Queue2Name = "test.queue2.BlockingQueueConsumerIntegrationTests";

    [Fact]
    public void TestTransactionalLowLevel()
    {
        var connectionFactory = new CachingConnectionFactory("localhost");
        var admin = new RabbitAdmin(connectionFactory);
        admin.DeclareQueue(new Queue(Queue1Name));
        admin.DeclareQueue(new Queue(Queue2Name));
        var template = new RabbitTemplate(connectionFactory);
        var blockingQueueConsumer = new BlockingQueueConsumer(
            connectionFactory,
            new DefaultMessageHeadersConverter(),
            new ActiveObjectCounter<BlockingQueueConsumer>(),
            AcknowledgeMode.Auto,
            true,
            1,
            null,
            Queue1Name,
            Queue2Name);
        var prefix = Guid.NewGuid().ToString();
        blockingQueueConsumer.TagStrategy = new TagStrategy(prefix);
        try
        {
            blockingQueueConsumer.Start();
            var n = 0;
            var consumers = blockingQueueConsumer.CurrentConsumers();

            // Wait for consumers
            while (n < 100)
            {
                if (consumers.Count < 2)
                {
                    n++;
                    Thread.Sleep(100);
                    consumers = blockingQueueConsumer.CurrentConsumers();
                }
                else
                {
                    break;
                }
            }

            Assert.Equal(2, consumers.Count);
            var tags = new List<string> { consumers[0].ConsumerTag, consumers[1].ConsumerTag };
            Assert.Contains($"{prefix}#{Queue1Name}", tags);
            Assert.Contains($"{prefix}#{Queue2Name}", tags);
            blockingQueueConsumer.Stop();
            Assert.Null(template.ReceiveAndConvert<object>(Queue1Name));
        }
        finally
        {
            admin.DeleteQueue(Queue1Name);
            admin.DeleteQueue(Queue2Name);
            connectionFactory.Destroy();
        }
    }

    [Fact]
    public void TestAvoidHangAMQP_508()
    {
        var connectionFactory = new CachingConnectionFactory("localhost");
        var longName = new string('x', 300);
        var blockingQueueConsumer = new BlockingQueueConsumer(
            connectionFactory,
            new DefaultMessageHeadersConverter(),
            new ActiveObjectCounter<BlockingQueueConsumer>(),
            AcknowledgeMode.Auto,
            true,
            1,
            null,
            longName,
            "foobar");
        try
        {
            blockingQueueConsumer.Start();
            throw new Exception("Expected exception");
        }
        catch (FatalListenerStartupException e)
        {
            Assert.IsType<RC.Exceptions.WireFormattingException>(e.InnerException);
        }
        finally
        {
            connectionFactory.Destroy();
        }
    }

    public class TagStrategy : IConsumerTagStrategy
    {
        public TagStrategy(string prefix)
        {
            Prefix = prefix;
        }

        public string ServiceName { get; set; } = nameof(TagStrategy);

        public string Prefix { get; }

        public string CreateConsumerTag(string queue)
        {
            return $"{Prefix}#{queue}";
        }
    }
}
