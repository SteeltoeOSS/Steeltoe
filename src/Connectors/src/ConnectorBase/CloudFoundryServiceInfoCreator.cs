// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Connector.Services;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;

namespace Steeltoe.Connector.CloudFoundry
{
    public class CloudFoundryServiceInfoCreator : ServiceInfoCreator
    {
        private static IConfiguration _config;
        private static CloudFoundryServiceInfoCreator _me = null;
        private static object _lock = new object();

        internal CloudFoundryServiceInfoCreator(IConfiguration config)
            : base(config)
        {
#pragma warning disable S3010 // Static fields should not be updated in constructors
            _config = config;
#pragma warning restore S3010 // Static fields should not be updated in constructors
            BuildServiceInfoFactories();
            BuildServiceInfos();
        }

        public static new CloudFoundryServiceInfoCreator Instance(IConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (config == _config)
            {
                return _me;
            }

            lock (_lock)
            {
                if (config == _config)
                {
                    return _me;
                }

                _me = new CloudFoundryServiceInfoCreator(config);
            }

            return _me;
        }

        private void BuildServiceInfos()
        {
            ServiceInfos.Clear();

            var appInfo = new CloudFoundryApplicationOptions(_config);
            var serviceOpts = new CloudFoundryServicesOptions(_config);

            foreach (var serviceopt in serviceOpts.Services)
            {
                foreach (var s in serviceopt.Value)
                {
                    var factory = FindFactory(s);
                    if (factory != null && factory.Create(s) is ServiceInfo info)
                    {
                        info.ApplicationInfo = appInfo;
                        ServiceInfos.Add(info);
                    }
                }
            }
        }
    }
}
