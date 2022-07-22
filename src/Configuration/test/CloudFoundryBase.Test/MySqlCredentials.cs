// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Extensions.Configuration.CloudFoundry.Test;

public class MySqlCredentials
{
    public string Hostname { get; set; }

    public int Port { get; set; }

    public string Name { get; set; }

    public string Username { get; set; }

    public string Password { get; set; }

    public string Uri { get; set; }

    public string JdbcUrl { get; set; }
}