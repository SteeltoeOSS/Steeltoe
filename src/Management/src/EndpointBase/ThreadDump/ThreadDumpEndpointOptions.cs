// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Management.Endpoint.ThreadDump
{
    public class ThreadDumpEndpointOptions : AbstractEndpointOptions, IThreadDumpOptions
    {
        private const string MANAGEMENT_INFO_PREFIX = "management:endpoints:dump";

        public ThreadDumpEndpointOptions()
            : base()
        {
            Id = "dump";
        }

        public ThreadDumpEndpointOptions(IConfiguration config)
            : base(MANAGEMENT_INFO_PREFIX, config)
        {
            if (string.IsNullOrEmpty(Id))
            {
                Id = "dump";
            }
        }
    }
}
