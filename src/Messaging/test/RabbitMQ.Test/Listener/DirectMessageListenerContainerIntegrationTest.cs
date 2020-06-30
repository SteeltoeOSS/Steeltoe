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

using Steeltoe.Messaging.Rabbit.Connection;
using Steeltoe.Messaging.Rabbit.Core;
using Steeltoe.Messaging.Rabbit.Listener.Adapters;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.Messaging.Rabbit.Listener
{
    public class DirectMessageListenerContainerIntegrationTest : IDisposable
    {
        public const string Q1 = "testQ1.DirectMessageListenerContainerIntegrationTests";
        public const string Q2 = "testQ2.DirectMessageListenerContainerIntegrationTests";
        public const string EQ1 = "eventTestQ1.DirectMessageListenerContainerIntegrationTests";
        public const string EQ2 = "eventTestQ2.DirectMessageListenerContainerIntegrationTests";
        public const string DLQ1 = "testDLQ1.DirectMessageListenerContainerIntegrationTests";

        private static int testNumber = 1;

        private CachingConnectionFactory adminCf;

        private RabbitAdmin admin;

        private string testName;

        private ITestOutputHelper _output;

        public DirectMessageListenerContainerIntegrationTest(ITestOutputHelper output)
        {
            adminCf = new CachingConnectionFactory("localhost");
            admin = new RabbitAdmin(adminCf);
            admin.DeclareQueue(new Config.Queue(Q1));
            admin.DeclareQueue(new Config.Queue(Q2));
            testName = "DirectMessageListenerContainerIntegrationTest-" + testNumber++;
            _output = output;
        }

        public void Dispose()
        {
            admin.DeleteQueue(Q1);
            admin.DeleteQueue(Q2);
            adminCf.Dispose();
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
            container.ConsumerTagStrategy = new TestConsumerTagStrategy(testName);
            await container.Start();
            Assert.True(container._startedLatch.Wait(TimeSpan.FromSeconds(10)));
            var template = new RabbitTemplate(cf);
            Assert.Equal("FOO", template.ConvertSendAndReceive<string>(Q1, "foo"));
            Assert.Equal("BAR", template.ConvertSendAndReceive<string>(Q2, "bar"));
            await container.Stop();
            Assert.True(await ConsumersOnQueue(Q1, 0));
            Assert.True(await ConsumersOnQueue(Q2, 0));
            Assert.True(await ActiveConsumerCount(container, 0));
            Assert.Empty(container._consumersByQueue);
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
                var queueProperties = admin.GetQueueProperties(queue);
                if (queueProperties != null)
                {
                    if (queueProperties.TryGetValue(RabbitAdmin.QUEUE_CONSUMER_COUNT, out var count))
                    {
                        currentQueueCount = (int)(uint)count;
                    }
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
            var consumers = container._consumers;
            while (n++ < 600 && consumers.Count != expected)
            {
                await Task.Delay(100);
            }

            return consumers.Count == expected;
        }

        private class ReplyingMessageListener : IReplyingMessageListener<string, string>
        {
            public AcknowledgeMode ContainerAckMode { get; set; }

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

        private class TestConsumerTagStrategy : IConsumerTagStrategy
        {
            private int n;
            private string testName;

            public TestConsumerTagStrategy(string testName)
            {
                this.testName = testName;
            }

            public string ServiceName { get; set; } = nameof(TestConsumerTagStrategy);

            public string CreateConsumerTag(string queue)
            {
                return queue + "/" + testName + n++;
            }
        }
    }
}
