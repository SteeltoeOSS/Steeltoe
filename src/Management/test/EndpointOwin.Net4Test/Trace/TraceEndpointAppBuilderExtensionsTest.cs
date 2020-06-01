// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Owin.Builder;
using Owin;
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.Endpoint.Trace;
using System;
using Xunit;

namespace Steeltoe.Management.EndpointOwin.Trace.Test
{
    public class TraceEndpointAppBuilderExtensionsTest : BaseTest
    {
        [Fact]
        public void UseTraceActuator_ThrowsIfBuilderNull()
        {
            IAppBuilder builder = null;
            var config = new ConfigurationBuilder().Build();
            var traceRepo = new TraceDiagnosticObserver(new TraceEndpointOptions(config));

#pragma warning disable CS0612 // Type or member is obsolete
            var exception = Assert.Throws<ArgumentNullException>(() => builder.UseTraceActuator(config, traceRepo));
#pragma warning restore CS0612 // Type or member is obsolete
            Assert.Equal("builder", exception.ParamName);
        }

        [Fact]
        public void UseTraceActuator_ThrowsIfConfigNull()
        {
            IAppBuilder builder = new AppBuilder();
            var traceRepo = new TraceDiagnosticObserver(new TraceEndpointOptions());

#pragma warning disable CS0612 // Type or member is obsolete
            var exception = Assert.Throws<ArgumentNullException>(() => builder.UseTraceActuator(null, traceRepo));
#pragma warning restore CS0612 // Type or member is obsolete
            Assert.Equal("config", exception.ParamName);
        }
    }
}
