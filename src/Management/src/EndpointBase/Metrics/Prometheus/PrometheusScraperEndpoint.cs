// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Management.Census.Stats;
using Steeltoe.Management.Endpoint.Metrics.Prometheus;
using System;
using System.IO;
using System.Text;

namespace Steeltoe.Management.Endpoint.Metrics
{
    public class PrometheusScraperEndpoint : AbstractEndpoint<string>
    {
        private readonly ILogger<PrometheusScraperEndpoint> _logger;
        private readonly IStats _stats;

        public PrometheusScraperEndpoint(IPrometheusOptions options, IStats stats,  ILogger<PrometheusScraperEndpoint> logger = null)
            : base(options)
        {
            _stats = stats ?? throw new ArgumentNullException(nameof(stats));
            _logger = logger;
        }

        public new IEndpointOptions Options
        {
            get
            {
                return options as IEndpointOptions;
            }
        }

        public override string Invoke()
        {
            string returnValue;
            using var output = new MemoryStream();
            using (var writer = new StreamWriter(output))
            {
                foreach (var view in _stats.ViewManager.AllExportedViews)
                {
                    var data = _stats.ViewManager.GetView(view.Name);

                    var builder = new PrometheusMetricBuilder()
                        .WithName(data.View.Name.AsString)
                        .WithDescription(data.View.Description);

                    builder = data.View.Aggregation.Match<PrometheusMetricBuilder>(
                        (agg) => { return builder.WithType("gauge"); }, // Func<ISum, M> p0
                        (agg) => { return builder.WithType("counter"); }, // Func< ICount, M > p1,
                        (agg) => { return builder.WithType("histogram"); }, // Func<IMean, M> p2,
                        (agg) => { return builder.WithType("histogram"); }, // Func< IDistribution, M > p3,
                        (agg) => { return builder.WithType("gauge"); }, // Func<ILastValue, M> p4,
                        (agg) => { return builder.WithType("gauge"); }); // Func< IAggregation, M > p6)

                    foreach (var value in data.AggregationMap)
                    {
                        var metricValueBuilder = builder.AddValue();

                        // TODO: This is not optimal. Need to refactor to split builder into separate functions
                        metricValueBuilder =
                            value.Value.Match<PrometheusMetricBuilder.PrometheusMetricValueBuilder>(
                                metricValueBuilder.WithValue,
                                metricValueBuilder.WithValue,
                                metricValueBuilder.WithValue,
                                metricValueBuilder.WithValue,
                                metricValueBuilder.WithValue,
                                metricValueBuilder.WithValue,
                                metricValueBuilder.WithValue,
                                metricValueBuilder.WithValue);

                        for (var i = 0; i < value.Key.Values.Count; i++)
                        {
                            metricValueBuilder.WithLabel(data.View.Columns[i].Name, value.Key.Values[i].AsString);
                        }
                    }

                    builder.Write(writer);
                }

                writer.Flush();
                returnValue = Encoding.Default.GetString(output.ToArray());
            }

            return returnValue;
        }
    }
}
