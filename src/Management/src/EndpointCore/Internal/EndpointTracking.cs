// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

﻿using System.Collections.Generic;

﻿namespace Steeltoe.Management.Endpoint.Internal
{
    /// <summary>
    /// Used to track what endpoints we have mapped
    /// </summary>
    internal class EndpointTracking
    {
        /// <summary>
        /// Gets the list of endpoints that have been mapped
        /// </summary>
        public List<string> Mapped { get; } = new List<string>();
    }
}
