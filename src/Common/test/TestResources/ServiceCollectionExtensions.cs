// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;

namespace Steeltoe.Common.TestResources;

internal static class ServiceCollectionExtensions
{
    /// <summary>
    /// Disables the internal cleanup tracking timers for all HttpClients in the service container by setting the factory handler rotation lifetime to
    /// infinite.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The default implementation of <see cref="IHttpClientFactory" /> assigns a 2-minute lifetime to all generated HTTP message handlers. To manage this
    /// rotation, the factory instantiates internal background timers (internal ActiveHandlerTrackingEntry). When integration tests repeatedly instantiate
    /// and dispose isolated host builders, the container stops referencing the internal factory. However, the background timers remain scheduled and active.
    /// Because active timers act as their own GC roots in .NET, they temporarily pin themselves and their parent dependencies in memory until the next
    /// Garbage Collection sweep. In suites with hundreds of sequential tests, these lingering timers climb and accumulate, leading to high memory overhead
    /// and unnecessary load on the system timer queue.
    /// </para>
    /// <para>
    /// This extension method hooks into <see cref="HttpClientFactoryOptions" /> and sets the global <see cref="HttpClientFactoryOptions.HandlerLifetime" />
    /// to <see cref="Timeout.InfiniteTimeSpan" />. Because the factory is explicitly told that handlers should never expire, it skips scheduling the
    /// background active handler tracking entries and tracking timers entirely. The handlers are neatly bound directly to the lifecycle of the test
    /// container and are cleanly torn down when the host is disposed, with zero floating roots.
    /// </para>
    /// <para>
    /// See also <seealso href="https://github.com/dotnet/runtime/issues/113494" /> and
    /// <seealso href="https://andrewlock.net/exporing-the-code-behind-ihttpclientfactory/" />.
    /// </para>
    /// </remarks>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    public static void DisableHttpClientRotationTimers(this IServiceCollection services)
    {
        // Applies this to all named, typed, or basic HTTP clients registered.
        services.ConfigureAll<HttpClientFactoryOptions>(options => options.HandlerLifetime = Timeout.InfiniteTimeSpan);
    }
}
