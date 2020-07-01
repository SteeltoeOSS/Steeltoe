// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Rabbit.Batch;
using Steeltoe.Messaging.Rabbit.Config;
using Steeltoe.Messaging.Rabbit.Connection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Messaging.Rabbit.Core
{
    public class BatchingRabbitTemplate : RabbitTemplate
    {
        private readonly IBatchingStrategy _batchingStrategy;
        private readonly object _batchlock = new object();
        private CancellationTokenSource _cancellationTokenSource;
        private Task _scheduledTask;
        private int count;

        public BatchingRabbitTemplate(
            IOptionsMonitor<RabbitOptions> optionsMonitor,
            Connection.IConnectionFactory connectionFactory,
            ISmartMessageConverter messageConverter,
            IBatchingStrategy batchingStrategy,
            ILogger logger = null)
            : base(optionsMonitor, connectionFactory, messageConverter, logger)
        {
            _batchingStrategy = batchingStrategy;
        }

        public BatchingRabbitTemplate(
            IOptionsMonitor<RabbitOptions> optionsMonitor,
            Connection.IConnectionFactory connectionFactory,
            IBatchingStrategy batchingStrategy,
            ILogger logger = null)
            : base(optionsMonitor, connectionFactory, logger)
        {
            _batchingStrategy = batchingStrategy;
        }

        public BatchingRabbitTemplate(IConnectionFactory connectionFactory, IBatchingStrategy batchingStrategy)
            : base(connectionFactory)
        {
            _batchingStrategy = batchingStrategy;
        }

        public BatchingRabbitTemplate(IBatchingStrategy batchingStrategy)
        {
            _batchingStrategy = batchingStrategy;
        }

        public override void Send(string exchange, string routingKey, IMessage message, CorrelationData correlationData)
        {
            lock (_batchlock)
            {
                count++;
                if (correlationData != null)
                {
                    _logger?.LogDebug("Cannot use batching with correlation data");
                    base.Send(exchange, routingKey, message, correlationData);
                }
                else
                {
                    // if (_scheduledTask != null)
                    // {
                    //    _cancellationTokenSource.Cancel(false);
                    // }
                    MessageBatch? batch = _batchingStrategy.AddToBatch(exchange, routingKey, message);
                    if (batch != null)
                    {
                        if (_scheduledTask != null)
                        {
                            _cancellationTokenSource.Cancel(false);
                            _scheduledTask = null;
                        }

                        base.Send(batch.Value.Exchange, batch.Value.RoutingKey, batch.Value.Message, null);
                    }

                    var next = _batchingStrategy.NextRelease();
                    if (next != null && _scheduledTask == null)
                    {
                        _cancellationTokenSource = new CancellationTokenSource();
                        var delay = next.Value - DateTime.Now;
                        _scheduledTask = Task.Run(() => ReleaseBatches(delay, _cancellationTokenSource.Token), _cancellationTokenSource.Token);
                    }
                }
            }
        }

        public void Flush()
        {
            FlushAsync(default).Wait();
        }

        public async Task FlushAsync(CancellationToken cancellationToken)
        {
            await ReleaseBatches(TimeSpan.Zero, cancellationToken);
        }

        private async Task ReleaseBatches(TimeSpan delay, CancellationToken cancellationToken)
        {
            await Task.Delay(delay, cancellationToken);
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            lock (_batchlock)
            {
                foreach (MessageBatch batch in _batchingStrategy.ReleaseBatches())
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    base.Send(batch.Exchange, batch.RoutingKey, batch.Message, null);
                }

                _scheduledTask = null;
            }
        }
    }
}
