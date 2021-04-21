// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Eureka.Transport;
using System;

namespace Steeltoe.Discovery.Eureka.AppInfo
{
    public class DataCenterInfo : IDataCenterInfo
    {
        public DataCenterName Name { get; internal set; }

        public DataCenterInfo(string dataCenterName)
        {
            if (Enum.TryParse(dataCenterName, out DataCenterName name))
            {
                Name = name;
            }
        }

        public DataCenterInfo(DataCenterName name)
        {
            Name = name;
        }

        internal static IDataCenterInfo FromJson(JsonInstanceInfo.JsonDataCenterInfo jcenter)
        {
            if (DataCenterName.MyOwn.ToString().Equals(jcenter.Name))
            {
                return new DataCenterInfo(DataCenterName.MyOwn);
            }
            else if (DataCenterName.Amazon.ToString().Equals(jcenter.Name))
            {
                return new DataCenterInfo(DataCenterName.Amazon);
            }
            else
            {
                throw new ArgumentOutOfRangeException("Datacenter name");
            }
        }

        internal JsonInstanceInfo.JsonDataCenterInfo ToJson()
        {
            // TODO: Other datacenters @class settings?
            return new JsonInstanceInfo.JsonDataCenterInfo(
                "com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo",
                Name.ToString());
        }
    }
}
