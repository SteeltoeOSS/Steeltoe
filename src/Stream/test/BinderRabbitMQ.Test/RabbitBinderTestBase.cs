// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.ExceptionServices;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Retry;
using Steeltoe.Common.TestResources;
using Steeltoe.Integration.RabbitMQ.Inbound;
using Steeltoe.Integration.RabbitMQ.Outbound;
using Steeltoe.Messaging;
using Steeltoe.Messaging.RabbitMQ.Configuration;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Listener;
using Steeltoe.Messaging.RabbitMQ.Support.PostProcessor;
using Steeltoe.Stream.Binder.RabbitMQ.Configuration;
using Steeltoe.Stream.Binder.Test;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.Stream.Binder.RabbitMQ.Test;

public abstract class RabbitBinderTestBase : PartitionCapableBinderTests<RabbitTestBinder, RabbitMessageChannelBinder>, IDisposable
{
    protected const string TestPrefix = "bindertest.";
    private static readonly string BigExceptionMessage = new('x', 10_000);
    private bool _isDisposed;

    protected RabbitTestBinder TestBinder { get; set; }

    protected int MaxStackTraceSize { get; set; }

    protected RabbitBinderTestBase(ITestOutputHelper output)
        : base(output, new XunitLoggerFactory(output))
    {
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public Spy SpyOn(string queue)
    {
        var template = new RabbitTemplate(GetResource());
        template.SetAfterReceivePostProcessors(new DelegatingDecompressingPostProcessor());

        return new Spy
        {
            Receive = expectNull =>
            {
                if (expectNull)
                {
                    Thread.Sleep(50);
                    return template.ReceiveAndConvert<object>(new RabbitConsumerOptions().Prefix + queue);
                }

                object bar = null;
                int n = 0;

                while (n++ < 100 && bar == null)
                {
                    bar = template.ReceiveAndConvert<object>(new RabbitConsumerOptions().Prefix + queue);
                    Thread.Sleep(100);
                }

                Assert.True(n < 100, "Message did not arrive in RabbitMQ");
                return bar;
            }
        };
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing && !_isDisposed)
        {
            Cleanup();
            _isDisposed = true;
        }
    }

    protected override RabbitTestBinder GetBinder(RabbitBindingsOptions bindingsOptions)
    {
        if (TestBinder == null)
        {
            var options = new RabbitOptions
            {
                PublisherReturns = true
            };

            CachingConnectionFactory ccf = GetResource();
            var rabbitOptions = new TestOptionsMonitor<RabbitOptions>(options);
            var binderOptions = new TestOptionsMonitor<RabbitBinderOptions>(null);
            var rabbitBindingsOptions = new TestOptionsMonitor<RabbitBindingsOptions>(bindingsOptions ?? new RabbitBindingsOptions());

            TestBinder = new RabbitTestBinder(ccf, rabbitOptions, binderOptions, rabbitBindingsOptions, LoggerFactory);
        }

        return TestBinder;
    }

    protected DirectMessageListenerContainer VerifyContainer(RabbitInboundChannelAdapter endpoint)
    {
        DirectMessageListenerContainer container;
        RetryTemplate retry;
        container = GetPropertyValue<DirectMessageListenerContainer>(endpoint, "MessageListenerContainer");

        Assert.Equal(AcknowledgeMode.None, container.AcknowledgeMode);
        Assert.StartsWith("foo.props.0", container.GetQueueNames()[0], StringComparison.Ordinal);
        Assert.False(container.IsChannelTransacted);
        Assert.Equal(2, container.ConsumersPerQueue);
        Assert.False(container.DefaultRequeueRejected);
        Assert.Equal(20, container.PrefetchCount);

        retry = endpoint.RetryTemplate;
        Assert.Equal(23, GetFieldValue<int>(retry, "_maxAttempts"));
        Assert.Equal(2000, GetFieldValue<int>(retry, "_backOffInitialInterval"));
        Assert.Equal(5.0, GetFieldValue<double>(retry, "_backOffMultiplier"));

        return container;
    }

    protected Exception BigCause(Exception innerException)
    {
        return BigCause(innerException, 0);
    }

    private Exception BigCause(Exception innerException, int recursionDepth)
    {
        if (recursionDepth > 1000)
        {
            throw new InvalidOperationException("Internal error: Infinite recursion detected.");
        }

        try
        {
            Exception capturedException = innerException ?? new Exception(BigExceptionMessage);
            ExceptionDispatchInfo.Capture(capturedException).Throw();
        }
        catch (Exception ex)
        {
            if (ex.StackTrace != null && ex.StackTrace.Length > MaxStackTraceSize)
            {
                return ex;
            }

            innerException = ex;
        }

        return BigCause(innerException, recursionDepth + 1);
    }

    protected void Cleanup()
    {
        if (TestBinder != null)
        {
            Cleanup(TestBinder);
        }

        if (CachingConnectionFactory != null)
        {
            CachingConnectionFactory.ResetConnection();
            CachingConnectionFactory.Destroy();
            CachingConnectionFactory = null;
        }

        TestBinder = null;
    }

    private void Cleanup(RabbitTestBinder binder)
    {
        binder.Cleanup();
        binder.CoreBinder.ConnectionFactory.Destroy();
        CachingConnectionFactory = null;
    }

    public class WrapperAccessor : RabbitOutboundEndpoint
    {
        public WrapperAccessor(IApplicationContext context, RabbitTemplate template)
            : base(context, template, null)
        {
        }

        public CorrelationData GetWrapper(IMessage message)
        {
            // var constructor = typeof(CorrelationDataWrapper).GetConstructor(typeof(string), typeof(object), typeof(Message));
            return Activator.CreateInstance(typeof(CorrelationDataWrapper), null, message, message) as CorrelationData;
        }
    }

    public class TestPartitionSupport : IPartitionKeyExtractorStrategy, IPartitionSelectorStrategy
    {
        public string ServiceName { get; set; }

        public TestPartitionSupport(string serviceName)
        {
            ServiceName = serviceName;
        }

        public object ExtractKey(IMessage message)
        {
            return message.Payload;
        }

        public int SelectPartition(object key, int partitionCount)
        {
            return (int)key;
        }
    }
}
