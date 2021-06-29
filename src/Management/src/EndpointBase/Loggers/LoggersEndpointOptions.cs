// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace Steeltoe.Management.Endpoint.Loggers
{
    public class LoggersEndpointOptions : AbstractEndpointOptions, ILoggersOptions
    {
        private const string MANAGEMENT_INFO_PREFIX = "management:endpoints:loggers";

        public LoggersEndpointOptions()
            : base()
        {
            Id = "loggers";
            AllowedVerbs = new List<string> { "Get", "Post" };
            ExactMatch = false;
        }

        public LoggersEndpointOptions(IConfiguration config)
            : base(MANAGEMENT_INFO_PREFIX, config)
        {
            if (string.IsNullOrEmpty(Id))
            {
                Id = "loggers";
            }

            AllowedVerbs = new List<string> { "Get", "Post" };
            ExactMatch = false;
        }
    }
}
