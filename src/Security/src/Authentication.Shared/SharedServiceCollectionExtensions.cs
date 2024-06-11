// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;

namespace Steeltoe.Security.Authentication.Shared;

internal static class SharedServiceCollectionExtensions
{
    internal static IServiceCollection AddSteeltoeSecurityHttpClient(this IServiceCollection services, Action<HttpClient>? configureHttpClient)
    {
        services.AddHttpClient(SteeltoeSecurityDefaults.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
            configureHttpClient?.Invoke(client);
        });

        return services;
    }
}
