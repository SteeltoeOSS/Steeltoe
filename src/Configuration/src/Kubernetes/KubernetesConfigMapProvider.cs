// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;
using Steeltoe.Common.Kubernetes;

namespace Steeltoe.Extensions.Configuration.Kubernetes;

internal sealed class KubernetesConfigMapProvider : KubernetesProviderBase, IDisposable
{
    private const string ConfigurationFileKeyPrefix = "appsettings";
    private const string ConfigurationFileKeySuffix = "json";

    private Watcher<V1ConfigMap> ConfigMapWatcher { get; set; }

    internal KubernetesConfigMapProvider(IKubernetes kubernetes, KubernetesConfigSourceSettings settings, CancellationToken cancellationToken = default)
        : base(kubernetes, settings, cancellationToken)
    {
        settings.Namespace ??= "default";
    }

    public override void Load()
    {
        try
        {
            HttpOperationResponse<V1ConfigMap> configMapResponse =
                KubernetesClient.ReadNamespacedConfigMapWithHttpMessagesAsync(Settings.Name, Settings.Namespace).GetAwaiter().GetResult();

            ProcessData(configMapResponse.Body);
            EnableReloading();
        }
        catch (HttpOperationException e)
        {
            if (e.Response.StatusCode == HttpStatusCode.Forbidden)
            {
                Logger?.LogCritical(e,
                    "Failed to retrieve configuration map '{configMapName}' in namespace '{configMapNamespace}'. Confirm that your service account has the necessary permissions",
                    Settings.Name, Settings.Namespace);
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
        ConfigMapWatcher?.Dispose();
        ConfigMapWatcher = null;

        KubernetesClient?.Dispose();
        KubernetesClient = null;
    }

    private static IDictionary<string, string> ParseConfigMapFile(Stream jsonFileContents)
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
        return key.Replace("__", ":");
    }

    private void EnableReloading()
    {
        if (Settings.ReloadSettings.ConfigMaps && !Polling)
        {
            switch (Settings.ReloadSettings.Mode)
            {
                case ReloadMethods.Event:
                    EnableEventReloading();
                    break;
                case ReloadMethods.Polling:
                    StartPolling(Settings.ReloadSettings.Period);
                    break;
                default:
                    Logger?.LogError("Unsupported reload method!");
                    break;
            }
        }
    }

    private void EnableEventReloading()
    {
        ConfigMapWatcher = KubernetesClient.WatchNamespacedConfigMapAsync(Settings.Name, Settings.Namespace, onEvent: (eventType, item) =>
                {
                    Logger?.LogInformation("Received {eventType} event for ConfigMap {configMapName} with {entries} values", eventType.ToString(),
                        Settings.Name,
                        item?.Data?.Count);

                    switch (eventType)
                    {
                        case WatchEventType.Added:
                        case WatchEventType.Modified:
                        case WatchEventType.Deleted:
                            ProcessData(item);
                            break;
                        default:
                            Logger?.LogDebug("Event type {eventType} is not supported, no action has been taken", eventType);
                            break;
                    }
                },
                onError: exception =>
                    Logger?.LogCritical(exception, "ConfigMap watcher on {namespace}.{name} encountered an error!", Settings.Namespace, Settings.Name),
                onClosed: () => Logger?.LogInformation("ConfigMap watcher on {namespace}.{name} connection has closed", Settings.Namespace, Settings.Name))
            .GetAwaiter()
            .GetResult();
    }

    private void ProcessData(V1ConfigMap item)
    {
        if (item is null)
        {
            Logger?.LogWarning("ConfigMap response is null, no data could be processed");
            return;
        }

        var configMapContents = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

        if (item?.Data != null)
        {
            foreach (KeyValuePair<string, string> data in item?.Data)
            {
                if (IsAppsettingsKey(data.Key))
                {
                    using Stream stream = GenerateStreamFromString(data.Value);
                    IDictionary<string, string> jsonConfiguration = ParseConfigMapFile(stream);

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
        return key.StartsWith(ConfigurationFileKeyPrefix, StringComparison.InvariantCultureIgnoreCase) &&
            key.EndsWith(ConfigurationFileKeySuffix, StringComparison.InvariantCultureIgnoreCase);
    }

    private Stream GenerateStreamFromString(string s)
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write(s);
        writer.Flush();
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

        public IDictionary<string, string> GetData()
        {
            return Data;
        }
    }
}
