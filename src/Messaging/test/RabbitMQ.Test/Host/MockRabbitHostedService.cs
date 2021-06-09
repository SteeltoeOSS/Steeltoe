using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Messaging.RabbitMQ.Host
{
    public class MockRabbitHostedService : IHostedService, IDisposable
    {
        public int StartCount { get; internal set; }

        public int StopCount { get; internal set; }

        public int DisposeCount { get; internal set; }

        public Action<CancellationToken> StartAction { get; set; }

        public Action<CancellationToken> StopAction { get; set; }

        public Action DisposeAction { get; set; }

        public void Dispose()
        {
            DisposeCount++;
            DisposeAction?.Invoke();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            StartCount++;
            StartAction?.Invoke(cancellationToken);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            StopCount++;
            StopAction?.Invoke(cancellationToken);
            return Task.CompletedTask;
        }
    }
}
