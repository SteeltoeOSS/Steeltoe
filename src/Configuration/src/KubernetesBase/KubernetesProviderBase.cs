// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using k8s;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Extensions.Configuration.Kubernetes;

internal class KubernetesProviderBase : ConfigurationProvider
{
    internal bool Polling { get; private set; }

    protected IKubernetes K8sClient { get; set; }

    protected KubernetesConfigSourceSettings Settings { get; set; }

    protected CancellationToken CancellationToken { get; set; }

    protected ILogger Logger => Settings.LoggerFactory?.CreateLogger(this.GetType());

    internal KubernetesProviderBase(IKubernetes kubernetes, KubernetesConfigSourceSettings settings, CancellationToken token = default)
    {
        if (kubernetes is null)
        {
            throw new ArgumentNullException(nameof(kubernetes));
        }

        if (settings is null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        K8sClient = kubernetes;
        Settings = settings;
        CancellationToken = token;
    }

    internal void ProvideRuntimeReplacements(ILoggerFactory loggerFactory)
    {
        if (loggerFactory is not null)
        {
            Settings.LoggerFactory = loggerFactory;
            Logger?.LogTrace("Replacing Bootstrapped loggerFactory with actual factory");
        }
    }

    protected void StartPolling(int interval)
    {
        Task.Factory.StartNew(
            () =>
            {
                Polling = true;
                while (Polling)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(interval));
                    Logger?.LogTrace("Interval completed for {namespace}.{name}, beginning reload", Settings.Namespace, Settings.Name);
                    Load();
                    if (CancellationToken.IsCancellationRequested)
                    {
                        Logger?.LogTrace("Cancellation requested for {namespace}.{name}, shutting down", Settings.Namespace, Settings.Name);
                        break;
                    }
                }
            },
            CancellationToken);
    }
}