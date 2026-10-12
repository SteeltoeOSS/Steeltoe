// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Management.Endpoint.Actuators.HttpExchanges.Diagnostics;

namespace Steeltoe.Management.Endpoint.Test.Actuators.HttpExchanges;

public sealed class DiagnosticsManagerTest
{
    private const string ListenerName = "Steeltoe.Test.DiagnosticsManagerTest";

    [Fact]
    public void Observes_new_listeners_while_started()
    {
        var observer = new CountingObserver();
        using var manager = new DiagnosticsManager([observer], NullLogger<DiagnosticsManager>.Instance);

        manager.Start();

        using var listener = new DiagnosticListener(ListenerName);
        listener.Write("Event", null);

        observer.EventCount.Should().Be(1);
    }

    [Fact]
    public void Ignores_new_listeners_after_stop()
    {
        var observer = new CountingObserver();
        using var manager = new DiagnosticsManager([observer], NullLogger<DiagnosticsManager>.Instance);

        manager.Start();
        manager.Stop();

        using var listener = new DiagnosticListener(ListenerName);
        listener.Write("Event", null);

        observer.EventCount.Should().Be(0);
    }

    [Fact]
    public void Ignores_new_listeners_after_dispose()
    {
        var observer = new CountingObserver();
        var manager = new DiagnosticsManager([observer], NullLogger<DiagnosticsManager>.Instance);

        manager.Start();
        manager.Dispose();

        using var listener = new DiagnosticListener(ListenerName);
        listener.Write("Event", null);

        observer.EventCount.Should().Be(0);
    }

    [Fact]
    public void Can_restart_after_stop()
    {
        var observer = new CountingObserver();
        using var manager = new DiagnosticsManager([observer], NullLogger<DiagnosticsManager>.Instance);

        manager.Start();
        manager.Stop();
        manager.Start();

        using var listener = new DiagnosticListener(ListenerName);
        listener.Write("Event", null);

        observer.EventCount.Should().Be(1);
    }

    private sealed class CountingObserver()
        : DiagnosticObserver("CountingObserver", ListenerName, NullLoggerFactory.Instance)
    {
        public int EventCount { get; private set; }

        public override void ProcessEvent(string eventName, object? value)
        {
            EventCount++;
        }
    }
}
