// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration.CloudFoundry;
using Steeltoe.Connectors.MongoDb;
using Xunit;

namespace Steeltoe.Connectors.Test.MongoDb;

public class MongoDbConnectionInfoTest
{
    [Fact]
    public void MongoDbConnectionInfo()
    {
        var cm = new ConnectionStringManager(new ConfigurationBuilder().Build());
        Connection connInfo = cm.Get<MongoDbConnectionInfo>();

        Assert.NotNull(connInfo);
        Assert.Equal("mongodb://localhost:27017", connInfo.ConnectionString);
        Assert.Equal("MongoDb", connInfo.Name);
    }

    [Fact]
    public void MongoDbConnectionInfo_FromCosmosVCAP()
    {
        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", TestHelpers.VcapApplication);
        using var servicesScope = new EnvironmentVariableScope("VCAP_SERVICES", MongoDbTestHelpers.SingleServerCosmosDbVcap);

        var cm = new ConnectionStringManager(new ConfigurationBuilder().AddCloudFoundry().Build());

        Connection connInfo = cm.Get<MongoDbConnectionInfo>();

        Assert.NotNull(connInfo);

        Assert.Equal(
            "mongodb://u83bde2c09fd:36SWUyZbIyuu4AwLWMbAal9QngyVbZJjyoH9m0kILXIiEA9fCUhb34JHOovSNk2jpbXEQDuGnsBrYj1vTlu41w==@u83bde2c09fd.documents.azure.com:10255/?ssl=true&replicaSet=globaldb",
            connInfo.ConnectionString);

        Assert.StartsWith("MongoDb", connInfo.Name, StringComparison.Ordinal);
    }
}
