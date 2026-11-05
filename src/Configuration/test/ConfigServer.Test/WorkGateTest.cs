// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.TestResources;

namespace Steeltoe.Configuration.ConfigServer.Test;

public sealed class WorkGateTest
{
    [Fact]
    public void TryEnter_does_not_enter_while_work_is_running()
    {
        var gate = new WorkGate();

        gate.TryEnter().Should().BeTrue();
        gate.TryEnter().Should().BeFalse();

        gate.Exit();

        gate.TryEnter().Should().BeTrue();
    }

    [Fact]
    public void Enter_waits_for_the_work_that_is_running()
    {
        var gate = new WorkGate();
        gate.Enter(TestContext.Current.CancellationToken);

        Thread waiter = StartThread(() =>
        {
            // Not the token of the TestContext: canceling a wait throws on a thread that nobody observes, which crashes the test run.
            // The test releases the gate itself, and this thread does not keep the process alive when the test fails before that.
            gate.Enter(CancellationToken.None);
            gate.Exit();
        });

        WaitUntilBlocked(waiter);

        gate.Exit();

        WaitUntilCompleted(waiter);
    }

    [Fact]
    public void CloseAndWait_waits_for_the_work_that_is_running_and_closes_the_gate()
    {
        var gate = new WorkGate();
        gate.Enter(TestContext.Current.CancellationToken);

        Thread closer = StartThread(gate.CloseAndWait);

        WaitUntilBlocked(closer);

        gate.Exit();

        WaitUntilCompleted(closer);

        gate.TryEnter().Should().BeFalse();
    }

    [Fact]
    public void Enter_stops_waiting_on_closed_gate_when_token_is_canceled()
    {
        var gate = new WorkGate();
        gate.CloseAndWait();

        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        // ReSharper disable once AccessToDisposedClosure
        Action action = () => gate.Enter(tokenSource.Token);

        action.Should().ThrowExactly<OperationCanceledException>();
    }

    private static Thread StartThread(Action action)
    {
        var thread = new Thread(() => action())
        {
            IsBackground = true
        };

        thread.Start();
        return thread;
    }

    /// <summary>
    /// Waits until the thread is blocked, which is observed instead of assumed after sleeping for a while, so that the outcome does not depend on how busy
    /// the machine is. A thread that is not blocked has either not started waiting yet (so this keeps polling), or did not wait at all (so it has stopped).
    /// </summary>
    private static void WaitUntilBlocked(Thread thread)
    {
        SpinWait.SpinUntil(() => thread.ThreadState.HasFlag(ThreadState.WaitSleepJoin) || thread.ThreadState.HasFlag(ThreadState.Stopped), Timeouts.Safety)
            .Should().BeTrue();

        thread.ThreadState.Should().HaveFlag(ThreadState.WaitSleepJoin, "the thread must be waiting for the gate instead of having passed it");
    }

    private static void WaitUntilCompleted(Thread thread)
    {
        thread.Join(Timeouts.Safety).Should().BeTrue();
    }
}
