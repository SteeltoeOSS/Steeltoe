// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Transport;

namespace Steeltoe.Discovery.Eureka.Configuration;

public sealed class DataCenterInfo
{
    public DataCenterName Name { get; set; } = DataCenterName.MyOwn;

    internal static DataCenterInfo? FromJson(JsonDataCenterInfo? jsonDataCenterInfo)
    {
        if (jsonDataCenterInfo == null)
        {
            return null;
        }

        if (jsonDataCenterInfo.Name == nameof(DataCenterName.MyOwn))
        {
            return new DataCenterInfo
            {
                Name = DataCenterName.MyOwn
            };
        }

        if (jsonDataCenterInfo.Name == nameof(DataCenterName.Amazon))
        {
            return new DataCenterInfo
            {
                Name = DataCenterName.Amazon
            };
        }

        throw new ArgumentException($"Unsupported datacenter name '{jsonDataCenterInfo.Name}'.", nameof(jsonDataCenterInfo));
    }

    internal JsonDataCenterInfo ToJson()
    {
        return new JsonDataCenterInfo
        {
            ClassName = "com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo",
            Name = Name.ToString()
        };
    }
}
