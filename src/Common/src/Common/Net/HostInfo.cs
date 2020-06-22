﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Net
{
    public class HostInfo
    {
        public string Hostname { get; set; }

        public string IpAddress { get; set; }

        public bool Override { get; set; }

        public HostInfo()
        {
        }

        public HostInfo(string hostname)
        {
            Hostname = hostname;
        }
    }
}
