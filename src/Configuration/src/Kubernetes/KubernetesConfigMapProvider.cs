// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using k8s;
using k8s.Autorest;
using k8s.Models;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Kubernetes;

namespace Steeltoe.Configuration.Kubernetes;

internal sealed class KubernetesConfigMapProvider : KubernetesProviderBase, IDisposable
{
    private Watcher<V1ConfigMap>? _configMapWatcher;

    public KubernetesConfigMapProvider(IKubernetes kubernetes, KubernetesConfigSourceSettings settings, CancellationToken cancellationToken)
        : base(kubernetes, settings, cancellationToken)
    {
    }

    public override void Load()
    {
        if (KubernetesClient != null)
        {
            try
            {
                HttpOperationResponse<V1ConfigMap> response = KubernetesClient.CoreV1
                    .ReadNamespacedConfigMapWithHttpMessagesAsync(Settings.Name, Settings.Namespace, cancellationToken: CancellationToken).GetAwaiter()
                    .GetResult();

                ProcessData(response.Body);
                EnableReloading();
            }
            catch (HttpOperationException exception)
            {
                if (exception.Response.StatusCode == HttpStatusCode.Forbidden)
                {
                    Logger.LogCritical(exception,
                        "Failed to retrieve ConfigMap '{configMapName}' in namespace '{configMapNamespace}'. Confirm that your service account has the necessary permissions",
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
        _configMapWatcher?.Dispose();
        _configMapWatcher = null;

        KubernetesClient?.Dispose();
        KubernetesClient = null;
    }

    private static IDictionary<string, string?> ParseConfigMapFile(Stream jsonFileContents)
    {
        var exposedJsonConfigurationProvider = new ExposedJsonStreamConfigurationParser(new JsonStreamConfigurationSource
        {
            Stream = jsonFileContents
        });

        exposedJsonConfigurationProvider.Load();
        return exposedJsonConfigurationProvider.GetData();
    }

    private static string NormalizeKey(string key)
    {
        return key.Replace("__", ":", StringComparison.Ordinal);
    }

    private void EnableReloading()
    {
        if (Settings.ReloadSettings.ConfigMaps && !IsPolling)
        {
            switch (Settings.ReloadSettings.Mode)
            {
                case ReloadMethod.Event:
                    EnableEventReloading();
                    break;
                case ReloadMethod.Polling:
                    StartPolling(Settings.ReloadSettings.Period);
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
            Task<HttpOperationResponse<V1ConfigMapList>> responseTask =
                KubernetesClient.CoreV1.ListNamespacedConfigMapWithHttpMessagesAsync(Settings.Namespace, watch: true, cancellationToken: CancellationToken);

            _configMapWatcher = responseTask.Watch<V1ConfigMap, V1ConfigMapList>((eventType, item) =>
                {
                    Logger.LogInformation("Received {eventType} event for ConfigMap {configMapName} with {entries} values", eventType.ToString(), Settings.Name,
                        item?.Data?.Count);

                    switch (eventType)
                    {
                        case WatchEventType.Added:
                        case WatchEventType.Modified:
                        case WatchEventType.Deleted:
                            ProcessData(item);
                            break;
                        default:
                            Logger.LogDebug("Event type {eventType} is not supported, no action has been taken", eventType);
                            break;
                    }
                },
                exception => Logger.LogCritical(exception, "ConfigMap watcher on {namespace}.{name} encountered an error!", Settings.Namespace, Settings.Name),
                () => Logger.LogInformation("ConfigMap watcher on {namespace}.{name} connection has closed", Settings.Namespace, Settings.Name));
        }
    }

    private void ProcessData(V1ConfigMap? item)
    {
        if (item is null)
        {
            Logger.LogWarning("ConfigMap response is null, no data could be processed");
            return;
        }

        var configMapContents = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        if (item.Data != null)
        {
            foreach (KeyValuePair<string, string> data in item.Data)
            {
                if (IsAppsettingsKey(data.Key))
                {
                    using Stream stream = GenerateStreamFromString(data.Value);
                    IDictionary<string, string?> jsonConfiguration = ParseConfigMapFile(stream);

                    foreach (string jsonKey in jsonConfiguration.Keys)
                    {
                        configMapContents[NormalizeKey(jsonKey)] = jsonConfiguration[jsonKey];
                    }
                }
                else
                {
                    configMapContents[NormalizeKey(data.Key)] = data.Value;
                }
            }
        }

        Data = configMapContents;
    }

    private bool IsAppsettingsKey(string key)
    {
        return key.StartsWith("appsettings", StringComparison.OrdinalIgnoreCase) && key.EndsWith("json", StringComparison.OrdinalIgnoreCase);
    }

    private Stream GenerateStreamFromString(string source)
    {
        var stream = new MemoryStream();

        using (var writer = new StreamWriter(stream, leaveOpen: true))
        {
            writer.Write(source);
        }

        stream.Position = 0;
        return stream;
    }

    /// <summary>
    /// A private class to get access to Data while still using the JsonStreamConfigurationProvider to parse the value of an appsettings.json key in a
    /// ConfigMap. This requires a dependency on the Microsoft.Extensions.Configuration.Json package, but will ensure users' appsettings.json values will be
    /// parsed consistently.
    /// </summary>
    private sealed class ExposedJsonStreamConfigurationParser : JsonStreamConfigurationProvider
    {
        public ExposedJsonStreamConfigurationParser(JsonStreamConfigurationSource source)
            : base(source)
        {
        }

        public IDictionary<string, string?> GetData()
        {
            return Data;
        }
    }
}
