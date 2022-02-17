// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.OpenTelemetry;
using Steeltoe.Management.OpenTelemetry.Exporters.Prometheus;
using System;
using System.Text;

namespace Steeltoe.Management.Endpoint.Metrics
{
    public class PrometheusScraperEndpoint : AbstractEndpoint<string>, IPrometheusScraperEndpoint
    {
        private readonly PrometheusExporterWrapper _exporter;
        //private readonly OpenTelemetryMetrics _metrics; //Temporarily 

        // private string _cachedMetrics;

        public PrometheusScraperEndpoint(IPrometheusEndpointOptions options, PrometheusExporterWrapper exporter)
            : base(options)
        {
             _exporter = exporter ?? throw new ArgumentNullException(nameof(exporter));
           //  _metrics = metrics;
        }

        public override string Invoke()
        {
            var result = string.Empty;
            try
            {
                var collectionResponse = _exporter.CollectionManager.EnterCollect().Result;
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
                //TODO: Handle ex
            }

            _exporter.OnExport = null;
            return result;
        }
    }
}
