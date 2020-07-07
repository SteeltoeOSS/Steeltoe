// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Moq;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.Rabbit.Connection;
using Steeltoe.Messaging.Rabbit.Core;
using Steeltoe.Messaging.Rabbit.Exceptions;
using Steeltoe.Messaging.Rabbit.Support;
using Steeltoe.Messaging.Rabbit.Util;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using R = RabbitMQ.Client;

namespace Steeltoe.Messaging.Rabbit.Listener
{
    public class BlockingQueueConsumerTest
    {
        [Fact]
        public void TestRequeue()
        {
            var ex = new Exception();
            TestRequeueOrNotDefaultYes(ex, true);
        }

        [Fact]
        public void TestRequeueNullException()
        {
            TestRequeueOrNotDefaultYes(null, true);
        }

        [Fact]
        public void TestDontRequeue()
        {
            TestRequeueOrNotDefaultYes(new RabbitRejectAndDontRequeueException("fail"), false);
        }

        [Fact]
        public void TestDontRequeueNested()
        {
            var ex = new Exception(string.Empty, new Exception(string.Empty, new RabbitRejectAndDontRequeueException("fail")));
            TestRequeueOrNotDefaultYes(ex, false);
        }

        [Fact]
        public void TestRequeueDefaultNot()
        {
            TestRequeueOrNotDefaultNo(new Exception(), false);
        }

        [Fact]
        public void TestRequeueNullExceptionDefaultNot()
        {
            TestRequeueOrNotDefaultNo(null, false);
        }

        [Fact]
        public void TestDontRequeueDefaultNot()
        {
            TestRequeueOrNotDefaultNo(new RabbitRejectAndDontRequeueException("fail"), false);
        }

        [Fact]
        public void TestDontRequeueNestedDefaultNot()
        {
            var ex = new Exception(string.Empty, new Exception(string.Empty, new RabbitRejectAndDontRequeueException("fail")));
            TestRequeueOrNotDefaultNo(ex, false);
        }

        [Fact]
        public void TestDoRequeueStoppingDefaultNot()
        {
            TestRequeueOrNotDefaultNo(new MessageRejectedWhileStoppingException(), true);
        }

        [Fact]
        public void TestPrefetchIsSetOnFailedPassiveDeclaration()
        {
            var connectionFactory = new Mock<IConnectionFactory>();
            var connection = new Mock<IConnection>();
            var channel = new Mock<R.IModel>();
            connectionFactory.Setup((f) => f.CreateConnection()).Returns(connection.Object);
            connection.Setup(c => c.CreateChannel(It.IsAny<bool>())).Returns(channel.Object);
            connection.Setup(c => c.IsOpen).Returns(true);
            channel.Setup(c => c.IsOpen).Returns(true);
            var throws = new AtomicBoolean(false);
            channel.Setup(c => c.QueueDeclarePassive(It.IsAny<string>()))
                .Callback<string>((arg) =>
                {
                    if (arg != "good")
                    {
                        throws.Value = true;
                    }
                    else
                    {
                        throws.Value = false;
                    }
                })
                .Returns(() =>
                {
                    if (throws.Value)
                    {
                        throw new R.Exceptions.OperationInterruptedException(new R.ShutdownEventArgs(R.ShutdownInitiator.Peer, 0, "Expected"));
                    }

                    return new R.QueueDeclareOk("any", 0, 0);
                });
            channel.Setup(c => c.BasicConsume(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<R.IBasicConsumer>()))
                .Returns("consumerTag");
            var blockingQueueConsumer = new BlockingQueueConsumer(
                connectionFactory.Object,
                new DefaultMessageHeadersConverter(),
                new ActiveObjectCounter<BlockingQueueConsumer>(),
                AcknowledgeMode.AUTO,
                true,
                20,
                null,
                "good",
                "bad")
            {
                DeclarationRetries = 1,
                RetryDeclarationInterval = 10,
                FailedDeclarationRetryInterval = 10
            };
            blockingQueueConsumer.Start();
            channel.Verify(c => c.BasicQos(It.IsAny<uint>(), 20, It.IsAny<bool>()));
            blockingQueueConsumer.Stop();
        }

        [Fact]
        public void TestNoLocalConsumerConfiguration()
        {
            var connectionFactory = new Mock<IConnectionFactory>();
            var connection = new Mock<IConnection>();
            var channel = new Mock<R.IModel>();
            connectionFactory.Setup((f) => f.CreateConnection()).Returns(connection.Object);
            connection.Setup(c => c.CreateChannel(It.IsAny<bool>())).Returns(channel.Object);
            connection.Setup(c => c.IsOpen).Returns(true);
            channel.Setup(c => c.IsOpen).Returns(true);

            var queue = "testQ";
            var noLocal = true;
            var blockingQueueConsumer = new BlockingQueueConsumer(
                connectionFactory.Object,
                new DefaultMessageHeadersConverter(),
                new ActiveObjectCounter<BlockingQueueConsumer>(),
                AcknowledgeMode.AUTO,
                true,
                1,
                true,
                null,
                noLocal,
                false,
                null,
                queue);
            blockingQueueConsumer.Start();
            channel.Verify(c => c.BasicConsume("testQ", AcknowledgeMode.AUTO.IsAutoAck(), string.Empty, noLocal, false, It.IsAny<IDictionary<string, object>>(), It.IsAny<R.IBasicConsumer>()));
            blockingQueueConsumer.Stop();
        }

