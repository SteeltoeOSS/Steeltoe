// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Configuration.ConfigServer;

public sealed class SpringCloudConfigRetry
{
    public bool Enabled { get; set; }
    public int InitialInterval { get; set; } = 1000;
    public int MaxInterval { get; set; } = 2000;
    public double Multiplier { get; set; } = 1.1;
    public int MaxAttempts { get; set; } = 6;
}
