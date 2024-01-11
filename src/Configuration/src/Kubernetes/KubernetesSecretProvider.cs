// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Text;
using k8s;
using k8s.Autorest;
using k8s.Models;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Kubernetes;

namespace Steeltoe.Configuration.Kubernetes;

internal sealed class KubernetesSecretProvider : KubernetesProviderBase, IDisposable
{
    private Watcher<V1Secret>? _secretWatcher;

    public KubernetesSecretProvider(IKubernetes kubernetes, KubernetesConfigSourceSettings settings, CancellationToken cancellationToken)
        : base(kubernetes, settings, cancellationToken)
    {
    }

    public override void Load()
    {
        if (KubernetesClient != null)
        {
            try
            {
                HttpOperationResponse<V1Secret> response = KubernetesClient.CoreV1
                    .ReadNamespacedSecretWithHttpMessagesAsync(Settings.Name, Settings.Namespace, cancellationToken: CancellationToken).GetAwaiter()
                    .GetResult();

                ProcessData(response.Body);
                EnableReloading();
            }
            catch (HttpOperationException exception)
            {
                if (exception.Response.StatusCode == HttpStatusCode.Forbidden)
                {
                    Logger.LogCritical(exception,
                        "Failed to retrieve secret '{SecretName}' in namespace '{SecretNamespace}'. Confirm that your service account has the necessary permissions",
                        Settings.Name, Settings.Namespace);
                }
                else if (exception.Response.StatusCode == HttpStatusCode.NotFound)
                {
                    EnableReloading();
                    return;
                }

                throw;
            }
        }
    }

    public void Dispose()
    {
        _secretWatcher?.Dispose();
        _secretWatcher = null;

        KubernetesClient?.Dispose();
        KubernetesClient = null;
    }

    private static string NormalizeKey(string key)
    {
        return key.Replace("__", ":", StringComparison.Ordinal);
    }

    private void EnableReloading()
    {
        if (Settings.ReloadSettings.Secrets)
        {
            switch (Settings.ReloadSettings.Mode)
            {
                case ReloadMethod.Event:
                    EnableEventReloading();
                    break;
                case ReloadMethod.Polling:
                    if (!IsPolling)
                    {
                        StartPolling(Settings.ReloadSettings.Period);
                    }

                    break;
                default:
                    Logger.LogError("Unsupported reload method!");
                    break;
            }
        }
    }

    private void EnableEventReloading()
    {
        if (KubernetesClient != null)
        {
            Task<HttpOperationResponse<V1SecretList>> responseTask =
                KubernetesClient.CoreV1.ListNamespacedSecretWithHttpMessagesAsync(Settings.Namespace, watch: true, cancellationToken: CancellationToken);

            _secretWatcher = responseTask.Watch<V1Secret, V1SecretList>((eventType, item) =>
                {
                    Logger.LogInformation("Received {eventType} event for Secret {secretName} with {entries} values", eventType, Settings.Name,
                        item?.Data?.Count);

                    switch (eventType)
                    {
                        case WatchEventType.Added:
                        case WatchEventType.Modified:
                        case WatchEventType.Deleted:
                            ProcessData(item);
                            break;
                        default:
                            Logger.LogDebug("Event type {eventType} is not support, no action has been taken", eventType);
                            break;
                    }
                }, exception => Logger.LogCritical(exception, "Secret watcher on {namespace}.{name} encountered an error!", Settings.Namespace, Settings.Name),
                () => Logger.LogInformation("Secret watcher on {namespace}.{name} connection has closed", Settings.Namespace, Settings.Name));
        }
    }

    private void ProcessData(V1Secret? item)
    {
        if (item is null)
        {
            Logger.LogWarning("ConfigMap response is null, no data could be processed");
            return;
        }

        var secretContents = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        if (item.Data != null)
        {
            foreach (KeyValuePair<string, byte[]> data in item.Data)
            {
                secretContents[NormalizeKey(data.Key)] = Encoding.UTF8.GetString(data.Value);
            }
        }

        Data = secretContents;
    }
}
