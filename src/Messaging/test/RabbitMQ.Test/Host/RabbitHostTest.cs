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
            MockRabbitHostedService hostedService;

            using (var host = RabbitHost.CreateDefaultBuilder()
                                .ConfigureServices(svc => svc.AddSingleton<IHostedService, MockRabbitHostedService>())
                                .Start())
            {
                Assert.NotNull(host);
                hostedService = (MockRabbitHostedService)host.Services.GetRequiredService<IHostedService>();
                Assert.NotNull(hostedService);
                Assert.Equal(1, hostedService.StartCount);
                Assert.Equal(0, hostedService.StopCount);
                Assert.Equal(0, hostedService.DisposeCount);
            }

            Assert.Equal(1, hostedService.StartCount);
            Assert.Equal(0, hostedService.StopCount);
            Assert.Equal(1, hostedService.DisposeCount);
        }

        [Fact]
        public void HostShouldInitializeServices()
        {
            using (var host = RabbitHost.CreateDefaultBuilder().Start())
            {
                var lifecycleProcessor = host.Services.GetRequiredService<ILifecycleProcessor>();
                var rabbitHostService = (RabbitHostService)host.Services.GetRequiredService<IHostedService>();

                Assert.True(lifecycleProcessor.IsRunning);
                Assert.NotNull(rabbitHostService);
            }
        }
    }
}
