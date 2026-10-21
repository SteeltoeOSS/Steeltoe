// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Discovery;

namespace Steeltoe.Management.Endpoint.Test.Actuators.ServiceRegistry;

internal sealed class FakeServiceRegistryDiscoveryClient(string description, bool isRegistrationEnabled) : IDiscoveryClient, IServiceRegistry
{
    private static readonly string[] SupportedStatuses = ["UP", "OUT_OF_SERVICE"];

    public string Description { get; } = description;
    public bool IsRegistrationEnabled { get; } = isRegistrationEnabled;
    public string Status { get; private set; } = "UP";

#pragma warning disable CS0067 // The event is never used
    public event EventHandler<DiscoveryInstancesFetchedEventArgs>? InstancesFetched;
#pragma warning restore CS0067 // The event is never used

    public Task<object> GetStatusAsync(CancellationToken cancellationToken)
    {
        object status = new Dictionary<string, string>
        {
            ["status"] = Status
        };

        return Task.FromResult(status);
    }

    public Task SetStatusAsync(string status, CancellationToken cancellationToken)
    {
        if (!SupportedStatuses.Contains(status))
        {
            throw new ArgumentException($"Unknown status: {status}", nameof(status));
        }

        Status = status;
        return Task.CompletedTask;
    }

    public IServiceInstance GetLocalServiceInstance()
    {
        throw new NotSupportedException();
    }

    public Task<ISet<string>> GetServiceIdsAsync(CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public Task<IList<IServiceInstance>> GetInstancesAsync(string serviceId, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public Task ShutdownAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
