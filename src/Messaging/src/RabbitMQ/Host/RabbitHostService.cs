// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Hosting;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Messaging.Rabbit.Config;
using Steeltoe.Messaging.Rabbit.Core;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Messaging.Rabbit.Host
{
    public class RabbitHostService : IHostedService
    {
        private readonly IApplicationContext _applicationContext;

        public RabbitHostService(IApplicationContext applicationContext)
        {
            _applicationContext = applicationContext;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // Ensure any admins in the container get initialized
            var admins = _applicationContext.GetServices<IAmqpAdmin>();

            // Allow for processing of RabbitListenerAttributes
            var listenerProcessor = _applicationContext.GetService<RabbitListenerAttributeProcessor>();
            listenerProcessor.Initialize();

            // Ensure any RabbitContainers get started
            var processor = _applicationContext.GetService<ILifecycleProcessor>();
            await processor.OnRefresh();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
