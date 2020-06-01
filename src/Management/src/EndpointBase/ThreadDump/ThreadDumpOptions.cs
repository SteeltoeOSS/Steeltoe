// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System;

namespace Steeltoe.Management.Endpoint.ThreadDump
{
    [Obsolete("Use ThreadDumpEndpointOptions instead")]
    public class ThreadDumpOptions : AbstractOptions, IThreadDumpOptions
    {
        private const string MANAGEMENT_INFO_PREFIX = "management:endpoints:dump";

        public ThreadDumpOptions()
            : base()
        {
            Id = "dump";
        }

        public ThreadDumpOptions(IConfiguration config)
            : base(MANAGEMENT_INFO_PREFIX, config)
        {
            if (string.IsNullOrEmpty(Id))
            {
                Id = "dump";
            }
        }
    }
}
