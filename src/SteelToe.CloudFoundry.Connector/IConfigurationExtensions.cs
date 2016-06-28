//
// Copyright 2015 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//


using Microsoft.Extensions.Configuration;
using SteelToe.CloudFoundry.Connector.Services;
using System;
using System.Collections.Generic;


namespace SteelToe.CloudFoundry.Connector
{
    public static class IConfigurationExtensions
    {
        public static List<SI> GetServiceInfos<SI>(this IConfiguration config) where SI : class
        {
            CloudFoundryServiceInfoCreator factory = CloudFoundryServiceInfoCreator.Instance(config);
            return factory.GetServiceInfos<SI>();
        }

        public static List<IServiceInfo> GetServiceInfos(this IConfiguration config, Type infoType)
        {
            CloudFoundryServiceInfoCreator factory = CloudFoundryServiceInfoCreator.Instance(config);
            return factory.GetServiceInfos(infoType);
        }

        public static IServiceInfo GetServiceInfo(this IConfiguration config, string id)
        {
            CloudFoundryServiceInfoCreator factory = CloudFoundryServiceInfoCreator.Instance(config);
            return factory.GetServiceInfo(id);
        }

        public static SI GetServiceInfo<SI>(this IConfiguration config, string id) where SI : class
        {
            CloudFoundryServiceInfoCreator factory = CloudFoundryServiceInfoCreator.Instance(config);
            return factory.GetServiceInfo<SI>(id);
        }

        public static SI GetSingletonServiceInfo<SI>(this IConfiguration config) where SI : class
        {
            List<SI> results = GetServiceInfos<SI>(config);
            if (results.Count > 0)
            {
                if (results.Count != 1)
                {
                    throw new ConnectorException(string.Format("Multiple services of type: {0}, bound to application.", typeof(SI)));
                }
                return results[0];
            }
            return null;
        }

        public static SI GetRequiredServiceInfo<SI>(this IConfiguration config, string serviceName) where SI : class
        {
            var info = GetServiceInfo<SI>(config, serviceName);
            if (info == null)
            {
                throw new ConnectorException(string.Format("No service with name: {0} found.", serviceName));
            }
            return info;
        }
    }
}
