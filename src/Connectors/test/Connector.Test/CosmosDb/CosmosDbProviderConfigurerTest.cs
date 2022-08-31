// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Connector.Services;
using Xunit;

namespace Steeltoe.Connector.CosmosDb.Test;

public class CosmosDbProviderConfigurerTest
{
    [Fact]
    public void UpdateConfiguration_WithNullCosmosDbServiceInfo_ReturnsExpected()
    {
        var configurer = new CosmosDbProviderConfigurer();

        var options = new CosmosDbConnectorOptions
        {
            Host = "https://someHost:443",
            MasterKey = "masterKey",
            ReadOnlyKey = "readOnlyKey",
            DatabaseId = "databaseId",
            DatabaseLink = "databaseLink"
        };

        configurer.UpdateConfiguration(null, options);

        Assert.Equal("https://someHost:443", options.Host);
        Assert.Equal("masterKey", options.MasterKey);
        Assert.Equal("readOnlyKey", options.ReadOnlyKey);
        Assert.Equal("databaseId", options.DatabaseId);
        Assert.Equal("databaseLink", options.DatabaseLink);
        Assert.Null(options.ConnectionString);
    }

    [Fact]
    public void Configure_NoServiceInfo_ReturnsExpected()
    {
        var options = new CosmosDbConnectorOptions
        {
            Host = "https://someHost:443",
            MasterKey = "masterKey",
            ReadOnlyKey = "readOnlyKey",
            DatabaseId = "databaseId",
            DatabaseLink = "databaseLink"
        };

        var configurer = new CosmosDbProviderConfigurer();

        string connString = configurer.Configure(null, options);

        Assert.Equal("AccountEndpoint=https://someHost:443;AccountKey=masterKey;", connString);
    }

    [Fact]
    public void Configure_ServiceInfoOverridesConfig_ReturnsExpected()
    {
        var options = new CosmosDbConnectorOptions
        {
            Host = "https://someHost:443",
            MasterKey = "masterKey",
            ReadOnlyKey = "readOnlyKey",
            DatabaseId = "databaseId",
            DatabaseLink = "databaseLink"
        };

        var configurer = new CosmosDbProviderConfigurer();

        var si = new CosmosDbServiceInfo("MyId")
        {
            Host = "https://u332d11658f3.documents.azure.com:443/",
            MasterKey = "lXYMGIE4mYITjXvHwQjkh0U07lwF513NdbTfeyGndeqjVXzwKQ3ZalKXQNYeIZovoyl57IY1J0KnJUH36EPufA==",
            ReadOnlyKey = "hy5XZOeVnBeMmbB9FGcD54tttGKExad9XkGhn5Esc4jAM60OF2U7TcCXgffqBtBRuPAp0uFqKvz1l13OX8auPw==",
            DatabaseId = "u33ba24fd208",
            DatabaseLink = "cbs/sTB+AA==/"
        };

        string connString = configurer.Configure(si, options);

        Assert.Equal("https://u332d11658f3.documents.azure.com:443/", options.Host);
        Assert.Equal("lXYMGIE4mYITjXvHwQjkh0U07lwF513NdbTfeyGndeqjVXzwKQ3ZalKXQNYeIZovoyl57IY1J0KnJUH36EPufA==", options.MasterKey);
        Assert.Equal("hy5XZOeVnBeMmbB9FGcD54tttGKExad9XkGhn5Esc4jAM60OF2U7TcCXgffqBtBRuPAp0uFqKvz1l13OX8auPw==", options.ReadOnlyKey);
        Assert.Equal("u33ba24fd208", options.DatabaseId);
        Assert.Equal("cbs/sTB+AA==/", options.DatabaseLink);

        Assert.Equal(
            "AccountEndpoint=https://u332d11658f3.documents.azure.com:443/;AccountKey=lXYMGIE4mYITjXvHwQjkh0U07lwF513NdbTfeyGndeqjVXzwKQ3ZalKXQNYeIZovoyl57IY1J0KnJUH36EPufA==;",
            connString);
    }
}
