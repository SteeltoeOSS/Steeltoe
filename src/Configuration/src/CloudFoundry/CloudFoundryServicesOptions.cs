// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Extensions.Configuration.CloudFoundry;

public class CloudFoundryServicesOptions : ServicesOptions
{
    public static string ServicesConfigurationRoot => "vcap";

    public override string ConfigurationPrefix { get; protected set; } = ServicesConfigurationRoot;

    // This constructor is for use with IOptions
    public CloudFoundryServicesOptions()
    {
    }

    public CloudFoundryServicesOptions(IConfigurationRoot root)
        : base(root, ServicesConfigurationRoot)
    {
    }

    public CloudFoundryServicesOptions(IConfiguration config)
        : base(config, ServicesConfigurationRoot)
    {
    }
}
