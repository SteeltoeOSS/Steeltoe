// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Stream.Binding;
using Steeltoe.Stream.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Stream.StreamsHost
{
    /// <summary>
    /// Implements a hosted service to be used in a Generic Host (as opposed to streams host)
    /// </summary>
    public class StreamsLifeCycleService : IHostedService
    {
        private readonly IApplicationContext _applicationContext;

        public StreamsLifeCycleService(IApplicationContext applicationContext)
        {
            _applicationContext = applicationContext;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var lifecycleProcessor = _applicationContext.GetService<ILifecycleProcessor>();

            await lifecycleProcessor.OnRefresh();
            var attributeProcessor = _applicationContext.GetService<StreamListenerAttributeProcessor>();

            attributeProcessor.Initialize();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            var lifecycleProcessor = _applicationContext.GetService<ILifecycleProcessor>();
            return lifecycleProcessor.Stop();
        }
    }
}
