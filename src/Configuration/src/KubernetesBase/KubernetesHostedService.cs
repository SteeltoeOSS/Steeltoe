// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Extensions.Configuration.Kubernetes
{
    /// <summary>
    /// Replace bootstrapped components used by KubernetesConfigurationProvider with objects provided by Dependency Injection
    /// </summary>
    public class KubernetesHostedService : IHostedService
    {
        private readonly IEnumerable<KubernetesConfigMapProvider> _configMapProviders;

        private readonly IEnumerable<KubernetesSecretProvider> _configSecretProviders;

        private readonly ILoggerFactory _loggerFactory;

        public KubernetesHostedService(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            _configMapProviders = ((IConfigurationRoot)configuration).Providers.OfType<KubernetesConfigMapProvider>();

            _configSecretProviders = ((IConfigurationRoot)configuration).Providers.OfType<KubernetesSecretProvider>();

            _loggerFactory = loggerFactory;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _configMapProviders.ToList().ForEach(p => p.ProvideRuntimeReplacements(_loggerFactory));

            _configSecretProviders.ToList().ForEach(p => p.ProvideRuntimeReplacements(_loggerFactory));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // Do Nothing
            return Task.CompletedTask;
        }
    }
}
