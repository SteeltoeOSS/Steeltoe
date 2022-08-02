// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;
using Microsoft.Extensions.Logging;
using Steeltoe.Management.OpenTelemetry.Exporters;

namespace Steeltoe.Management.Endpoint.Metrics;

public class PrometheusScraperEndpoint : AbstractEndpoint<string>, IPrometheusScraperEndpoint
{
    private readonly SteeltoePrometheusExporter _exporter;
    private readonly ILogger<PrometheusScraperEndpoint> _logger;

    public PrometheusScraperEndpoint(IPrometheusEndpointOptions options, IEnumerable<MetricsExporter> exporters,
        ILogger<PrometheusScraperEndpoint> logger = null)
        : base(options)
    {
        _exporter = exporters?.OfType<SteeltoePrometheusExporter>().FirstOrDefault() ?? throw new ArgumentNullException(nameof(exporters));
        _logger = logger;
    }

    public override string Invoke()
    {
        string result = string.Empty;

        try
        {
            ICollectionResponse response = _exporter.CollectionManager.EnterCollect().Result;

            if (response is PrometheusCollectionResponse collectionResponse)
            {
                try
                {
                    if (collectionResponse.View.Count > 0)
                    {
                        result = Encoding.UTF8.GetString(collectionResponse.View.Array, 0, collectionResponse.View.Count);
                    }
                    else
                    {
                        throw new InvalidOperationException("Collection failure.");
                    }
                }
                finally
                {
                    _exporter.CollectionManager.ExitCollect();
                }
            }
            else
            {
                _logger?.LogWarning("Please ensure OpenTelemetry is configured via Steeltoe extension methods.");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex.Message);
        }

        _exporter.OnExport = null;
        return result;
    }
}
