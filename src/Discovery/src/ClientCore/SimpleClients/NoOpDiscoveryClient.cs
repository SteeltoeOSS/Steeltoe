// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.Discovery;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Steeltoe.Discovery.Client
{
    internal class NoOpDiscoveryClient : IDiscoveryClient
    {
        internal NoOpDiscoveryClient(ILogger<NoOpDiscoveryClient> logger = null)
        {
            logger?.LogWarning("A NoOp Discovery Client has been created! Running in container: {IsContainerized}", Platform.IsContainerized);
        }

        public string Description => "The IDiscoveryClient that didn't";

        public IList<string> Services => new List<string>();

        private readonly IList<IServiceInstance> _serviceInstances = new List<IServiceInstance>();

        public IList<IServiceInstance> GetInstances(string serviceId)
        {
            return _serviceInstances;
        }

        public IServiceInstance GetLocalServiceInstance()
        {
            throw new NotImplementedException("No known use case for implementing this method");
        }

        public Task ShutdownAsync()
        {
            return Task.CompletedTask;
        }
    }
}
