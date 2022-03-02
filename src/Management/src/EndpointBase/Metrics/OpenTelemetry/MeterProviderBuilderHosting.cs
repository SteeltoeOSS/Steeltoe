using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Endpoint.Metrics.OpenTelemetry
{
    /// <summary>
    /// A <see cref="MeterProviderBuilderBase"/> with support for deferred initialization using <see cref="IServiceProvider"/> for dependency injection.
    /// </summary>
    internal sealed class MeterProviderBuilderHosting : MeterProviderBuilderBase, IDeferredMeterProviderBuilder
    {
        private readonly List<Action<IServiceProvider, MeterProviderBuilder>> configurationActions = new List<Action<IServiceProvider, MeterProviderBuilder>>();

        public MeterProviderBuilderHosting(IServiceCollection services)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
        }

        public IServiceCollection Services { get; }

        public MeterProviderBuilder Configure(Action<IServiceProvider, MeterProviderBuilder> configure)
        {
            configurationActions.Add(configure ?? throw new ArgumentNullException(nameof(configure)));
            return this;
        }

        public MeterProvider Build(IServiceProvider serviceProvider)
        {
            var provider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            // Note: Not using a foreach loop because additional actions can be
            // added during each call.
            for (int i = 0; i < configurationActions.Count; i++)
            {
                configurationActions[i](serviceProvider, this);
            }

            return Build();
        }
    }
}
