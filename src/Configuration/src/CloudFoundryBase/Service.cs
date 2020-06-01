// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Steeltoe.Extensions.Configuration.CloudFoundry
{
    public class Service
    {
        public string Name { get; set; }

        public string Label { get; set; }

        public string[] Tags { get; set; }

        public string Plan { get; set; }

        public Dictionary<string, Credential> Credentials { get; set; } = new Dictionary<string, Credential>(StringComparer.InvariantCultureIgnoreCase);
    }
}
