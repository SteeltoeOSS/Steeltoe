// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Steeltoe.Discovery.Eureka.AppInfo;

[JsonConverter(typeof(JsonStringEnumConverter<DataCenterName>))]
public enum DataCenterName
{
    /// <summary>
    /// Legacy identifier reserved for Netflix's internal infrastructure and custom deployment environment.
    /// </summary>
    Netflix,

    /// <summary>
    /// Indicates that the service instance is running on Amazon Web Services (AWS).
    /// </summary>
    Amazon,

    /// <summary>
    /// Indicates the instance is running on standard, non-AWS infrastructure.
    /// </summary>
    MyOwn
}
