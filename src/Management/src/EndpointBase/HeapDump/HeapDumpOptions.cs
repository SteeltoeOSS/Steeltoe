// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System;

namespace Steeltoe.Management.Endpoint.HeapDump
{
    [Obsolete("Use HeapdumpEndpointOptions instead.")]
    public class HeapDumpOptions : AbstractOptions, IHeapDumpOptions
    {
        private const string MANAGEMENT_INFO_PREFIX = "management:endpoints:heapdump";

        public HeapDumpOptions()
            : base()
        {
            Id = "heapdump";
        }

        public HeapDumpOptions(IConfiguration config)
            : base(MANAGEMENT_INFO_PREFIX, config)
        {
            if (string.IsNullOrEmpty(Id))
            {
                Id = "heapdump";
            }
        }
    }
}
