﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Steeltoe.Common.Diagnostics;
using System;

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
            return JsonConvert.SerializeObject(
                value,
                GetSerializerSettings());
        }

        public JsonSerializer GetSerializer()
        {
            return JsonSerializer.Create(GetSerializerSettings());
        }

        public JsonSerializerSettings GetSerializerSettings()
        {
            return new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() }
            };
        }
    }
}
