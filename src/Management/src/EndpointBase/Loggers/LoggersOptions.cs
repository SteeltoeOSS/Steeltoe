// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System;

namespace Steeltoe.Management.Endpoint.Loggers
{
    [Obsolete("Use LoggersEndpointOptions instead.")]
    public class LoggersOptions : AbstractOptions, ILoggersOptions
    {
        private const string MANAGEMENT_INFO_PREFIX = "management:endpoints:loggers";

        public LoggersOptions()
            : base()
        {
            Id = "loggers";
        }

        public LoggersOptions(IConfiguration config)
            : base(MANAGEMENT_INFO_PREFIX, config)
        {
            if (string.IsNullOrEmpty(Id))
            {
                Id = "loggers";
            }
        }
    }
}
