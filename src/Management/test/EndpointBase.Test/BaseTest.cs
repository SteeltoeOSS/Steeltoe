// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Diagnostics;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.Metrics;
using System;
using System.Text.Json;

namespace Steeltoe.Management.Endpoint.Test
{
    public class BaseTest : IDisposable
    {
        public virtual void Dispose()
        {
            DiagnosticsManager.Instance.Dispose();
        }

        public ILogger<T> GetLogger<T>()
        {
            var lf = new LoggerFactory();
            return lf.CreateLogger<T>();
        }

        public string Serialize<T>(T value)
        {
            return JsonSerializer.Serialize(
                value,
                GetSerializerOptions());
        }

        public JsonSerializerOptions GetSerializerOptions()
        {
            var options = new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                IgnoreNullValues = true
            };
            options.Converters.Add(new HealthConverter());
            options.Converters.Add(new MetricsResponseConverter());
            return options;
        }
    }
}
