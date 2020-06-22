﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.Reflection;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace Steeltoe.Extensions.Configuration.ConfigServer
{
    internal class ConfigServerDiscoveryService
    {
        protected internal IConfiguration _configuration;
        protected internal ConfigServerClientSettings _settings;
        protected internal ILoggerFactory _logFactory;
        protected internal ILogger _logger;

        private static readonly string[] _discoveryServiceAssemblies = new string[] { "Steeltoe.Discovery.EurekaBase" };
        private static readonly string[] _discoveryServiceTypeNames = new string[] { "Steeltoe.Discovery.Eureka.EurekaClientService" };

        internal ConfigServerDiscoveryService(IConfiguration configuration, ConfigServerClientSettings settings, ILoggerFactory logFactory = null)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _logFactory = logFactory;
            _logger = _logFactory?.CreateLogger(typeof(ConfigServerDiscoveryService));
        }

        internal IList<IServiceInstance> GetConfigServerInstances()
        {
            MethodInfo method = FindGetInstancesMethod();
            if (method == null)
            {
                _logger?.LogWarning("Could not locate discovery assembly or GetInstances() method!");
                return null;
            }

            return InvokeGetInstances(method);
        }

        internal MethodInfo FindGetInstancesMethod()
        {
            Type discoveryService = ReflectionHelpers.FindType(_discoveryServiceAssemblies, _discoveryServiceTypeNames);
            MethodInfo method = null;

            if (discoveryService != null)
            {
                method = ReflectionHelpers.FindMethod(discoveryService, "GetInstances");
            }

            return method;
        }

        internal IList<IServiceInstance> InvokeGetInstances(MethodInfo method)
        {
            var attempts = 0;
            var backOff = _settings.RetryInitialInterval;
            IList<IServiceInstance> instances = null;

            do
            {
                instances = null;
                try
                {
                    _logger?.LogDebug("Locating configserver {serviceId} via discovery", _settings.DiscoveryServiceId);
                    instances = method.Invoke(null, new object[] { _configuration, _settings.DiscoveryServiceId, _logFactory }) as IList<IServiceInstance>;
                }
                catch (Exception e)
                {
                    _logger?.LogError(e, "Exception invoking GetInstances() during config server lookup");
                }

                if (!_settings.RetryEnabled || (instances != null && instances.Count > 0))
                {
                    break;
                }

                attempts++;
                if (attempts < _settings.RetryAttempts)
                {
                    Thread.CurrentThread.Join(backOff);
                    var nextBackoff = (int)(backOff * _settings.RetryMultiplier);
                    backOff = Math.Min(nextBackoff, _settings.RetryMaxInterval);
                }
                else
                {
                    break;
                }
            }
            while (true);

            return instances;
        }
    }
}
