﻿// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
                foreach (var view in this._stats.ViewManager.AllExportedViews)
                {
                    var data = this._stats.ViewManager.GetView(view.Name);

                    var builder = new PrometheusMetricBuilder()
                        .WithName(data.View.Name.AsString)
                        .WithDescription(data.View.Description);

                    builder = data.View.Aggregation.Match<PrometheusMetricBuilder>(
                        (agg) => { return builder.WithType("gauge"); }, // Func<ISum, M> p0
                        (agg) => { return builder.WithType("counter"); }, // Func< ICount, M > p1,
                        (agg) => { return builder.WithType("histogram"); }, // Func<IMean, M> p2,
                        (agg) => { return builder.WithType("histogram"); }, // Func< IDistribution, M > p3,
                        (agg) => { return builder.WithType("gauge"); }, // Func<ILastValue, M> p4,
                        (agg) => { return builder.WithType("gauge"); }); // Func< IAggregation, M > p6);

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
