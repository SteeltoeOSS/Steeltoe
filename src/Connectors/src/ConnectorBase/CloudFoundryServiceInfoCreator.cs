// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
        private static readonly object _lock = new object();

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
