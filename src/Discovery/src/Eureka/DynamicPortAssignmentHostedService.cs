// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Steeltoe.Common;
using Steeltoe.Discovery.Eureka.Configuration;

namespace Steeltoe.Discovery.Eureka;

/// <summary>
/// Configures <see cref="EurekaInstanceOptions" /> with the dynamic port(s) this app listens on, once ASP.NET has determined them.
/// </summary>
internal sealed class DynamicPortAssignmentHostedService : IHostedLifecycleService
{
    private readonly IServer _server;
    private readonly AspNetServerListenState _listenState;
    private readonly EurekaInstanceOptionsChangeTokenSource _changeTokenSource;

    public DynamicPortAssignmentHostedService(IServer server, AspNetServerListenState listenState, EurekaInstanceOptionsChangeTokenSource changeTokenSource)
    {
        ArgumentGuard.NotNull(server);
        ArgumentGuard.NotNull(listenState);
        ArgumentGuard.NotNull(changeTokenSource);

        _server = server;
        _listenState = listenState;
        _changeTokenSource = changeTokenSource;
    }

    public static void Wire(IServiceCollection services)
    {
        ArgumentGuard.NotNull(services);

        // Skip wire-up when running from a Hosted Service project instead of an ASP.NET project.
        if (services.Any(descriptor => descriptor.ServiceType == typeof(IServer)))
        {
            services.AddHostedService<DynamicPortAssignmentHostedService>();
            services.AddSingleton<AspNetServerListenState>();
            services.AddSingleton<EurekaInstanceOptionsChangeTokenSource>();

            services.AddSingleton<IOptionsChangeTokenSource<EurekaInstanceOptions>>(serviceProvider =>
                serviceProvider.GetRequiredService<EurekaInstanceOptionsChangeTokenSource>());

            services.ConfigureOptions<EurekaInstanceDynamicPortsPostConfigureOptions>();
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StartingAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StartedAsync(CancellationToken cancellationToken)
    {
        // https://andrewlock.net/how-to-automatically-choose-a-free-port-in-asp-net-core/#how-do-i-find-out-which-port-was-selected-
        // https://andrewlock.net/8-ways-to-set-the-urls-for-an-aspnetcore-app/
        var serverAddressesFeature = _server.Features.Get<IServerAddressesFeature>();

        if (serverAddressesFeature != null)
        {
            _listenState.ListenOnAddresses = serverAddressesFeature.Addresses;
            _changeTokenSource.ReloadOptions();
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StoppingAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StoppedAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Enables to trigger change in <see cref="OptionsMonitor{EurekaInstanceOptions}" />.
    /// </summary>
    internal sealed class EurekaInstanceOptionsChangeTokenSource : IOptionsChangeTokenSource<EurekaInstanceOptions>
    {
        private ConfigurationReloadToken _changeToken = new();

        public string Name => string.Empty;

        public IChangeToken GetChangeToken()
        {
            return _changeToken;
        }

        public void ReloadOptions()
        {
            ConfigurationReloadToken previousToken = Interlocked.Exchange(ref _changeToken, new ConfigurationReloadToken());
            previousToken.OnReload();
        }
    }

    /// <summary>
    /// Configures <see cref="EurekaInstanceOptions" /> with the dynamic port(s) this app listens on.
    /// </summary>
    internal sealed class EurekaInstanceDynamicPortsPostConfigureOptions : IPostConfigureOptions<EurekaInstanceOptions>
    {
        private readonly AspNetServerListenState _listenState;
        private readonly ILogger<EurekaInstanceOptions> _optionsLogger;

        public EurekaInstanceDynamicPortsPostConfigureOptions(AspNetServerListenState listenState, ILoggerFactory loggerFactory)
        {
            ArgumentGuard.NotNull(listenState);
            ArgumentGuard.NotNull(loggerFactory);

            _listenState = listenState;
            _optionsLogger = loggerFactory.CreateLogger<EurekaInstanceOptions>();
        }

        public void PostConfigure(string? name, EurekaInstanceOptions options)
        {
            ArgumentGuard.NotNull(options);

            if (_listenState.ListenOnAddresses != null)
            {
                options.SetPortsFromListenAddresses(_listenState.ListenOnAddresses, "address features", _optionsLogger);
            }
        }
    }

    /// <summary>
    /// provides access to the list of addresses this app listens on.
    /// </summary>
    internal sealed class AspNetServerListenState
    {
        public ICollection<string>? ListenOnAddresses { get; set; }
    }
}
