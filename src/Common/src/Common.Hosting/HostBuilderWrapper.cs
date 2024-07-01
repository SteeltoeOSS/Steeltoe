// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Steeltoe.Common.Hosting;

/// <summary>
/// A host-agnostic wrapper for <see cref="IHostBuilder" />, <see cref="IWebHostBuilder" /> and <see cref="WebApplicationBuilder" />. Intended to reduce
/// code duplication when targeting the various host builders.
/// </summary>
internal sealed class HostBuilderWrapper
{
    private readonly List<Action<HostBuilderContextWrapper, IServiceCollection>> _configureServicesActions = new();
    private readonly List<Action<HostBuilderContextWrapper, IConfigurationBuilder>> _configureAppConfigurationActions = new();
    private readonly List<Action<HostBuilderContextWrapper, ILoggingBuilder>> _configureLoggingActions = new();
    private readonly object _innerBuilder;

    private HostBuilderWrapper(object innerBuilder)
    {
        _innerBuilder = innerBuilder;
    }

    public static HostBuilderWrapper Wrap(IHostBuilder builder)
    {
        ArgumentGuard.NotNull(builder);

        var wrapper = new HostBuilderWrapper(builder);

        builder.ConfigureServices((context, services) =>
        {
            HostBuilderContextWrapper contextWrapper = HostBuilderContextWrapper.Wrap(context);
            InvokeDeferredActions(wrapper._configureServicesActions, contextWrapper, services);
        });

        builder.ConfigureAppConfiguration((context, configurationBuilder) =>
        {
            HostBuilderContextWrapper contextWrapper = HostBuilderContextWrapper.Wrap(context);
            InvokeDeferredActions(wrapper._configureAppConfigurationActions, contextWrapper, configurationBuilder);
        });

        builder.ConfigureLogging((context, loggingBuilder) =>
        {
            HostBuilderContextWrapper contextWrapper = HostBuilderContextWrapper.Wrap(context);
            InvokeDeferredActions(wrapper._configureLoggingActions, contextWrapper, loggingBuilder);
        });

        return wrapper;
    }

    public static HostBuilderWrapper Wrap(IWebHostBuilder builder)
    {
        ArgumentGuard.NotNull(builder);

        var wrapper = new HostBuilderWrapper(builder);

        builder.ConfigureServices((context, services) =>
        {
            HostBuilderContextWrapper contextWrapper = HostBuilderContextWrapper.Wrap(context);
            InvokeDeferredActions(wrapper._configureServicesActions, contextWrapper, services);
        });

        builder.ConfigureAppConfiguration((context, configurationBuilder) =>
        {
            HostBuilderContextWrapper contextWrapper = HostBuilderContextWrapper.Wrap(context);
            InvokeDeferredActions(wrapper._configureAppConfigurationActions, contextWrapper, configurationBuilder);
        });

        builder.ConfigureLogging((context, loggingBuilder) =>
        {
            HostBuilderContextWrapper contextWrapper = HostBuilderContextWrapper.Wrap(context);
            InvokeDeferredActions(wrapper._configureLoggingActions, contextWrapper, loggingBuilder);
        });

        return wrapper;
    }

    public static HostBuilderWrapper Wrap(IHostApplicationBuilder builder)
    {
        ArgumentGuard.NotNull(builder);

        // WebApplicationBuilder/IHostApplicationBuilder immediately execute callbacks, so don't capture them for deferred execution.

        return new HostBuilderWrapper(builder);
    }

    private static void InvokeDeferredActions<TArgument>(IEnumerable<Action<HostBuilderContextWrapper, TArgument>> actions,
        HostBuilderContextWrapper contextWrapper, TArgument argument)
    {
        foreach (Action<HostBuilderContextWrapper, TArgument> action in actions)
        {
            action(contextWrapper, argument);
        }
    }

    public HostBuilderWrapper ConfigureServices(Action<IServiceCollection> configureAction)
    {
        ArgumentGuard.NotNull(configureAction);

        return ConfigureServices((_, services) => configureAction(services));
    }

    public HostBuilderWrapper ConfigureServices(Action<HostBuilderContextWrapper, IServiceCollection> configureAction)
    {
        ArgumentGuard.NotNull(configureAction);

        if (_innerBuilder is IHostApplicationBuilder applicationBuilder)
        {
            HostBuilderContextWrapper contextWrapper = HostBuilderContextWrapper.Wrap(applicationBuilder);
            configureAction(contextWrapper, applicationBuilder.Services);
        }
        else
        {
            _configureServicesActions.Add(configureAction);
        }

        return this;
    }

    public HostBuilderWrapper ConfigureAppConfiguration(Action<IConfigurationBuilder> configureAction)
    {
        ArgumentGuard.NotNull(configureAction);

        return ConfigureAppConfiguration((_, configurationBuilder) => configureAction(configurationBuilder));
    }

    public HostBuilderWrapper ConfigureAppConfiguration(Action<HostBuilderContextWrapper, IConfigurationBuilder> configureAction)
    {
        ArgumentGuard.NotNull(configureAction);

        if (_innerBuilder is IHostApplicationBuilder applicationBuilder)
        {
            HostBuilderContextWrapper contextWrapper = HostBuilderContextWrapper.Wrap(applicationBuilder);
            configureAction(contextWrapper, applicationBuilder.Configuration);
        }
        else
        {
            _configureAppConfigurationActions.Add(configureAction);
        }

        return this;
    }

    public HostBuilderWrapper ConfigureLogging(Action<ILoggingBuilder> configureAction)
    {
        ArgumentGuard.NotNull(configureAction);

        return ConfigureLogging((_, configurationBuilder) => configureAction(configurationBuilder));
    }

    public HostBuilderWrapper ConfigureLogging(Action<HostBuilderContextWrapper, ILoggingBuilder> configureAction)
    {
        ArgumentGuard.NotNull(configureAction);

        if (_innerBuilder is IHostApplicationBuilder applicationBuilder)
        {
            HostBuilderContextWrapper contextWrapper = HostBuilderContextWrapper.Wrap(applicationBuilder);
            configureAction(contextWrapper, applicationBuilder.Logging);
        }
        else
        {
            _configureLoggingActions.Add(configureAction);
        }

        return this;
    }

    public HostBuilderWrapper ConfigureWebHost(Action<IWebHostBuilder> configureAction)
    {
        ArgumentGuard.NotNull(configureAction);

        if (_innerBuilder is IHostBuilder hostBuilder)
        {
            hostBuilder.ConfigureWebHost(configureAction);
        }
        else if (_innerBuilder is IWebHostBuilder webHostBuilder)
        {
            configureAction(webHostBuilder);
        }
        else if (_innerBuilder is WebApplicationBuilder webApplicationBuilder)
        {
            configureAction(webApplicationBuilder.WebHost);
        }
        else if (_innerBuilder is IHostApplicationBuilder)
        {
            // This is not a web application, so silently ignore.
        }
        else
        {
            throw new NotSupportedException($"Unknown host builder type '{_innerBuilder.GetType()}'.");
        }

        return this;
    }
}
