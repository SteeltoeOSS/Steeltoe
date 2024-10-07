// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Management.Endpoint.Actuators.Services;
using Xunit.Abstractions;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Services;

public sealed class ServicesEndpointTest(ITestOutputHelper testOutputHelper) : BaseTest
{
    private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

    [Fact]
    public async Task Invoke_ReturnsExpected()
    {
        ServiceRegistration registration = await GetRegistrationForAsync(typeof(IServicesEndpointHandler).FullName);

        registration.Scope.Should().Be(nameof(ServiceLifetime.Singleton));
        registration.Type.Should().Be(typeof(ServicesEndpointHandler).FullName);
        registration.Key.Should().BeNull();
        registration.AssemblyName.Should().Be(typeof(ServicesEndpointHandler).Assembly.FullName);
        registration.Dependencies.Should().HaveCount(2);
        registration.Dependencies.ElementAt(0).Should().Be($"Microsoft.Extensions.Options.IOptionsMonitor`1[{typeof(ServicesEndpointOptions).FullName}]");
        registration.Dependencies.ElementAt(1).Should().Be(typeof(IServiceCollection).FullName);
    }

    [Fact]
    public async Task Can_resolve_transient_for_implementation_type()
    {
        ServiceRegistration registration = await GetRegistrationForAsync(typeof(ExampleService).FullName, services => services.AddTransient<ExampleService>());

        registration.Scope.Should().Be(nameof(ServiceLifetime.Transient));
        registration.Type.Should().Be(typeof(ExampleService).FullName);
        registration.Key.Should().BeNull();
        registration.AssemblyName.Should().Be(typeof(ExampleService).Assembly.FullName);
        registration.Dependencies.Should().BeEmpty();
    }

    [Fact]
    public async Task Can_resolve_keyed_transient_for_implementation_type()
    {
        ServiceRegistration registration =
            await GetRegistrationForAsync(typeof(ExampleService).FullName, services => services.AddKeyedTransient<ExampleService>("exampleKey"));

        registration.Scope.Should().Be(nameof(ServiceLifetime.Transient));
        registration.Type.Should().Be(typeof(ExampleService).FullName);
        registration.Key.Should().Be("exampleKey");
        registration.AssemblyName.Should().Be(typeof(ExampleService).Assembly.FullName);
        registration.Dependencies.Should().BeEmpty();
    }

    [Fact]
    public async Task Can_resolve_transient_for_interface_type_with_implementation_type()
    {
        ServiceRegistration registration =
            await GetRegistrationForAsync(typeof(IExampleService).FullName, services => services.AddTransient<IExampleService, ExampleService>());

        registration.Scope.Should().Be(nameof(ServiceLifetime.Transient));
        registration.Type.Should().Be(typeof(ExampleService).FullName);
        registration.Key.Should().BeNull();
        registration.AssemblyName.Should().Be(typeof(ExampleService).Assembly.FullName);
        registration.Dependencies.Should().BeEmpty();
    }

    [Fact]
    public async Task Can_resolve_keyed_transient_for_interface_type_with_implementation_type()
    {
        ServiceRegistration registration = await GetRegistrationForAsync(typeof(IExampleService).FullName,
            services => services.AddKeyedTransient<IExampleService, ExampleService>("exampleKey"));

        registration.Scope.Should().Be(nameof(ServiceLifetime.Transient));
        registration.Type.Should().Be(typeof(ExampleService).FullName);
        registration.Key.Should().Be("exampleKey");
        registration.AssemblyName.Should().Be(typeof(ExampleService).Assembly.FullName);
        registration.Dependencies.Should().BeEmpty();
    }

    [Fact]
    public async Task Can_resolve_scoped_for_implementation_factory()
    {
        ServiceRegistration registration =
            await GetRegistrationForAsync(typeof(ExampleService).FullName, services => services.AddScoped(_ => new ExampleService()));

        registration.Scope.Should().Be(nameof(ServiceLifetime.Scoped));
        registration.Type.Should().Be(typeof(ExampleService).FullName);
        registration.Key.Should().BeNull();
        registration.AssemblyName.Should().Be(typeof(ExampleService).Assembly.FullName);
        registration.Dependencies.Should().BeEmpty();
    }

    [Fact]
    public async Task Can_resolve_keyed_scoped_for_implementation_factory()
    {
        ServiceRegistration registration = await GetRegistrationForAsync(typeof(ExampleService).FullName,
            services => services.AddKeyedScoped<ExampleService>("exampleKey", (_, _) => new ExampleService()));

        registration.Scope.Should().Be(nameof(ServiceLifetime.Scoped));
        registration.Type.Should().Be(typeof(ExampleService).FullName);
        registration.Key.Should().Be("exampleKey");
        registration.AssemblyName.Should().Be(typeof(ExampleService).Assembly.FullName);
        registration.Dependencies.Should().BeEmpty();
    }

