using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.Lifecycle;
using Xunit;

namespace Steeltoe.Messaging.RabbitMQ.Host
{
    public class RabbitHostTest
    {
        [Fact]
        public void HostCanBeStarted()
        {
            MockRabbitHostedService service;
            using (var host = RabbitHost.CreateDefaultBuilder()
                                .ConfigureServices(svc => svc.AddSingleton<IHostedService, MockRabbitHostedService>())
                                .Start())
            {
                Assert.NotNull(host);
                service = (MockRabbitHostedService)host.Services.GetRequiredService<IHostedService>();
                Assert.NotNull(service);
                Assert.Equal(1, service.StartCount);
                Assert.Equal(0, service.StopCount);
                Assert.Equal(0, service.DisposeCount);
            }

            Assert.Equal(1, service.StartCount);
            Assert.Equal(0, service.StopCount);
            Assert.Equal(1, service.DisposeCount);
        }

        [Fact]
        public void HostShouldInitializeServices()
        {
            using (var host = RabbitHost.CreateDefaultBuilder().Start())
            {
                var service = host.Services.GetRequiredService<ILifecycleProcessor>();
                var rabbitHostService = (RabbitHostService)host.Services.GetRequiredService<IHostedService>();

                Assert.True(service.IsRunning);
                Assert.NotNull(rabbitHostService);
            }
        }
    }
}