        [Fact]
        public void TestRecoverAfterDeletedQueueAndLostConnection()
        {
            var connectionFactory = new Mock<IConnectionFactory>();
            var connection = new Mock<IConnection>();
            var channel = new Mock<R.IModel>();
            connectionFactory.Setup((f) => f.CreateConnection()).Returns(connection.Object);
            connection.Setup(c => c.CreateChannel(It.IsAny<bool>())).Returns(channel.Object);
            connection.Setup(c => c.IsOpen).Returns(true);
            channel.Setup(c => c.IsOpen).Returns(true);
            var n = new AtomicInteger();
            var consumerCaptor = new AtomicReference<R.IBasicConsumer>();
            var consumerLatch = new CountdownEvent(2);
            channel.Setup(c => c.BasicConsume(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<R.IBasicConsumer>()))
                .Callback<string, bool, string, bool, bool, IDictionary<string, object>, R.IBasicConsumer>((a1, a2, a3, a4, a5, a6, a7) =>
                {
                    consumerCaptor.Value = a7;
                    consumerLatch.Signal();
                })
               .Returns("consumer" + n.IncrementAndGet());
            channel.Setup(c => c.BasicCancel("consumer2"))
                .Throws(new Exception("Intentional cancel fail"));
            var blockingQueueConsumer = new BlockingQueueConsumer(
                connectionFactory.Object,
                new DefaultMessageHeadersConverter(),
                new ActiveObjectCounter<BlockingQueueConsumer>(),
                AcknowledgeMode.AUTO,
                false,
                1,
                null,
                "testQ1",
                "testQ2");
            var latch = new CountdownEvent(1);
            Task.Run(() =>
            {
                blockingQueueConsumer.Start();
                while (true)
                {
                    try
                    {
                        blockingQueueConsumer.NextMessage(1000);
                    }
                    catch (ConsumerCancelledException)
                    {
                        latch.Signal();
                        break;
                    }
                    catch (ShutdownSignalException)
                    {
                        // Noop
                    }
                    catch (Exception)
                    {
                        // noop
                    }
                }
            });
            Assert.True(consumerLatch.Wait(TimeSpan.FromSeconds(10)));
            var consumer = consumerCaptor.Value;
            consumer.HandleBasicCancel("consumer1");
            Assert.True(latch.Wait(TimeSpan.FromSeconds(10)));
        }

        private void TestRequeueOrNotDefaultYes(Exception ex, bool expectedRequeue)
        {
            var connectionFactory = new Mock<IConnectionFactory>();
            var channel = new Mock<R.IModel>();
            var blockingQueueConsumer = new BlockingQueueConsumer(
                connectionFactory.Object,
                new DefaultMessageHeadersConverter(),
                new ActiveObjectCounter<BlockingQueueConsumer>(),
                AcknowledgeMode.AUTO,
                true,
                1,
                null,
                "testQ");
            TestRequeueOrNotGuts(ex, expectedRequeue, channel, blockingQueueConsumer);
        }

        private void TestRequeueOrNotDefaultNo(Exception ex, bool expectedRequeue)
        {
            var connectionFactory = new Mock<IConnectionFactory>();
            var channel = new Mock<R.IModel>();
            var blockingQueueConsumer = new BlockingQueueConsumer(
               connectionFactory.Object,
               new DefaultMessageHeadersConverter(),
               new ActiveObjectCounter<BlockingQueueConsumer>(),
               AcknowledgeMode.AUTO,
               true,
               1,
               false,
               null,
               "testQ");

            TestRequeueOrNotGuts(ex, expectedRequeue, channel, blockingQueueConsumer);
        }

        private void TestRequeueOrNotGuts(Exception ex, bool expectedRequeue, Mock<R.IModel> channel, BlockingQueueConsumer blockingQueueConsumer)
        {
            blockingQueueConsumer.Channel = channel.Object;
            var deliveryTags = new HashSet<ulong>
            {
                1UL
            };
            blockingQueueConsumer.DeliveryTags = deliveryTags;
            blockingQueueConsumer.RollbackOnExceptionIfNecessary(ex);
            channel.Verify((c) => c.BasicNack(1UL, true, expectedRequeue));
        }
    }
}
