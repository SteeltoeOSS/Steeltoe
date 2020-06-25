﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.Env
{
    public class EnvironmentDescriptor
    {
        public EnvironmentDescriptor(IList<string> activeProfiles, IList<PropertySourceDescriptor> sources)
        {
            ActiveProfiles = activeProfiles;
            PropertySources = sources;
        }

        [JsonPropertyName("activeProfiles")]
        public IList<string> ActiveProfiles { get; }

        [JsonPropertyName("propertySources")]
        public IList<PropertySourceDescriptor> PropertySources { get; }
    }
}
