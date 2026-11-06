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

        Thread waiter = StartThreadThatBlocks(() =>
        {
            // Not passing the token of the TestContext: canceling a wait throws on a thread that nobody observes, which crashes the test run.
            // The test releases the gate itself, and this thread does not keep the process alive when the test fails before that.
            gate.Enter(CancellationToken.None);
            gate.Exit();
        });

        waiter.IsAlive.Should().BeTrue();

        gate.Exit();

        WaitUntilCompleted(waiter);
    }

    [Fact]
    public void CloseAndWait_waits_for_the_work_that_is_running_and_closes_the_gate()
    {
        var gate = new WorkGate();
        gate.Enter(TestContext.Current.CancellationToken);

        Thread closer = StartThreadThatBlocks(gate.CloseAndWait);

        closer.IsAlive.Should().BeTrue();

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

    /// <summary>
    /// Starts a thread that is expected to block, and gives it the chance to get there: this returns when the thread is waiting or has finished, or after a
    /// second when the machine is too busy to tell. The assertions that follow hold in every case, so a busy machine can only make a gate that does not
    /// block less likely to be caught, and never makes the tests fail. A fixed sleep would have the same effect, but always takes that long.
    /// </summary>
    private static Thread StartThreadThatBlocks(Action action)
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var thread = new Thread(() =>
        {
            started.SetResult();
            action();
        })
        {
            IsBackground = true
        };

        thread.Start();

        started.Task.Wait(Timeouts.Safety).Should().BeTrue();

        SpinWait.SpinUntil(() => thread.ThreadState.HasFlag(ThreadState.WaitSleepJoin) || !thread.IsAlive, TimeSpan.FromSeconds(1));

        return thread;
    }

    private static void WaitUntilCompleted(Thread thread)
    {
        thread.Join(Timeouts.Safety).Should().BeTrue();
    }
}
