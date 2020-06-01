// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Steeltoe.Management.Endpoint.CloudFoundry
{
    // TODO: Move this to the Hypermedia namespace in 3.0
    [Obsolete("This will move to Hypermedia namespace")]
    public class Links
    {
#pragma warning disable SA1307 // Accessible fields must begin with upper-case letter
        public string type = "steeltoe";
#pragma warning restore SA1307 // Accessible fields must begin with upper-case letter
        public Dictionary<string, Link> _links = new Dictionary<string, Link>();
    }
}
