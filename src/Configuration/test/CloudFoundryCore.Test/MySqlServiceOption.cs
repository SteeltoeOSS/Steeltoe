// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Extensions.Configuration.CloudFoundry.Test;

public class MySqlServiceOption : CloudFoundryServicesOptions
{
    public MySqlServiceOption()
    {
    }

    public MySqlServiceOption(IConfiguration config)
        : base(config)
    {
    }

    public MySqlCredentials Credentials { get; set; }
}
