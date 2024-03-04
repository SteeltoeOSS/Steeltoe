// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Configuration.CloudFoundry;

internal sealed class CloudFoundryEnvironmentSettingsReader : ICloudFoundrySettingsReader
{
    public string? ApplicationJson => Environment.GetEnvironmentVariable("VCAP_APPLICATION");
    public string? InstanceId => Environment.GetEnvironmentVariable("CF_INSTANCE_GUID");
    public string? InstanceIndex => Environment.GetEnvironmentVariable("CF_INSTANCE_INDEX");
    public string? InstanceInternalIP => Environment.GetEnvironmentVariable("CF_INSTANCE_INTERNAL_IP");
    public string? InstanceIP => Environment.GetEnvironmentVariable("CF_INSTANCE_IP");
    public string? InstancePort => Environment.GetEnvironmentVariable("CF_INSTANCE_PORT");
    public string? ServicesJson => Environment.GetEnvironmentVariable("VCAP_SERVICES");
}
