// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Extensions.Configuration.CloudFoundry;

public interface ICloudFoundrySettingsReader
{
    string ApplicationJson { get; }

    string InstanceId { get; }

    string InstanceIndex { get; }

    string InstanceInternalIp { get; }

    string InstanceIp { get; }

    string InstancePort { get; }

    string ServicesJson { get; }
}