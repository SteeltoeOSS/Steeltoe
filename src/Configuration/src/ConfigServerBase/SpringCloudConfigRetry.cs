// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Extensions.Configuration.ConfigServer;

public class SpringCloudConfigRetry
{
    public bool Enabled { get; set; } = ConfigServerClientSettings.DEFAULT_RETRY_ENABLED;

    public int InitialInterval { get; set; } = ConfigServerClientSettings.DEFAULT_INITIAL_RETRY_INTERVAL;

    public int MaxInterval { get; set; } = ConfigServerClientSettings.DEFAULT_MAX_RETRY_INTERVAL;

    public double Multiplier { get; set; } = ConfigServerClientSettings.DEFAULT_RETRY_MULTIPLIER;

    public int MaxAttempts { get; set; } = ConfigServerClientSettings.DEFAULT_MAX_RETRY_ATTEMPTS;
}
