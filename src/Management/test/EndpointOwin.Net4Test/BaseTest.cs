// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Steeltoe.Common.Diagnostics;
using System;

namespace Steeltoe.Management.Endpoint.Test
{
    public class BaseTest : IDisposable
    {
        public BaseTest()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            ManagementOptions.SetInstance(null);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        public virtual void Dispose()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            ManagementOptions.SetInstance(null);
#pragma warning restore CS0618 // Type or member is obsolete
            DiagnosticsManager.Instance.Dispose();
        }

        public ILogger<T> GetLogger<T>()
        {
            var lf = new LoggerFactory();
            return lf.CreateLogger<T>();
        }

        public string Serialize<T>(T value)
        {
            return JsonConvert.SerializeObject(
                value,
                new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
        }
    }
}
