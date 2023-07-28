// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.Info;
using Steeltoe.Management.Endpoint.Info.Contributor;
using Steeltoe.Management.Info;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.Info;

public sealed class EndpointServiceCollectionTest : BaseTest
{
    [Fact]
    public void AddInfoActuator_AddsCorrectServices()
    {
        var services = new ServiceCollection();

        var appSettings = new Dictionary<string, string>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:path"] = "/management",
            ["management:endpoints:info:enabled"] = "false",
            ["management:endpoints:info:id"] = "infomanagement"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appSettings);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();
        services.AddSingleton<IConfiguration>(configurationRoot);
        services.AddLogging();
        services.AddInfoActuator();

        IInfoContributor extra = new TestInfoContributor();
        services.AddSingleton(extra);
        ILogger<InfoEndpointHandler> logger = new TestLogger();
        services.AddSingleton(logger);

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        IEnumerable<IInfoContributor> contributors = serviceProvider.GetServices<IInfoContributor>();

        Assert.NotNull(contributors);
        List<IInfoContributor> listOfContributors = contributors.ToList();
        Assert.Equal(4, listOfContributors.Count);
        Assert.Contains(listOfContributors, item => item is GitInfoContributor or AppSettingsInfoContributor or BuildInfoContributor or TestInfoContributor);

        var handler = serviceProvider.GetService<IInfoEndpointHandler>();
        Assert.NotNull(handler);
    }
}
