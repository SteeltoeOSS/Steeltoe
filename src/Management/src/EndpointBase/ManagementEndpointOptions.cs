// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint
{
    public class ManagementEndpointOptions : IManagementOptions
    {
        private const string DEFAULT_PATH = "/actuator";
        private const string MANAGEMENT_INFO_PREFIX = "management:endpoints";

        public ManagementEndpointOptions()
        {
            Path = DEFAULT_PATH;
            EndpointOptions = new List<IEndpointOptions>();
        }

        public ManagementEndpointOptions(IConfiguration config)
            : this()
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var section = config.GetSection(MANAGEMENT_INFO_PREFIX);
            if (section != null)
            {
                section.Bind(this);
                foreach (var converterTypeName in CustomJsonConverters ?? Array.Empty<string>())
                {
                    var converterType = Type.GetType(converterTypeName);
                    if (converterType != null)
                    {
                        var converterInstance = (JsonConverter)Activator.CreateInstance(converterType);
                        SerializerOptions.Converters.Add(converterInstance);
                    }
                }
            }
        }

        public bool? Enabled { get; set; }

        public bool? Sensitive { get; set; }

        public string Path { get; set; }

        public List<IEndpointOptions> EndpointOptions { get; set; }

        public bool UseStatusCodeFromResponse { get; set; } = true;

        public JsonSerializerOptions SerializerOptions { get; set; } = new () { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        /// <summary>
        /// Gets or sets a list of <see href="https://docs.microsoft.com/dotnet/api/system.type.assemblyqualifiedname">assembly-qualified</see> custom JsonCoverters
        /// </summary>
        public string[] CustomJsonConverters { get; set; }
    }
}
