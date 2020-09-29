// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Steeltoe.Management.Endpoint
{
    public interface IManagementOptions
    {
        bool? Enabled { get; }

        [Obsolete("Use Exposure Options instead.")]
        bool? Sensitive { get; }

        string Path { get; }

        List<IEndpointOptions> EndpointOptions { get;  }

        /// <summary>
        /// Gets or sets a value indicating whether or not to use accurate HTTP status codes
        /// </summary>
        /// <remarks>IIS or HWC the buildpack will (by default) scrub the response body when 503 is returned</remarks>
        public bool UseStatusCodeFromResponse { get; set; }
    }
}