    [Fact]
    public async Task Can_resolve_scoped_for_interface_type_with_implementation_factory()
    {
        ServiceRegistration registration = await GetRegistrationForAsync(typeof(IExampleService).FullName,
            services => services.AddScoped<IExampleService>(_ => new ExampleService()));

        registration.Scope.Should().Be(nameof(ServiceLifetime.Scoped));
        registration.Type.Should().Be(typeof(IExampleService).FullName);
        registration.Key.Should().BeNull();
        registration.AssemblyName.Should().Be(typeof(ExampleService).Assembly.FullName);
        registration.Dependencies.Should().BeEmpty();
    }

    [Fact]
    public async Task Can_resolve_keyed_scoped_for_interface_type_with_implementation_factory()
    {
        ServiceRegistration registration = await GetRegistrationForAsync(typeof(IExampleService).FullName,
            services => services.AddKeyedScoped<IExampleService>("exampleKey", (_, _) => new ExampleService()));

        registration.Scope.Should().Be(nameof(ServiceLifetime.Scoped));
        registration.Type.Should().Be(typeof(IExampleService).FullName);
        registration.Key.Should().Be("exampleKey");
        registration.AssemblyName.Should().Be(typeof(ExampleService).Assembly.FullName);
        registration.Dependencies.Should().BeEmpty();
    }

    [Fact]
    public async Task Can_resolve_singleton_for_implementation_instance()
    {
        ServiceRegistration registration =
            await GetRegistrationForAsync(typeof(ExampleService).FullName, services => services.AddSingleton(new ExampleService()));

        registration.Scope.Should().Be(nameof(ServiceLifetime.Singleton));
        registration.Type.Should().Be(typeof(ExampleService).FullName);
        registration.Key.Should().BeNull();
        registration.AssemblyName.Should().Be(typeof(ExampleService).Assembly.FullName);
        registration.Dependencies.Should().BeEmpty();
    }

    [Fact]
    public async Task Can_resolve_keyed_singleton_for_implementation_instance()
    {
        ServiceRegistration registration = await GetRegistrationForAsync(typeof(ExampleService).FullName,
            services => services.AddKeyedSingleton("exampleKey", new ExampleService()));

        registration.Scope.Should().Be(nameof(ServiceLifetime.Singleton));
        registration.Type.Should().Be(typeof(ExampleService).FullName);
        registration.Key.Should().Be("exampleKey");
        registration.AssemblyName.Should().Be(typeof(ExampleService).Assembly.FullName);
        registration.Dependencies.Should().BeEmpty();
    }

    [Fact]
    public async Task Can_resolve_singleton_for_interface_with_implementation_instance()
    {
        ServiceRegistration registration = await GetRegistrationForAsync(typeof(IExampleService).FullName,
            services => services.AddSingleton<IExampleService>(new ExampleService()));

        registration.Scope.Should().Be(nameof(ServiceLifetime.Singleton));
        registration.Type.Should().Be(typeof(ExampleService).FullName);
        registration.Key.Should().BeNull();
        registration.AssemblyName.Should().Be(typeof(ExampleService).Assembly.FullName);
        registration.Dependencies.Should().BeEmpty();
    }

    [Fact]
    public async Task Can_resolve_keyed_singleton_for_interface_with_implementation_instance()
    {
        ServiceRegistration registration = await GetRegistrationForAsync(typeof(IExampleService).FullName,
            services => services.AddKeyedSingleton<IExampleService>("exampleKey", new ExampleService()));

        registration.Scope.Should().Be(nameof(ServiceLifetime.Singleton));
        registration.Type.Should().Be(typeof(ExampleService).FullName);
        registration.Key.Should().Be("exampleKey");
        registration.AssemblyName.Should().Be(typeof(ExampleService).Assembly.FullName);
        registration.Dependencies.Should().BeEmpty();
    }

    private async Task<ServiceRegistration> GetRegistrationForAsync(string? registrationName, Action<IServiceCollection>? registerService = null)
    {
        using var testContext = new TestContext(_testOutputHelper);

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddServicesActuator();
            registerService?.Invoke(services);
        };

        var handler = testContext.GetRequiredService<IServicesEndpointHandler>();
        IList<ServiceRegistration> registrations = await handler.InvokeAsync(null, CancellationToken.None);

        ServiceRegistration? registration = registrations.SingleOrDefault(registration => registration.Name == registrationName);
        registration.Should().NotBeNull();

        return registration!;
    }

    private interface IExampleService;

    internal sealed class ExampleService : IExampleService;
}
