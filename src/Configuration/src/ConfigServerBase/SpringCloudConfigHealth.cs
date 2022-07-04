// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Extensions.Configuration.ConfigServer;

public class SpringCloudConfigHealth
{
    public bool Enabled { get; set; } = ConfigServerClientSettings.DefaultHealthEnabled;

    public long TimeToLive { get; set; } = ConfigServerClientSettings.DefaultHealthTimetolive;
}
