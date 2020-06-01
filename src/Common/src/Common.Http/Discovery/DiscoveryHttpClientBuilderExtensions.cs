// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;

namespace Steeltoe.Common.Http.Discovery
{
    /// <summary>
    /// Extension method for configuring <see cref="DiscoveryHttpMessageHandler"/> in <see cref="HttpClient"/> message handler pipelines
    /// </summary>
    public static class DiscoveryHttpClientBuilderExtensions
    {
        /// <summary>
        /// Adds a <see cref="DiscoveryHttpMessageHandler"/> for performing service discovery
        /// </summary>
        /// <param name="builder">The <see cref="IHttpClientBuilder"/>.</param>
        /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
        public static IHttpClientBuilder AddServiceDiscovery(this IHttpClientBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddHttpMessageHandler<DiscoveryHttpMessageHandler>();
            return builder;
        }
    }
}
