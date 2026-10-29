// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions.Extensions;

namespace Steeltoe.Discovery.Eureka.Test;

public sealed class GatedActionTest
{
    [Fact]
    public async Task Run_Enforces_SingleActiveTask()
    {
        int runCount = 0;
        using var firstRunStarted = new ManualResetEventSlim();
        using var releaseFirstRun = new ManualResetEventSlim();

        var gatedAction = new GatedAction(() =>
        {
            // Only the first run blocks, so a second concurrent run would be visible in the count instead of hanging the test.
            // ReSharper disable once AccessToModifiedClosure
            if (Interlocked.Increment(ref runCount) == 1)
            {
                // ReSharper disable AccessToDisposedClosure
                firstRunStarted.Set();
                releaseFirstRun.Wait(TestContext.Current.CancellationToken);
                // ReSharper restore AccessToDisposedClosure
            }
        });

        Task firstRun = Task.Run(gatedAction.Run, CancellationToken.None);
        firstRunStarted.Wait(30.Seconds(), TestContext.Current.CancellationToken).Should().BeTrue();

        // Calls that arrive while the action is running return immediately without running it.
        gatedAction.Run();
        gatedAction.Run();

        Volatile.Read(ref runCount).Should().Be(1);

        releaseFirstRun.Set();
        await firstRun;

        Volatile.Read(ref runCount).Should().Be(1);
    }

    [Fact]
    public void Run_RunsAgain_AfterPreviousRunCompleted()
    {
        int runCount = 0;
        var gatedAction = new GatedAction(() => runCount++);

        gatedAction.Run();
        gatedAction.Run();

        runCount.Should().Be(2);
    }

    [Fact]
    public void Run_RunsAgain_AfterPreviousRunFailed()
    {
        int runCount = 0;

        var gatedAction = new GatedAction(() =>
        {
            runCount++;

            if (runCount == 1)
            {
                throw new InvalidOperationException("The action failed.");
            }
        });

        Action firstRun = gatedAction.Run;
        firstRun.Should().ThrowExactly<InvalidOperationException>();

        gatedAction.Run();

        runCount.Should().Be(2);
    }
}
