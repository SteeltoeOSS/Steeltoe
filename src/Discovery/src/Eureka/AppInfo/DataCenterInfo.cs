// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Eureka.Transport;

namespace Steeltoe.Discovery.Eureka.AppInfo;

public class DataCenterInfo : IDataCenterInfo
{
    public DataCenterName Name { get; }

    public DataCenterInfo(DataCenterName name)
    {
        Name = name;
    }

    internal static IDataCenterInfo FromJson(JsonInstanceInfo.JsonDataCenterInfo dataCenterInfo)
    {
        if (DataCenterName.MyOwn.ToString() == dataCenterInfo.Name)
        {
            return new DataCenterInfo(DataCenterName.MyOwn);
        }

        if (DataCenterName.Amazon.ToString() == dataCenterInfo.Name)
        {
            return new DataCenterInfo(DataCenterName.Amazon);
        }

        throw new ArgumentException($"Unsupported datacenter name '{dataCenterInfo.Name}'.", nameof(dataCenterInfo));
    }

    internal JsonInstanceInfo.JsonDataCenterInfo ToJson()
    {
        return new JsonInstanceInfo.JsonDataCenterInfo("com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo", Name.ToString());
    }
}
