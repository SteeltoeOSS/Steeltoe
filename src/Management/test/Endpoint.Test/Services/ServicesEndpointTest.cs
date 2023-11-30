// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Steeltoe.Management.Endpoint.Services;
using Steeltoe.Management.Endpoint.Test.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.Management.Endpoint.Test.Services;

public sealed class ServicesEndpointTest : BaseTest
{
    private readonly ITestOutputHelper _output;

    public ServicesEndpointTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task Invoke_ReturnsExpected()
    {
        using var testContext = new TestContext(_output);
        testContext.AdditionalServices = (services, _) => services.AddServicesActuatorServices();

        var handler = testContext.GetRequiredService<IServicesEndpointHandler>();
        IList<ServiceRegistration> registrations = await handler.InvokeAsync(null, CancellationToken.None);

        ServiceRegistration? handlerRegistration = registrations.SingleOrDefault(registration => registration.Name == nameof(IServicesEndpointHandler));
        handlerRegistration.Should().NotBeNull();

        handlerRegistration!.Scope.Should().Be("Singleton");
        handlerRegistration.Type.Should().Be("Steeltoe.Management.Endpoint.Services.IServicesEndpointHandler");
        handlerRegistration.AssemblyName.Should().Be(typeof(IServicesEndpointHandler).AssemblyQualifiedName);

        handlerRegistration.Dependencies.Should().HaveCount(2);

        handlerRegistration.Dependencies.ElementAt(0).Should().Be(
            "Microsoft.Extensions.Options.IOptionsMonitor`1[Steeltoe.Management.Endpoint.Services.ServicesEndpointOptions]");

        handlerRegistration.Dependencies.ElementAt(1).Should().Be("Microsoft.Extensions.DependencyInjection.IServiceCollection");
    }
}
