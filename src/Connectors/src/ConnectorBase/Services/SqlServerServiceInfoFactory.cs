// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Extensions.Configuration.CloudFoundry;

namespace Steeltoe.CloudFoundry.Connector.Services
{
    public class SqlServerServiceInfoFactory : ServiceInfoFactory
    {
        public SqlServerServiceInfoFactory()
            : base(new Tags("sqlserver"), SqlServerServiceInfo.SQLSERVER_SCHEME)
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
            var uri = GetUriFromCredentials(binding.Credentials);
            var username = GetUsernameFromCredentials(binding.Credentials);
            var password = GetPasswordFromCredentials(binding.Credentials);

            if (uri == null)
            {
                var host = GetHostFromCredentials(binding.Credentials);
                var port = GetPortFromCredentials(binding.Credentials);

                var database = GetStringFromCredentials(binding.Credentials, "name");

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
            else
            {
                return new SqlServerServiceInfo(id, url, username, password);
            }
        }
    }
}
