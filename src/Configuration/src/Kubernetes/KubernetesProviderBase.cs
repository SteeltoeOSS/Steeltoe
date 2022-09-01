// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using k8s;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common;

namespace Steeltoe.Extensions.Configuration.Kubernetes;

internal abstract class KubernetesProviderBase : ConfigurationProvider
{
    private readonly CancellationToken _cancellationToken;
    internal bool IsPolling { get; private set; }

    protected IKubernetes KubernetesClient { get; set; }
    protected KubernetesConfigSourceSettings Settings { get; }
    protected ILogger Logger => Settings.LoggerFactory?.CreateLogger(GetType()) ?? NullLoggerFactory.Instance.CreateLogger(GetType());

    protected KubernetesProviderBase(IKubernetes kubernetes, KubernetesConfigSourceSettings settings, CancellationToken token = default)
    {
        ArgumentGuard.NotNull(kubernetes);
        ArgumentGuard.NotNull(settings);

        KubernetesClient = kubernetes;
        Settings = settings;
        _cancellationToken = token;
    }

    internal void ProvideRuntimeReplacements(ILoggerFactory loggerFactory)
    {
        if (loggerFactory is not null)
        {
            Settings.LoggerFactory = loggerFactory;
            Logger.LogTrace("Replacing Bootstrapped loggerFactory with actual factory");
        }
    }

    protected void StartPolling(int interval)
    {
        Task.Factory.StartNew(() =>
        {
            IsPolling = true;

            while (IsPolling)
            {
                Thread.Sleep(TimeSpan.FromSeconds(interval));
                Logger.LogTrace("Interval completed for {namespace}.{name}, beginning reload", Settings.Namespace, Settings.Name);
                Load();

                if (_cancellationToken.IsCancellationRequested)
                {
                    Logger.LogTrace("Cancellation requested for {namespace}.{name}, shutting down", Settings.Namespace, Settings.Name);
                    break;
                }
            }
        }, _cancellationToken);
    }
}
