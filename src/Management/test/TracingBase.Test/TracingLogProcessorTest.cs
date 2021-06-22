// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using OpenTelemetry;
using OpenTelemetry.Trace;
using Steeltoe.Common;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.Tracing.Test
{
    public class TracingLogProcessorTest
    {
        [Fact]
        public void Process_NoCurrentSpan_DoesNothing()
        {
            // Arrange
            using var openTelemetry = Sdk.CreateTracerProviderBuilder().AddSource("tracername").Build();
            var opts = new TracingOptions(null, new ConfigurationBuilder().Build());
            var processor = new TracingLogProcessor(opts);

            // Act
            var result = processor.Process("InputLogMessage");

            // Assert
            Assert.Equal("InputLogMessage", result);
        }

        [Fact]
        public void Process_CurrentSpan_ReturnsExpected()
        {
            // Arrange
            using var openTelemetry = Sdk.CreateTracerProviderBuilder().AddSource("tracername").Build();
            var config = TestHelpers.GetConfigurationFromDictionary(new Dictionary<string, string> { ["management:tracing:name"] = "foobar" });
            var processor = new TracingLogProcessor(new TracingOptions(new ApplicationInstanceInfo(config), config));
            var tracer = TracerProvider.Default.GetTracer("tracername");
            var span = tracer.StartActiveSpan("spanName");

            // Act
            var result = processor.Process("InputLogMessage");

            // Assert
            Assert.Contains("InputLogMessage", result);
            Assert.Contains("[", result);
            Assert.Contains("]", result);
            Assert.Contains(span.Context.TraceId.ToHexString(), result);
            Assert.Contains(span.Context.SpanId.ToHexString(), result);
            Assert.Contains("foobar", result);

            var childSpan = tracer.StartActiveSpan("spanName2", SpanKind.Internal, span);

            result = processor.Process("InputLogMessage2");

            Assert.Contains("InputLogMessage2", result);
            Assert.Contains("[", result);
            Assert.Contains("]", result);
            Assert.Contains(childSpan.Context.TraceId.ToHexString(), result);
            Assert.Contains(childSpan.Context.SpanId.ToHexString(), result);

            // Assert.Contains(span.Context.SpanId.ToHexString(), result);  TODO: ParentID not supported
            Assert.Contains("foobar", result);
        }
    }
}
