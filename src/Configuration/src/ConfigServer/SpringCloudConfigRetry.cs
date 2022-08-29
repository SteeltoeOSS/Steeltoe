// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Extensions.Configuration.ConfigServer;

public sealed class SpringCloudConfigRetry
{
    public bool Enabled { get; set; } = ConfigServerClientSettings.DefaultRetryEnabled;

    public int InitialInterval { get; set; } = ConfigServerClientSettings.DefaultInitialRetryInterval;

    public int MaxInterval { get; set; } = ConfigServerClientSettings.DefaultMaxRetryInterval;

    public double Multiplier { get; set; } = ConfigServerClientSettings.DefaultRetryMultiplier;

    public int MaxAttempts { get; set; } = ConfigServerClientSettings.DefaultMaxRetryAttempts;
}
