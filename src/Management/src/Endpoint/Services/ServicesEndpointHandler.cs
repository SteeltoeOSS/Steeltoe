// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.Services;

internal sealed class ServicesEndpointHandler : IServicesEndpointHandler
{
    private readonly IOptionsMonitor<ServicesEndpointOptions> _optionsMonitor;
    private readonly Lazy<IList<ServiceRegistration>> _lazyServiceRegistrations;

    public EndpointOptions Options => _optionsMonitor.CurrentValue;

    public ServicesEndpointHandler(IOptionsMonitor<ServicesEndpointOptions> optionsMonitor, IServiceCollection services)
    {
        ArgumentGuard.NotNull(optionsMonitor);
        ArgumentGuard.NotNull(services);

        _optionsMonitor = optionsMonitor;
        _lazyServiceRegistrations = new Lazy<IList<ServiceRegistration>>(() => ConvertToRegistrations(services), LazyThreadSafetyMode.PublicationOnly);
    }

    private static List<ServiceRegistration> ConvertToRegistrations(IServiceCollection services)
    {
        return services.Select(descriptor => new ServiceRegistration(descriptor)).ToList();
    }

    public Task<IList<ServiceRegistration>> InvokeAsync(object? argument, CancellationToken cancellationToken)
    {
        return Task.FromResult(_lazyServiceRegistrations.Value);
    }
}
