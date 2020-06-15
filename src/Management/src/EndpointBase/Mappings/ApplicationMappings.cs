﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using System.Collections.Generic;

namespace Steeltoe.Management.Endpoint.Mappings
{
    public class ApplicationMappings
    {
        public ApplicationMappings(ContextMappings contextMappings)
        {
            // At this point, .NET will only ever has one application => "application"
            ContextMappings = new Dictionary<string, ContextMappings>()
            {
                { "application", contextMappings }
            };
        }

        [JsonProperty("contexts")]
        public IDictionary<string, ContextMappings> ContextMappings { get; }
    }
}
