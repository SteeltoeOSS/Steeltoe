// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Steeltoe.Common.Hosting;

/// <summary>
/// Wraps a host builder context for <see cref="HostBuilderWrapper" />.
/// </summary>
internal sealed class HostBuilderContextWrapper
{
    private readonly object _innerContext;

    public IConfiguration Configuration { get; }
    public IHostEnvironment HostEnvironment { get; }

    private HostBuilderContextWrapper(IConfiguration configuration, IHostEnvironment hostEnvironment, object innerContext)
    {
        _innerContext = innerContext;
        Configuration = configuration;
        HostEnvironment = hostEnvironment;
    }

    public static HostBuilderContextWrapper Wrap(HostBuilderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new HostBuilderContextWrapper(context.Configuration, context.HostingEnvironment, context);
    }

    public static HostBuilderContextWrapper Wrap(WebHostBuilderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new HostBuilderContextWrapper(context.Configuration, context.HostingEnvironment, context);
    }

    public static HostBuilderContextWrapper Wrap(IHostApplicationBuilder context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new HostBuilderContextWrapper(context.Configuration, context.Environment, context);
    }

    public static Action<HostBuilderContextWrapper, TArgument>? WrapAction<TArgument>(Action<HostBuilderContext, TArgument>? action)
    {
        return WrapGenericAction(action);
    }

    public static Action<HostBuilderContextWrapper, TArgument>? WrapAction<TArgument>(Action<WebHostBuilderContext, TArgument>? action)
    {
        return WrapGenericAction(action);
    }

    public static Action<HostBuilderContextWrapper, TArgument>? WrapAction<TArgument>(Action<IHostApplicationBuilder, TArgument>? action)
    {
        return WrapGenericAction(action);
    }

    private static Action<HostBuilderContextWrapper, TArgument>? WrapGenericAction<TContext, TArgument>(Action<TContext, TArgument>? action)
    {
        if (action == null)
        {
            return null;
        }

        return (contextWrapper, argument) =>
        {
            var context = Unwrap<TContext>(contextWrapper);
            action(context, argument);
        };
    }

    private static TContext Unwrap<TContext>(HostBuilderContextWrapper contextWrapper)
    {
        if (contextWrapper._innerContext is not TContext context)
        {
            throw new InvalidOperationException($"Type '{typeof(TContext)}' is incompatible with type '{contextWrapper._innerContext.GetType()}'.");
        }

        return context;
    }
}
