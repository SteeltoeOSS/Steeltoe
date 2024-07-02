// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Configuration.ConfigServer;

/// <summary>
/// Holds the settings used to configure retries with the Spring Cloud Config Server provider.
/// </summary>
public sealed class ConfigServerRetrySettings
{
    /// <summary>
    /// Gets or sets a value indicating whether retries are enabled on failures. Default value: false.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets initial retry interval in milliseconds. Default value: 1000.
    /// </summary>
    public int InitialInterval { get; set; } = 1000;

    /// <summary>
    /// Gets or sets max retry interval in milliseconds. Default value: 2000.
    /// </summary>
    public int MaxInterval { get; set; } = 2000;

    /// <summary>
    /// Gets or sets the multiplier for next retry interval. Default value: 1.1.
    /// </summary>
    public double Multiplier { get; set; } = 1.1;

    /// <summary>
    /// Gets or sets the max number of retries the client will attempt. Default value: 6.
    /// </summary>
    public int Attempts { get; set; } = 6;
}
