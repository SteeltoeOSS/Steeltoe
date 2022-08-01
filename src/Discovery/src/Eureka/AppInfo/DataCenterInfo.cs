// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Eureka.Transport;

namespace Steeltoe.Discovery.Eureka.AppInfo;

public class DataCenterInfo : IDataCenterInfo
{
    public DataCenterName Name { get; private set; }

    public DataCenterInfo(DataCenterName name)
    {
        Name = name;
    }

    internal static IDataCenterInfo FromJson(JsonInstanceInfo.JsonDataCenterInfo dataCenterInfo)
    {
        if (DataCenterName.MyOwn.ToString().Equals(dataCenterInfo.Name))
        {
            return new DataCenterInfo(DataCenterName.MyOwn);
        }
        else if (DataCenterName.Amazon.ToString().Equals(dataCenterInfo.Name))
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
        // TODO: Other data centers @class settings?
        return new JsonInstanceInfo.JsonDataCenterInfo(
            "com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo",
            Name.ToString());
    }
}
