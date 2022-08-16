// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Expression.Internal.Contexts;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.RabbitMQ.Batch;
using Steeltoe.Messaging.RabbitMQ.Config;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Extensions;
using Steeltoe.Messaging.RabbitMQ.Listener;
using Steeltoe.Messaging.RabbitMQ.Support;
using Steeltoe.Messaging.RabbitMQ.Support.PostProcessor;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.Messaging.RabbitMQ.Core;

[Trait("Category", "Integration")]
public sealed class BatchingRabbitTemplateTest : IDisposable
{
    public const string Route = "test.queue.BatchingRabbitTemplateTests";
    private readonly CachingConnectionFactory _connectionFactory;
    private readonly ITestOutputHelper _testOutputHelper;

    public BatchingRabbitTemplateTest(ITestOutputHelper testOutputHelper)
    {
        _connectionFactory = new CachingConnectionFactory("localhost");
        var admin = new RabbitAdmin(_connectionFactory);
        admin.DeclareQueue(new Queue(Route));
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void TestSimpleBatch()
    {
        var batchingStrategy = new SimpleBatchingStrategy(2, int.MaxValue, 30000);

        var template = new BatchingRabbitTemplate(batchingStrategy)
        {
            ConnectionFactory = _connectionFactory
        };

        IMessage<byte[]> message = Message.Create(Encoding.UTF8.GetBytes("foo"));
        template.Send(string.Empty, Route, message);
        message = Message.Create(Encoding.UTF8.GetBytes("bar"));
        template.Send(string.Empty, Route, message);
        IMessage received = Receive(template);
        Assert.Equal("\u0000\u0000\u0000\u0003foo\u0000\u0000\u0000\u0003bar", Encoding.UTF8.GetString((byte[])received.Payload));
    }

    [Fact]
    public void TestSimpleBatchTimeout()
    {
        var batchingStrategy = new SimpleBatchingStrategy(2, int.MaxValue, 50);

        var template = new BatchingRabbitTemplate(batchingStrategy)
        {
            ConnectionFactory = _connectionFactory
        };

        IMessage<byte[]> message = Message.Create(Encoding.UTF8.GetBytes("foo"));
        template.Send(string.Empty, Route, message);
        IMessage received = Receive(template);
        Assert.Equal("foo", Encoding.UTF8.GetString((byte[])received.Payload));
    }

    [Fact]
    public void TestSimpleBatchTimeoutMultiple()
    {
        var batchingStrategy = new SimpleBatchingStrategy(2, int.MaxValue, 50);

        var template = new BatchingRabbitTemplate(batchingStrategy)
        {
            ConnectionFactory = _connectionFactory
        };

        IMessage<byte[]> message = Message.Create(Encoding.UTF8.GetBytes("foo"));
        template.Send(string.Empty, Route, message);
        template.Send(string.Empty, Route, message);
        IMessage received = Receive(template);
        Assert.Equal("\u0000\u0000\u0000\u0003foo\u0000\u0000\u0000\u0003foo", Encoding.UTF8.GetString((byte[])received.Payload));
    }

    [Fact]
    public void TestSimpleBatchBufferLimit()
    {
        var batchingStrategy = new SimpleBatchingStrategy(2, 8, 50);

        var template = new BatchingRabbitTemplate(batchingStrategy)
        {
            ConnectionFactory = _connectionFactory
        };

        IMessage<byte[]> message = Message.Create(Encoding.UTF8.GetBytes("foo"));
        template.Send(string.Empty, Route, message);
        message = Message.Create(Encoding.UTF8.GetBytes("bar"));
        template.Send(string.Empty, Route, message);
        IMessage received = Receive(template);
        Assert.Equal("foo", Encoding.UTF8.GetString((byte[])received.Payload));
        received = Receive(template);
        Assert.Equal("bar", Encoding.UTF8.GetString((byte[])received.Payload));
    }

    [Fact]
    public void TestSimpleBatchBufferLimitMultiple()
    {
        var batchingStrategy = new SimpleBatchingStrategy(2, 15, 30000);

        var template = new BatchingRabbitTemplate(batchingStrategy)
        {
            ConnectionFactory = _connectionFactory
        };

        IMessage<byte[]> message = Message.Create(Encoding.UTF8.GetBytes("foo"));
        template.Send(string.Empty, Route, message);
        template.Send(string.Empty, Route, message);
        message = Message.Create(Encoding.UTF8.GetBytes("bar"));
        template.Send(string.Empty, Route, message);
        template.Send(string.Empty, Route, message);
        IMessage received = Receive(template);
        Assert.Equal("\u0000\u0000\u0000\u0003foo\u0000\u0000\u0000\u0003foo", Encoding.UTF8.GetString((byte[])received.Payload));
        received = Receive(template);
        Assert.Equal("\u0000\u0000\u0000\u0003bar\u0000\u0000\u0000\u0003bar", Encoding.UTF8.GetString((byte[])received.Payload));
    }

    [Fact]
    public void TestSimpleBatchBiggerThanBufferLimit()
    {
        var batchingStrategy = new SimpleBatchingStrategy(2, 2, 30000);

        var template = new BatchingRabbitTemplate(batchingStrategy)
        {
            ConnectionFactory = _connectionFactory
        };

        IMessage<byte[]> message = Message.Create(Encoding.UTF8.GetBytes("foo"));
        template.Send(string.Empty, Route, message);
        message = Message.Create(Encoding.UTF8.GetBytes("bar"));
        template.Send(string.Empty, Route, message);
        IMessage received = Receive(template);
        Assert.Equal("foo", Encoding.UTF8.GetString((byte[])received.Payload));
        received = Receive(template);
        Assert.Equal("bar", Encoding.UTF8.GetString((byte[])received.Payload));
    }

    [Fact]
    public void TestSimpleBatchBiggerThanBufferLimitMultiple()
    {
        var batchingStrategy = new SimpleBatchingStrategy(2, 6, 30000);

        var template = new BatchingRabbitTemplate(batchingStrategy)
        {
            ConnectionFactory = _connectionFactory
        };

        IMessage<byte[]> message = Message.Create(Encoding.UTF8.GetBytes("f"));
        template.Send(string.Empty, Route, message);
        message = Message.Create(Encoding.UTF8.GetBytes("bar"));
        template.Send(string.Empty, Route, message);
        IMessage received = Receive(template);
        Assert.Equal("f", Encoding.UTF8.GetString((byte[])received.Payload));
        received = Receive(template);
        Assert.Equal("bar", Encoding.UTF8.GetString((byte[])received.Payload));
    }

    [Fact]
    public void TestSimpleBatchTwoEqualBufferLimit()
    {
        var batchingStrategy = new SimpleBatchingStrategy(10, 14, 30000);

        var template = new BatchingRabbitTemplate(batchingStrategy)
        {
            ConnectionFactory = _connectionFactory
        };

        IMessage<byte[]> message = Message.Create(Encoding.UTF8.GetBytes("foo"));
        template.Send(string.Empty, Route, message);
        message = Message.Create(Encoding.UTF8.GetBytes("bar"));
        template.Send(string.Empty, Route, message);
        IMessage received = Receive(template);
        Assert.Equal("\u0000\u0000\u0000\u0003foo\u0000\u0000\u0000\u0003bar", Encoding.UTF8.GetString((byte[])received.Payload));
    }

    [Fact]
    public void TestDebatchByContainer()
    {
        ServiceProvider provider = new ServiceCollection().BuildServiceProvider();
        IConfigurationRoot config = new ConfigurationBuilder().Build();
        var received = new List<IMessage>();
        var latch = new CountdownEvent(2);
        var context = new GenericApplicationContext(provider, config);
        context.ServiceExpressionResolver = new StandardServiceExpressionResolver();
        var container = new DirectMessageListenerContainer(context, _connectionFactory);
        container.SetQueueNames(Route);
        var lastInBatch = new List<bool>();
        var batchSize = new AtomicInteger();
        container.MessageListener = new TestDebatchListener(received, lastInBatch, batchSize, latch);
        container.Initialize();
        container.StartAsync();

        try
        {
            var batchingStrategy = new SimpleBatchingStrategy(2, int.MaxValue, 30000);

            var template = new BatchingRabbitTemplate(batchingStrategy)
            {
                ConnectionFactory = _connectionFactory
            };

            IMessage<byte[]> message = Message.Create(Encoding.UTF8.GetBytes("foo"));
            template.Send(string.Empty, Route, message);
            message = Message.Create(Encoding.UTF8.GetBytes("bar"));
            template.Send(string.Empty, Route, message);
            Assert.True(latch.Wait(TimeSpan.FromSeconds(10)));
            Assert.Equal(2, received.Count);
            Assert.Equal("foo", Encoding.UTF8.GetString((byte[])received[0].Payload));
            Assert.Equal(3, received[0].Headers.ContentLength());
            Assert.False(lastInBatch[0]);

            Assert.Equal("bar", Encoding.UTF8.GetString((byte[])received[1].Payload));
            Assert.Equal(3, received[1].Headers.ContentLength());
            Assert.True(lastInBatch[1]);
            Assert.Equal(2, batchSize.Value);
        }
        finally
        {
            container.StopAsync();
            container.Dispose();
        }
    }

    [Fact]
    public void TestDebatchByContainerPerformance()
    {
        ServiceProvider provider = new ServiceCollection().BuildServiceProvider();
        IConfigurationRoot config = new ConfigurationBuilder().Build();
        var received = new List<IMessage>();
        const int count = 10_000;
        var latch = new CountdownEvent(count);
        var context = new GenericApplicationContext(provider, config);
        context.ServiceExpressionResolver = new StandardServiceExpressionResolver();
        var container = new DirectMessageListenerContainer(context, _connectionFactory);
        container.SetQueueNames(Route);
        container.MessageListener = new TestDebatchListener(received, null, null, latch);
        container.PrefetchCount = 1000;
        container.BatchingStrategy = new SimpleBatchingStrategy(1000, int.MaxValue, 30000);
        container.Initialize();
        container.StartAsync();

        try
        {
            var batchingStrategy = new SimpleBatchingStrategy(1000, int.MaxValue, 30000);

            var template = new BatchingRabbitTemplate(batchingStrategy)
            {
                ConnectionFactory = _connectionFactory
            };

            RabbitHeaderAccessor accessor = RabbitHeaderAccessor.GetMutableAccessor(new MessageHeaders());
            accessor.DeliveryMode = MessageDeliveryMode.NonPersistent;
            IMessage<byte[]> message = Message.Create(new byte[256], accessor.MessageHeaders);
            var watch = new Stopwatch();
            watch.Start();

            for (int i = 0; i < count; i++)
            {
                template.Send(string.Empty, Route, message);
            }

            Assert.True(latch.Wait(TimeSpan.FromSeconds(60)));
            watch.Stop();
            _testOutputHelper.WriteLine(watch.ElapsedMilliseconds.ToString());
            Assert.Equal(count, received.Count);
        }
        finally
        {
            container.StopAsync();
            container.Dispose();
        }
    }

    [Fact]
    public void TestDebatchByContainerBadMessageRejected()
    {
        ServiceProvider provider = new ServiceCollection().BuildServiceProvider();
        IConfigurationRoot config = new ConfigurationBuilder().Build();
        var context = new GenericApplicationContext(provider, config);
        context.ServiceExpressionResolver = new StandardServiceExpressionResolver();
        var container = new DirectMessageListenerContainer(context, _connectionFactory);
        container.SetQueueNames(Route);
        var listener = new EmptyListener();
        container.MessageListener = listener;
        var errorHandler = new TestConditionalRejectingErrorHandler();
        container.ErrorHandler = errorHandler;
        container.Initialize();
        container.StartAsync();

        try
        {
            var template = new RabbitTemplate
            {
                ConnectionFactory = _connectionFactory
            };

            var headers = new MessageHeaders(new Dictionary<string, object>
            {
                { RabbitMessageHeaders.SpringBatchFormat, RabbitMessageHeaders.BatchFormatLengthHeader4 }
            });

            IMessage<byte[]> message = Message.Create(Encoding.UTF8.GetBytes("\u0000\u0000\u0000\u0004foo"), headers);
            template.Send(string.Empty, Route, message);
            Thread.Sleep(1000);
            Assert.Equal(0, listener.Count);
            Assert.True(errorHandler.HandleErrorCalled);
        }
        finally
        {
            container.StopAsync();
            container.Dispose();
        }
    }

    [Fact]
    public void TestSimpleBatchGZipped()
    {
        var batchingStrategy = new SimpleBatchingStrategy(2, int.MaxValue, 30000);

        var template = new BatchingRabbitTemplate(batchingStrategy)
        {
            ConnectionFactory = _connectionFactory
        };

        var gZipPostProcessor = new GZipPostProcessor();
        Assert.Equal(CompressionLevel.Fastest, gZipPostProcessor.Level);
        template.SetBeforePublishPostProcessors(gZipPostProcessor);
        var props = new MessageHeaders();
        IMessage<byte[]> message = Message.Create(Encoding.UTF8.GetBytes("foo"), props);
        template.Send(string.Empty, Route, message);
        message = Message.Create(Encoding.UTF8.GetBytes("bar"), props);
        template.Send(string.Empty, Route, message);
        IMessage result = Receive(template);
        Assert.Equal("gzip", result.Headers.ContentEncoding());
        var unzipper = new GUnzipPostProcessor();
        IMessage unzip = unzipper.PostProcessMessage(result);
        Assert.Equal("\u0000\u0000\u0000\u0003foo\u0000\u0000\u0000\u0003bar", Encoding.UTF8.GetString((byte[])unzip.Payload));
    }

    [Fact]
    public void TestSimpleBatchGZippedUsingAdd()
    {
        var batchingStrategy = new SimpleBatchingStrategy(2, int.MaxValue, 30000);

        var template = new BatchingRabbitTemplate(batchingStrategy)
        {
            ConnectionFactory = _connectionFactory
        };

        var gZipPostProcessor = new GZipPostProcessor();
        Assert.Equal(CompressionLevel.Fastest, gZipPostProcessor.Level);
        template.AddBeforePublishPostProcessors(gZipPostProcessor);
        var props = new MessageHeaders();
        IMessage<byte[]> message = Message.Create(Encoding.UTF8.GetBytes("foo"), props);
        template.Send(string.Empty, Route, message);
        message = Message.Create(Encoding.UTF8.GetBytes("bar"), props);
        template.Send(string.Empty, Route, message);
        IMessage result = Receive(template);
        Assert.Equal("gzip", result.Headers.ContentEncoding());
        var unzipper = new GUnzipPostProcessor();
        IMessage unzip = unzipper.PostProcessMessage(result);
        Assert.Equal("\u0000\u0000\u0000\u0003foo\u0000\u0000\u0000\u0003bar", Encoding.UTF8.GetString((byte[])unzip.Payload));
    }

    [Fact]
    public void TestSimpleBatchGZippedUsingAddAndRemove()
    {
        var batchingStrategy = new SimpleBatchingStrategy(2, int.MaxValue, 30000);

        var template = new BatchingRabbitTemplate(batchingStrategy)
        {
            ConnectionFactory = _connectionFactory
        };

        var gZipPostProcessor = new GZipPostProcessor();
        Assert.Equal(CompressionLevel.Fastest, gZipPostProcessor.Level);
        template.AddBeforePublishPostProcessors(gZipPostProcessor);
        var headerPostProcessor = new HeaderPostProcessor();
        template.AddBeforePublishPostProcessors(headerPostProcessor);
        template.RemoveBeforePublishPostProcessor(headerPostProcessor);
        var props = new MessageHeaders();
        IMessage<byte[]> message = Message.Create(Encoding.UTF8.GetBytes("foo"), props);
        template.Send(string.Empty, Route, message);
        message = Message.Create(Encoding.UTF8.GetBytes("bar"), props);
        template.Send(string.Empty, Route, message);
        IMessage result = Receive(template);
        Assert.Equal("gzip", result.Headers.ContentEncoding());
        var unzipper = new GUnzipPostProcessor();
        IMessage unzip = unzipper.PostProcessMessage(result);
        Assert.Equal("\u0000\u0000\u0000\u0003foo\u0000\u0000\u0000\u0003bar", Encoding.UTF8.GetString((byte[])unzip.Payload));
        Assert.Null(unzip.Headers.Get<string>("someHeader"));
    }

    [Fact]
    public void TestSimpleBatchGZippedConfiguredUnzipper()
    {
        var batchingStrategy = new SimpleBatchingStrategy(2, int.MaxValue, 30000);

        var template = new BatchingRabbitTemplate(batchingStrategy)
        {
            ConnectionFactory = _connectionFactory
        };

        var gZipPostProcessor = new GZipPostProcessor
        {
            Level = CompressionLevel.Optimal
        };

        Assert.Equal(CompressionLevel.Optimal, gZipPostProcessor.Level);
        template.SetBeforePublishPostProcessors(gZipPostProcessor);
        template.SetAfterReceivePostProcessors(new GUnzipPostProcessor());
        var props = new MessageHeaders();
        IMessage<byte[]> message = Message.Create(Encoding.UTF8.GetBytes("foo"), props);
        template.Send(string.Empty, Route, message);
        message = Message.Create(Encoding.UTF8.GetBytes("bar"), props);
        template.Send(string.Empty, Route, message);
        IMessage result = Receive(template);
        Assert.Null(result.Headers.ContentEncoding());
        Assert.Equal("\u0000\u0000\u0000\u0003foo\u0000\u0000\u0000\u0003bar", Encoding.UTF8.GetString((byte[])result.Payload));
    }

    [Fact]
    public void TestSimpleBatchGZippedConfiguredUnzipperUsingAdd()
    {
        var batchingStrategy = new SimpleBatchingStrategy(2, int.MaxValue, 30000);

        var template = new BatchingRabbitTemplate(batchingStrategy)
        {
            ConnectionFactory = _connectionFactory
        };

        var gZipPostProcessor = new GZipPostProcessor
        {
            Level = CompressionLevel.Optimal
        };

        Assert.Equal(CompressionLevel.Optimal, gZipPostProcessor.Level);
        template.AddBeforePublishPostProcessors(gZipPostProcessor);
        template.AddAfterReceivePostProcessors(new GUnzipPostProcessor());
        var props = new MessageHeaders();
        IMessage<byte[]> message = Message.Create(Encoding.UTF8.GetBytes("foo"), props);
        template.Send(string.Empty, Route, message);
        message = Message.Create(Encoding.UTF8.GetBytes("bar"), props);
        template.Send(string.Empty, Route, message);
        IMessage result = Receive(template);
        Assert.Null(result.Headers.ContentEncoding());
        Assert.Equal("\u0000\u0000\u0000\u0003foo\u0000\u0000\u0000\u0003bar", Encoding.UTF8.GetString((byte[])result.Payload));
    }

    [Fact]
    public void TestSimpleBatchGZippedWithEncoding()
    {
        var batchingStrategy = new SimpleBatchingStrategy(2, int.MaxValue, 30000);

        var template = new BatchingRabbitTemplate(batchingStrategy)
        {
            ConnectionFactory = _connectionFactory
        };

        var gZipPostProcessor = new GZipPostProcessor();
        template.SetBeforePublishPostProcessors(gZipPostProcessor);

        var accessor = new RabbitHeaderAccessor(new MessageHeaders())
        {
            ContentEncoding = "foo"
        };

        IMessageHeaders props = accessor.ToMessageHeaders();
        IMessage<byte[]> message = Message.Create(Encoding.UTF8.GetBytes("foo"), props);
        template.Send(string.Empty, Route, message);
        message = Message.Create(Encoding.UTF8.GetBytes("bar"), props);
        template.Send(string.Empty, Route, message);
        IMessage result = Receive(template);
        Assert.Equal("gzip:foo", result.Headers.ContentEncoding());
        var unzipper = new GUnzipPostProcessor();
        IMessage unzip = unzipper.PostProcessMessage(result);
        Assert.Equal("\u0000\u0000\u0000\u0003foo\u0000\u0000\u0000\u0003bar", Encoding.UTF8.GetString((byte[])unzip.Payload));
    }

    [Fact]
    public void TestSimpleBatchGZippedWithEncodingInflated()
    {
        var batchingStrategy = new SimpleBatchingStrategy(2, int.MaxValue, 30000);

        var template = new BatchingRabbitTemplate(batchingStrategy)
        {
            ConnectionFactory = _connectionFactory
        };

        var gZipPostProcessor = new GZipPostProcessor();
        template.SetBeforePublishPostProcessors(gZipPostProcessor);
        template.SetAfterReceivePostProcessors(new DelegatingDecompressingPostProcessor());

        var accessor = new RabbitHeaderAccessor(new MessageHeaders())
        {
            ContentEncoding = "foo"
        };

        IMessageHeaders props = accessor.ToMessageHeaders();
        IMessage<byte[]> message = Message.Create(Encoding.UTF8.GetBytes("foo"), props);
        template.Send(string.Empty, Route, message);
        message = Message.Create(Encoding.UTF8.GetBytes("bar"), props);
        template.Send(string.Empty, Route, message);
        Thread.Sleep(100);
        byte[] output = template.ReceiveAndConvert<byte[]>(Route);
        Assert.NotNull(output);
        Assert.Equal("\u0000\u0000\u0000\u0003foo\u0000\u0000\u0000\u0003bar", Encoding.UTF8.GetString(output));
    }

    [Fact]
    public void TestSimpleBatchZippedBestCompression()
    {
        var batchingStrategy = new SimpleBatchingStrategy(2, int.MaxValue, 30000);

        var template = new BatchingRabbitTemplate(batchingStrategy)
        {
            ConnectionFactory = _connectionFactory
        };

        var zipPostProcessor = new ZipPostProcessor
        {
            Level = CompressionLevel.Optimal
        };

        template.SetBeforePublishPostProcessors(zipPostProcessor);
        var accessor = new RabbitHeaderAccessor(new MessageHeaders());
        IMessageHeaders props = accessor.ToMessageHeaders();
        IMessage<byte[]> message = Message.Create(Encoding.UTF8.GetBytes("foo"), props);
        template.Send(string.Empty, Route, message);
        message = Message.Create(Encoding.UTF8.GetBytes("bar"), props);
        template.Send(string.Empty, Route, message);
        IMessage result = Receive(template);
        Assert.Equal("zip", result.Headers.ContentEncoding());
        var unzipper = new UnzipPostProcessor();
        IMessage unzip = unzipper.PostProcessMessage(result);
        Assert.Equal("\u0000\u0000\u0000\u0003foo\u0000\u0000\u0000\u0003bar", Encoding.UTF8.GetString((byte[])unzip.Payload));
    }

    [Fact]
    public void TestSimpleBatchZippedWithEncoding()
    {
        var batchingStrategy = new SimpleBatchingStrategy(2, int.MaxValue, 30000);

        var template = new BatchingRabbitTemplate(batchingStrategy)
        {
            ConnectionFactory = _connectionFactory
        };

        var zipPostProcessor = new ZipPostProcessor
        {
            Level = CompressionLevel.Optimal
        };

        template.SetBeforePublishPostProcessors(zipPostProcessor);

        var accessor = new RabbitHeaderAccessor(new MessageHeaders())
        {
            ContentEncoding = "foo"
        };

        IMessageHeaders props = accessor.ToMessageHeaders();
        IMessage<byte[]> message = Message.Create(Encoding.UTF8.GetBytes("foo"), props);
        template.Send(string.Empty, Route, message);
        message = Message.Create(Encoding.UTF8.GetBytes("bar"), props);
        template.Send(string.Empty, Route, message);
        IMessage result = Receive(template);
        Assert.Equal("zip:foo", result.Headers.ContentEncoding());
        var unzipper = new UnzipPostProcessor();
        IMessage unzip = unzipper.PostProcessMessage(result);
        Assert.Equal("\u0000\u0000\u0000\u0003foo\u0000\u0000\u0000\u0003bar", Encoding.UTF8.GetString((byte[])unzip.Payload));
    }

    [Fact]
    public void TestSimpleBatchDeflater()
    {
        var batchingStrategy = new SimpleBatchingStrategy(2, int.MaxValue, 30000);

        var template = new BatchingRabbitTemplate(batchingStrategy)
        {
            ConnectionFactory = _connectionFactory
        };

        var deflaterPostProcessor = new DeflaterPostProcessor
        {
            Level = CompressionLevel.Optimal
        };

        template.SetBeforePublishPostProcessors(deflaterPostProcessor);
        var accessor = new RabbitHeaderAccessor(new MessageHeaders());
        IMessageHeaders props = accessor.ToMessageHeaders();
        IMessage<byte[]> message = Message.Create(Encoding.UTF8.GetBytes("foo"), props);
        template.Send(string.Empty, Route, message);
        message = Message.Create(Encoding.UTF8.GetBytes("bar"), props);
        template.Send(string.Empty, Route, message);
        IMessage result = Receive(template);
        Assert.Equal("deflate", result.Headers.ContentEncoding());
        var unzipper = new InflaterPostProcessor();
        IMessage unzip = unzipper.PostProcessMessage(result);
        Assert.Equal("\u0000\u0000\u0000\u0003foo\u0000\u0000\u0000\u0003bar", Encoding.UTF8.GetString((byte[])unzip.Payload));
    }

    [Fact]
    public void TestSimpleBatchDeflaterFastestCompression()
    {
        var batchingStrategy = new SimpleBatchingStrategy(2, int.MaxValue, 30000);

        var template = new BatchingRabbitTemplate(batchingStrategy)
        {
            ConnectionFactory = _connectionFactory
        };

        var deflaterPostProcessor = new DeflaterPostProcessor
        {
            Level = CompressionLevel.Fastest
        };

        template.SetBeforePublishPostProcessors(deflaterPostProcessor);
        var accessor = new RabbitHeaderAccessor(new MessageHeaders());
        IMessageHeaders props = accessor.ToMessageHeaders();
        IMessage<byte[]> message = Message.Create(Encoding.UTF8.GetBytes("foo"), props);
        template.Send(string.Empty, Route, message);
        message = Message.Create(Encoding.UTF8.GetBytes("bar"), props);
        template.Send(string.Empty, Route, message);
        IMessage result = Receive(template);
        Assert.Equal("deflate", result.Headers.ContentEncoding());
        var unzipper = new InflaterPostProcessor();
        IMessage unzip = unzipper.PostProcessMessage(result);
        Assert.Equal("\u0000\u0000\u0000\u0003foo\u0000\u0000\u0000\u0003bar", Encoding.UTF8.GetString((byte[])unzip.Payload));
    }

    [Fact]
    public void TestSimpleBatchDeflaterWithEncoding()
    {
        var batchingStrategy = new SimpleBatchingStrategy(2, int.MaxValue, 30000);

        var template = new BatchingRabbitTemplate(batchingStrategy)
        {
            ConnectionFactory = _connectionFactory
        };

        var deflaterPostProcessor = new DeflaterPostProcessor
        {
            Level = CompressionLevel.Fastest
        };

        template.SetBeforePublishPostProcessors(deflaterPostProcessor);

        var accessor = new RabbitHeaderAccessor(new MessageHeaders())
        {
            ContentEncoding = "foo"
        };

        IMessageHeaders props = accessor.ToMessageHeaders();
        IMessage<byte[]> message = Message.Create(Encoding.UTF8.GetBytes("foo"), props);
        template.Send(string.Empty, Route, message);
        message = Message.Create(Encoding.UTF8.GetBytes("bar"), props);
        template.Send(string.Empty, Route, message);
        IMessage result = Receive(template);
        Assert.Equal("deflate:foo", result.Headers.ContentEncoding());
        var unzipper = new InflaterPostProcessor();
        IMessage unzip = unzipper.PostProcessMessage(result);
        Assert.Equal("\u0000\u0000\u0000\u0003foo\u0000\u0000\u0000\u0003bar", Encoding.UTF8.GetString((byte[])unzip.Payload));
    }

    public void Dispose()
    {
        var admin = new RabbitAdmin(_connectionFactory);
        admin.DeleteQueue(Route);
        _connectionFactory.Dispose();
    }

    private IMessage Receive(BatchingRabbitTemplate template)
    {
        IMessage message = template.Receive(Route);
        int n = 0;

        while (n++ < 200 && message == null)
        {
            Thread.Sleep(50);
            message = template.Receive(Route);
        }

        Assert.NotNull(message);
        return message;
    }

    private sealed class HeaderPostProcessor : IMessagePostProcessor
    {
        public IMessage PostProcessMessage(IMessage message, CorrelationData correlation)
        {
            return PostProcessMessage(message);
        }

        public IMessage PostProcessMessage(IMessage message)
        {
            RabbitHeaderAccessor headers = RabbitHeaderAccessor.GetMutableAccessor(message);
            headers.SetHeader("someHeader", "someValue");
            return message;
        }
    }

    private sealed class TestConditionalRejectingErrorHandler : ConditionalRejectingErrorHandler
    {
        public bool HandleErrorCalled;

        public override bool HandleError(Exception exception)
        {
            HandleErrorCalled = true;
            return base.HandleError(exception);
        }
    }

    private sealed class EmptyListener : IMessageListener
    {
        public int Count;

        public AcknowledgeMode ContainerAckMode { get; set; }

        public void OnMessage(IMessage message)
        {
            Count++;
        }

        public void OnMessageBatch(IEnumerable<IMessage> messages)
        {
        }
    }

    private sealed class TestDebatchListener : IMessageListener
    {
        public readonly List<IMessage> Received;
        public readonly List<bool> LastInBatch;
        public readonly AtomicInteger BatchSize;
        public readonly CountdownEvent Latch;

        public AcknowledgeMode ContainerAckMode { get; set; }

        public TestDebatchListener(List<IMessage> received, List<bool> lastInBatch, AtomicInteger batchSize, CountdownEvent latch)
        {
            Received = received;
            LastInBatch = lastInBatch;
            BatchSize = batchSize;
            Latch = latch;
        }

        public void OnMessage(IMessage message)
        {
            Received.Add(message);

            if (LastInBatch != null)
            {
                LastInBatch.Add(message.Headers.LastInBatch().HasValue && message.Headers.LastInBatch().Value);
            }

            if (BatchSize != null)
            {
                BatchSize.Value = message.Headers.Get<int>(RabbitMessageHeaders.BatchSize);
            }

            Latch.Signal();
        }

        public void OnMessageBatch(IEnumerable<IMessage> messages)
        {
        }
    }
}
