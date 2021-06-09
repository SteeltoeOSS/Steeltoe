using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.Lifecycle;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Messaging.RabbitMQ.Host
{
    public class RabbitHost : IHost
    {
        public static IHostBuilder CreateDefaultBuilder() =>
            new RabbitHostBuilder(Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder());

        public IServiceProvider Services => _host.Services;

        private readonly IHost _host;

        public RabbitHost(IHost host)
        {
            _host = host;
        }

        public void Dispose()
        {
            _host.Dispose();
        }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            using (var scope = _host.Services.CreateScope())
            {
                var lifecycleProcessor = scope.ServiceProvider.GetRequiredService<ILifecycleProcessor>();
                lifecycleProcessor.OnRefresh();
            }

            return _host.StartAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            using (var scope = _host.Services.CreateScope())
            {
                var lifecycleProcessor = scope.ServiceProvider.GetRequiredService<ILifecycleProcessor>();
                lifecycleProcessor.Stop();
            }

            return _host.StopAsync(cancellationToken);
        }
    }
}
