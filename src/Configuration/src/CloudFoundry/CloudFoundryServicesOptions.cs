// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;

namespace Steeltoe.Configuration.CloudFoundry;

public sealed class CloudFoundryServicesOptions : BaseServiceOptions
{
    internal const string ServicesConfigurationRoot = "vcap";

    protected override string ConfigurationPrefix => ServicesConfigurationRoot;

    // This constructor is for use with IOptions.
    public CloudFoundryServicesOptions()
    {
    }

    public CloudFoundryServicesOptions(IConfiguration configuration)
    {
        ArgumentGuard.NotNull(configuration);

        configuration.GetSection(ServicesConfigurationRoot).Bind(this);
    }
}
