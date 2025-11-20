// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.Actuators.CloudFoundry;

namespace Steeltoe.Management.Endpoint;

internal sealed class ConfigureActuatorsMiddlewareStartupFilter : IStartupFilter
{
    private readonly ILogger<ConfigureActuatorsMiddlewareStartupFilter> _logger;

    public ConfigureActuatorsMiddlewareStartupFilter(ILogger<ConfigureActuatorsMiddlewareStartupFilter> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
    }

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        ArgumentNullException.ThrowIfNull(next);

        return app =>
        {
            // According to https://learn.microsoft.com/en-us/aspnet/core/fundamentals/routing, apps typically don't need to call UseRouting; if not
            // explicitly called, UseRouting is implicitly inserted as the first middleware to execute.
            // However, UseActuatorEndpoints currently fails without an explicit call to UseRouting, and
            // https://learn.microsoft.com/en-us/aspnet/core/security/cors states that UseCors must be placed after UseRouting.
            // The ordering used here allows for next() to call UseAuthentication/UseAuthorization, which must be placed between UseRouting and UseActuatorEndpoints.

            app.UseManagementPort();

            app.UseRouting();
            app.UseActuatorsCorsPolicy();

            if (app.ApplicationServices.GetService<ICloudFoundryEndpointHandler>() != null)
            {
                app.UseCloudFoundrySecurity();
            }

            int? beforeMiddlewareCount = GetMiddlewareCount(app);
            next.Invoke(app);
            int? afterMiddlewareCount = GetMiddlewareCount(app);

            if (beforeMiddlewareCount != afterMiddlewareCount)
            {
                _logger.LogWarning(
                    "Actuators were registered with automatic middleware setup, and at least one additional middleware was registered afterward. This combination is usually undesired. " +
                    "To remove this warning, either remove the additional middleware registration or set configureMiddleware to false when registering actuators.");
            }

            app.UseActuatorEndpoints();
        };
    }

    private static int? GetMiddlewareCount(IApplicationBuilder app)
    {
        FieldInfo? componentsField = app.GetType().GetField("_components", BindingFlags.NonPublic | BindingFlags.Instance);
        return componentsField?.GetValue(app) is List<Func<RequestDelegate, RequestDelegate>> components ? components.Count(IsMiddleware) : null;
    }

    private static bool IsMiddleware(Func<RequestDelegate, RequestDelegate> component)
    {
        // This type exists so that ASP.NET Core can identify where to inject UseRouting. It is not a real middleware.
        return component.Target == null || component.Target.ToString() != "Microsoft.AspNetCore.Builder.WebApplicationBuilder+WireSourcePipeline";
    }
}
