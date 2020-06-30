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

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Messaging.Rabbit.Config;
using Steeltoe.Messaging.Rabbit.Core;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Messaging.Rabbit.Host
{
    public class RabbitHostService : IHostedService
    {
        private readonly IApplicationContext _applicationContext;
        private readonly ILogger _logger;

        public RabbitHostService(IApplicationContext applicationContext, ILogger<RabbitHostService> logger = null)
        {
            _applicationContext = applicationContext;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger?.LogInformation("Starting RabbitHostService ...");

            // Ensure any admins in the container get initialized
            var admins = _applicationContext.GetServices<IRabbitAdmin>();
            if (admins == null || admins.Count() == 0)
            {
                _logger?.LogInformation("Found no IRabbitAdmin services to initialize");
            }

            // Allow for processing of RabbitListenerAttributes
            var listenerProcessor = _applicationContext.GetService<IRabbitListenerAttributeProcessor>();
            if (listenerProcessor == null)
            {
                _logger?.LogInformation("Found no IRabbitListenerAttributeProcessor services to initialize");
            }
            else
            {
                listenerProcessor.Initialize();
            }

            // Ensure any RabbitContainers get started
            var processor = _applicationContext.GetService<ILifecycleProcessor>();
            if (processor != null)
            {
                await processor.OnRefresh();
            }
            else
            {
                _logger?.LogInformation("Found no ILifecycleProcessor service to initialize");
            }

            return;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger?.LogInformation("Stopping RabbitHostService ...");
            return Task.CompletedTask;
        }
    }
}
