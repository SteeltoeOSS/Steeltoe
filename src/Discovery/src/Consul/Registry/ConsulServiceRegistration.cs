// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using Steeltoe.Discovery.Consul.Configuration;
using Steeltoe.Discovery.Consul.Util;

namespace Steeltoe.Discovery.Consul.Registry;

/// <summary>
/// What to register with Consul for this app, decided once from a snapshot of the options: the registration payload, the kind of health check it
/// carries, and the heartbeat that belongs to that health check (if any).
/// </summary>
/// <remarks>
/// Only meaningful when this app registers at all (<see cref="ConsulDiscoveryOptions.Enabled" /> and <see cref="ConsulDiscoveryOptions.Register" />).
/// </remarks>
internal sealed class ConsulServiceRegistration
{
    /// <summary>
    /// Gets how Consul learns about the health of this app.
    /// </summary>
    public HealthCheckMode CheckMode { get; }

    /// <summary>
    /// Gets the payload to submit to Consul.
    /// </summary>
    public AgentServiceRegistration Payload { get; }

    /// <summary>
    /// Gets the options for the heartbeat to send, which is only set when the payload contains a TTL check.
    /// </summary>
    public ConsulHeartbeatOptions? HeartbeatOptions { get; }

    /// <summary>
    /// Gets a value indicating whether an HTTP health check was configured, but is disabled because the port of this app is unknown.
    /// </summary>
    public bool SkipHttpCheckDueToUnknownPort { get; }

    public ConsulServiceRegistration(ConsulServiceInstance serviceInstance, ConsulDiscoveryOptions options)
    {
        ArgumentNullException.ThrowIfNull(serviceInstance);
        ArgumentNullException.ThrowIfNull(options);

        AgentServiceCheck? check = null;

        if (options.RegisterHealthCheck)
        {
            if (options.Heartbeat is { Enabled: true } heartbeatOptions)
            {
                CheckMode = HealthCheckMode.Ttl;
                HeartbeatOptions = heartbeatOptions;
                check = CreateTtlCheck(heartbeatOptions, options);
            }
            else if (serviceInstance.Port > 0)
            {
                // Consul polls this app, so an HTTP check is only possible when the port is known.
                CheckMode = HealthCheckMode.Http;
                check = CreateHttpCheck(serviceInstance.Port, options);
            }
            else
            {
                SkipHttpCheckDueToUnknownPort = true;
            }
        }

        Payload = CreatePayload(serviceInstance, check);
    }

    private static AgentServiceCheck CreateTtlCheck(ConsulHeartbeatOptions heartbeatOptions, ConsulDiscoveryOptions options)
    {
        var check = new AgentServiceCheck
        {
            TTL = DateTimeConversions.ToTimeSpan(heartbeatOptions.TimeToLive)
        };

        SetDeregisterCriticalServiceAfter(check, options);
        return check;
    }

    private static AgentServiceCheck CreateHttpCheck(int port, ConsulDiscoveryOptions options)
    {
        var check = new AgentServiceCheck
        {
            Header = new Dictionary<string, List<string>>
            {
                // Override Management:Endpoints:UseStatusCodeFromResponse, Consul only looks at HTTP status code.
                ["X-Use-Status-Code-From-Response"] = ["true"]
            },
            TLSSkipVerify = options.HealthCheckTlsSkipVerify
        };

        if (!string.IsNullOrEmpty(options.HealthCheckUrl))
        {
            check.HTTP = options.HealthCheckUrl;
        }
        else
        {
            var uri = new Uri($"{options.EffectiveScheme}://{options.HostName}:{port}{options.HealthCheckPath}");
            check.HTTP = uri.ToString();
        }

        if (!string.IsNullOrEmpty(options.HealthCheckInterval))
        {
            check.Interval = DateTimeConversions.ToTimeSpan(options.HealthCheckInterval);
        }

        if (!string.IsNullOrEmpty(options.HealthCheckTimeout))
        {
            check.Timeout = DateTimeConversions.ToTimeSpan(options.HealthCheckTimeout);
        }

        SetDeregisterCriticalServiceAfter(check, options);
        return check;
    }

    private static void SetDeregisterCriticalServiceAfter(AgentServiceCheck check, ConsulDiscoveryOptions options)
    {
        if (!string.IsNullOrEmpty(options.HealthCheckCriticalTimeout))
        {
            check.DeregisterCriticalServiceAfter = DateTimeConversions.ToTimeSpan(options.HealthCheckCriticalTimeout);
        }
    }

    private static AgentServiceRegistration CreatePayload(ConsulServiceInstance serviceInstance, AgentServiceCheck? check)
    {
        var payload = new AgentServiceRegistration
        {
            ID = serviceInstance.InstanceId,
            Address = serviceInstance.Host,
            Name = serviceInstance.ServiceId,
            Tags = [.. serviceInstance.Tags],
            Meta = serviceInstance.Metadata.ToDictionary(pair => pair.Key, pair => pair.Value),
            Check = check
        };

        if (serviceInstance.Port > 0)
        {
            payload.Port = serviceInstance.Port;
        }

        return payload;
    }
}
