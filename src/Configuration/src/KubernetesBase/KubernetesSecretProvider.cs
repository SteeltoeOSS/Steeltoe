// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;
using Steeltoe.Common.Kubernetes;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;

namespace Steeltoe.Extensions.Configuration.Kubernetes;

internal class KubernetesSecretProvider : KubernetesProviderBase, IDisposable
{
    private Watcher<V1Secret> SecretWatcher { get; set; }

    private bool _disposed = false;

    internal KubernetesSecretProvider(IKubernetes kubernetes, KubernetesConfigSourceSettings settings, CancellationToken cancellationToken = default)
        : base(kubernetes, settings, cancellationToken)
    {
        Settings.Namespace ??= "default";
    }

    public override void Load()
    {
        try
        {
            var secretResponse = K8sClient.ReadNamespacedSecretWithHttpMessagesAsync(Settings.Name, Settings.Namespace).GetAwaiter().GetResult();
            ProcessData(secretResponse.Body);
            EnableReloading();
        }
        catch (HttpOperationException e)
        {
            if (e.Response.StatusCode == HttpStatusCode.Forbidden)
            {
                Logger?.LogCritical(e, "Failed to retrieve secret '{SecretName}' in namespace '{SecretNamespace}'. Confirm that your service account has the necessary permissions", Settings.Name, Settings.Namespace);
            }
            else if (e.Response.StatusCode == HttpStatusCode.NotFound)
            {
                EnableReloading();
                return;
            }

            throw;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                SecretWatcher?.Dispose();
                SecretWatcher = null;
                K8sClient.Dispose();
                K8sClient = null;
            }

            _disposed = true;
        }
    }

    private static string NormalizeKey(string key) => key.Replace("__", ":");

    private void EnableReloading()
    {
        if (Settings.ReloadSettings.Secrets)
        {
            switch (Settings.ReloadSettings.Mode)
            {
                case ReloadMethods.Event:
                    SecretWatcher = K8sClient.WatchNamespacedSecretAsync(
                        Settings.Name,
                        Settings.Namespace,
                        onEvent: (eventType, item) =>
                        {
                            Logger?.LogInformation("Receved {eventType} event for Secret {secretName} with {entries} values", eventType.ToString(), Settings.Name, item?.Data?.Count);
                            switch (eventType)
                            {
                                case WatchEventType.Added:
                                case WatchEventType.Modified:
                                case WatchEventType.Deleted:
                                    ProcessData(item);
                                    break;
                                default:
                                    Logger?.LogDebug("Event type {eventType} is not support, no action has been taken", eventType);
                                    break;
                            }
                        },
                        onError: (exception) =>
                        {
                            Logger?.LogCritical(exception, "Secret watcher on {namespace}.{name} encountered an error!", Settings.Namespace, Settings.Name);
                        },
                        onClosed: () => { Logger?.LogInformation("Secret watcher on {namespace}.{name} connection has closed", Settings.Namespace, Settings.Name); }).GetAwaiter().GetResult();
                    break;
                case ReloadMethods.Polling:
                    if (!Polling)
                    {
                        StartPolling(Settings.ReloadSettings.Period);
                    }

                    break;
                default:
                    Logger?.LogError("Unsupported reload method!");
                    break;
            }
        }
    }

    private void ProcessData(V1Secret item)
    {
        if (item is null)
        {
            Logger?.LogWarning("ConfigMap response is null, no data could be processed");
            return;
        }

        var secretContents = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        if (item?.Data != null)
        {
            foreach (var data in item.Data)
            {
                secretContents[NormalizeKey(data.Key)] = Encoding.UTF8.GetString(data.Value);
            }
        }

        Data = secretContents;
    }
}