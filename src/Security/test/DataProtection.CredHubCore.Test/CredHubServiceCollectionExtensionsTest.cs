// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Security.DataProtection.CredHub.Test
{
    public class CredHubServiceCollectionExtensionsTest
    {
        ////[Fact(Skip = "This test is incomplete, would require a mocked server to return the token")]
        ////public void AddCredHubClient_WithUAACreds_UsesUAACreds()
        ////{
        ////    // Arrange
        ////    IServiceCollection services = new ServiceCollection();
        ////    var appsettings = new Dictionary<string, string>()
        ////    {
        ////        ["CredHubClient:CredHubUser"] = "credhub_client",
        ////        ["CredHubClient:CredHubPassword"] = "secret",
        ////    };

        ////    ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
        ////    configurationBuilder.AddInMemoryCollection(appsettings);
        ////    var config = configurationBuilder.Build();

        ////    // Act and Assert
        ////    services.AddCredHubClient(config);
        ////    Assert.True(services.Contains(new ServiceDescriptor(typeof(ICredHubClient), null)));
        ////}
    }
}
