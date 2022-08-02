// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Extensions.Configuration;

namespace Steeltoe.Connector.Services;

public class SqlServerServiceInfoFactory : ServiceInfoFactory
{
    public SqlServerServiceInfoFactory()
        : base(new Tags("sqlserver"), SqlServerServiceInfo.SqlServerScheme)
    {
    }

    public SqlServerServiceInfoFactory(Tags tags, string scheme)
        : base(tags, scheme)
    {
    }

    public SqlServerServiceInfoFactory(Tags tags, string[] schemes)
        : base(tags, schemes)
    {
    }

    public override IServiceInfo Create(Service binding)
    {
        string uri = GetUriFromCredentials(binding.Credentials);
        string username = GetUsernameFromCredentials(binding.Credentials);
        string password = GetPasswordFromCredentials(binding.Credentials);

        if (uri == null)
        {
            string host = GetHostFromCredentials(binding.Credentials);
            int port = GetPortFromCredentials(binding.Credentials);

            string database = GetStringFromCredentials(binding.Credentials, "name");

            if (host != null)
            {
                uri = new UriInfo(DefaultUriScheme, host, port, username, password, database).ToString();
            }
        }

        return Create(binding.Name, uri, username, password);
    }

    public IServiceInfo Create(string id, string url, string username, string password)
    {
        if (username == null && password == null)
        {
            return new SqlServerServiceInfo(id, url);
        }

        return new SqlServerServiceInfo(id, url, username, password);
    }
}
