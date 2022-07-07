// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Extensions.Configuration.ConfigServer;

public class SpringCloudConfigDiscovery
{
    public bool Enabled { get; set; } = ConfigServerClientSettings.DEFAULT_DISCOVERY_ENABLED;

    public string ServiceId { get; set; } = ConfigServerClientSettings.DEFAULT_CONFIGSERVER_SERVICEID;
}
