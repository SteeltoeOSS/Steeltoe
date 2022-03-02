// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Management.OpenTelemetry.Exporters;
using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace Steeltoe.Management.Endpoint.Metrics
{
    public class PrometheusScraperEndpoint : AbstractEndpoint<string>, IPrometheusScraperEndpoint
    {
        private readonly SteeltoePrometheusExporter _exporter;
        private readonly ILogger<PrometheusScraperEndpoint> _logger;

        public PrometheusScraperEndpoint(IPrometheusEndpointOptions options, IEnumerable<IMetricsExporter> exporters, ILogger<PrometheusScraperEndpoint> logger = null)
            : base(options)
        {
            _exporter = exporters?.OfType<SteeltoePrometheusExporter>().FirstOrDefault() ?? throw new ArgumentNullException(nameof(exporters));
            _logger = logger;
        }

        public override string Invoke()
        {
            var result = string.Empty;
            try
            {
                var collectionResponse = (PrometheusCollectionResponse)_exporter.CollectionManager.EnterCollect().Result;
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
            catch (Exception ex)
            {
                _logger?.LogError(ex.Message);
            }

            _exporter.OnExport = null;
            return result;
        }
    }
}
